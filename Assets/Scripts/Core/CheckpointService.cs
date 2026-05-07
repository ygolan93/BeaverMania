using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class CheckpointService : MonoBehaviour
{
    public static CheckpointService Instance { get; private set; }

    public Vector3 LastCheckpointPosition { get; private set; }

    public static CheckpointService GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindObjectOfType<CheckpointService>();
        if (Instance != null)
        {
            return Instance;
        }

        var gameObject = new GameObject(nameof(CheckpointService));
        return gameObject.AddComponent<CheckpointService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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
