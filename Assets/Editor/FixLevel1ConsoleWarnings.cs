using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beavermania.Objects;
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
            FixGivingTreeTerrainPrototypes(log);

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
            FixGivingTreeMaterialsPublic(log);
        }

        internal static void FixGivingTreeMaterialsPublic(StringBuilder log)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GivingTreePrefabPath);
            if (prefab == null)
            {
                log?.AppendLine("WARNING: Missing prefab at " + GivingTreePrefabPath);
                return;
            }

            var tree = prefab.GetComponent<Tree>();
            if (tree != null)
            {
                log?.AppendLine("Removing legacy Tree component from TheGivingTree prefab (mesh uses MeshRenderer).");
                UnityEngine.Object.DestroyImmediate(tree, true);
            }

            var bark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TheGivingTree/TheGivingTree_Bark.mat");
            var leaves = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TheGivingTree/TheGivingTree_Leaves.mat");
            if (bark == null || leaves == null)
            {
                log?.AppendLine("WARNING: Missing TheGivingTree bark/leaves materials under Assets/Materials/TheGivingTree/.");
                return;
            }

            if (!UsesNatureTreeShader(bark.shader) || !UsesNatureTreeShader(leaves.shader))
            {
                log?.AppendLine("WARNING: TheGivingTree bark/leaves materials are not Nature/Soft Occlusion shaders.");
            }

            var rootRenderer = prefab.GetComponent<MeshRenderer>();
            var changedMaterials = 0;
            if (rootRenderer != null)
            {
                var current = rootRenderer.sharedMaterials;
                if (current == null
                    || current.Length != 2
                    || current[0] != bark
                    || current[1] != leaves)
                {
                    rootRenderer.sharedMaterials = new[] { bark, leaves };
                    changedMaterials = 2;
                }
            }
            else
            {
                log?.AppendLine("WARNING: TheGivingTree prefab has no root MeshRenderer.");
            }

            var logSpawner = prefab.GetComponent<LogSpawner>();
            if (logSpawner != null)
            {
                var so = new SerializedObject(logSpawner);
                so.FindProperty("givingTreeBark").objectReferenceValue = bark;
                so.FindProperty("givingTreeLeaves").objectReferenceValue = leaves;
                so.ApplyModifiedPropertiesWithoutUndo();
                log?.AppendLine("Wired LogSpawner nature material references on TheGivingTree.");
            }

            if (changedMaterials > 0 || tree != null)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                log?.AppendLine("Saved TheGivingTree prefab with Nature/Soft Occlusion bark + leaves on root MeshRenderer.");
            }
            else
            {
                log?.AppendLine("TheGivingTree root MeshRenderer already uses Nature/Soft Occlusion materials.");
            }
        }

        static void FixGivingTreeTerrainPrototypes(StringBuilder log)
        {
            FixGivingTreeTerrainPrototypesPublic(log);
        }

        internal static int FixGivingTreeTerrainPrototypesPublic(StringBuilder log)
        {
            var givingTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GivingTreePrefabPath);
            if (givingTreePrefab == null)
                return 0;

            var terrainDataPaths = AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets" });
            var removedPrototypes = 0;

            foreach (var guid in terrainDataPaths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (terrainData == null || terrainData.treePrototypes == null || terrainData.treePrototypes.Length == 0)
                    continue;

                var prototypes = terrainData.treePrototypes.ToList();
                var indicesToRemove = new List<int>();
                for (var i = 0; i < prototypes.Count; i++)
                {
                    var prototype = prototypes[i];
                    if (prototype.prefab == null)
                    {
                        indicesToRemove.Add(i);
                        continue;
                    }

                    var isGivingTree = prototype.prefab == givingTreePrefab
                        || string.Equals(prototype.prefab.name, "TheGivingTree", StringComparison.OrdinalIgnoreCase)
                        || prototype.prefab.name.StartsWith("TheGivingTree", StringComparison.OrdinalIgnoreCase);

                    if (isGivingTree)
                        indicesToRemove.Add(i);
                }

                if (indicesToRemove.Count == 0)
                    continue;

                var removeSet = new HashSet<int>(indicesToRemove);
                var keptPrototypes = new List<TreePrototype>(prototypes.Count - indicesToRemove.Count);
                for (var i = 0; i < prototypes.Count; i++)
                {
                    if (removeSet.Contains(i))
                        continue;

                    keptPrototypes.Add(prototypes[i]);
                }

                terrainData.treePrototypes = keptPrototypes.ToArray();
                terrainData.treeInstances = RemapTreeInstances(terrainData.treeInstances, removeSet);
                terrainData.RefreshPrototypes();
                removedPrototypes += indicesToRemove.Count;
                EditorUtility.SetDirty(terrainData);
                log?.AppendLine("Removed invalid terrain tree prototype(s) from " + path + " (removed=" + indicesToRemove.Count + ").");
            }

            if (removedPrototypes == 0)
                log?.AppendLine("No terrain tree prototypes named TheGivingTree found in TerrainData assets.");

            return removedPrototypes;
        }

        static TreeInstance[] RemapTreeInstances(TreeInstance[] instances, HashSet<int> removedPrototypeIndices)
        {
            if (instances == null || instances.Length == 0 || removedPrototypeIndices == null || removedPrototypeIndices.Count == 0)
                return instances ?? Array.Empty<TreeInstance>();

            var kept = new List<TreeInstance>(instances.Length);
            for (var i = 0; i < instances.Length; i++)
            {
                var instance = instances[i];
                if (removedPrototypeIndices.Contains(instance.prototypeIndex))
                    continue;

                var newIndex = instance.prototypeIndex;
                foreach (var removedIndex in removedPrototypeIndices.OrderBy(i => i))
                {
                    if (removedIndex < newIndex)
                        newIndex--;
                }

                instance.prototypeIndex = newIndex;
                kept.Add(instance);
            }

            return kept.ToArray();
        }

        static void FixSceneTerrains(Scene scene, StringBuilder log)
        {
            var terrains = UnityEngine.Object.FindObjectsOfType<Terrain>(true)
                .Where(t => t.gameObject.scene == scene)
                .ToList();

            if (terrains.Count == 0)
            {
                log?.AppendLine("WARNING: No terrains found in " + TargetScenePath);
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
                    log?.AppendLine("Removing duplicate terrain '" + list[i].name + "' at " + list[i].transform.position
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
    }
}
