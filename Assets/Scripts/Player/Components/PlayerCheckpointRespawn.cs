using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Player
{
public class PlayerCheckpointRespawn : MonoBehaviour
{
    [SerializeField] BeaverPlayer player;

    public void Bind(BeaverPlayer behaviour)
    {
        player = behaviour;
    }

    public void RestartCheckpoint()
    {
        if (player == null)
            player = GetComponent<BeaverPlayer>();

        if (player == null)
            return;

        Vector3 checkpoint = ResolveCheckpointPosition();
        ApplyCheckpointRespawn(checkpoint, decrementLife: true);
    }

    /// <summary>
    /// Teleports the player to a safe position without consuming a life (out-of-bounds recovery).
    /// </summary>
    public void RecoverOutOfBounds(Vector3 safePosition)
    {
        if (player == null)
            player = GetComponent<BeaverPlayer>();

        if (player == null)
            return;

        ApplyCheckpointRespawn(safePosition, decrementLife: false);
    }

    Vector3 ResolveCheckpointPosition()
    {
        if (player.GM != null && player.GM.lastCheckPointPos != Vector3.zero)
            return player.GM.lastCheckPointPos;

        return player.transform.position;
    }

    void ApplyCheckpointRespawn(Vector3 position, bool decrementLife)
    {
        if (player.HealthBar != null)
        {
            player.HealthBar.SetHealth(player.MaxHealth);
            player.CurrentHealth = player.MaxHealth;
        }
        else
        {
            player.CurrentHealth = player.MaxHealth;
        }

        player.transform.position = position;
        player.SpawnCheckpointPopUpEffect(player.transform.position);

        if (decrementLife)
            player.Lives--;
    }
}
}
