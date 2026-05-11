using UnityEngine;

namespace Beavermania.Player
{
public class PlayerCheckpointRespawn : MonoBehaviour
{
    [SerializeField] global::Behaviour player;

    public void Bind(global::Behaviour behaviour)
    {
        player = behaviour;
    }

    public void RestartCheckpoint()
    {
        if (player == null)
        {
            player = GetComponent<global::Behaviour>();
        }

        player.HealthBar.SetHealth(player.MaxHealth);
        player.CurrentHealth = player.MaxHealth;
        player.transform.position = player.GM.lastCheckPointPos;
        Instantiate(player.CheckpointPopUpEffect, player.transform.position, Quaternion.identity);
        player.Lives--;
    }
}
}
