using UnityEngine;

namespace Beavermania.Player.Combat
{
    public enum PlayerAttackKind
    {
        Unspecified = 0,
        BareHands = 1,
        HurricaneKick = 2,
        Arrow = 3,
        SwordSwing = 4,
        HurricaneSword = 5,
    }

    public static class PlayerAttackDamageRules
    {
        public static float GetAttackMultiplier(PlayerAttackKind attackKind)
        {
            switch (attackKind)
            {
                case PlayerAttackKind.HurricaneKick:
                    return 0.8f;
                case PlayerAttackKind.Arrow:
                    return 2f;
                case PlayerAttackKind.SwordSwing:
                    return 3f;
                case PlayerAttackKind.HurricaneSword:
                    return 1.5f;
                case PlayerAttackKind.BareHands:
                case PlayerAttackKind.Unspecified:
                default:
                    return 1f;
            }
        }

        public static int ResolveDamage(
            int baseDamage,
            PlayerAttackKind attackKind,
            bool isStunned,
            float stunnedDamageMultiplier)
        {
            if (baseDamage <= 0)
                return 0;

            float attackAdjustedDamage = baseDamage * GetAttackMultiplier(attackKind);
            float fullyAdjustedDamage = isStunned
                ? attackAdjustedDamage * Mathf.Clamp(stunnedDamageMultiplier, 1.5f, 3f)
                : attackAdjustedDamage;

            return Mathf.Max(1, Mathf.RoundToInt(fullyAdjustedDamage));
        }
    }
}
