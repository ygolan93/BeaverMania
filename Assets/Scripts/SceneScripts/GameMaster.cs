using System;
using UnityEngine;

[Obsolete("Use CheckpointService directly.")]
public class GameMaster : MonoBehaviour
{
    public Vector3 lastCheckPointPos
    {
        get { return CheckpointService.GetOrCreate().LastCheckpointPosition; }
        set { CheckpointService.GetOrCreate().RegisterCheckpoint(value); }
    }

    public Vector3 LastCheckpointPosition
    {
        get { return CheckpointService.GetOrCreate().LastCheckpointPosition; }
    }

    public void SaveCheckpoint(Vector3 position)
    {
        CheckpointService.GetOrCreate().RegisterCheckpoint(position);
    }

    public Vector3 RespawnPosition(Vector3 offset)
    {
        return CheckpointService.GetOrCreate().LastCheckpointPosition + offset;
    }
}
