using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMovementController : MonoBehaviour
{
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
        ValidateOwnerReferences();
    }

    public bool HasOwner()
    {
        return owner != null;
    }

    public bool CanMove()
    {
        return ValidateOwnerReferences() && owner.enabled && owner.Player.gameObject.activeInHierarchy;
    }

    public bool ValidateOwnerReferences()
    {
        return HasOwner() && owner.Player != null;
    }

    public void ResetRuntimeState()
    {
        if (HasOwner())
        {
            owner.ResetMovementRuntimeState();
        }
    }
}
