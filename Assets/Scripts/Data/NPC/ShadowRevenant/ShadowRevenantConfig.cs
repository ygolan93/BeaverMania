using Beavermania.NPC;
using UnityEngine;

namespace Beavermania.Data.NPC
{
    [CreateAssetMenu(fileName = "ShadowRevenantConfig", menuName = "Beavermania/NPC/Shadow Revenant Config")]
    public sealed class ShadowRevenantConfig : ScriptableObject
    {
        [Header("Health")]
        public int maxHealth = 3200;
        [Range(0f, 2f)] public float normalDamageMultiplier = 1f;
        [Range(0f, 1f)] public float phasedDamageMultiplier = 0f;
        [Range(1f, 4f)] public float lightBrokenDamageMultiplier = 1.5f;

        [Header("Detection")]
        public float aggroRange = 42f;
        public float leashRange = 70f;
        public float faceTurnSpeed = 8f;
        public float strafeSpeed = 4f;

        [Header("Phase Shift")]
        public float phaseCooldown = 8f;
        public float phaseDuration = 2.4f;
        public float phaseWindup = 0.35f;
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
        public int projectileDamage = 24;
        public float projectileSpeed = 24f;
        public float projectileLifetime = 5f;
        public float projectileCooldown = 2.2f;
        public float projectileWindup = 0.45f;
        public float projectileRecover = 0.45f;
        public float projectileRange = 36f;

        [Header("Dread Fog")]
        public ShadowRevenantDreadFogZone fogPrefab;
        public float fogRadius = 5f;
        public float fogDuration = 5f;
        public float fogDamagePerTick = 10f;
        public float fogTickInterval = 0.75f;
        [Range(0f, 1f)] public float fogSlowPercent = 0.3f;
        public float fogCooldown = 7f;
        public float fogWindup = 0.55f;
        public float fogRecover = 0.6f;
        public float fogRange = 24f;

        [Header("Shade Summon")]
        public ShadowRevenantShadeMinion shadeMinionPrefab;
        public int maxActiveMinions = 3;
        public int summonCount = 2;
        public float summonCooldown = 12f;
        public float summonWindup = 0.8f;
        public float summonRecover = 0.7f;
        public float shadeMoveSpeed = 6f;
        public float shadeDamage = 12f;
        public float shadeDamageCooldown = 1.2f;
        public float shadeLifetime = 18f;

        [Header("Light Break")]
        public float lightBreakVulnerableDuration = 4f;
        public float lightBreakStaggerSeconds = 0.45f;

        [Header("Pools")]
        public int projectilePrewarmCount = 8;
        public int projectileMaxActive = 24;
        public int fogPrewarmCount = 4;
        public int fogMaxActive = 8;
        public int shadePrewarmCount = 3;
        public int shadeMaxActive = 6;

        [Header("Drops and VFX")]
        public GameObject[] deathDropPrefabs;
        public GameObject hitVfxPrefab;
        public GameObject deathVfxPrefab;
        public GameObject phaseVfxPrefab;
        public GameObject lightBreakVfxPrefab;

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
        }

        void WarnIf(bool condition, string message)
        {
            if (condition)
                Debug.LogWarning($"{name}: {message}", this);
        }
    }
}
