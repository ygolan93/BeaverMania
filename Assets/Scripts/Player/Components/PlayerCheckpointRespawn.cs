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
        {
            player = GetComponent<BeaverPlayer>();
        }

        player.HealthBar.SetHealth(player.MaxHealth);
        player.CurrentHealth = player.MaxHealth;
        player.transform.position = player.GM.lastCheckPointPos;
        player.SpawnCheckpointPopUpEffect(player.transform.position);
        player.Lives--;
    }
}
}
