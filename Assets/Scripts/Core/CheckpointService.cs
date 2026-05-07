using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class CheckpointService : MonoBehaviour
{
    public static CheckpointService Instance { get; private set; }

    public Vector3 StartingCheckpointPosition { get; private set; }
    public Vector3 LastCheckpointPosition { get; private set; }
    public int RemainingLives { get; private set; }
    public bool IsRespawnInProgress { get; private set; }
    public bool IsGameOver { get; private set; }

    public static CheckpointService GetOrCreate()
    {
        return RuntimeServices.GetOrCreate<CheckpointService>(ServiceLifetime.Scene);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
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

    public void RegisterCheckpoint(Vector3 position)
    {
        LastCheckpointPosition = position;
    }

    public bool TryRespawn(Behaviour player)
    {
        if (player == null || IsRespawnInProgress || IsGameOver)
        {
            return false;
        }

        if (RemainingLives <= 0)
        {
            IsGameOver = true;
            player.Lives = 0;
            return false;
        }

        IsRespawnInProgress = true;
        RemainingLives--;
        player.Lives = RemainingLives;
        player.CurrentHealth = player.MaxHealth;

        if (player.HealthBar != null)
        {
            player.HealthBar.SetHealth(player.MaxHealth);
        }

        player.transform.position = LastCheckpointPosition;
        StartCoroutine(ClearRespawnGuardNextFrame());
        return true;
    }

    public void ResetForNewRun(Vector3 startPosition, int startingLives)
    {
        StartingCheckpointPosition = startPosition;
        LastCheckpointPosition = startPosition;
        RemainingLives = Mathf.Max(0, startingLives);
        IsRespawnInProgress = false;
        IsGameOver = RemainingLives <= 0;
    }

    [System.Obsolete("Use RegisterCheckpoint(Vector3).")]
    public void SaveCheckpoint(Vector3 position)
    {
        RegisterCheckpoint(position);
    }

    [System.Obsolete("Use LastCheckpointPosition.")]
    public Vector3 RespawnPosition(Vector3 offset)
    {
        return LastCheckpointPosition + offset;
    }

    private IEnumerator ClearRespawnGuardNextFrame()
    {
        yield return null;
        IsRespawnInProgress = false;
    }
}
