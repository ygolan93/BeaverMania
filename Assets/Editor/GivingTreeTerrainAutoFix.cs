using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Removes TheGivingTree from terrain tree prototypes (use scene prefab instances instead).
    /// </summary>
    public static class GivingTreeTerrainAutoFix
    {
        const string GivingTreePrefabPath = "Assets/Prefabs/Objects/Interactable/TheGivingTree.prefab";

        [MenuItem("Beavermania/Fix/TheGivingTree — remove terrain tree prototypes")]
        public static void ExecuteFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Remove GivingTree terrain prototypes",
                    "Remove TheGivingTree from all TerrainData tree prototypes and clear painted tree instances?\n\nSave assets first.",
                    "Run",
                    "Cancel"))
                return;

            var log = new StringBuilder();
            RemoveGivingTreeTerrainPrototypes(log);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("Terrain prototypes", "Done. Check the Console log.", "OK");
        }

        public static void RemoveGivingTreeTerrainPrototypes(StringBuilder log)
        {
            var givingTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GivingTreePrefabPath);
            if (givingTreePrefab == null)
            {
                log.AppendLine("WARNING: Missing prefab at " + GivingTreePrefabPath);
                return;
            }

            var terrainDataPaths = AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets" });
            var removedPrototypes = 0;
            var clearedInstances = 0;

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
                        continue;

                    var isGivingTree = prototype.prefab == givingTreePrefab
                        || string.Equals(prototype.prefab.name, "TheGivingTree", StringComparison.OrdinalIgnoreCase)
                        || prototype.prefab.name.StartsWith("TheGivingTree", StringComparison.OrdinalIgnoreCase);

                    if (!isGivingTree)
                        continue;

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
                terrainData.treeInstances = Array.Empty<TreeInstance>();
                terrainData.RefreshPrototypes();
                removedPrototypes += indicesToRemove.Count;
                clearedInstances++;
                EditorUtility.SetDirty(terrainData);
                log.AppendLine("Removed TheGivingTree terrain prototype(s) from " + path + " (use scene prefab instances instead).");
            }

            if (removedPrototypes == 0)
                log.AppendLine("No terrain tree prototypes named TheGivingTree found in TerrainData assets.");
        }
    }
}
