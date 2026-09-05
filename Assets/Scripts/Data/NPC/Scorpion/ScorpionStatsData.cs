using UnityEngine;

namespace Beavermania.Data.NPC
{
    [CreateAssetMenu(fileName = "ScorpionStatsData", menuName = "Beavermania/NPC/Scorpion Stats Data")]
    public sealed class ScorpionStatsData : ScriptableObject
    {
        public const float MinimumChargeWindup = 0.35f;
        public const float MinimumActionRecovery = 0.2f;

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

        [Header("Boss Combat Contract")]
        [Min(0f)] public float bossStunDuration = 3.5f;
        [Min(0f)] public float bossStunCooldown = 10f;
        [Range(1.5f, 3f)] public float stunnedDamageMultiplier = 2f;
        [Min(0f)] public float hurricaneKickRetreatSpeed = 12f;
        [Min(0f)] public float hurricaneKickRetreatDuration = 0.6f;

        [Header("Advanced Combat AI")]
        public bool advancedAiEnabled;
        [Min(MinimumChargeWindup)] public float chargeWindupMin = MinimumChargeWindup;
        [Min(MinimumChargeWindup)] public float chargeWindupMax = 0.75f;
        [Min(0f)] public float chargeTrackingDuration = 0.35f;
        [Min(0f)] public float chargeMaximumDuration = 2.5f;
        [Min(0f)] public float chargeMaximumDistance = 20f;
        [Min(0f)] public float attackWindowDuration = 0.8f;
        [Min(0f)] public float decisionHoldMin = 0.2f;
        [Min(0f)] public float decisionHoldMax = 0.6f;
        [Min(0f)] public float phaseOneAttackWeight = 4f;
        [Min(0f)] public float phaseOneChargeWeight = 5f;
        [Min(0f)] public float phaseOneReverseWeight = 2f;
        [Min(0f)] public float phaseOneHoldWeight = 1f;
        [Min(0f)] public float phaseTwoAttackWeight = 5f;
        [Min(0f)] public float phaseTwoChargeWeight = 6f;
        [Min(0f)] public float phaseTwoReverseWeight = 1.5f;
        [Min(0f)] public float phaseTwoHoldWeight = 1.5f;
        [Min(0f)] public float phaseThreeAttackWeight = 7f;
        [Min(0f)] public float phaseThreeChargeWeight = 8f;
        [Min(0f)] public float phaseThreeReverseWeight = 1f;
        [Min(0f)] public float phaseThreeHoldWeight = 0.5f;

        [Header("Advanced Charge Variants")]
        [Min(0f)] public float phaseOneShortChargeWeight = 1f;
        [Min(0f)] public float phaseOneNormalChargeWeight = 6f;
        [Min(0f)] public float phaseOneCommittedChargeWeight = 1f;
        [Min(0f)] public float phaseTwoShortChargeWeight = 3f;
        [Min(0f)] public float phaseTwoNormalChargeWeight = 4f;
        [Min(0f)] public float phaseTwoCommittedChargeWeight = 1.5f;
        [Min(0f)] public float phaseThreeShortChargeWeight = 2f;
        [Min(0f)] public float phaseThreeNormalChargeWeight = 3f;
        [Min(0f)] public float phaseThreeCommittedChargeWeight = 5f;
        [Range(0f, 1f)] public float shortChargeDurationMultiplier = 0.65f;
        [Range(0f, 1f)] public float shortChargeDistanceMultiplier = 0.65f;
        [Min(1f)] public float committedChargeDurationMultiplier = 1.35f;
        [Min(1f)] public float committedChargeDistanceMultiplier = 1.35f;
        [Range(0f, 1f)] public float committedChargeTrackingMultiplier = 0.5f;

        [Header("Advanced Pacing")]
        [Min(0f)] public float phaseTwoDecisionHoldMin = 0.2f;
        [Min(0f)] public float phaseTwoDecisionHoldMax = 0.45f;
        [Min(0f)] public float phaseThreeDecisionHoldMin = 0.15f;
        [Min(0f)] public float phaseThreeDecisionHoldMax = 0.3f;
        [Min(MinimumActionRecovery)] public float phaseOneAttackRecovery = 1f;
        [Min(MinimumActionRecovery)] public float phaseTwoAttackRecovery = 0.7f;
        [Min(MinimumActionRecovery)] public float phaseThreeAttackRecovery = 0.45f;
        [Min(MinimumActionRecovery)] public float phaseOneChargeRecovery = 1.2f;
        [Min(MinimumActionRecovery)] public float phaseTwoChargeRecovery = 0.9f;
        [Min(MinimumActionRecovery)] public float phaseThreeChargeRecovery = 0.65f;
        [Min(MinimumActionRecovery)] public float reverseVulnerabilityDuration = 0.8f;

        [Header("Advanced Post-Stun Pressure")]
        [Min(0f)] public float postStunPressureDuration = 4f;
        [Min(1f)] public float postStunChargeWeightMultiplier = 1.2f;
        [Range(0f, 1f)] public float postStunHoldWeightMultiplier = 0.35f;
        [Range(0.1f, 1f)] public float postStunRecoveryMultiplier = 0.8f;

        void OnValidate()
        {
            chargeWindupMin = Mathf.Max(MinimumChargeWindup, chargeWindupMin);
            chargeWindupMax = Mathf.Max(chargeWindupMin, chargeWindupMax);
            chargeTrackingDuration = Mathf.Min(chargeTrackingDuration, chargeMaximumDuration);
            decisionHoldMax = Mathf.Max(decisionHoldMin, decisionHoldMax);
            phaseTwoDecisionHoldMax = Mathf.Max(phaseTwoDecisionHoldMin, phaseTwoDecisionHoldMax);
            phaseThreeDecisionHoldMax = Mathf.Max(phaseThreeDecisionHoldMin, phaseThreeDecisionHoldMax);
            phaseOneAttackRecovery = Mathf.Max(MinimumActionRecovery, phaseOneAttackRecovery);
            phaseTwoAttackRecovery = Mathf.Max(MinimumActionRecovery, phaseTwoAttackRecovery);
            phaseThreeAttackRecovery = Mathf.Max(MinimumActionRecovery, phaseThreeAttackRecovery);
            phaseOneChargeRecovery = Mathf.Max(MinimumActionRecovery, phaseOneChargeRecovery);
            phaseTwoChargeRecovery = Mathf.Max(MinimumActionRecovery, phaseTwoChargeRecovery);
            phaseThreeChargeRecovery = Mathf.Max(MinimumActionRecovery, phaseThreeChargeRecovery);
            reverseVulnerabilityDuration = Mathf.Max(MinimumActionRecovery, reverseVulnerabilityDuration);
            bossStunDuration = Mathf.Max(0f, bossStunDuration);
            bossStunCooldown = Mathf.Max(0f, bossStunCooldown);
            stunnedDamageMultiplier = Mathf.Clamp(stunnedDamageMultiplier, 1.5f, 3f);
            hurricaneKickRetreatSpeed = Mathf.Max(0f, hurricaneKickRetreatSpeed);
            hurricaneKickRetreatDuration = Mathf.Max(0f, hurricaneKickRetreatDuration);

            WarnIf(chargeDistance > lookDistance, "chargeDistance should be <= lookDistance.");
            WarnIf(attackDistance > chargeDistance, "attackDistance should be <= chargeDistance.");
            WarnIf(
                phaseOneAttackRecovery < phaseTwoAttackRecovery
                || phaseTwoAttackRecovery < phaseThreeAttackRecovery,
                "Attack recovery should shorten from phase one through phase three.");
            WarnIf(
                phaseOneChargeRecovery < phaseTwoChargeRecovery
                || phaseTwoChargeRecovery < phaseThreeChargeRecovery,
                "Charge recovery should shorten from phase one through phase three.");
        }

        void WarnIf(bool condition, string message)
        {
            if (condition)
                Debug.LogWarning($"{name}: {message}", this);
        }
    }
}
