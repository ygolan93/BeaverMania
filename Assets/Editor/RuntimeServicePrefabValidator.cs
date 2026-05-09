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
        var prefabPathsByOwnerId = new Dictionary<string, List<string>>();
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (var prefabGuid in prefabGuids)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            foreach (var owner in prefab.GetComponentsInChildren<RuntimeBootstrapOwner>(true))
            {
                if (owner == null || string.IsNullOrEmpty(owner.OwnerId))
                {
                    continue;
                }

                List<string> ownerPrefabPaths;
                if (!prefabPathsByOwnerId.TryGetValue(owner.OwnerId, out ownerPrefabPaths))
                {
                    ownerPrefabPaths = new List<string>();
                    prefabPathsByOwnerId.Add(owner.OwnerId, ownerPrefabPaths);
                }

                ownerPrefabPaths.Add(prefabPath);
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

        var duplicateTypes = prefabPathsByType
            .Where(entry => entry.Value.Count > 1)
            .OrderBy(entry => entry.Key.FullName)
            .ToArray();
        var duplicateOwnerIds = prefabPathsByOwnerId
            .Where(entry => entry.Value.Count > 1)
            .OrderBy(entry => entry.Key)
            .ToArray();

        foreach (var duplicate in duplicateTypes)
        {
            Debug.LogError("Duplicate RuntimeServices.Register component type found: "
                + duplicate.Key.FullName + "\n" + string.Join("\n", duplicate.Value.ToArray()));
        }

        foreach (var duplicate in duplicateOwnerIds)
        {
            Debug.LogError("Duplicate RuntimeBootstrapOwner ownerId found: "
                + duplicate.Key + "\n" + string.Join("\n", duplicate.Value.ToArray()));
        }

        if (duplicateTypes.Length > 0 || duplicateOwnerIds.Length > 0)
        {
            return false;
        }

        if (logSuccess)
        {
            Debug.Log("Runtime service prefab validation passed: no duplicate RuntimeServices.Register component types or RuntimeBootstrapOwner ownerIds found.");
        }

        return true;
    }

    static bool RegistersRuntimeService(MonoBehaviour component)
    {
        var script = MonoScript.FromMonoBehaviour(component);
        return script != null && script.text.Contains(RegisterCall);
    }
}
