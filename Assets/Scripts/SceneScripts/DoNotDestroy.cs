using UnityEngine;

/// <summary>Legacy compatibility marker. Prefer RuntimeBootstrapOwner for runtime bootstrap persistence metadata.</summary>
[AddComponentMenu("Legacy/DoNotDestroy (Legacy)")]
public class DoNotDestroy : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<System.Type> PersistentTypes =
        new System.Collections.Generic.HashSet<System.Type>();

    private bool registered;

    private void Awake()
    {
        var serviceType = GetType();
        if (PersistentTypes.Contains(serviceType))
        {
            Destroy(gameObject);
            return;
        }

        PersistentTypes.Add(serviceType);
        registered = true;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (!registered || !Application.isPlaying)
        {
            return;
        }

        PersistentTypes.Remove(GetType());
    }
}
