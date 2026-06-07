#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beavermania.EditorTools
{
    public static class LevelPerformanceEditorTools
    {
        static readonly string[] VillageRootNameContains =
        {
            "Bakery_market",
            "Meat_market",
            "Fish_market",
            "lamp_post",
            "fence-classic",
            "wall-medieval",
            "water-tower",
            "dryer-outside",
        };

        static readonly string[] SkipTags =
        {
            "Coin",
            "Life",
            "Player",
            "NPC",
        };

        [MenuItem("Beavermania/Performance/Mark Village Environment Static")]
        public static void MarkVillageEnvironmentStatic()
        {
            int markedCount = 0;
            int skippedCount = 0;
            var roots = CollectVillageRoots();

            Undo.SetCurrentGroupName("Mark Village Environment Static");
            int undoGroup = Undo.GetCurrentGroup();

            for (int i = 0; i < roots.Count; i++)
                MarkStaticRecursive(roots[i].transform, ref markedCount, ref skippedCount);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[Perf] Marked {markedCount} village/environment objects static. Skipped {skippedCount} gameplay/dynamic objects.");
        }

        [MenuItem("Beavermania/Performance/Tune All Terrains")]
        public static void TuneAllTerrains()
        {
            Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
            if (terrains.Length == 0)
            {
                Debug.LogWarning("[Perf] No terrains found in open scenes.");
                return;
            }

            Undo.SetCurrentGroupName("Tune Terrains For Performance");
            int undoGroup = Undo.GetCurrentGroup();

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null)
                    continue;

                Undo.RecordObject(terrain, "Tune Terrain Performance");
                terrain.treeDistance = 2500f;
                terrain.treeBillboardDistance = 50f;
                terrain.treeCrossFadeLength = 5f;
                terrain.treeMaximumFullLODCount = 50;
                terrain.detailObjectDistance = 40f;
                terrain.detailObjectDensity = 0.65f;
                terrain.basemapDistance = 600f;
                terrain.heightmapPixelError = 8f;
                EditorUtility.SetDirty(terrain);
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[Perf] Tuned {terrains.Length} terrain tile(s) for tree/detail/basemap distances.");
        }

        [MenuItem("Beavermania/Performance/Bake Occlusion Culling")]
        public static void BakeOcclusionCulling()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Perf] Exit Play Mode before baking occlusion culling.");
                return;
            }

            StaticOcclusionCulling.Compute();
            Debug.Log("[Perf] Occlusion culling bake started/completed. Save the scene after bake finishes.");
        }

        [MenuItem("Beavermania/Performance/Report Large Props Missing LOD")]
        public static void ReportLargePropsMissingLod()
        {
            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>(true);
            int missingLodCount = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (ShouldSkipStatic(renderer.gameObject))
                    continue;

                Transform root = renderer.transform.root;
                if (root.GetComponentInParent<LODGroup>() != null || root.GetComponent<LODGroup>() != null)
                    continue;

                Bounds bounds = renderer.bounds;
                if (bounds.size.magnitude < 12f)
                    continue;

                missingLodCount++;
                Debug.Log($"[Perf][LOD candidate] {GetHierarchyPath(renderer.transform)} size={bounds.size}", renderer.gameObject);
            }

            Debug.Log($"[Perf] Found {missingLodCount} large static-ish props without LODGroup (manual LOD setup recommended).");
        }

        static List<GameObject> CollectVillageRoots()
        {
            var roots = new List<GameObject>();
            var seen = new HashSet<int>();
            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                string name = transform.name;
                for (int j = 0; j < VillageRootNameContains.Length; j++)
                {
                    if (!name.Contains(VillageRootNameContains[j]))
                        continue;

                    int instanceId = transform.gameObject.GetInstanceID();
                    if (seen.Add(instanceId))
                        roots.Add(transform.gameObject);
                    break;
                }
            }

            return roots;
        }

        static void MarkStaticRecursive(Transform transform, ref int markedCount, ref int skippedCount)
        {
            GameObject gameObject = transform.gameObject;
            if (ShouldSkipStatic(gameObject))
            {
                skippedCount++;
                return;
            }

            if (!gameObject.isStatic)
            {
                Undo.RecordObject(gameObject, "Mark Static");
                gameObject.isStatic = true;
                markedCount++;
            }

            for (int i = 0; i < transform.childCount; i++)
                MarkStaticRecursive(transform.GetChild(i), ref markedCount, ref skippedCount);
        }

        static bool ShouldSkipStatic(GameObject gameObject)
        {
            for (int i = 0; i < SkipTags.Length; i++)
            {
                if (gameObject.CompareTag(SkipTags[i]))
                    return true;
            }

            if (gameObject.GetComponent<Rigidbody>() != null)
                return true;

            if (gameObject.GetComponent<Animator>() != null)
                return true;

            if (gameObject.GetComponent<Collider>() != null && gameObject.GetComponent<Collider>().isTrigger)
                return true;

            return false;
        }

        static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
#endif
