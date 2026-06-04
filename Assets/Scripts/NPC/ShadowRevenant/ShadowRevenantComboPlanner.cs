using Beavermania.Data.NPC;
using UnityEngine;

namespace Beavermania.NPC
{
    public static class ShadowRevenantComboPlanner
    {
        public static bool TryResolveFollowUp(
            ShadowRevenantConfig config,
            ShadowRevenantAbilityKind completedAction,
            float distanceToTarget,
            bool targetInsideFog,
            float comboCooldownRemaining,
            int followUpsThisChain,
            float projectileCooldownRemaining,
            float chargeCooldownRemaining,
            out ShadowRevenantAbilityKind followUpAction)
        {
            followUpAction = ShadowRevenantAbilityKind.None;

            if (config == null || !config.enableCombos)
                return false;

            if (comboCooldownRemaining > 0f || followUpsThisChain >= config.maxComboFollowUps)
                return false;

            if (Random.value > config.comboChance)
                return false;

            switch (completedAction)
            {
                case ShadowRevenantAbilityKind.Summon:
                    if (config.allowProjectileAfterSummon && CanProjectile(config, distanceToTarget, projectileCooldownRemaining))
                        followUpAction = ShadowRevenantAbilityKind.Projectile;
                    break;

                case ShadowRevenantAbilityKind.Phase:
                    if (config.allowProjectileAfterPhase && CanProjectile(config, distanceToTarget, projectileCooldownRemaining))
                        followUpAction = ShadowRevenantAbilityKind.Projectile;
                    break;

                case ShadowRevenantAbilityKind.Projectile:
                    if (config.allowChargeAfterProjectile && CanCharge(config, distanceToTarget, chargeCooldownRemaining))
                        followUpAction = ShadowRevenantAbilityKind.Charge;
                    break;

                case ShadowRevenantAbilityKind.Fog:
                    if (config.disallowChargeAfterFog)
                    {
                        if (config.allowProjectileAfterFog
                            && distanceToTarget > config.closeRange
                            && CanProjectile(config, distanceToTarget, projectileCooldownRemaining))
                        {
                            followUpAction = ShadowRevenantAbilityKind.Projectile;
                        }
                    }
                    break;
            }

            if (followUpAction == ShadowRevenantAbilityKind.None)
                return false;

            if (completedAction == ShadowRevenantAbilityKind.Fog && followUpAction == ShadowRevenantAbilityKind.Charge)
                return false;

            if (completedAction == ShadowRevenantAbilityKind.Charge && followUpAction == ShadowRevenantAbilityKind.Fog)
                return false;

            if (targetInsideFog && followUpAction == ShadowRevenantAbilityKind.Charge && config.disallowChargeAfterFog)
                return false;

            return true;
        }

        static bool CanProjectile(ShadowRevenantConfig config, float distance, float cooldownRemaining)
        {
            return cooldownRemaining <= 0f
                && distance <= config.projectileRange
                && distance >= config.closeRange * 0.75f;
        }

        static bool CanCharge(ShadowRevenantConfig config, float distance, float cooldownRemaining)
        {
            if (!config.enableChargeAttack || cooldownRemaining > 0f)
                return false;

            return distance >= config.chargeMinRange && distance <= config.chargeMaxRange;
        }
    }
}
