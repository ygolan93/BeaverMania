using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RuntimeServicePrefabValidator
{
    const string MenuPath = "Tools/Runtime Services/Validate Prefab Registrations";
    const string RegisterCall = "RuntimeServices.Register";
    const string GameMusicPrefabPath = "Assets/Prefabs/Objects/UI/GameMusic.prefab";

    static readonly System.Type[] PersistentServiceTypes =
    {
        typeof(SceneTransitionService),
        typeof(CursorStateService),
        typeof(GameFlowController),
        typeof(GameInputReader)
    };

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
        var gameMusicPersistentServices = prefabPathsByType
            .Where(entry => IsPersistentServiceType(entry.Key) && entry.Value.Contains(GameMusicPrefabPath))
            .OrderBy(entry => entry.Key.FullName)
            .ToArray();
        var persistentServicePrefabCounts = prefabPathsByType
            .Where(entry => IsPersistentServiceType(entry.Key))
            .SelectMany(entry => entry.Value)
            .Distinct()
            .OrderBy(path => path)
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

        foreach (var gameMusicService in gameMusicPersistentServices)
        {
            Debug.LogError("Persistent runtime service component is not allowed on GameMusic prefab: "
                + gameMusicService.Key.FullName + "\n" + GameMusicPrefabPath);
        }

        if (persistentServicePrefabCounts.Length != 1)
        {
            Debug.LogError("Persistent runtime service components must be isolated to exactly one prefab.\n"
                + string.Join("\n", persistentServicePrefabCounts));
        }

        if (duplicateTypes.Length > 0 || duplicateOwnerIds.Length > 0 || gameMusicPersistentServices.Length > 0 || persistentServicePrefabCounts.Length != 1)
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

    static bool IsPersistentServiceType(System.Type type)
    {
        return PersistentServiceTypes.Contains(type);
    }
}
