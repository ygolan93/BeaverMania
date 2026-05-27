using UnityEngine;

namespace Beavermania.Player.AI
{
    public enum AutoCombatActionKind
    {
        None,
        Defend,
        Melee,
        Bow,
        FireBreath,
        RollEvade
    }

    public struct AutoCombatPlan
    {
        public AutoCombatActionKind Action;
        public string PreferredArsenalEntry;
        public bool HoldPrimary;
        public bool HoldSecondary;
        public bool HoldDefend;
        public bool SprintToTarget;
    }

    /// <summary>
    /// Picks the most effective attack option for the current threat and player loadout.
    /// </summary>
    public class AutoPlayerCombatSelector : MonoBehaviour
    {
        [SerializeField] AutoPlayerActionAdapter adapter;
        [SerializeField] AutoPlayerPlaystyleApplier playstyleApplier;
        [SerializeField] float bowPreferredMinDistance = 5f;
        [SerializeField] float bowMaxDistance = 22f;
        [SerializeField] float hammerPreferredMaxDistance = 4f;
        [SerializeField] float defendHealthRatio = 0.3f;
        [SerializeField] float fireBreathMinDistance = 3f;
        [SerializeField] float fireBreathMaxDistance = 14f;

        public AutoCombatPlan LastPlan { get; private set; }

        void Awake()
        {
            if (adapter == null)
                adapter = GetComponent<AutoPlayerActionAdapter>();
            if (playstyleApplier == null)
                playstyleApplier = GetComponent<AutoPlayerPlaystyleApplier>();
        }

        public AutoCombatPlan BuildPlan(Transform threat, float distance, string threatTag)
        {
            AutoCombatPlan rulePlan = BuildRuleBasedPlan(threat, distance, threatTag);

            if (playstyleApplier != null
                && playstyleApplier.TryBuildLearnedCombatPlan(threatTag, distance, out AutoCombatPlan learnedPlan, out float confidence))
            {
                LastPlan = playstyleApplier.BlendPlans(rulePlan, learnedPlan, confidence);
                return LastPlan;
            }

            LastPlan = rulePlan;
            return rulePlan;
        }

        AutoCombatPlan BuildRuleBasedPlan(Transform threat, float distance, string threatTag)
        {
            AutoCombatPlan plan = new AutoCombatPlan();
            BeaverPlayerBehaviour player = adapter != null ? adapter.Player : null;
            if (player == null || threat == null)
            {
                return plan;
            }

            bool lowHp = adapter.HealthRatio() < defendHealthRatio;
            bool hasHammers = player.OwnsArsenalItem("Hammers");
            bool hasBow = player.OwnsArsenalItem("Bow");
            bool hasArmor = player.OwnsArsenalItem("ArmorSet");
            bool canBow = hasBow && player.CanFireArrow();
            bool isHive = threatTag == "Hive";
            bool isScorpion = threatTag == "Scorpion";
            bool isWasp = threatTag == "NPC";

            if (lowHp && hasArmor && distance < 3.5f)
            {
                plan.Action = AutoCombatActionKind.Defend;
                plan.PreferredArsenalEntry = "ArmorSet";
                plan.HoldDefend = true;
                return plan;
            }

            if (canBow && distance >= bowPreferredMinDistance && distance <= bowMaxDistance && (isWasp || isHive))
            {
                plan.Action = AutoCombatActionKind.Bow;
                plan.PreferredArsenalEntry = "Bow";
                plan.HoldSecondary = true;
                plan.SprintToTarget = distance > bowPreferredMinDistance + 2f;
                return plan;
            }

            if (hasArmor && player.CanActivateFireBreath()
                && distance >= fireBreathMinDistance && distance <= fireBreathMaxDistance
                && (isHive || isScorpion))
            {
                plan.Action = AutoCombatActionKind.FireBreath;
                plan.PreferredArsenalEntry = "ArmorSet";
                plan.HoldPrimary = true;
                return plan;
            }

            if (hasHammers && distance <= hammerPreferredMaxDistance + (isScorpion ? 2f : 0f))
            {
                plan.Action = AutoCombatActionKind.Melee;
                plan.PreferredArsenalEntry = "Hammers";
                plan.HoldPrimary = true;
                plan.SprintToTarget = distance > hammerPreferredMaxDistance;
                return plan;
            }

            if (hasArmor && distance <= 3.5f)
            {
                plan.Action = AutoCombatActionKind.Melee;
                plan.PreferredArsenalEntry = "ArmorSet";
                plan.HoldPrimary = true;
                return plan;
            }

            if (canBow && distance > hammerPreferredMaxDistance)
            {
                plan.Action = AutoCombatActionKind.Bow;
                plan.PreferredArsenalEntry = "Bow";
                plan.HoldSecondary = true;
                plan.SprintToTarget = true;
                return plan;
            }

            if (hasHammers)
            {
                plan.Action = AutoCombatActionKind.Melee;
                plan.PreferredArsenalEntry = "Hammers";
                plan.HoldPrimary = true;
                plan.SprintToTarget = true;
            }
            else if (hasArmor)
            {
                plan.Action = AutoCombatActionKind.Melee;
                plan.PreferredArsenalEntry = "ArmorSet";
                plan.HoldPrimary = true;
                plan.SprintToTarget = true;
            }
            else
            {
                plan.Action = AutoCombatActionKind.Melee;
                plan.PreferredArsenalEntry = "Bare Hands";
                plan.HoldPrimary = true;
                plan.SprintToTarget = distance > 2.5f;
            }

            return plan;
        }

        public void ApplyPlan(AutoPlayerActionAdapter actionAdapter, AutoCombatPlan plan)
        {
            if (actionAdapter == null || actionAdapter.Player == null)
                return;

            if (!string.IsNullOrEmpty(plan.PreferredArsenalEntry))
                actionAdapter.TryEquipArsenal(plan.PreferredArsenalEntry);

            actionAdapter.SetSprint(plan.SprintToTarget);

            if (plan.HoldDefend)
                actionAdapter.RequestDefend();
            if (plan.HoldPrimary)
                actionAdapter.RequestPrimaryAttack();
            if (plan.HoldSecondary)
                actionAdapter.RequestBowAimFire();
        }
    }
}
