using Beavermania.Player;
using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Core.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class LevelBoundsController : MonoBehaviour
    {
        [SerializeField] BeaverPlayer player;
        [SerializeField] Vector3 boundsCenter;
        [SerializeField] Vector3 boundsSize = new(220f, 80f, 220f);
        [SerializeField] float minWorldY = -25f;
        [SerializeField] bool usePlayerAsCenterOnStart;

        void Start()
        {
            if (player == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    player = playerObject.GetComponent<BeaverPlayer>();
            }

            if (usePlayerAsCenterOnStart && player != null)
                boundsCenter = player.transform.position;
        }

        void FixedUpdate()
        {
            if (player == null)
                return;

            if (!IsOutsideBounds(player.transform.position))
                return;

            RecoverPlayer();
        }

        bool IsOutsideBounds(Vector3 position)
        {
            if (position.y < minWorldY)
                return true;

            Vector3 half = boundsSize * 0.5f;
            Vector3 min = boundsCenter - half;
            Vector3 max = boundsCenter + half;
            return position.x < min.x || position.x > max.x
                || position.z < min.z || position.z > max.z;
        }

        void RecoverPlayer()
        {
            if (player.GobletPicked)
                player.GobletOFF();

            if (player.Player != null)
            {
                player.Player.velocity = Vector3.zero;
                player.Player.angularVelocity = Vector3.zero;
            }

            player.RestartCheckpoint();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.35f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawLine(boundsCenter + Vector3.left * boundsSize.x, boundsCenter + Vector3.right * boundsSize.x);
            Gizmos.DrawLine(new Vector3(boundsCenter.x - boundsSize.x * 0.5f, minWorldY, boundsCenter.z - boundsSize.z * 0.5f),
                new Vector3(boundsCenter.x + boundsSize.x * 0.5f, minWorldY, boundsCenter.z + boundsSize.z * 0.5f));
        }
    }
}
