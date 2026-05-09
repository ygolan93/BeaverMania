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

    public bool ValidateOwnerReferences()
    {
        return HasOwner() && owner.Player != null;
    }
}
