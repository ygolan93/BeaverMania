using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFailureController : MonoBehaviour
{
    [SerializeField] Behaviour owner;
    [SerializeField] RuntimeResetService runtimeResetService;
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

        if (runtimeResetService == null)
        {
            runtimeResetService = GetComponent<RuntimeResetService>();
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
            ResetRuntimeState();
            isResolvingFailure = false;
            return;
        }

        owner.Lives = 0;
        owner.ActivateLooseMenu();
    }

    void ResetRuntimeState()
    {
        if (runtimeResetService == null)
        {
            runtimeResetService = RuntimeResetService.Instance;
        }

        if (runtimeResetService != null)
        {
            runtimeResetService.ResetAll();
        }
    }
}
