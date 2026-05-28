using UnityEngine;

namespace Beavermania.NPC
{
    public interface IShadowRevenantTarget
    {
        Transform TargetTransform { get; }
        bool CanReceiveShadowDamage { get; }
        bool IsAvoidingDamage { get; }
        bool IsParrying { get; }

        void ReceiveShadowDamage(float damage);
        bool TryApplyDreadFogSlow(float slowPercent, float duration);
    }
}
