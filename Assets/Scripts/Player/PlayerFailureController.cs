using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFailureController : MonoBehaviour
{
    [SerializeField] Behaviour owner;
    bool isResolvingFailure;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    void Awake()
    {
        if (owner == null)
        {
            owner = GetComponent<Behaviour>();
        }
    }

    public void HandleFailure(PlayerFailureReason reason)
    {
        if (isResolvingFailure)
        {
            return;
        }

        if (owner == null)
        {
            owner = GetComponent<Behaviour>();
            if (owner == null)
            {
                return;
            }
        }

        isResolvingFailure = true;

        if (owner.Lives > 1)
        {
            owner.Lives--;
            owner.RestoreHealth();
            owner.MoveToCheckpoint();
            isResolvingFailure = false;
            return;
        }

        owner.Lives = 0;
        owner.ActivateLooseMenu();
    }
}
