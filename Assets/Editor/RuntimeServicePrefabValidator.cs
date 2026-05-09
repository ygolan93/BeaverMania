using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RuntimeServicePrefabValidator
{
    const string MenuPath = "Tools/Runtime Services/Validate Prefab Registrations";
    const string RegisterCall = "RuntimeServices.Register";

    [MenuItem(MenuPath)]
    public static void ValidatePrefabRegistrationsMenu()
    {
        ValidatePrefabRegistrations(true);
    }

    public static bool ValidatePrefabRegistrations(bool logSuccess)
    {
        var prefabPathsByType = new Dictionary<System.Type, List<string>>();
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (var prefabGuid in prefabGuids)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || !RegistersRuntimeService(component))
                {
                    continue;
                }

                var serviceType = component.GetType();
                List<string> prefabPaths;
                if (!prefabPathsByType.TryGetValue(serviceType, out prefabPaths))
                {
                    prefabPaths = new List<string>();
                    prefabPathsByType.Add(serviceType, prefabPaths);
                }

                prefabPaths.Add(prefabPath);
            }
        }

        var duplicates = prefabPathsByType
            .Where(entry => entry.Value.Count > 1)
            .OrderBy(entry => entry.Key.FullName)
            .ToArray();

        if (duplicates.Length == 0)
        {
            if (logSuccess)
            {
                Debug.Log("Runtime service prefab validation passed: no duplicate RuntimeServices.Register component types found.");
            }

            return true;
        }

        foreach (var duplicate in duplicates)
        {
            Debug.LogError("Duplicate RuntimeServices.Register component type found: "
                + duplicate.Key.FullName + "\n" + string.Join("\n", duplicate.Value.ToArray()));
        }

        return false;
    }

    static bool RegistersRuntimeService(MonoBehaviour component)
    {
        var script = MonoScript.FromMonoBehaviour(component);
        return script != null && script.text.Contains(RegisterCall);
    }
}
