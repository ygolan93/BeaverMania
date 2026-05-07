using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMovementController : MonoBehaviour
{
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }
}
