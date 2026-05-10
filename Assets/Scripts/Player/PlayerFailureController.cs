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
        try
        {
            if (reason == PlayerFailureReason.CompatibilityRestart)
            {
                RestartCheckpointForCompatibility();
                return;
            }

            ResolveFailure();
        }
        finally
        {
            isResolvingFailure = false;
        }
    }

    void ResolveFailure()
    {
        if (owner.Lives > 1)
        {
            owner.Lives--;
            owner.RestoreHealth();
            owner.MoveToCheckpoint();
            ResetRuntimeState();
            return;
        }

        owner.Lives = 0;
        owner.ActivateLooseMenu();
    }

    void RestartCheckpointForCompatibility()
    {
        if (owner.Lives <= 0)
        {
            owner.Lives = 0;
            owner.ActivateLooseMenu();
            return;
        }

        owner.Lives--;
        owner.RestoreHealth();
        owner.MoveToCheckpoint();
        ResetRuntimeState();
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
