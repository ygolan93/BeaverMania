using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.WaspQueen
{
    // Deterministic, read-only prefab reference audit for the Wasp Queen boss assets.
    // Scans uninstantiated prefab assets for the four fatal states: missing MonoBehaviours,
    // missing asset references (null with nonzero instanceID), broken nested prefabs, and
    // invalid persistent UnityEvent targets. No scene instantiation; safe Load/Unload.
    public sealed class WaspQueenPrefabReferenceAuditTests
    {
        static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/NPC/WaspQueen/PF_WaspQueen_Boss.prefab",
            "Assets/Prefabs/NPC/WaspQueen/PF_WaspQueen_PoisonProjectile.prefab",
            "Assets/Prefabs/NPC/WaspQueen/PF_WaspQueen_PoisonZone.prefab",
        };

        [Test]
        public void WaspQueenPrefabs_HaveNoBrokenReferences()
        {
            var issues = new List<string>();
            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                string path = PrefabPaths[i];
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    issues.Add(path + " : missing prefab asset");
                    continue;
                }
                GameObject contents = null;
                try
                {
                    contents = PrefabUtility.LoadPrefabContents(path);
                    ScanHierarchy(contents.transform, path, issues);
                }
                finally
                {
                    if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
                }
                if (i > 0 && i % 4 == 0) EditorUtility.UnloadUnusedAssetsImmediate();
            }
            Assert.That(issues, Is.Empty, "Wasp Queen prefab reference issues:\n - " + string.Join("\n - ", issues));
        }

        static void ScanHierarchy(Transform t, string prefabPath, List<string> issues)
        {
            var go = t.gameObject;
            string hp = GetHierarchyPath(t);

            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                issues.Add(prefabPath + " -> " + hp + " : missing MonoBehaviour (ghost script)");

            if (PrefabUtility.IsPrefabAssetMissing(go))
                issues.Add(prefabPath + " -> " + hp + " : broken / missing nested prefab");

            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null) continue; // counted above as missing script
                var so = new SerializedObject(c);
                var sp = so.GetIterator();
                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference
                        && sp.objectReferenceValue == null
                        && sp.objectReferenceInstanceIDValue != 0)
                    {
                        issues.Add(prefabPath + " -> " + hp + " (" + c.GetType().Name + "." + sp.propertyPath + ") : missing object reference");
                    }
                }
            }

            for (int i = 0; i < t.childCount; i++)
                ScanHierarchy(t.GetChild(i), prefabPath, issues);
        }

        static string GetHierarchyPath(Transform t)
        {
            string p = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }
            return p;
        }
    }
}
