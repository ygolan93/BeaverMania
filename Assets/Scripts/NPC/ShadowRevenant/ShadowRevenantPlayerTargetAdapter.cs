using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantPlayerTargetAdapter : MonoBehaviour, IShadowRevenantTarget
    {
        [SerializeField] BeaverPlayer player;

        public Transform TargetTransform => player != null ? player.transform : transform;
        public bool CanReceiveShadowDamage => player != null && player.isActiveAndEnabled;
        public bool IsAvoidingDamage => player != null && player.Rolling;
        public bool IsParrying => player != null && player.isParried;

        void Awake()
        {
            if (player == null)
                player = GetComponent<BeaverPlayer>();
        }

        public void ReceiveShadowDamage(float damage)
        {
            if (!CanReceiveShadowDamage || damage <= 0f || IsAvoidingDamage)
                return;

            player.TakeDamage(damage);
        }

        public bool TryApplyDreadFogSlow(float slowPercent, float duration)
        {
            return false;
        }
    }
}
