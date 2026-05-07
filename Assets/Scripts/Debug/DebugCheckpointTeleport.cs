using UnityEngine;

public sealed class DebugCheckpointTeleport : MonoBehaviour
{
    [SerializeField] DebugQAConfig config;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;

    void Update()
    {
        if (config == null || !config.enableCheckpointTeleport || !Input.GetKeyDown(config.checkpointTeleportKey))
        {
            return;
        }

        if (!PlayerReference.TryGetPlayer(out Behaviour player) || player == null)
        {
            return;
        }

        Vector3 position = CheckpointService.GetOrCreate().RespawnPosition(config.checkpointTeleportOffset);
        player.transform.position = position;

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
#else
    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;
#endif
}
