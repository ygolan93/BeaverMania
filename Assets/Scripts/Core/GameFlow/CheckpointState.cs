using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    /// <summary>
    /// Minimal serializable checkpoint snapshot holder. Today <see cref="GameMaster"/> owns
    /// <c>lastCheckPointPos</c>; this type is safe to add to a scene or wire from <see cref="GameMaster"/>
    /// in a later PR without changing existing prefab GUIDs.
    /// </summary>
    public sealed class CheckpointState : MonoBehaviour
    {
        [SerializeField] Vector3 lastCheckpointPosition;
        [SerializeField] bool hasCheckpoint;

        public Vector3 LastCheckpointPosition => lastCheckpointPosition;

        public bool HasCheckpoint => hasCheckpoint;

        public void SetLastCheckpoint(Vector3 worldPosition)
        {
            lastCheckpointPosition = worldPosition;
            hasCheckpoint = true;
        }

        public void Clear()
        {
            hasCheckpoint = false;
        }
    }
}
