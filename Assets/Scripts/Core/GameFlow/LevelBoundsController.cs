using Beavermania.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Core.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class LevelBoundsController : MonoBehaviour
    {
        const string GameplayLevelSceneName = SceneRestartController.DefaultLevelSceneName;

        [SerializeField] BeaverPlayer player;
        [SerializeField] Vector3 boundsCenter;
        [SerializeField] Vector3 boundsSize = new(220f, 80f, 220f);
        [SerializeField] float minWorldY = -25f;
        [SerializeField] float startupGraceSeconds = 3f;
        [SerializeField] float recoveryCooldownSeconds = 1.5f;

        float graceEndsAt;
        float nextRecoveryAllowedTime;
        bool boundsInitialized;
        string activeSceneName;

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ResetForScene(SceneManager.GetActiveScene());
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetForScene(scene);
        }

        void ResetForScene(Scene scene)
        {
            activeSceneName = scene.name;
            graceEndsAt = Time.time + Mathf.Max(0f, startupGraceSeconds);
            nextRecoveryAllowedTime = 0f;
            boundsInitialized = false;
            player = null;
        }

        void Start()
        {
            TryInitializeBounds();
        }

        void FixedUpdate()
        {
            if (!IsGameplayLevelScene())
                return;

            if (Time.time < graceEndsAt)
                return;

            if (!TryInitializeBounds())
                return;

            if (player == null)
                return;

            if (Time.time < nextRecoveryAllowedTime)
                return;

            if (!IsOutsideBounds(player.transform.position))
                return;

            RecoverPlayer();
        }

        bool IsGameplayLevelScene()
        {
            return activeSceneName == GameplayLevelSceneName;
        }

        bool TryInitializeBounds()
        {
            if (boundsInitialized && player != null)
                return true;

            if (player == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    player = playerObject.GetComponent<BeaverPlayer>();
            }

            if (player == null)
                return false;

            boundsCenter = player.transform.position;
            boundsInitialized = true;

            if (player.GM != null && player.GM.lastCheckPointPos == Vector3.zero)
                player.GM.lastCheckPointPos = player.transform.position;

            return true;
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
            nextRecoveryAllowedTime = Time.time + Mathf.Max(0.25f, recoveryCooldownSeconds);

            if (player.GobletPicked)
                player.GobletOFF();

            if (player.Player != null)
            {
                player.Player.velocity = Vector3.zero;
                player.Player.angularVelocity = Vector3.zero;
            }

            Vector3 safePosition = ResolveSafePosition();
            player.RecoverOutOfBounds(safePosition);
        }

        Vector3 ResolveSafePosition()
        {
            if (player.GM != null && player.GM.lastCheckPointPos != Vector3.zero)
                return player.GM.lastCheckPointPos + new Vector3(0f, 1f, 0f);

            return player.transform.position + new Vector3(0f, 1f, 0f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.35f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawLine(
                new Vector3(boundsCenter.x - boundsSize.x * 0.5f, minWorldY, boundsCenter.z - boundsSize.z * 0.5f),
                new Vector3(boundsCenter.x + boundsSize.x * 0.5f, minWorldY, boundsCenter.z + boundsSize.z * 0.5f));
        }
    }
}
