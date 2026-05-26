using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Fixes Level 1 Remastered Steam console warnings:
    /// terrain neighbor heightmap resolution mismatches and TheGivingTree Nature/Soft Occlusion shader warnings.
    /// </summary>
    public static class FixLevel1ConsoleWarnings
    {
        const string TargetScenePath = "Assets/Scenes/Level 1 - Remastered - Steam.unity";
        const string GivingTreePrefabPath = "Assets/Prefabs/Objects/Interactable/TheGivingTree.prefab";
        const int DefaultHeightmapResolution = 513;

        static readonly string[] NatureTreeShaders =
        {
            "Nature/Soft Occlusion Bark",
            "Nature/Soft Occlusion Leaves",
        };

        public static void ExecuteBatch()
        {
            try
            {
                RunInternal();
                Debug.Log("[FixLevel1ConsoleWarnings] Completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[FixLevel1ConsoleWarnings] Failed: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        [MenuItem("Beavermania/Fix/Level 1 Remastered — console warnings (terrain + GivingTree)")]
        public static void ExecuteFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Fix console warnings",
                    "Fix terrain neighbor heightmap mismatches and TheGivingTree tree shader warnings in Level 1 - Remastered - Steam?\n\nSave scenes first.",
                    "Run",
                    "Cancel"))
                return;

            RunInternal();
            EditorUtility.DisplayDialog("Fix console warnings", "Done. Check the Console log.", "OK");
        }

        static void RunInternal()
        {
            var log = new StringBuilder();
            log.AppendLine("[FixLevel1ConsoleWarnings] Starting…");

            FixGivingTreeMaterials(log);
            GivingTreeTerrainAutoFix.RemoveGivingTreeTerrainPrototypes(log);

            var scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            FixSceneTerrains(scene, log);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            log.AppendLine("[FixLevel1ConsoleWarnings] Finished.");
            Debug.Log(log.ToString());
        }

        static void FixGivingTreeMaterials(StringBuilder log)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GivingTreePrefabPath);
            if (prefab == null)
            {
                log.AppendLine("WARNING: Missing prefab at " + GivingTreePrefabPath);
                return;
            }

            var tree = prefab.GetComponent<Tree>();
            if (tree != null)
            {
                log.AppendLine("Removing legacy Tree component from TheGivingTree prefab (mesh uses MeshRenderer).");
                UnityEngine.Object.DestroyImmediate(tree, true);
            }

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            var changedMaterials = 0;
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || UsesNatureTreeShader(material.shader))
                        continue;

                    var replacement = CreateNatureTreeMaterial(material, i, log);
                    if (replacement == null)
                        continue;

                    materials[i] = replacement;
                    changedMaterials++;
                }

                renderer.sharedMaterials = materials;
            }

            if (changedMaterials > 0)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                log.AppendLine("Updated " + changedMaterials + " material slot(s) on TheGivingTree prefab.");
            }
            else
            {
                log.AppendLine("TheGivingTree prefab materials already use Nature/Soft Occlusion shaders.");
            }
        }

        static void FixSceneTerrains(Scene scene, StringBuilder log)
        {
            var terrains = UnityEngine.Object.FindObjectsOfType<Terrain>(true)
                .Where(t => t.gameObject.scene == scene)
                .ToList();

            if (terrains.Count == 0)
            {
                log.AppendLine("WARNING: No terrains found in " + TargetScenePath);
                return;
            }

            RemoveDuplicateTerrains(terrains, log);

            var resolutionGroups = terrains
                .GroupBy(t => t.terrainData != null ? t.terrainData.heightmapResolution : -1)
                .OrderByDescending(g => g.Count())
                .ToList();

            var targetResolution = resolutionGroups.First().Key;
            if (targetResolution <= 0)
                targetResolution = DefaultHeightmapResolution;

            log.AppendLine("Terrain heightmap resolutions in scene: "
                + string.Join(", ", resolutionGroups.Select(g => g.Key + " (" + g.Count() + " tiles)")));

            foreach (var terrain in terrains)
            {
                if (terrain.terrainData == null)
                    continue;

                if (terrain.terrainData.heightmapResolution != targetResolution)
                {
                    AlignHeightmapResolution(terrain.terrainData, targetResolution, log);
                }
            }

            foreach (var terrain in terrains)
            {
                terrain.allowAutoConnect = true;
                terrain.groupingID = 0;
            }

            Terrain.SetConnectivityDirty();

            log.AppendLine("Reconnected terrain neighbors after aligning heightmap resolution to " + targetResolution + ".");
        }

        static void RemoveDuplicateTerrains(List<Terrain> terrains, StringBuilder log)
        {
            var groups = terrains
                .GroupBy(t => (
                    data: t.terrainData,
                    pos: new Vector3(
                        Mathf.Round(t.transform.position.x),
                        Mathf.Round(t.transform.position.y),
                        Mathf.Round(t.transform.position.z))))
                .Where(g => g.Key.data != null);

            foreach (var group in groups)
            {
                var list = group.ToList();
                if (list.Count <= 1)
                    continue;

                for (var i = 1; i < list.Count; i++)
                {
                    log.AppendLine("Removing duplicate terrain '" + list[i].name + "' at " + list[i].transform.position
                        + " (kept '" + list[0].name + "').");
                    UnityEngine.Object.DestroyImmediate(list[i].gameObject);
                }
            }
        }

        static void AlignHeightmapResolution(TerrainData terrainData, int targetResolution, StringBuilder log)
        {
            var path = AssetDatabase.GetAssetPath(terrainData);
            var oldResolution = terrainData.heightmapResolution;
            if (oldResolution == targetResolution)
                return;

            var size = terrainData.size;
            terrainData.heightmapResolution = targetResolution;
            terrainData.size = size;
            EditorUtility.SetDirty(terrainData);
            log.AppendLine("Set heightmap resolution " + oldResolution + " -> " + targetResolution + " on " + path);
        }

        static bool UsesNatureTreeShader(Shader shader)
        {
            if (shader == null)
                return false;

            var name = shader.name;
            return name.StartsWith("Nature/Soft Occlusion", StringComparison.Ordinal)
                || name.StartsWith("Nature/SpeedTree", StringComparison.Ordinal);
        }

        static Material CreateNatureTreeMaterial(Material source, int materialIndex, StringBuilder log)
        {
            var shaderName = materialIndex == 0
                ? NatureTreeShaders[0]
                : NatureTreeShaders[Mathf.Min(1, NatureTreeShaders.Length - 1)];

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                log.AppendLine("WARNING: Shader not found: " + shaderName);
                return null;
            }

            var targetPath = "Assets/Materials/TheGivingTree/"
                + source.name.Replace("/", "_") + "_" + shaderName.Replace("/", "_").Replace(" ", "_") + ".mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
            if (existing != null && existing.shader == shader)
                return existing;

            var material = existing != null ? existing : new Material(shader);
            if (source.HasProperty("_MainTex") && material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", source.GetTexture("_MainTex"));
            if (source.HasProperty("_Color") && material.HasProperty("_Color"))
                material.SetColor("_Color", source.GetColor("_Color"));

            if (existing == null)
            {
                EnsureFolder("Assets/Materials/TheGivingTree");
                AssetDatabase.CreateAsset(material, targetPath);
                log.AppendLine("Created material: " + targetPath);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
