using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public Vector3 LastCheckpointPosition
    {
        get { return CheckpointService.GetOrCreate().LastCheckpointPosition; }
    }

    public void SaveCheckpoint(Vector3 position)
    {
        CheckpointService.GetOrCreate().SaveCheckpoint(position);
    }

    public Vector3 RespawnPosition(Vector3 offset)
    {
        return CheckpointService.GetOrCreate().RespawnPosition(offset);
    }
}
