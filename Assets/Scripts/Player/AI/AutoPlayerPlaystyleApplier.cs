using Beavermania.Data.Player;
using UnityEngine;

namespace Beavermania.Player.AI
{
    public enum CombatPlanSource
    {
        RulesOnly,
        Learned,
        Blended
    }

    public struct PlaystyleRuntimeTuning
    {
        public float EngageRadius;
        public float SprintDistance;
        public float StuckTimeThreshold;
        public float ObstacleAvoidDistance;
        public float JumpRate;
        public float BridgeUrgency;
        public float BlendFactor;
        public bool HasProfile;
    }

    /// <summary>
    /// Applies a baked playstyle profile to AutoPlayer modules at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoPlayerPlaystyleApplier : MonoBehaviour
    {
        [SerializeField] AutoPlayerPlaystyleProfile profile;
        [Range(0f, 1f)] [SerializeField] float blendFactor = 0.75f;
        [SerializeField] int minSamplesPerBucket = 8;

        [Header("Clamp learned values")]
        [SerializeField] float minEngageRadius = 5f;
        [SerializeField] float maxEngageRadius = 18f;
        [SerializeField] float minSprintDistance = 3f;
        [SerializeField] float maxSprintDistance = 12f;

        PlaystyleRuntimeTuning _tuning;
        CombatPlanSource _lastCombatSource = CombatPlanSource.RulesOnly;
        float _jumpRoll;

        public AutoPlayerPlaystyleProfile Profile => profile;
        public PlaystyleRuntimeTuning Tuning => _tuning;
        public CombatPlanSource LastCombatSource => _lastCombatSource;

        public void SetProfile(AutoPlayerPlaystyleProfile newProfile) => profile = newProfile;

        public void RefreshTuning()
        {
            _tuning = new PlaystyleRuntimeTuning
            {
                EngageRadius = 10f,
                SprintDistance = 6f,
                StuckTimeThreshold = 2.5f,
                ObstacleAvoidDistance = 1.2f,
                JumpRate = 0.15f,
                BridgeUrgency = 0.85f,
                BlendFactor = blendFactor,
                HasProfile = profile != null && profile.totalSamples > 0
            };

            if (!_tuning.HasProfile || blendFactor <= 0f)
                return;

            float t = blendFactor;
            _tuning.EngageRadius = Mathf.Lerp(_tuning.EngageRadius, profile.learnedEngageRadius, t);
            _tuning.SprintDistance = Mathf.Lerp(_tuning.SprintDistance, profile.learnedSprintDistance, t);
            _tuning.StuckTimeThreshold = Mathf.Lerp(_tuning.StuckTimeThreshold, profile.learnedStuckTimeThreshold, t);
            _tuning.ObstacleAvoidDistance = Mathf.Lerp(_tuning.ObstacleAvoidDistance, profile.learnedObstacleAvoidDistance, t);
            _tuning.JumpRate = Mathf.Lerp(_tuning.JumpRate, profile.learnedJumpRate, t);
            _tuning.BridgeUrgency = Mathf.Lerp(_tuning.BridgeUrgency, profile.learnedBridgeUrgency, t);

            _tuning.EngageRadius = Mathf.Clamp(_tuning.EngageRadius, minEngageRadius, maxEngageRadius);
            _tuning.SprintDistance = Mathf.Clamp(_tuning.SprintDistance, minSprintDistance, maxSprintDistance);
        }

        void Awake() => RefreshTuning();

        void OnValidate() => RefreshTuning();

        public bool TryBuildLearnedCombatPlan(string threatTag, float distance, out AutoCombatPlan plan, out float confidence)
        {
            plan = new AutoCombatPlan();
            confidence = 0f;
            _lastCombatSource = CombatPlanSource.RulesOnly;

            if (profile == null || blendFactor <= 0f)
                return false;

            if (!profile.TryGetLearnedCombat(threatTag, distance, minSamplesPerBucket, out AutoCombatActionKind action, out string arsenal, out confidence))
                return false;

            plan = PlanFromLearned(action, arsenal, distance);
            _lastCombatSource = confidence >= 0.55f ? CombatPlanSource.Learned : CombatPlanSource.Blended;
            return true;
        }

        static AutoCombatPlan PlanFromLearned(AutoCombatActionKind action, string arsenal, float distance)
        {
            AutoCombatPlan plan = new AutoCombatPlan
            {
                PreferredArsenalEntry = arsenal,
                Action = action
            };

            switch (action)
            {
                case AutoCombatActionKind.Defend:
                    plan.HoldDefend = true;
                    break;
                case AutoCombatActionKind.Bow:
                    plan.HoldSecondary = true;
                    plan.SprintToTarget = distance > 7f;
                    break;
                case AutoCombatActionKind.FireBreath:
                    plan.HoldPrimary = true;
                    break;
                case AutoCombatActionKind.RollEvade:
                    plan.SprintToTarget = true;
                    break;
                case AutoCombatActionKind.Melee:
                    plan.HoldPrimary = true;
                    plan.SprintToTarget = distance > 3f;
                    break;
            }

            return plan;
        }

        public AutoCombatPlan BlendPlans(AutoCombatPlan rulePlan, AutoCombatPlan learnedPlan, float learnedConfidence)
        {
            if (profile == null || blendFactor <= 0f || learnedConfidence <= 0f)
            {
                _lastCombatSource = CombatPlanSource.RulesOnly;
                return rulePlan;
            }

            float learnedWeight = blendFactor * learnedConfidence;
            if (learnedWeight >= 0.65f)
            {
                _lastCombatSource = CombatPlanSource.Learned;
                return learnedPlan;
            }

            _lastCombatSource = CombatPlanSource.Blended;
            if (learnedPlan.Action == rulePlan.Action)
                return rulePlan;

            return learnedWeight > 0.4f ? learnedPlan : rulePlan;
        }

        public bool ShouldJumpThisStep()
        {
            if (!_tuning.HasProfile || _tuning.JumpRate <= 0f)
                return false;

            _jumpRoll = Random.value;
            return _jumpRoll <= _tuning.JumpRate * _tuning.BlendFactor;
        }
    }
}
