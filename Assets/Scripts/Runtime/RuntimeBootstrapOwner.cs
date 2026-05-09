using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RuntimeBootstrapOwner : MonoBehaviour
{
    static readonly Dictionary<string, RuntimeBootstrapOwner> OwnersById = new Dictionary<string, RuntimeBootstrapOwner>();

    [SerializeField] string ownerId;
    [SerializeField] ServiceLifetime lifetime;
    [SerializeField] Component[] ownedServices;

    public string OwnerId { get { return ownerId; } }
    public ServiceLifetime Lifetime { get { return lifetime; } }
    public Component[] OwnedServices { get { return ownedServices; } }

    void Awake()
    {
        ValidateOwnedServices();

        if (lifetime == ServiceLifetime.Persistent)
        {
            DontDestroyOnLoad(gameObject);
        }

        ValidateUniqueOwnerId();
    }

    void OnDestroy()
    {
        RuntimeBootstrapOwner owner;
        if (!string.IsNullOrEmpty(ownerId) && OwnersById.TryGetValue(ownerId, out owner) && owner == this)
        {
            OwnersById.Remove(ownerId);
        }
    }

    void ValidateOwnedServices()
    {
        if (ownedServices == null)
        {
            return;
        }

        var serviceTypes = new HashSet<System.Type>();
        for (var i = 0; i < ownedServices.Length; i++)
        {
            var service = ownedServices[i];
            if (service == null)
            {
                Debug.LogError("RuntimeBootstrapOwner ownedServices contains a null entry at index " + i + ".", this);
                continue;
            }

            var serviceType = service.GetType();
            if (!serviceTypes.Add(serviceType))
            {
                Debug.LogError("Duplicate owned runtime service component type found under owner " + OwnerLabel() + ": " + serviceType.FullName + ".", this);
            }

            ServiceLifetime serviceLifetime;
            if (!RuntimeServices.TryGetDeclaredLifetime(serviceType, out serviceLifetime))
            {
                continue;
            }

            if (lifetime == ServiceLifetime.Persistent && serviceLifetime == ServiceLifetime.Scene)
            {
                Debug.LogError("Persistent RuntimeBootstrapOwner " + OwnerLabel() + " contains scene-lifetime-only service: " + serviceType.FullName + ".", service);
            }
            else if (lifetime == ServiceLifetime.Scene && serviceLifetime == ServiceLifetime.Persistent)
            {
                Debug.LogError("Scene RuntimeBootstrapOwner " + OwnerLabel() + " contains persistent service: " + serviceType.FullName + ".", service);
            }
        }
    }

    string OwnerLabel()
    {
        return string.IsNullOrEmpty(ownerId) ? name : ownerId;
    }

    void ValidateUniqueOwnerId()
    {
        if (string.IsNullOrEmpty(ownerId))
        {
            Debug.LogError("RuntimeBootstrapOwner requires a non-empty ownerId.", this);
            return;
        }

        RuntimeBootstrapOwner existing;
        if (OwnersById.TryGetValue(ownerId, out existing) && existing != null && existing != this)
        {
            Debug.LogError("Duplicate RuntimeBootstrapOwner ownerId found: " + ownerId + ".", this);
            return;
        }

        OwnersById[ownerId] = this;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ValidateOwnedServiceTypesOnly();

        if (string.IsNullOrEmpty(ownerId))
        {
            return;
        }

        foreach (var owner in FindObjectsOfType<RuntimeBootstrapOwner>(true))
        {
            if (owner != this && owner.ownerId == ownerId)
            {
                Debug.LogError("Duplicate RuntimeBootstrapOwner ownerId found: " + ownerId + ".", this);
                return;
            }
        }
    }

    void ValidateOwnedServiceTypesOnly()
    {
        if (ownedServices == null)
        {
            return;
        }

        var serviceTypes = new HashSet<System.Type>();
        foreach (var service in ownedServices)
        {
            if (service == null)
            {
                continue;
            }

            var serviceType = service.GetType();
            if (!serviceTypes.Add(serviceType))
            {
                Debug.LogError("Duplicate owned runtime service component type found under owner " + OwnerLabel() + ": " + serviceType.FullName + ".", this);
            }
        }
    }
#endif
}
