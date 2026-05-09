using UnityEngine;

public enum RuntimeServiceOwnerKind
{
    PrefabOwned = 0,
    FallbackCreated = 1
}

public sealed class RuntimeServiceOwnerMarker : MonoBehaviour
{
    [SerializeField] RuntimeServiceOwnerKind ownerKind;
    [SerializeField] bool destroyOnSceneServiceReset;

    public RuntimeServiceOwnerKind OwnerKind { get { return ownerKind; } }
    public bool DestroyOnSceneServiceReset { get { return destroyOnSceneServiceReset; } }

    public void MarkFallbackCreated()
    {
        ownerKind = RuntimeServiceOwnerKind.FallbackCreated;
        destroyOnSceneServiceReset = true;
    }
}
