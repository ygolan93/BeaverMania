using Beavermania.NPC;
using UnityEngine;

namespace Beavermania.Data.NPC
{
    [CreateAssetMenu(fileName = "ShadowRevenantConfig", menuName = "Beavermania/NPC/Shadow Revenant Config")]
    public sealed class ShadowRevenantConfig : ScriptableObject
    {
        [Header("Health")]
        public int maxHealth = 4200;
        [Range(0f, 2f)] public float normalDamageMultiplier = 1f;
        [Range(0f, 1f)] public float phasedDamageMultiplier = 0f;
        [Range(1f, 4f)] public float lightBrokenDamageMultiplier = 1.75f;
        [Range(0.05f, 1f)] public float playerProjectileDamageMultiplier = 0.4f;

        [Header("Detection")]
        public float aggroRange = 48f;
        public float leashRange = 70f;
        public float faceTurnSpeed = 8f;
        public float strafeSpeed = 5.25f;
        public float closeRange = 6f;
        public float mediumRange = 18f;
        public float farRange = 30f;
        public bool preferPhaseWhenClose = true;

        [Header("Hover / Grounding")]
        public bool usePhysicsGravity;
        public float hoverHeight = 1.6f;
        public float groundCheckStartHeight = 12f;
        public float groundCheckDistance = 24f;
        public LayerMask groundMask;
        [Tooltip("0 = instant vertical snap to hover height.")]
        public float verticalSnapSpeed = 8f;

        [Header("Phase Shift")]
        public float phaseCooldown = 6.5f;
        public float phaseDuration = 1.8f;
        public float phaseWindup = 0.45f;
        public float phaseTriggerRange = 18f;
        public float teleportMinRadius = 9f;
        public float teleportMaxRadius = 17f;
        public float teleportRaycastHeight = 12f;
        public float teleportClearanceRadius = 1.2f;
        public int teleportValidationAttempts = 12;
        public LayerMask teleportGroundMask = ~0;
        public LayerMask teleportObstructionMask;

        [Header("Shadow Projectile")]
        public ShadowRevenantProjectile projectilePrefab;
        public int projectileDamage = 30;
        public float projectileSpeed = 28f;
        public float projectileLifetime = 5f;
        public float projectileCooldown = 1.85f;
        public float projectileWindup = 0.55f;
        public float projectileRecover = 0.35f;
        public float projectileRange = 36f;

        [Header("Projectile Aim Line")]
        public bool enableProjectileAimLine = true;
        public float projectileAimLineWidth = 0.15f;
        public Color projectileAimLineColor = new Color(0.2f, 1f, 0.45f, 0.85f);
        public GameObject projectileTracerVfxPrefab;
        public LayerMask projectileObstructionMask;

        [Header("Dread Fog")]
        public ShadowRevenantDreadFogZone fogPrefab;
        public float fogRadius = 4.5f;
        public float fogDuration = 3.75f;
        public float fogDamagePerTick = 12f;
        public float fogTickInterval = 0.6f;
        [Range(0f, 1f)] public float fogSlowPercent = 0.45f;
        public float fogCooldown = 6.25f;
        public float fogWindup = 0.9f;
        public float fogRecover = 0.45f;
        public float fogRange = 24f;
        public float fogVisualScaleMultiplier = 1f;
        [Tooltip("Seconds to shrink/fade active hazard visuals before returning to pool. 0 = instant release.")]
        public float fogFadeOutTime = 0.25f;

        [Header("Shade Summon")]
        public ShadowRevenantShadeMinion shadeMinionPrefab;
        public int maxActiveMinions = 4;
        public int summonCount = 3;
        public float summonCooldown = 9.5f;
        public float summonWindup = 0.8f;
        public float summonRecover = 0.7f;
        public float shadeMoveSpeed = 7f;
        public float shadeDamage = 14f;
        public float shadeDamageCooldown = 1.1f;
        public float shadeLifetime = 16f;
        public int shadeMaxHealth = 2;
        public GameObject shadeHitVfxPrefab;
        public GameObject shadeDeathVfxPrefab;
        public float shadeAttackSfxCooldown = 1.1f;
        public float shadeOrbitRadius = 5.5f;
        public float shadeRetreatRange = 2.4f;
        public float shadeRetreatSpeed = 9f;
        public float shadeRetreatDuration = 0.85f;
        public float shadeApproachDuration = 0.55f;
        public float shadeApproachInterval = 2.75f;
        public float shadeApproachIntervalVariance = 1.25f;
        [Range(0f, 1f)] public float shadeApproachChance = 0.7f;
        public float shadeMaxApproachDuration = 8f;
        [Range(1f, 2f)] public float shadeApproachSpeedMultiplier = 1.35f;
        public float shadeHoverHeight = 1.2f;
        public float shadeOrbitSfxInterval = 2.4f;
        public float bossStrafePulseInterval = 0.45f;

        [Header("Charge Attack")]
        public bool enableChargeAttack = true;
        public int chargeDamage = 24;
        public float chargeCooldown = 10f;
        public float chargeWindup = 0.75f;
        public float chargeDuration = 0.55f;
        public float chargeRecover = 0.65f;
        public float chargeSpeed = 18f;
        public float chargeMinRange = 6f;
        public float chargeMaxRange = 11f;
        public float chargeHitRadius = 1.4f;
        public LayerMask chargeObstructionMask;
        public GameObject chargeImpactVfxPrefab;
        public GameObject chargeWindupVfxPrefab;

        [Header("Combos")]
        public bool enableCombos = true;
        [Range(0f, 1f)] public float comboChance = 0.18f;
        public float comboCooldown = 7.5f;
        public int maxComboFollowUps = 1;
        public bool allowProjectileAfterSummon = true;
        public bool allowProjectileAfterPhase = true;
        public bool allowChargeAfterProjectile = true;
        public bool allowProjectileAfterFog = true;
        public bool disallowChargeAfterFog = true;
        public bool disallowFogAfterCharge = true;

        [Header("Light Break")]
        public float lightBreakVulnerableDuration = 3.25f;
        public float lightBreakStaggerSeconds = 0.45f;

        [Header("Pools")]
        public int projectilePrewarmCount = 8;
        public int projectileMaxActive = 24;
        public int fogPrewarmCount = 4;
        public int fogMaxActive = 4;
        public int shadePrewarmCount = 3;
        public int shadeMaxActive = 6;

        [Header("Drops and VFX")]
        public GameObject[] deathDropPrefabs;
        public GameObject remainsPrefab;
        public float remainsLifetime = 45f;
        public GameObject hitVfxPrefab;
        public GameObject deathVfxPrefab;
        public GameObject phaseVfxPrefab;
        public GameObject lightBreakVfxPrefab;

        [Header("Audio")]
        public ShadowRevenantAudioProfile audioProfile;

        void OnValidate()
        {
            WarnIf(maxHealth <= 0, "maxHealth must be > 0.");
            WarnIf(aggroRange <= 0f, "aggroRange must be > 0.");
            WarnIf(leashRange < aggroRange, "leashRange should be >= aggroRange.");
            WarnIf(teleportMaxRadius < teleportMinRadius, "teleportMaxRadius must be >= teleportMinRadius.");
            WarnIf(teleportValidationAttempts <= 0, "teleportValidationAttempts must be > 0.");
            WarnIf(projectileSpeed <= 0f, "projectileSpeed must be > 0.");
            WarnIf(projectileLifetime <= 0f, "projectileLifetime must be > 0.");
            WarnIf(fogTickInterval <= 0f, "fogTickInterval must be > 0.");
            WarnIf(maxActiveMinions < 0, "maxActiveMinions must be >= 0.");
            WarnIf(summonCount < 0, "summonCount must be >= 0.");
            WarnIf(shadeMaxHealth <= 0, "shadeMaxHealth must be > 0.");
            WarnIf(shadeOrbitRadius <= 0f, "shadeOrbitRadius must be > 0.");
            WarnIf(shadeRetreatRange <= 0f, "shadeRetreatRange must be > 0.");
            WarnIf(shadeRetreatDuration <= 0f, "shadeRetreatDuration must be > 0.");
            WarnIf(shadeApproachDuration <= 0f, "shadeApproachDuration must be > 0.");
            WarnIf(shadeApproachInterval <= 0f, "shadeApproachInterval must be > 0.");
            WarnIf(shadeApproachIntervalVariance < 0f, "shadeApproachIntervalVariance must be >= 0.");
            WarnIf(shadeMaxApproachDuration <= 0f, "shadeMaxApproachDuration must be > 0.");
            WarnIf(shadeOrbitSfxInterval <= 0f, "shadeOrbitSfxInterval must be > 0.");
            WarnIf(bossStrafePulseInterval <= 0f, "bossStrafePulseInterval must be > 0.");
            WarnIf(shadeHoverHeight < 0f, "shadeHoverHeight must be >= 0.");
            WarnIf(hoverHeight < 0f, "hoverHeight must be >= 0.");
            WarnIf(groundCheckDistance <= 0f, "groundCheckDistance must be > 0.");
            WarnIf(groundCheckStartHeight <= 0f, "groundCheckStartHeight must be > 0.");
            WarnIf(closeRange <= 0f, "closeRange must be > 0.");
            WarnIf(mediumRange < closeRange, "mediumRange should be >= closeRange.");
            WarnIf(farRange < mediumRange, "farRange should be >= mediumRange.");
            WarnIf(fogVisualScaleMultiplier <= 0f, "fogVisualScaleMultiplier must be > 0.");
            WarnIf(fogFadeOutTime < 0f, "fogFadeOutTime must be >= 0.");
            WarnIf(remainsLifetime <= 0f, "remainsLifetime must be > 0.");
            WarnIf(chargeDuration <= 0f, "chargeDuration must be > 0.");
            WarnIf(chargeSpeed <= 0f, "chargeSpeed must be > 0.");
            WarnIf(chargeMaxRange < chargeMinRange, "chargeMaxRange should be >= chargeMinRange.");
            WarnIf(maxComboFollowUps < 0, "maxComboFollowUps must be >= 0.");
            WarnIf(comboCooldown < 0f, "comboCooldown must be >= 0.");
        }

        void WarnIf(bool condition, string message)
        {
            if (condition)
                Debug.LogWarning($"{name}: {message}", this);
        }
    }
}
