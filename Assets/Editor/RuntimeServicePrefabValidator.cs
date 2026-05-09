using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RuntimeServicePrefabValidator
{
    const string MenuPath = "Tools/Runtime Services/Validate Prefab Registrations";
    const string RegisterCall = "RuntimeServices.Register";
    const string GameMusicPrefabPath = "Assets/Prefabs/Objects/UI/GameMusic.prefab";

    [MenuItem(MenuPath)]
    public static void ValidatePrefabRegistrationsMenu()
    {
        ValidatePrefabRegistrations(true);
    }

    public static bool ValidatePrefabRegistrations(bool logSuccess)
    {
        var prefabPathsByType = new Dictionary<System.Type, List<string>>();
        var prefabPathsByOwnerId = new Dictionary<string, List<string>>();
        var ownerServiceErrors = new List<string>();
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
                if (owner == null)
                {
                    continue;
                }

                ValidateOwnedServices(prefabPath, owner, ownerServiceErrors);

                if (string.IsNullOrEmpty(owner.OwnerId))
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

        foreach (var ownerServiceError in ownerServiceErrors)
        {
            Debug.LogError(ownerServiceError);
        }

        if (persistentServicePrefabCounts.Length != 1)
        {
            Debug.LogError("Persistent runtime service components must be isolated to exactly one prefab.\n"
                + string.Join("\n", persistentServicePrefabCounts));
        }

        if (duplicateTypes.Length > 0 || duplicateOwnerIds.Length > 0 || gameMusicPersistentServices.Length > 0 || ownerServiceErrors.Count > 0 || persistentServicePrefabCounts.Length != 1)
        {
            return false;
        }

        if (logSuccess)
        {
            Debug.Log("Runtime service prefab validation passed: no duplicate RuntimeServices.Register component types, RuntimeBootstrapOwner ownerIds, or owner service mismatches found.");
        }

        return true;
    }

    static void ValidateOwnedServices(string prefabPath, RuntimeBootstrapOwner owner, List<string> errors)
    {
        var ownedServices = owner.OwnedServices ?? new Component[0];
        var actualServices = owner.GetComponentsInChildren<MonoBehaviour>(true)
            .Where(component => component != null && component != owner && RegistersRuntimeService(component))
            .Cast<Component>()
            .ToArray();

        var ownedServiceSet = new HashSet<Component>(ownedServices.Where(service => service != null));
        var actualServiceSet = new HashSet<Component>(actualServices);
        var ownedServiceTypes = new HashSet<System.Type>();

        for (var i = 0; i < ownedServices.Length; i++)
        {
            var ownedService = ownedServices[i];
            if (ownedService == null)
            {
                errors.Add(prefabPath + "\nRuntimeBootstrapOwner " + OwnerLabel(owner) + " ownedServices contains a null entry at index " + i + ".");
                continue;
            }

            if (!ownedServiceTypes.Add(ownedService.GetType()))
            {
                errors.Add(prefabPath + "\nRuntimeBootstrapOwner " + OwnerLabel(owner) + " has duplicate owned service component type: " + ownedService.GetType().FullName + ".");
            }

            if (!actualServiceSet.Contains(ownedService))
            {
                errors.Add(prefabPath + "\nRuntimeBootstrapOwner " + OwnerLabel(owner) + " references a service that is not an actual registered component under the owner: " + ownedService.GetType().FullName + ".");
            }

            ServiceLifetime serviceLifetime;
            if (RuntimeServices.TryGetDeclaredLifetime(ownedService.GetType(), out serviceLifetime) && serviceLifetime != owner.Lifetime)
            {
                errors.Add(prefabPath + "\nRuntimeBootstrapOwner " + OwnerLabel(owner) + " lifetime " + owner.Lifetime + " does not match owned service " + ownedService.GetType().FullName + " lifetime " + serviceLifetime + ".");
            }
        }

        foreach (var actualService in actualServices)
        {
            if (!ownedServiceSet.Contains(actualService))
            {
                errors.Add(prefabPath + "\nRuntimeBootstrapOwner " + OwnerLabel(owner) + " ownedServices is missing actual registered component: " + actualService.GetType().FullName + ".");
            }
        }
    }

    static string OwnerLabel(RuntimeBootstrapOwner owner)
    {
        return string.IsNullOrEmpty(owner.OwnerId) ? owner.name : owner.OwnerId;
    }

    static bool RegistersRuntimeService(MonoBehaviour component)
    {
        var script = MonoScript.FromMonoBehaviour(component);
        return script != null && script.text.Contains(RegisterCall);
    }

    static bool IsPersistentServiceType(System.Type type)
    {
        ServiceLifetime lifetime;
        return RuntimeServices.TryGetDeclaredLifetime(type, out lifetime) && lifetime == ServiceLifetime.Persistent;
    }
}
