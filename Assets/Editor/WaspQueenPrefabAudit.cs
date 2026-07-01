using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeavermaniaEditor
{
    // On-demand, read-only audit of the Wasp Queen prefab assets for missing scripts,
    // missing object references, and broken nested prefabs. Logs each issue with asset
    // context so the developer can ping the broken prefab in the Project window.
    public static class WaspQueenPrefabAudit
    {
        static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/NPC/WaspQueen/PF_WaspQueen_Boss.prefab",
            "Assets/Prefabs/NPC/WaspQueen/PF_WaspQueen_PoisonProjectile.prefab",
            "Assets/Prefabs/NPC/WaspQueen/PF_WaspQueen_PoisonZone.prefab",
        };

        [MenuItem("Beavermania/WaspQueen/Audit Prefabs")]
        public static void Audit()
        {
            var issues = new List<string>();
            foreach (var path in PrefabPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) { issues.Add(path + " : missing prefab asset"); continue; }
                GameObject contents = null;
                try
                {
                    contents = PrefabUtility.LoadPrefabContents(path);
                    Scan(contents.transform, path, asset, issues);
                }
                finally
                {
                    if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            Directory.CreateDirectory("Temp");
            File.WriteAllLines("Temp/wq_prefab_audit.txt", issues.Count == 0 ? new[] { "OK: 0 issues" } : issues.ToArray());
            Debug.Log("[WQ Prefab Audit] " + (issues.Count == 0 ? "PASS - 0 issues" : issues.Count + " issue(s); see Temp/wq_prefab_audit.txt"));
        }

        static void Scan(Transform t, string path, Object pingContext, List<string> issues)
        {
            var go = t.gameObject;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            {
                string m = path + " -> " + go.name + " : missing MonoBehaviour";
                issues.Add(m); Debug.LogError(m, pingContext);
            }
            if (PrefabUtility.IsPrefabAssetMissing(go))
            {
                string m = path + " -> " + go.name + " : broken nested prefab";
                issues.Add(m); Debug.LogError(m, pingContext);
            }
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var so = new SerializedObject(c);
                var sp = so.GetIterator();
                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference
                        && sp.objectReferenceValue == null
                        && sp.objectReferenceInstanceIDValue != 0)
                    {
                        string m = path + " -> " + go.name + " (" + c.GetType().Name + "." + sp.propertyPath + ") : missing reference";
                        issues.Add(m); Debug.LogError(m, pingContext);
                    }
                }
            }
            for (int i = 0; i < t.childCount; i++) Scan(t.GetChild(i), path, pingContext, issues);
        }
    }
}
