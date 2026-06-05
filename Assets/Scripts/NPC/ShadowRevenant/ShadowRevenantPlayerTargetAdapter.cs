using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantPlayerTargetAdapter : MonoBehaviour, IShadowRevenantTarget
    {
        const float MaxSlowPercent = 0.9f;

        [SerializeField] BeaverPlayer player;

        float cachedWalkSpeed;
        float cachedRunSpeed;
        float slowExpiresAt;
        bool dreadFogSlowActive;

        public Transform TargetTransform => player != null ? player.transform : transform;
        public bool CanReceiveShadowDamage => player != null && player.isActiveAndEnabled;
        public bool IsAvoidingDamage => player != null && player.Rolling;
        public bool IsParrying => player != null && player.isParried;

        void Awake()
        {
            if (player != null)
                return;

            player = GetComponent<BeaverPlayer>();
            if (player != null)
                return;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.GetComponent<BeaverPlayer>();
        }

        void Update()
        {
            if (!dreadFogSlowActive || Time.time < slowExpiresAt)
                return;

            RestoreDreadFogSlow();
        }

        void OnDisable()
        {
            RestoreDreadFogSlow();
        }

        public void ReceiveShadowDamage(float damage)
        {
            if (!CanReceiveShadowDamage || damage <= 0f || IsAvoidingDamage)
                return;

            player.TakeDamage(damage);
        }

        public bool TryApplyDreadFogSlow(float slowPercent, float duration)
        {
            if (!CanReceiveShadowDamage || slowPercent <= 0f || duration <= 0f)
                return false;

            if (!dreadFogSlowActive)
            {
                cachedWalkSpeed = player.Walk;
                cachedRunSpeed = player.Run;
                dreadFogSlowActive = true;
            }

            float speedMultiplier = 1f - Mathf.Clamp(slowPercent, 0f, MaxSlowPercent);
            player.Walk = cachedWalkSpeed * speedMultiplier;
            player.Run = cachedRunSpeed * speedMultiplier;
            slowExpiresAt = Mathf.Max(slowExpiresAt, Time.time + duration);
            return true;
        }

        void RestoreDreadFogSlow()
        {
            if (!dreadFogSlowActive)
                return;

            if (player != null)
            {
                player.Walk = cachedWalkSpeed;
                player.Run = cachedRunSpeed;
            }

            cachedWalkSpeed = 0f;
            cachedRunSpeed = 0f;
            slowExpiresAt = 0f;
            dreadFogSlowActive = false;
        }
    }
}
