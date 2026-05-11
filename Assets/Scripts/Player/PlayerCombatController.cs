using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCombatController : MonoBehaviour
{
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    public bool HasOwner()
    {
        return owner != null;
    }

    public bool CanUseCombatInput()
    {
        return HasOwner() && owner.CurrentStamina > 0f;
    }

    public bool ValidateOwnerReferences()
    {
        return HasOwner() && owner.Otter != null;
    }
}
