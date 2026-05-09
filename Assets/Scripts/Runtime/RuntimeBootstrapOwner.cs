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
#endif
}
