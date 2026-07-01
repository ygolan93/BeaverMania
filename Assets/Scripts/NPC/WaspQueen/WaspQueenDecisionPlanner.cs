using Beavermania.Data.NPC;
using UnityEngine;

namespace Beavermania.NPC
{
    public readonly struct WaspQueenDecisionContext
    {
        public WaspQueenDecisionContext(
            float distanceToPlayer,
            int activeSummonedWasps,
            float rangedCooldownRemaining,
            float aoeCooldownRemaining,
            float chargeCooldownRemaining,
            float summonCooldownRemaining,
            float stingCooldownRemaining,
            WaspQueenAbility previousAbility,
            int repeatedAbilityCount,
            bool forcePhaseTransition = false)
        {
            DistanceToPlayer = distanceToPlayer;
            ActiveSummonedWasps = activeSummonedWasps;
            RangedCooldownRemaining = rangedCooldownRemaining;
            AoeCooldownRemaining = aoeCooldownRemaining;
            ChargeCooldownRemaining = chargeCooldownRemaining;
            SummonCooldownRemaining = summonCooldownRemaining;
            StingCooldownRemaining = stingCooldownRemaining;
            PreviousAbility = previousAbility;
            RepeatedAbilityCount = repeatedAbilityCount;
            ForcePhaseTransition = forcePhaseTransition;
        }

        public float DistanceToPlayer { get; }
        public int ActiveSummonedWasps { get; }
        public float RangedCooldownRemaining { get; }
        public float AoeCooldownRemaining { get; }
        public float ChargeCooldownRemaining { get; }
        public float SummonCooldownRemaining { get; }
        public float StingCooldownRemaining { get; }
        public WaspQueenAbility PreviousAbility { get; }
        public int RepeatedAbilityCount { get; }
        public bool ForcePhaseTransition { get; }
    }

    public static class WaspQueenDecisionPlanner
    {
        public static WaspQueenAbility ChooseAbility(
            WaspQueenConfig config,
            WaspQueenConfig.PhaseSettings phase,
            WaspQueenDecisionContext context)
        {
            if (config == null || phase == null)
                return WaspQueenAbility.Idle;

            if (context.ForcePhaseTransition)
                return WaspQueenAbility.PhaseTransition;

            float bestScore = float.NegativeInfinity;
            WaspQueenAbility bestAbility = WaspQueenAbility.Idle;

            TryPromoteAbility(config, phase, context, WaspQueenAbility.SummonWasps, ScoreSummon(config, phase, context), ref bestAbility, ref bestScore);
            TryPromoteAbility(config, phase, context, WaspQueenAbility.RangedPoisonShot, ScoreRanged(config, phase, context), ref bestAbility, ref bestScore);
            TryPromoteAbility(config, phase, context, WaspQueenAbility.PoisonAoE, ScorePoisonAoe(config, phase, context), ref bestAbility, ref bestScore);
            TryPromoteAbility(config, phase, context, WaspQueenAbility.Charge, ScoreCharge(config, phase, context), ref bestAbility, ref bestScore);
            TryPromoteAbility(config, phase, context, WaspQueenAbility.StingLunge, ScoreStingLunge(config, phase, context), ref bestAbility, ref bestScore);

            return bestAbility;
        }

        static void TryPromoteAbility(
            WaspQueenConfig config,
            WaspQueenConfig.PhaseSettings phase,
            WaspQueenDecisionContext context,
            WaspQueenAbility ability,
            float score,
            ref WaspQueenAbility bestAbility,
            ref float bestScore)
        {
            if (float.IsNegativeInfinity(score))
                return;

            if (context.PreviousAbility == ability)
                score -= phase.sameAbilityPenalty * Mathf.Max(1, context.RepeatedAbilityCount + 1);

            if (score <= bestScore)
                return;

            bestAbility = ability;
            bestScore = score;
        }

        static float ScoreSummon(
            WaspQueenConfig config,
            WaspQueenConfig.PhaseSettings phase,
            WaspQueenDecisionContext context)
        {
            if (phase.waspsPerSummon <= 0 || phase.maxActiveSummonedWasps <= 0)
                return float.NegativeInfinity;

            if (context.SummonCooldownRemaining > 0f)
                return float.NegativeInfinity;

            if (context.ActiveSummonedWasps >= phase.maxActiveSummonedWasps)
                return float.NegativeInfinity;

            float summonUrgency = 1f - Mathf.Clamp01(
                context.ActiveSummonedWasps / Mathf.Max(1f, phase.maxActiveSummonedWasps));
            float distancePreference = Mathf.InverseLerp(
                config.closeRange,
                Mathf.Max(config.mediumRange, config.closeRange + 0.01f),
                context.DistanceToPlayer);

            return phase.summonWeight + summonUrgency + distancePreference * 0.5f;
        }

        static float ScoreRanged(
            WaspQueenConfig config,
            WaspQueenConfig.PhaseSettings phase,
            WaspQueenDecisionContext context)
        {
            if (context.RangedCooldownRemaining > 0f)
                return float.NegativeInfinity;

            float distance = context.DistanceToPlayer;
            if (distance < config.closeRange * 0.75f || distance > config.farRange)
                return float.NegativeInfinity;

            float normalizedDistance = Mathf.InverseLerp(config.closeRange, config.farRange, distance);
            return phase.rangedWeight + normalizedDistance * 2f;
        }

        static float ScorePoisonAoe(
            WaspQueenConfig config,
            WaspQueenConfig.PhaseSettings phase,
            WaspQueenDecisionContext context)
        {
            if (context.AoeCooldownRemaining > 0f)
                return float.NegativeInfinity;

            float distance = context.DistanceToPlayer;
            if (distance > config.mediumRange)
                return float.NegativeInfinity;

            float proximity = 1f - Mathf.InverseLerp(0f, Mathf.Max(config.mediumRange, 0.01f), distance);
            return phase.aoeWeight + proximity * 2f;
        }

        static float ScoreCharge(
            WaspQueenConfig config,
            WaspQueenConfig.PhaseSettings phase,
            WaspQueenDecisionContext context)
        {
            if (context.ChargeCooldownRemaining > 0f)
                return float.NegativeInfinity;

            float distance = context.DistanceToPlayer;
            if (distance < config.chargeMinRange || distance > config.chargeMaxRange)
                return float.NegativeInfinity;

            float centered = 1f - Mathf.Abs(distance - ((config.chargeMinRange + config.chargeMaxRange) * 0.5f))
                / Mathf.Max(0.01f, (config.chargeMaxRange - config.chargeMinRange) * 0.5f);
            return phase.chargeWeight + Mathf.Clamp01(centered) * 1.5f;
        }

        static float ScoreStingLunge(
            WaspQueenConfig config,
            WaspQueenConfig.PhaseSettings phase,
            WaspQueenDecisionContext context)
        {
            if (phase.stingWeight <= 0f || context.StingCooldownRemaining > 0f)
                return float.NegativeInfinity;

            float distance = context.DistanceToPlayer;
            if (distance < config.stingMinRange || distance > config.stingMaxRange)
                return float.NegativeInfinity;

            // Prefer farther distances so the queen flies forward across a gap to sting.
            float reach = Mathf.InverseLerp(config.stingMinRange, config.stingMaxRange, distance);
            return phase.stingWeight + reach * 1.5f;
        }
    }
}
