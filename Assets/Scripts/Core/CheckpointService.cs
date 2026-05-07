using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class CheckpointService : MonoBehaviour
{
    public static CheckpointService Instance { get; private set; }

    public Vector3 LastCheckpointPosition { get; private set; }

    public static CheckpointService GetOrCreate()
    {
        return RuntimeServices.GetOrCreate<CheckpointService>(ServiceLifetime.Scene);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            BuildSafeLogger.WarnOnce(
                nameof(CheckpointService) + ".DuplicateManager",
                "Duplicate manager destroyed: " + nameof(CheckpointService) + ".",
                this,
                nameof(CheckpointService));
            Destroy(gameObject);
            return;
        }

        if (!RuntimeServices.Register(this, ServiceLifetime.Scene))
        {
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            RuntimeServices.Unregister(this);
        }
    }

    public void SaveCheckpoint(Vector3 position)
    {
        LastCheckpointPosition = position;
    }

    public Vector3 RespawnPosition(Vector3 offset)
    {
        return LastCheckpointPosition + offset;
    }
}
