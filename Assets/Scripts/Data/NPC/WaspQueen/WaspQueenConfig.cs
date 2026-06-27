using Beavermania.NPC;
using UnityEngine;

namespace Beavermania.Data.NPC
{
    [CreateAssetMenu(fileName = "WaspQueenConfig", menuName = "Beavermania/NPC/Wasp Queen Config")]
    public sealed class WaspQueenConfig : ScriptableObject
    {
        [System.Serializable]
        public sealed class PhaseSettings
        {
            [Header("Summons")]
            public int maxActiveSummonedWasps = 3;
            public int waspsPerSummon = 1;
            public float summonCooldown = 8f;
            public float summonTelegraphDuration = 0.8f;
            public float summonRecoveryDuration = 0.45f;

            [Header("Ranged")]
            public float rangedCooldown = 4f;
            public float rangedTelegraphDuration = 0.75f;
            public float rangedRecoveryDuration = 0.45f;
            public int rangedDamage = 15;
            public float projectileSpeed = 16f;

            [Header("Poison AoE")]
            public float aoeCooldown = 6f;
            public float aoeTelegraphDuration = 1f;
            public float aoeRecoveryDuration = 0.6f;
            public float aoeRadius = 4f;
            public float aoeDamage = 10f;
            public float aoeDuration = 3f;
            public float aoeTickRate = 0.5f;
            [Tooltip("Ground warning-ring time the poison zone shows before it starts dealing damage.")]
            public float aoeGroundTelegraphTime = 0.5f;

            [Header("Charge")]
            public float chargeCooldown = 7f;
            public float chargeTelegraphDuration = 0.9f;
            public float chargeSpeed = 18f;
            public float chargeDuration = 0.45f;
            public float chargeDamage = 20f;
            public float chargeRecoveryDuration = 0.8f;

            [Header("Sting Lunge")]
            public float stingCooldown = 6f;
            public float stingSpeed = 24f;
            public float stingDamage = 18f;

            [Header("Decision Weights")]
            public float rangedWeight = 3f;
            public float aoeWeight = 1f;
            public float chargeWeight = 2f;
            public float summonWeight = 2f;
            public float stingWeight = 2.5f;
            public float sameAbilityPenalty = 1.75f;
        }

        [Header("Health")]
        [Min(1)] public int maxHealth = 3500;
        [Range(0.05f, 1f)] public float phaseTwoHealthThresholdNormalized = 0.7f;
        [Range(0.05f, 1f)] public float phaseThreeHealthThresholdNormalized = 0.3f;
        [Min(0f)] public float victoryDelay = 2f;

        [Header("Activation")]
        [Min(0f)] public float activateRange = 25f;
        [Min(0f)] public float introDuration = 0.75f;
        [Min(0f)] public float idleDecisionDelay = 0.55f;
        [Min(0f)] public float phaseTransitionDuration = 0.9f;

        [Header("Ranges")]
        [Min(0.5f)] public float closeRange = 4f;
        [Min(0.5f)] public float mediumRange = 9f;
        [Min(0.5f)] public float farRange = 18f;
        [Min(0.5f)] public float chargeMinRange = 5f;
        [Min(0.5f)] public float chargeMaxRange = 11f;
        [Min(0.5f)] public float stingMinRange = 6f;
        [Min(0.5f)] public float stingMaxRange = 16f;

        [Header("Sting Lunge")]
        [Min(0f)] public float stingTelegraphDuration = 0.55f;
        [Min(0.05f)] public float stingActiveDuration = 0.9f;
        [Min(0f)] public float stingRecoveryDuration = 0.7f;
        [Range(0f, 1f)] public float stingHomingStrength = 0.5f;
        [Min(0.5f)] public float stingRetreatSpeed = 18f;
        [Min(0f)] public float stingRetreatDuration = 1.2f;

        [Header("Hover / Grounding")]
        [Min(0f)] public float hoverHeight = 1.5f;
        [Min(0.1f)] public float groundCheckStartHeight = 10f;
        [Min(0.1f)] public float groundCheckDistance = 24f;
        public LayerMask groundMask = ~0;
        public LayerMask chargeObstructionMask;

        [Header("Hazard Caps")]
        [Min(1)] public int maxActiveProjectiles = 4;
        [Min(1)] public int maxActivePoisonZones = 2;

        [Header("Arena / Leash")]
        [Tooltip("Soft arena radius around arenaCenter; drifting past it makes the boss recenter.")]
        [Min(0f)] public float arenaRadius = 22f;
        [Tooltip("If the player gets farther than this, the boss disengages and returns to center. 0 disables.")]
        [Min(0f)] public float leashRange = 32f;
        [Tooltip("Player distance at which the boss re-engages once it has returned to the arena.")]
        [Min(0f)] public float reengageRange = 20f;
        [Min(0f)] public float recenterSpeed = 14f;

        [Header("Reposition")]
        [Range(0f, 1f)] public float repositionChance = 0.5f;
        [Min(0f)] public float repositionDuration = 0.6f;
        [Min(0f)] public float repositionSpeed = 9f;
        [Min(0.5f)] public float repositionTooCloseRange = 4f;
        [Min(0.5f)] public float repositionStep = 4f;

        [Header("Prefabs")]
        public GameObject waspPrefab;
        public WaspQueenProjectile poisonProjectilePrefab;
        public WaspQueenPoisonZone poisonZonePrefab;
        public GameObject deathExplosionPrefab;
        public GameObject[] fragmentPrefabs;

        [Header("Phases")]
        public PhaseSettings phase1 = new PhaseSettings
        {
            maxActiveSummonedWasps = 3,
            waspsPerSummon = 1,
            rangedCooldown = 4.5f,
            rangedTelegraphDuration = 0.85f,
            rangedRecoveryDuration = 0.6f,
            rangedDamage = 14,
            projectileSpeed = 15f,
            aoeCooldown = 7f,
            aoeTelegraphDuration = 1.1f,
            aoeRecoveryDuration = 0.75f,
            aoeRadius = 3.5f,
            aoeDamage = 10f,
            aoeDuration = 2.5f,
            aoeTickRate = 0.6f,
            aoeGroundTelegraphTime = 0.5f,
            chargeCooldown = 8f,
            chargeTelegraphDuration = 0.95f,
            chargeSpeed = 16f,
            chargeDuration = 0.4f,
            chargeDamage = 18f,
            chargeRecoveryDuration = 0.95f,
            summonCooldown = 9f,
            summonTelegraphDuration = 0.9f,
            summonRecoveryDuration = 0.6f,
            rangedWeight = 4f,
            aoeWeight = 1f,
            chargeWeight = 2f,
            summonWeight = 3f,
            stingCooldown = 7f,
            stingSpeed = 22f,
            stingDamage = 16f,
            stingWeight = 2.5f,
            sameAbilityPenalty = 1.5f
        };

        public PhaseSettings phase2 = new PhaseSettings
        {
            maxActiveSummonedWasps = 5,
            waspsPerSummon = 2,
            rangedCooldown = 3.25f,
            rangedTelegraphDuration = 0.65f,
            rangedRecoveryDuration = 0.5f,
            rangedDamage = 18,
            projectileSpeed = 17f,
            aoeCooldown = 5.5f,
            aoeTelegraphDuration = 0.8f,
            aoeRecoveryDuration = 0.55f,
            aoeRadius = 4.25f,
            aoeDamage = 12f,
            aoeDuration = 3f,
            aoeTickRate = 0.5f,
            aoeGroundTelegraphTime = 0.45f,
            chargeCooldown = 6.5f,
            chargeTelegraphDuration = 0.75f,
            chargeSpeed = 18f,
            chargeDuration = 0.45f,
            chargeDamage = 22f,
            chargeRecoveryDuration = 0.8f,
            summonCooldown = 7.5f,
            summonTelegraphDuration = 0.75f,
            summonRecoveryDuration = 0.5f,
            rangedWeight = 3.5f,
            aoeWeight = 3f,
            chargeWeight = 3f,
            summonWeight = 4f,
            stingCooldown = 5.5f,
            stingSpeed = 26f,
            stingDamage = 20f,
            stingWeight = 3f,
            sameAbilityPenalty = 1.75f
        };

        public PhaseSettings phase3 = new PhaseSettings
        {
            maxActiveSummonedWasps = 7,
            waspsPerSummon = 3,
            rangedCooldown = 2.6f,
            rangedTelegraphDuration = 0.55f,
            rangedRecoveryDuration = 0.45f,
            rangedDamage = 20,
            projectileSpeed = 19f,
            aoeCooldown = 4.5f,
            aoeTelegraphDuration = 0.7f,
            aoeRecoveryDuration = 0.5f,
            aoeRadius = 4.75f,
            aoeDamage = 14f,
            aoeDuration = 3.5f,
            aoeTickRate = 0.45f,
            aoeGroundTelegraphTime = 0.4f,
            chargeCooldown = 5.5f,
            chargeTelegraphDuration = 0.65f,
            chargeSpeed = 20f,
            chargeDuration = 0.5f,
            chargeDamage = 25f,
            chargeRecoveryDuration = 0.7f,
            summonCooldown = 6f,
            summonTelegraphDuration = 0.65f,
            summonRecoveryDuration = 0.45f,
            rangedWeight = 3f,
            aoeWeight = 4f,
            chargeWeight = 4f,
            summonWeight = 4.5f,
            stingCooldown = 4.5f,
            stingSpeed = 30f,
            stingDamage = 24f,
            stingWeight = 3.5f,
            sameAbilityPenalty = 2f
        };

        void OnValidate()
        {
            if (mediumRange < closeRange)
                Debug.LogWarning($"{name}: mediumRange should be >= closeRange.", this);

            if (farRange < mediumRange)
                Debug.LogWarning($"{name}: farRange should be >= mediumRange.", this);

            if (chargeMaxRange < chargeMinRange)
                Debug.LogWarning($"{name}: chargeMaxRange should be >= chargeMinRange.", this);

            if (stingMaxRange < stingMinRange)
                Debug.LogWarning($"{name}: stingMaxRange should be >= stingMinRange.", this);

            if (phaseThreeHealthThresholdNormalized > phaseTwoHealthThresholdNormalized)
                Debug.LogWarning($"{name}: phaseThree threshold should be <= phaseTwo threshold.", this);
        }
    }
}
