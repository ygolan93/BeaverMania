using UnityEngine;

namespace Beavermania.Data.NPC
{
    [CreateAssetMenu(fileName = "ScorpionStatsData", menuName = "Beavermania/NPC/Scorpion Stats Data")]
    public sealed class ScorpionStatsData : ScriptableObject
    {
        [Min(1)] public int maxHealth = 8000;
        [Min(1)] public int comboLimit = 15;
        [Min(0f)] public float stunDuration = 10f;
        [Min(0f)] public float chargeSpeed = 8f;
        [Min(0f)] public float chargeDuration = 1f;
        [Min(0f)] public float lookDistance = 30f;
        [Min(0f)] public float chargeDistance = 20f;
        [Min(0f)] public float attackDistance = 2.2f;
        [Min(0)] public int attackDamage = 15;
        [Min(0)] public int stingDamage = 30;
        [Min(0f)] public float rotationSpeed = 0.05f;
        [Min(0f)] public float recoveryDuration = 0f;
        [Range(0.05f, 1f)] public float projectileDamageMultiplier = 1f;

        void OnValidate()
        {
            WarnIf(chargeDistance > lookDistance, "chargeDistance should be <= lookDistance.");
            WarnIf(attackDistance > chargeDistance, "attackDistance should be <= chargeDistance.");
        }

        void WarnIf(bool condition, string message)
        {
            if (condition)
                Debug.LogWarning($"{name}: {message}", this);
        }
    }
}
