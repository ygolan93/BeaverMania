using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCombatController : MonoBehaviour
{
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }
}
