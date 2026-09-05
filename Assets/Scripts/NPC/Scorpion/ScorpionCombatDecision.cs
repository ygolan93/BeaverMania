using Beavermania.Data.NPC;
using UnityEngine;

namespace Beavermania.NPC
{
    public enum ScorpionMacroAction
    {
        Attack = 0,
        Charge = 1,
        Reverse = 2,
        Hold = 3,
    }

    public enum ScorpionHealthProfile
    {
        Controlled = 0,
        Aggressive = 1,
        Frenzy = 2,
    }

    public enum ScorpionChargeVariant
    {
        Short = 0,
        Normal = 1,
        Committed = 2,
    }

    public readonly struct ScorpionDecisionContext
    {
        public ScorpionDecisionContext(
            float distance,
            float attackDistance,
            float chargeDistance,
            float lookDistance,
            ScorpionMacroAction previousAction,
            int consecutiveSelections)
        {
            Distance = distance;
            AttackDistance = Mathf.Max(0f, attackDistance);
            ChargeDistance = Mathf.Max(AttackDistance, chargeDistance);
            LookDistance = Mathf.Max(ChargeDistance, lookDistance);
            PreviousAction = previousAction;
            ConsecutiveSelections = Mathf.Max(0, consecutiveSelections);
        }

        public float Distance { get; }
        public float AttackDistance { get; }
        public float ChargeDistance { get; }
        public float LookDistance { get; }
        public ScorpionMacroAction PreviousAction { get; }
        public int ConsecutiveSelections { get; }
    }

    public readonly struct ScorpionDecisionWeights
    {
        public ScorpionDecisionWeights(float attack, float charge, float reverse, float hold)
        {
            Attack = Mathf.Max(0f, attack);
            Charge = Mathf.Max(0f, charge);
            Reverse = Mathf.Max(0f, reverse);
            Hold = Mathf.Max(0f, hold);
        }

        public float Attack { get; }
        public float Charge { get; }
        public float Reverse { get; }
        public float Hold { get; }
    }

    public readonly struct ScorpionChargeVariantWeights
    {
        public ScorpionChargeVariantWeights(float shortCharge, float normalCharge, float committedCharge)
        {
            Short = Mathf.Max(0f, shortCharge);
            Normal = Mathf.Max(0f, normalCharge);
            Committed = Mathf.Max(0f, committedCharge);
        }

        public float Short { get; }
        public float Normal { get; }
        public float Committed { get; }
    }

    public readonly struct ScorpionChargeLimits
    {
        public ScorpionChargeLimits(float maximumDuration, float maximumDistance, float trackingDuration)
        {
            MaximumDuration = Mathf.Max(0f, maximumDuration);
            MaximumDistance = Mathf.Max(0f, maximumDistance);
            TrackingDuration = Mathf.Clamp(trackingDuration, 0f, MaximumDuration);
        }

        public float MaximumDuration { get; }
        public float MaximumDistance { get; }
        public float TrackingDuration { get; }
    }

    public static class ScorpionCombatDecision
    {
        const int MaximumConsecutiveSelections = 2;
        const float DirectionEpsilon = 0.0001f;

        public static ScorpionHealthProfile SelectHealthProfile(float normalizedHealth)
        {
            float health = Mathf.Clamp01(normalizedHealth);
            if (health > 0.65f)
                return ScorpionHealthProfile.Controlled;
            if (health >= 0.3f)
                return ScorpionHealthProfile.Aggressive;

            return ScorpionHealthProfile.Frenzy;
        }

        public static T SelectProfileValue<T>(
            ScorpionHealthProfile profile,
            T controlled,
            T aggressive,
            T frenzy)
        {
            switch (profile)
            {
                case ScorpionHealthProfile.Controlled:
                    return controlled;
                case ScorpionHealthProfile.Aggressive:
                    return aggressive;
                case ScorpionHealthProfile.Frenzy:
                    return frenzy;
                default:
                    return controlled;
            }
        }

        public static ScorpionChargeVariant SelectChargeVariant(
            ScorpionChargeVariantWeights weights,
            float roll)
        {
            float totalWeight = weights.Short + weights.Normal + weights.Committed;
            if (totalWeight <= 0f)
                return ScorpionChargeVariant.Normal;

            float selection = Mathf.Clamp01(roll) * totalWeight;
            if (selection < weights.Short)
                return ScorpionChargeVariant.Short;

            selection -= weights.Short;
            if (selection < weights.Normal)
                return ScorpionChargeVariant.Normal;

            if (weights.Committed > 0f)
                return ScorpionChargeVariant.Committed;
            if (weights.Normal > 0f)
                return ScorpionChargeVariant.Normal;
            if (weights.Short > 0f)
                return ScorpionChargeVariant.Short;

            return ScorpionChargeVariant.Normal;
        }

        public static ScorpionChargeLimits ResolveChargeLimits(
            ScorpionChargeVariant variant,
            float normalMaximumDuration,
            float normalMaximumDistance,
            float normalTrackingDuration,
            float shortDurationMultiplier,
            float shortDistanceMultiplier,
            float committedDurationMultiplier,
            float committedDistanceMultiplier,
            float committedTrackingMultiplier)
        {
            switch (variant)
            {
                case ScorpionChargeVariant.Short:
                    return new ScorpionChargeLimits(
                        normalMaximumDuration * Mathf.Clamp01(shortDurationMultiplier),
                        normalMaximumDistance * Mathf.Clamp01(shortDistanceMultiplier),
                        normalTrackingDuration);
                case ScorpionChargeVariant.Committed:
                    return new ScorpionChargeLimits(
                        normalMaximumDuration * Mathf.Max(0f, committedDurationMultiplier),
                        normalMaximumDistance * Mathf.Max(0f, committedDistanceMultiplier),
                        normalTrackingDuration * Mathf.Clamp01(committedTrackingMultiplier));
                case ScorpionChargeVariant.Normal:
                default:
                    return new ScorpionChargeLimits(
                        normalMaximumDuration,
                        normalMaximumDistance,
                        normalTrackingDuration);
            }
        }

        public static ScorpionDecisionWeights ApplyPostStunPressure(
            ScorpionDecisionWeights weights,
            float chargeMultiplier,
            float holdMultiplier)
        {
            return new ScorpionDecisionWeights(
                weights.Attack,
                weights.Charge * Mathf.Max(1f, chargeMultiplier),
                weights.Reverse,
                weights.Hold * Mathf.Max(0f, holdMultiplier));
        }

        public static float ResolveRecovery(
            float profileRecovery,
            bool postStunPressureActive,
            float postStunMultiplier)
        {
            float recovery = Mathf.Max(ScorpionStatsData.MinimumActionRecovery, profileRecovery);
            float resolvedRecovery = postStunPressureActive
                ? recovery * Mathf.Clamp(postStunMultiplier, 0.1f, 1f)
                : recovery;
            return Mathf.Max(ScorpionStatsData.MinimumActionRecovery, resolvedRecovery);
        }

        public static ScorpionMacroAction SelectAction(
            ScorpionDecisionContext context,
            ScorpionDecisionWeights weights,
            float roll)
        {
            if (context.Distance > context.LookDistance)
                return ScorpionMacroAction.Hold;

            bool blockPrevious = context.ConsecutiveSelections >= MaximumConsecutiveSelections;
            float attackWeight = IsEligible(
                ScorpionMacroAction.Attack,
                context,
                context.Distance <= context.AttackDistance,
                blockPrevious) ? weights.Attack : 0f;
            float chargeWeight = IsEligible(
                ScorpionMacroAction.Charge,
                context,
                context.Distance <= context.LookDistance,
                blockPrevious) ? weights.Charge * ChargeDistancePreference(context) : 0f;
            float reverseWeight = IsEligible(
                ScorpionMacroAction.Reverse,
                context,
                context.Distance <= context.AttackDistance,
                blockPrevious) ? weights.Reverse : 0f;
            float holdWeight = IsEligible(
                ScorpionMacroAction.Hold,
                context,
                true,
                blockPrevious) ? weights.Hold : 0f;

            float totalWeight = attackWeight + chargeWeight + reverseWeight + holdWeight;
            if (totalWeight <= 0f)
                return SelectFallback(context, blockPrevious);

            float selection = Mathf.Clamp01(roll) * totalWeight;
            if (selection < attackWeight)
                return ScorpionMacroAction.Attack;

            selection -= attackWeight;
            if (selection < chargeWeight)
                return ScorpionMacroAction.Charge;

            selection -= chargeWeight;
            if (selection < reverseWeight)
                return ScorpionMacroAction.Reverse;

            if (holdWeight > 0f)
                return ScorpionMacroAction.Hold;
            if (reverseWeight > 0f)
                return ScorpionMacroAction.Reverse;
            if (chargeWeight > 0f)
                return ScorpionMacroAction.Charge;

            return ScorpionMacroAction.Attack;
        }

        public static Vector3 LockHorizontalDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > DirectionEpsilon ? direction.normalized : Vector3.zero;
        }

        public static float AccumulateHorizontalDistance(
            float accumulatedDistance,
            Vector3 previousPosition,
            Vector3 currentPosition)
        {
            Vector3 travelled = currentPosition - previousPosition;
            travelled.y = 0f;
            return Mathf.Max(0f, accumulatedDistance) + travelled.magnitude;
        }

        static bool IsEligible(
            ScorpionMacroAction action,
            ScorpionDecisionContext context,
            bool distanceEligible,
            bool blockPrevious)
        {
            return distanceEligible && (!blockPrevious || context.PreviousAction != action);
        }

        static ScorpionMacroAction SelectFallback(ScorpionDecisionContext context, bool blockPrevious)
        {
            if (IsEligible(
                ScorpionMacroAction.Attack,
                context,
                context.Distance <= context.AttackDistance,
                blockPrevious))
                return ScorpionMacroAction.Attack;

            if (IsEligible(
                ScorpionMacroAction.Charge,
                context,
                context.Distance <= context.LookDistance,
                blockPrevious))
                return ScorpionMacroAction.Charge;

            if (IsEligible(
                ScorpionMacroAction.Reverse,
                context,
                context.Distance <= context.AttackDistance,
                blockPrevious))
                return ScorpionMacroAction.Reverse;

            return ScorpionMacroAction.Hold;
        }

        static float ChargeDistancePreference(ScorpionDecisionContext context)
        {
            if (context.Distance <= context.ChargeDistance)
                return 1f;

            float extendedRange = context.LookDistance - context.ChargeDistance;
            if (extendedRange <= 0f)
                return 1f;

            float extendedRangeProgress = (context.Distance - context.ChargeDistance) / extendedRange;
            return Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(extendedRangeProgress));
        }
    }
}
