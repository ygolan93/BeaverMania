using System;
using Beavermania.Data.NPC;
using Beavermania.Objects;
using Beavermania.Player.Combat;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beavermania.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class ScorpionScript : MonoBehaviour, IEnemyDamageReceiver, IPlayerAttackReceiver, IBossVictorySource
    {
        const float LookRotationEpsilon = 0.0001f;
        const float ChargeObstructionDotThreshold = -0.1f;
        const float FrontalTreeImpactDotThreshold = 0.5f;
        const float ReverseSpeed = 5f;
        const float HitEffectDuration = 0.1f;
        const int BridgeComboOverride = 10;
        const int DefaultAttackDamage = 15;
        const int DefaultStingDamage = 30;
        const int NormalHitComboReward = 1;
        const int CounterHitComboReward = 2;
        const string StunnedAnimatorParameter = "Stunned";
        static readonly int BackwardsAnimatorStateHash = Animator.StringToHash("Base Layer.Backwards");
        [Header("General")]
        Rigidbody rbScorpion;
        [SerializeField] Animator Scorpion;
        [SerializeField] ScorpionStatsData statsData;
        [SerializeField] string defaultStatsResourcePath;
        public NPC_Health BossHealth;
        public int CurrentHealth;
        public BeaverPlayer Player;
        [SerializeField] BoostChargeController boostCharge;
        public GameObject[] drops;

        [Header("Runtime Combat")]
        public int combo;
        public bool isAttacking;
        public Collider Jaw1A;
        public Collider Jaw1B;
        public Collider Jaw2A;
        public Collider Jaw2B;
        public Collider Sting;
        public string state = nameof(ScorpionState.Idle);

        [Header("Runtime Mobility")]
        public float currentDistance;
        public Quaternion rotGoal;

        [Header("Effects & Sound")]
        public GameObject HitEffect;
        public GameObject Explosion;
        public GameObject StunEffect;
        public NPC_Audio Sound;

        [Header("Legacy Stats Fallback")]
        [FormerlySerializedAs("MaxHealth")]
        [SerializeField] int legacyMaxHealth = 2000;
        [FormerlySerializedAs("comboLimit")]
        [SerializeField] int legacyComboLimit = 15;
        [FormerlySerializedAs("StunnedClock")]
        [SerializeField] float legacyStunDuration = 10f;
        [FormerlySerializedAs("chargeSpeed")]
        [SerializeField] float legacyChargeSpeed = 8f;
        [FormerlySerializedAs("chargeClock")]
        [SerializeField] float legacyChargeDuration = 1f;
        [FormerlySerializedAs("lookDistance")]
        [SerializeField] float legacyLookDistance = 30f;
        [FormerlySerializedAs("chargeDistance")]
        [SerializeField] float legacyChargeDistance = 20f;
        [FormerlySerializedAs("attackDistance")]
        [SerializeField] float legacyAttackDistance = 2.2f;
        [SerializeField] int legacyAttackDamage = DefaultAttackDamage;
        [SerializeField] int legacyStingDamage = DefaultStingDamage;
        [SerializeField] float legacyRotationSpeed = 0.05f;
        [SerializeField] float legacyRecoveryDuration;

        ScorpionState currentState = ScorpionState.Idle;
        ScorpionStatsData fallbackStats;
        Vector3 distanceToTarget;
        Transform targetTransform;
        float stateTimer;
        float minimumChargeTimeRemaining;
        float hitEffectTimer;
        float chargeElapsed;
        float chargeTravelledDistance;
        Vector3 previousChargePosition;
        Vector3 lockedChargeDirection;
        ScorpionChargeVariant activeChargeVariant = ScorpionChargeVariant.Normal;
        ScorpionChargeLimits activeChargeLimits;
        float postStunPressureTimer;
        ScorpionMacroAction previousMacroAction = ScorpionMacroAction.Hold;
        int consecutiveMacroSelections;
        float stunCooldownRemaining;
        Vector3 hurricaneRetreatDirection;
        bool hurricaneRetreatActive;
        bool startStunCooldownAfterRecovery;
        bool deathHandled;
        [SerializeField] float victoryDelay = 2f;

        public ScorpionState State => currentState;
        public int MaxHealth => ActiveStats.maxHealth;
        public float VictoryDelay => Mathf.Max(0f, victoryDelay);
        public int comboLimit => ActiveStats.comboLimit;
        public float chargeSpeed => ActiveStats.chargeSpeed;
        public float chargeDuration => ActiveStats.chargeDuration;
        public float lookDistance => ActiveStats.lookDistance;
        public float chargeDistance => ActiveStats.chargeDistance;
        public float attackDistance => ActiveStats.attackDistance;
        public int AttackDamageAmount => ActiveStats.attackDamage;
        public int StingDamageAmount => ActiveStats.stingDamage;
        public float rotationSpeed => ActiveStats.rotationSpeed;
        public event Action<ScorpionScript> Defeated;
        event Action<IBossVictorySource> IBossVictorySource.Defeated
        {
            add => genericDefeated += value;
            remove => genericDefeated -= value;
        }
        public bool UsesAdvancedAi => ActiveStats.advancedAiEnabled;
        bool IsAggressive => combo >= Mathf.Max(0, comboLimit - 5);
        bool HasPostStunPressure => postStunPressureTimer > 0f;
        ScorpionHealthProfile ActiveHealthProfile => ScorpionCombatDecision.SelectHealthProfile(
            MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f);
        float ReverseDistanceThreshold => Mathf.Max(0f, chargeDistance - 10f);
        ScorpionStatsData ActiveStats => statsData != null ? statsData : ResolveFallbackStats();
        Action<IBossVictorySource> genericDefeated;

        void Awake()
        {
            rbScorpion = GetComponent<Rigidbody>();
            if (Scorpion == null)
                Scorpion = GetComponentInChildren<Animator>();
            if (BossHealth == null)
                BossHealth = GetComponent<NPC_Health>();

            ResolvePlayerReferences();
            Physics.IgnoreLayerCollision(10, 10);

            CurrentHealth = MaxHealth;
            combo = 0;
            minimumChargeTimeRemaining = chargeDuration;
            rotGoal = transform.rotation;
            currentState = ScorpionState.Idle;
            state = currentState.ToString();

            if (BossHealth != null)
            {
                BossHealth.SetMaxNPCHealth(MaxHealth);
                BossHealth.SetNPCHealth(CurrentHealth);
            }

            var bossHealthVisibility = GetComponent<EnemyHealthBarVisibility>();
            if (bossHealthVisibility == null)
                bossHealthVisibility = gameObject.AddComponent<EnemyHealthBarVisibility>();
            bossHealthVisibility.EnableAlwaysShow();

            SetEffectActive(Explosion, false);
            SetEffectActive(HitEffect, false);
            SetEffectActive(StunEffect, false);
        }

        void Start()
        {
            ResolvePlayerReferences();
        }

        void Update()
        {
            if (hitEffectTimer <= 0f)
                return;

            hitEffectTimer -= Time.deltaTime;
            if (hitEffectTimer <= 0f)
                SetEffectActive(HitEffect, false);
        }

        public void FixedUpdate()
        {
            if (deathHandled)
                return;

            if (postStunPressureTimer > 0f)
                postStunPressureTimer = Mathf.Max(0f, postStunPressureTimer - Time.fixedDeltaTime);
            if (stunCooldownRemaining > 0f)
                stunCooldownRemaining = Mathf.Max(0f, stunCooldownRemaining - Time.fixedDeltaTime);

            RefreshTargetContext();

            if (CurrentHealth <= 0)
            {
                Death();
                return;
            }

            if (!UsesAdvancedAi
                && combo >= comboLimit
                && currentState != ScorpionState.Stunned
                && currentState != ScorpionState.Recovered)
                EnterState(ScorpionState.Stunned);

            switch (currentState)
            {
                case ScorpionState.Idle:
                    TickIdle();
                    break;
                case ScorpionState.Look:
                    TickLook();
                    break;
                case ScorpionState.ChargeWindup:
                    TickChargeWindup();
                    break;
                case ScorpionState.Charge:
                    TickCharge();
                    break;
                case ScorpionState.Attack:
                    TickAttack();
                    break;
                case ScorpionState.Reverse:
                    TickReverse();
                    break;
                case ScorpionState.Stunned:
                    TickStunned();
                    break;
                case ScorpionState.Recovered:
                    TickRecovered();
                    break;
                case ScorpionState.Dead:
                    ApplyDeadPresentation();
                    break;
            }
        }

        void TickIdle()
        {
            ApplyIdlePresentation();
            if (HasCombatTargetInRange())
                EnterState(ScorpionState.Look);
        }

        void TickLook()
        {
            bool hasTarget = HasCombatTargetInRange();
            ApplyLookPresentation();

            if (!hasTarget)
            {
                EnterState(ScorpionState.Idle);
                return;
            }

            if (UsesAdvancedAi)
            {
                TickAdvancedLook();
                return;
            }

            if (IsAggressive)
            {
                EnterState(ScorpionState.ChargeWindup);
                return;
            }

            if (ShouldReverse())
            {
                EnterState(ScorpionState.Reverse);
                return;
            }

            if (ShouldStartCharge())
                EnterState(ScorpionState.ChargeWindup);
        }

        void TickAdvancedLook()
        {
            if (stateTimer > 0f)
            {
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer > 0f)
                    return;
            }

            ScorpionStatsData stats = ActiveStats;
            ScorpionHealthProfile profile = ActiveHealthProfile;
            var context = new ScorpionDecisionContext(
                currentDistance,
                attackDistance,
                chargeDistance,
                lookDistance,
                previousMacroAction,
                consecutiveMacroSelections);
            ScorpionDecisionWeights weights = ResolveDecisionWeights(stats, profile);
            if (HasPostStunPressure)
            {
                weights = ScorpionCombatDecision.ApplyPostStunPressure(
                    weights,
                    stats.postStunChargeWeightMultiplier,
                    stats.postStunHoldWeightMultiplier);
            }

            ScorpionMacroAction selectedAction = ScorpionCombatDecision.SelectAction(context, weights, UnityEngine.Random.value);
            RecordMacroSelection(selectedAction);

            switch (selectedAction)
            {
                case ScorpionMacroAction.Attack:
                    EnterState(ScorpionState.Attack);
                    break;
                case ScorpionMacroAction.Charge:
                    EnterState(ScorpionState.ChargeWindup);
                    break;
                case ScorpionMacroAction.Reverse:
                    EnterState(ScorpionState.Reverse);
                    break;
                case ScorpionMacroAction.Hold:
                    ResolveDecisionHoldRange(stats, profile, out float holdMinimum, out float holdMaximum);
                    stateTimer = RandomRange(holdMinimum, holdMaximum);
                    break;
            }
        }

        void TickChargeWindup()
        {
            ApplyLookPresentation();
            if (!HasCombatTargetInRange())
            {
                EnterState(ScorpionState.Idle);
                return;
            }

            if (!UsesAdvancedAi && currentDistance > chargeDistance)
            {
                EnterState(ScorpionState.Look);
                return;
            }

            stateTimer -= Time.fixedDeltaTime;
            if (stateTimer <= 0f)
                EnterState(ScorpionState.Charge);
        }

        void TickCharge()
        {
            if (UsesAdvancedAi)
            {
                TickAdvancedCharge();
                return;
            }

            if (!HasCombatTargetInRange())
            {
                ResetChargeCycle();
                EnterState(ScorpionState.Idle);
                return;
            }

            MoveTowardsTarget(chargeSpeed);
            SetAnimatorState(walk: true, backwards: false, attack: false, stunned: false);
            isAttacking = true;

            minimumChargeTimeRemaining = Mathf.Max(0f, minimumChargeTimeRemaining - Time.fixedDeltaTime);
            if (minimumChargeTimeRemaining <= 0f && currentDistance <= attackDistance)
                EnterState(ScorpionState.Attack);
        }

        void TickAdvancedCharge()
        {
            if (!HasCombatTargetInRange())
            {
                EnterState(ScorpionState.Idle);
                return;
            }

            chargeElapsed += Time.fixedDeltaTime;
            chargeTravelledDistance = ScorpionCombatDecision.AccumulateHorizontalDistance(
                chargeTravelledDistance,
                previousChargePosition,
                rbScorpion.position);
            previousChargePosition = rbScorpion.position;
            Vector3 horizontalToTarget = HorizontalTargetDirection();

            if (chargeElapsed <= activeChargeLimits.TrackingDuration)
            {
                Vector3 trackingDirection = ScorpionCombatDecision.LockHorizontalDirection(horizontalToTarget);
                if (trackingDirection.sqrMagnitude > LookRotationEpsilon)
                    lockedChargeDirection = trackingDirection;

                RotateTowards(horizontalToTarget);
            }

            bool passedTarget = lockedChargeDirection.sqrMagnitude > LookRotationEpsilon
                && Vector3.Dot(lockedChargeDirection, horizontalToTarget) <= 0f;
            bool chargeExpired = chargeElapsed >= activeChargeLimits.MaximumDuration;
            bool travelledMaximumDistance = chargeTravelledDistance >= activeChargeLimits.MaximumDistance;
            bool contactOpportunity = currentDistance <= attackDistance;
            if (chargeExpired || travelledMaximumDistance || passedTarget || contactOpportunity)
            {
                EnterState(ScorpionState.Look);
                return;
            }

            SetHorizontalVelocity(lockedChargeDirection * chargeSpeed);
            SetAnimatorState(walk: true, backwards: false, attack: false, stunned: false);
            isAttacking = true;
        }

        void RecordMacroSelection(ScorpionMacroAction selectedAction)
        {
            if (selectedAction == previousMacroAction)
                consecutiveMacroSelections++;
            else
            {
                previousMacroAction = selectedAction;
                consecutiveMacroSelections = 1;
            }
        }

        static float RandomRange(float minimum, float maximum)
        {
            float clampedMinimum = Mathf.Max(0f, minimum);
            return UnityEngine.Random.Range(clampedMinimum, Mathf.Max(clampedMinimum, maximum));
        }

        static ScorpionDecisionWeights ResolveDecisionWeights(
            ScorpionStatsData stats,
            ScorpionHealthProfile profile)
        {
            var controlled = new ScorpionDecisionWeights(
                stats.phaseOneAttackWeight,
                stats.phaseOneChargeWeight,
                stats.phaseOneReverseWeight,
                stats.phaseOneHoldWeight);
            var aggressive = new ScorpionDecisionWeights(
                stats.phaseTwoAttackWeight,
                stats.phaseTwoChargeWeight,
                stats.phaseTwoReverseWeight,
                stats.phaseTwoHoldWeight);
            var frenzy = new ScorpionDecisionWeights(
                stats.phaseThreeAttackWeight,
                stats.phaseThreeChargeWeight,
                stats.phaseThreeReverseWeight,
                stats.phaseThreeHoldWeight);
            return ScorpionCombatDecision.SelectProfileValue(profile, controlled, aggressive, frenzy);
        }

        static ScorpionChargeVariantWeights ResolveChargeVariantWeights(
            ScorpionStatsData stats,
            ScorpionHealthProfile profile)
        {
            var controlled = new ScorpionChargeVariantWeights(
                stats.phaseOneShortChargeWeight,
                stats.phaseOneNormalChargeWeight,
                stats.phaseOneCommittedChargeWeight);
            var aggressive = new ScorpionChargeVariantWeights(
                stats.phaseTwoShortChargeWeight,
                stats.phaseTwoNormalChargeWeight,
                stats.phaseTwoCommittedChargeWeight);
            var frenzy = new ScorpionChargeVariantWeights(
                stats.phaseThreeShortChargeWeight,
                stats.phaseThreeNormalChargeWeight,
                stats.phaseThreeCommittedChargeWeight);
            return ScorpionCombatDecision.SelectProfileValue(profile, controlled, aggressive, frenzy);
        }

        static void ResolveDecisionHoldRange(
            ScorpionStatsData stats,
            ScorpionHealthProfile profile,
            out float minimum,
            out float maximum)
        {
            minimum = ScorpionCombatDecision.SelectProfileValue(
                profile,
                stats.decisionHoldMin,
                stats.phaseTwoDecisionHoldMin,
                stats.phaseThreeDecisionHoldMin);
            maximum = ScorpionCombatDecision.SelectProfileValue(
                profile,
                stats.decisionHoldMax,
                stats.phaseTwoDecisionHoldMax,
                stats.phaseThreeDecisionHoldMax);
        }

        float ResolveActionRecovery(ScorpionState completedState)
        {
            ScorpionStatsData stats = ActiveStats;
            ScorpionHealthProfile profile = ActiveHealthProfile;
            float profileRecovery;
            if (completedState == ScorpionState.Attack)
            {
                profileRecovery = ScorpionCombatDecision.SelectProfileValue(
                    profile,
                    stats.phaseOneAttackRecovery,
                    stats.phaseTwoAttackRecovery,
                    stats.phaseThreeAttackRecovery);
            }
            else if (completedState == ScorpionState.Charge)
            {
                profileRecovery = ScorpionCombatDecision.SelectProfileValue(
                    profile,
                    stats.phaseOneChargeRecovery,
                    stats.phaseTwoChargeRecovery,
                    stats.phaseThreeChargeRecovery);
            }
            else if (completedState == ScorpionState.Reverse)
                profileRecovery = stats.reverseVulnerabilityDuration;
            else
                return 0f;

            return ScorpionCombatDecision.ResolveRecovery(
                profileRecovery,
                HasPostStunPressure,
                stats.postStunRecoveryMultiplier);
        }

        void TickAttack()
        {
            ApplyAttackPresentation();
            if (!HasCombatTargetInRange())
            {
                ResetChargeCycle();
                EnterState(ScorpionState.Idle);
                return;
            }

            if (UsesAdvancedAi)
            {
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                    EnterState(ScorpionState.Look);

                return;
            }

            if (currentDistance > attackDistance)
            {
                ResetChargeCycle();
                EnterState(currentDistance <= chargeDistance ? ScorpionState.ChargeWindup : ScorpionState.Look);
            }
        }

        void TickReverse()
        {
            if (UsesAdvancedAi && hurricaneRetreatActive)
            {
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                {
                    EnterState(HasCombatTargetInRange() ? ScorpionState.Look : ScorpionState.Idle);
                    SetAnimatorState(walk: false, backwards: false, attack: false, stunned: false);
                    return;
                }

                ApplyHurricaneRetreatPresentation();
                return;
            }

            if (!HasCombatTargetInRange())
            {
                EnterState(ScorpionState.Idle);
                return;
            }

            if (UsesAdvancedAi)
            {
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                {
                    EnterState(ScorpionState.Look);
                    return;
                }

                MoveAwayFromTarget(ReverseSpeed);
                SetAnimatorState(walk: false, backwards: true, attack: false, stunned: false);
                isAttacking = true;
                return;
            }

            if (!ShouldReverse())
            {
                EnterState(ScorpionState.Look);
                return;
            }

            MoveAwayFromTarget(ReverseSpeed);
            SetAnimatorState(walk: false, backwards: true, attack: false, stunned: false);
            isAttacking = true;
        }

        void TickStunned()
        {
            ApplyStunnedPresentation();
            stateTimer -= Time.fixedDeltaTime;
            if (stateTimer <= 0f)
                EnterState(ScorpionState.Recovered);
        }

        void TickRecovered()
        {
            ApplyRecoveredPresentation();
            if (stateTimer > 0f)
                stateTimer -= Time.fixedDeltaTime;

            if (stateTimer <= 0f)
                EnterState(HasCombatTargetInRange() ? ScorpionState.Look : ScorpionState.Idle);
        }

        public void TakeDamage(int Damage)
        {
            ReceiveDamage(Damage, EnemyDamageType.Normal, null);
        }

        public bool ReceiveDamage(int damage, EnemyDamageType damageType, Transform source)
        {
            return ReceivePlayerAttack(damage, PlayerAttackKind.Unspecified, damageType, source);
        }

        public bool ReceivePlayerAttack(
            int baseDamage,
            PlayerAttackKind attackKind,
            EnemyDamageType damageType,
            Transform source)
        {
            int resolvedDamage = PlayerAttackDamageRules.ResolveDamage(
                baseDamage,
                attackKind,
                UsesAdvancedAi && currentState == ScorpionState.Stunned,
                ActiveStats.stunnedDamageMultiplier);
            bool accepted = ApplyDamage(resolvedDamage, damageType, source, NormalHitComboReward);
            if (accepted && (attackKind == PlayerAttackKind.HurricaneKick
                || attackKind == PlayerAttackKind.HurricaneSword))
                TryStartHurricaneRetreat(source);

            return accepted;
        }

        public bool ReceiveCounterHit(int damage, EnemyDamageType damageType, Transform source)
        {
            int resolvedDamage = PlayerAttackDamageRules.ResolveDamage(
                damage,
                PlayerAttackKind.Unspecified,
                UsesAdvancedAi && currentState == ScorpionState.Stunned,
                ActiveStats.stunnedDamageMultiplier);
            return ApplyDamage(resolvedDamage, damageType, source, CounterHitComboReward);
        }

        bool ApplyDamage(int damage, EnemyDamageType damageType, Transform source, int comboReward)
        {
            if (deathHandled || CurrentHealth <= 0 || damage <= 0)
                return false;

            transform.rotation = rotGoal;
            SetEffectActive(HitEffect, true);
            hitEffectTimer = HitEffectDuration;
            combo += comboReward;
            Sound?.Beat();

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (BossHealth != null)
                BossHealth.SetNPCHealth(CurrentHealth);

            EnsureBoostChargeResolved();
            boostCharge?.RegisterHit(damage);

            if (CurrentHealth <= 0)
                Death();

            return true;
        }

        void TryStartHurricaneRetreat(Transform source)
        {
            if (!UsesAdvancedAi
                || rbScorpion == null
                || deathHandled
                || CurrentHealth <= 0
                || currentState == ScorpionState.Stunned
                || currentState == ScorpionState.Dead)
            {
                return;
            }

            if (hurricaneRetreatActive && currentState == ScorpionState.Reverse)
                return;

            Vector3 retreatDirection = source != null
                ? transform.position - source.position
                : targetTransform != null
                    ? transform.position - targetTransform.position
                    : -transform.forward;
            hurricaneRetreatDirection = ScorpionCombatDecision.LockHorizontalDirection(retreatDirection);
            if (hurricaneRetreatDirection.sqrMagnitude <= LookRotationEpsilon)
                hurricaneRetreatDirection = ScorpionCombatDecision.LockHorizontalDirection(-transform.forward);

            hurricaneRetreatActive = true;
            if (currentState == ScorpionState.Reverse)
                stateTimer = Mathf.Max(0f, ActiveStats.hurricaneKickRetreatDuration);
            else
                EnterState(ScorpionState.Reverse);

            rotGoal = Quaternion.LookRotation(-hurricaneRetreatDirection);
            rbScorpion.rotation = rotGoal;
            ApplyHurricaneRetreatPresentation();
            // Walk's exit transition is longer than this retreat. Enter the existing
            // rapid, looping claw-cover animation once; later hits must not rewind it.
            if (Scorpion != null && Scorpion.HasState(0, BackwardsAnimatorStateHash))
                Scorpion.Play(BackwardsAnimatorStateHash, 0, 0f);
        }

        void ApplyHurricaneRetreatPresentation()
        {
            SetHorizontalVelocity(hurricaneRetreatDirection * ActiveStats.hurricaneKickRetreatSpeed);
            SetAnimatorState(walk: false, backwards: true, attack: false, stunned: false);
            isAttacking = true;
        }

        void Death()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            currentState = ScorpionState.Dead;
            state = currentState.ToString();
            CurrentHealth = 0;
            isAttacking = false;
            hurricaneRetreatActive = false;
            EnsureBoostChargeResolved();
            boostCharge?.RegisterKill();
            if (BossHealth != null)
                BossHealth.SetNPCHealth(CurrentHealth);

            ApplyDeadPresentation();
            if (Explosion != null)
            {
                Explosion.SetActive(true);
                Explosion.transform.parent = null;
            }

            if (drops != null)
            {
                foreach (var item in drops)
                {
                    if (item == null)
                        continue;

                    CurrencySpin.ConfigureEnemyDrop(
                        Instantiate(item, transform.position + new Vector3(0f, 0.3f, 0f), Quaternion.identity));
                }
            }

            Defeated?.Invoke(this);
            genericDefeated?.Invoke(this);
            gameObject.SetActive(false);
        }

        void OnCollisionEnter(Collision OBJ)
        {
            if (HandleMarkedGivingTreeChargeCollision(OBJ))
                return;

            if (OBJ.gameObject.CompareTag("Strike"))
            {
                ApplyStrikeCollision(OBJ.transform);
                return;
            }

            if (OBJ.gameObject.CompareTag("Damage"))
                ReceiveDamage(5, EnemyDamageType.Normal, OBJ.transform);

            if (OBJ.gameObject.CompareTag("Bridge"))
            {
                OBJ.gameObject.GetComponent<LogSpawner>()?.DestroyTree(OBJ.transform);
                ReceiveDamage(10, EnemyDamageType.Normal, OBJ.transform);
                combo = Mathf.Max(combo, BridgeComboOverride);
            }

            HandleChargeCollision(OBJ);
        }

        bool ApplyStrikeCollision(Transform source)
        {
            if (deathHandled || CurrentHealth <= 0)
                return false;

            return ReceiveDamage(CurrentHealth, EnemyDamageType.Normal, source);
        }

        void OnCollisionStay(Collision collision)
        {
            HandleChargeCollision(collision);
        }

        void HandleChargeCollision(Collision collision)
        {
            if (!UsesAdvancedAi || currentState != ScorpionState.Charge || collision == null)
                return;

            if (HandleMarkedGivingTreeChargeCollision(collision))
                return;

            for (int index = 0; index < collision.contactCount; index++)
            {
                Vector3 contactNormal = collision.GetContact(index).normal;
                if (HandleChargeCollisionNormal(contactNormal))
                    return;
            }
        }

        bool HandleMarkedGivingTreeChargeCollision(Collision collision)
        {
            if (!UsesAdvancedAi || currentState != ScorpionState.Charge || collision == null)
                return false;

            LogSpawner tree = collision.collider != null
                ? collision.collider.GetComponentInParent<LogSpawner>()
                : null;
            if (tree == null || !tree.CanStunScorpionBoss)
                return false;

            for (int index = 0; index < collision.contactCount; index++)
            {
                if (HandleMarkedGivingTreeChargeContact(tree, collision.GetContact(index).normal))
                    return true;
            }

            return false;
        }

        bool HandleMarkedGivingTreeChargeContact(LogSpawner tree, Vector3 contactNormal)
        {
            if (!UsesAdvancedAi
                || currentState != ScorpionState.Charge
                || tree == null
                || !tree.CanStunScorpionBoss
                || !IsFrontalChargeContact(contactNormal))
            {
                return false;
            }

            tree.DestroyTree(transform);
            ScorpionState nextState = stunCooldownRemaining <= 0f
                ? ScorpionState.Stunned
                : HasCombatTargetInRange()
                    ? ScorpionState.Look
                    : ScorpionState.Idle;
            EnterState(nextState);
            return true;
        }

        bool IsFrontalChargeContact(Vector3 contactNormal)
        {
            Vector3 horizontalNormal = ScorpionCombatDecision.LockHorizontalDirection(contactNormal);
            if (horizontalNormal.sqrMagnitude <= LookRotationEpsilon
                || lockedChargeDirection.sqrMagnitude <= LookRotationEpsilon)
            {
                return false;
            }

            Vector3 impactDirection = -horizontalNormal;
            return Vector3.Dot(lockedChargeDirection, impactDirection) >= FrontalTreeImpactDotThreshold;
        }

        bool HandleChargeCollisionNormal(Vector3 contactNormal)
        {
            if (!UsesAdvancedAi || currentState != ScorpionState.Charge)
                return false;

            Vector3 horizontalContactNormal = contactNormal;
            horizontalContactNormal.y = 0f;
            if (contactNormal.y > horizontalContactNormal.magnitude)
                return false;

            Vector3 horizontalNormal = ScorpionCombatDecision.LockHorizontalDirection(contactNormal);
            if (horizontalNormal.sqrMagnitude <= LookRotationEpsilon
                || lockedChargeDirection.sqrMagnitude <= LookRotationEpsilon
                || Vector3.Dot(lockedChargeDirection, horizontalNormal) > ChargeObstructionDotThreshold)
                return false;

            EnterState(HasCombatTargetInRange() ? ScorpionState.Look : ScorpionState.Idle);
            return true;
        }

        void ResolvePlayerReferences()
        {
            if (Player == null)
                Player = FindObjectOfType<BeaverPlayer>();

            if (boostCharge == null && Player != null)
                boostCharge = Player.BoostCharge != null ? Player.BoostCharge : Player.GetComponent<BoostChargeController>();

            targetTransform = Player != null ? Player.transform : null;
        }

        void EnsureBoostChargeResolved()
        {
            if (boostCharge != null)
                return;

            ResolvePlayerReferences();
        }

        void RefreshTargetContext()
        {
            if (Player == null || targetTransform == null)
                ResolvePlayerReferences();

            if (targetTransform == null)
            {
                distanceToTarget = Vector3.zero;
                currentDistance = float.PositiveInfinity;
                return;
            }

            distanceToTarget = targetTransform.position - rbScorpion.position;
            currentDistance = distanceToTarget.magnitude;
        }

        void EnterState(ScorpionState nextState)
        {
            if (currentState == nextState)
                return;

            ScorpionState previousState = currentState;
            currentState = nextState;
            state = nextState.ToString();
            if (UsesAdvancedAi && previousState == ScorpionState.Charge && nextState != ScorpionState.Charge)
                ResetAdvancedChargeTracking();
            if (previousState == ScorpionState.Reverse && nextState != ScorpionState.Reverse)
                hurricaneRetreatActive = false;
            if (UsesAdvancedAi && previousState == ScorpionState.Recovered)
            {
                postStunPressureTimer = Mathf.Max(0f, ActiveStats.postStunPressureDuration);
                if (startStunCooldownAfterRecovery)
                {
                    stunCooldownRemaining = Mathf.Max(0f, ActiveStats.bossStunCooldown);
                    startStunCooldownAfterRecovery = false;
                }
            }

            switch (nextState)
            {
                case ScorpionState.ChargeWindup:
                    minimumChargeTimeRemaining = chargeDuration;
                    if (UsesAdvancedAi)
                    {
                        ScorpionStatsData stats = ActiveStats;
                        ScorpionChargeVariantWeights variantWeights = ResolveChargeVariantWeights(
                            stats,
                            ActiveHealthProfile);
                        activeChargeVariant = ScorpionCombatDecision.SelectChargeVariant(
                            variantWeights,
                            UnityEngine.Random.value);
                        activeChargeLimits = ScorpionCombatDecision.ResolveChargeLimits(
                            activeChargeVariant,
                            stats.chargeMaximumDuration,
                            stats.chargeMaximumDistance,
                            stats.chargeTrackingDuration,
                            stats.shortChargeDurationMultiplier,
                            stats.shortChargeDistanceMultiplier,
                            stats.committedChargeDurationMultiplier,
                            stats.committedChargeDistanceMultiplier,
                            stats.committedChargeTrackingMultiplier);
                        float minimumWindup = Mathf.Max(ScorpionStatsData.MinimumChargeWindup, stats.chargeWindupMin);
                        stateTimer = RandomRange(minimumWindup, Mathf.Max(minimumWindup, stats.chargeWindupMax));
                    }
                    else
                        stateTimer = Time.fixedDeltaTime;
                    break;
                case ScorpionState.Charge:
                    if (UsesAdvancedAi)
                    {
                        chargeElapsed = 0f;
                        chargeTravelledDistance = 0f;
                        previousChargePosition = rbScorpion.position;
                        lockedChargeDirection = ScorpionCombatDecision.LockHorizontalDirection(HorizontalTargetDirection());
                    }
                    stateTimer = 0f;
                    break;
                case ScorpionState.Attack:
                    stateTimer = UsesAdvancedAi ? Mathf.Max(0f, ActiveStats.attackWindowDuration) : 0f;
                    break;
                case ScorpionState.Reverse:
                    stateTimer = UsesAdvancedAi && hurricaneRetreatActive
                        ? Mathf.Max(0f, ActiveStats.hurricaneKickRetreatDuration)
                        : UsesAdvancedAi
                        ? RandomRange(ActiveStats.decisionHoldMin, ActiveStats.decisionHoldMax)
                        : 0f;
                    break;
                case ScorpionState.Look:
                    stateTimer = UsesAdvancedAi ? ResolveActionRecovery(previousState) : 0f;
                    if (UsesAdvancedAi)
                    {
                        isAttacking = false;
                        SetHorizontalVelocity(Vector3.zero);
                    }
                    break;
                case ScorpionState.Idle:
                    stateTimer = 0f;
                    if (UsesAdvancedAi)
                    {
                        isAttacking = false;
                        SetHorizontalVelocity(Vector3.zero);
                    }
                    break;
                case ScorpionState.Stunned:
                    stateTimer = UsesAdvancedAi
                        ? Mathf.Max(0f, ActiveStats.bossStunDuration)
                        : ActiveStats.stunDuration;
                    if (UsesAdvancedAi)
                        startStunCooldownAfterRecovery = true;
                    break;
                case ScorpionState.Recovered:
                    combo = 0;
                    ResetChargeCycle();
                    stateTimer = ActiveStats.recoveryDuration;
                    break;
                default:
                    stateTimer = 0f;
                    break;
            }
        }

        bool HasCombatTargetInRange()
        {
            return targetTransform != null && currentDistance <= lookDistance;
        }

        bool ShouldReverse()
        {
            return !IsAggressive && currentDistance < ReverseDistanceThreshold;
        }

        bool ShouldStartCharge()
        {
            return currentDistance <= chargeDistance;
        }

        void ResetChargeCycle()
        {
            minimumChargeTimeRemaining = chargeDuration;
        }

        void ResetAdvancedChargeTracking()
        {
            chargeElapsed = 0f;
            chargeTravelledDistance = 0f;
            previousChargePosition = Vector3.zero;
            lockedChargeDirection = Vector3.zero;
        }

        Vector3 HorizontalTargetDirection()
        {
            return new Vector3(distanceToTarget.x, 0f, distanceToTarget.z);
        }

        void MoveTowardsTarget(float speed)
        {
            Vector3 horizontal = HorizontalTargetDirection();
            RotateTowards(horizontal);
            if (horizontal.sqrMagnitude <= LookRotationEpsilon)
            {
                SetHorizontalVelocity(Vector3.zero);
                return;
            }

            SetHorizontalVelocity(horizontal.normalized * speed);
        }

        void MoveAwayFromTarget(float speed)
        {
            Vector3 horizontal = HorizontalTargetDirection();
            RotateTowards(horizontal);
            if (horizontal.sqrMagnitude <= LookRotationEpsilon)
            {
                SetHorizontalVelocity(Vector3.zero);
                return;
            }

            SetHorizontalVelocity(-horizontal.normalized * speed);
        }

        void RotateTowards(Vector3 horizontalDirection)
        {
            if (horizontalDirection.sqrMagnitude > LookRotationEpsilon)
                rotGoal = Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up);

            rbScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, Mathf.Clamp01(rotationSpeed));
        }

        void SetHorizontalVelocity(Vector3 horizontalVelocity)
        {
            rbScorpion.velocity = new Vector3(horizontalVelocity.x, rbScorpion.velocity.y, horizontalVelocity.z);
        }

        void ApplyIdlePresentation()
        {
            isAttacking = false;
            SetHorizontalVelocity(Vector3.zero);
            rotGoal = transform.rotation;
            rbScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, Mathf.Clamp01(rotationSpeed));
            SetAnimatorState(walk: false, backwards: false, attack: false, stunned: false);
            SetEffectActive(StunEffect, false);
        }

        void ApplyLookPresentation()
        {
            isAttacking = false;
            SetHorizontalVelocity(Vector3.zero);
            RotateTowards(HorizontalTargetDirection());
            bool isTurning = Quaternion.Angle(transform.rotation, rotGoal) > 1f;
            SetAnimatorState(walk: isTurning, backwards: false, attack: false, stunned: false);
            SetEffectActive(StunEffect, false);
        }

        void ApplyAttackPresentation()
        {
            isAttacking = true;
            SetHorizontalVelocity(Vector3.zero);
            RotateTowards(HorizontalTargetDirection());
            SetAnimatorState(walk: false, backwards: false, attack: true, stunned: false);
            SetEffectActive(StunEffect, false);
        }

        void ApplyStunnedPresentation()
        {
            isAttacking = false;
            SetHorizontalVelocity(Vector3.zero);
            SetAnimatorState(walk: false, backwards: false, attack: false, stunned: true);
            SetEffectActive(StunEffect, true);
        }

        void ApplyRecoveredPresentation()
        {
            isAttacking = false;
            SetHorizontalVelocity(Vector3.zero);
            SetAnimatorState(walk: false, backwards: false, attack: false, stunned: false);
            SetEffectActive(StunEffect, false);
        }

        void ApplyDeadPresentation()
        {
            SetHorizontalVelocity(Vector3.zero);
            SetAnimatorState(walk: false, backwards: false, attack: false, stunned: false);
            SetEffectActive(StunEffect, false);
            SetEffectActive(HitEffect, false);
        }

        void SetAnimatorState(bool walk, bool backwards, bool attack, bool stunned)
        {
            if (Scorpion == null)
                return;

            Scorpion.speed = 1f;
            Scorpion.SetBool("Walk", walk);
            Scorpion.SetBool("Backwards", backwards);
            Scorpion.SetBool("Attack", attack);
            Scorpion.SetBool(StunnedAnimatorParameter, stunned);
        }

        void SetEffectActive(GameObject effect, bool active)
        {
            if (effect != null)
                effect.SetActive(active);
        }

        ScorpionStatsData ResolveFallbackStats()
        {
            if (fallbackStats != null)
                return fallbackStats;

            ScorpionStatsData loadedStats = LoadConfiguredStatsData();
            if (loadedStats != null)
            {
                fallbackStats = loadedStats;
                return fallbackStats;
            }

            fallbackStats = ScriptableObject.CreateInstance<ScorpionStatsData>();
            fallbackStats.hideFlags = HideFlags.HideAndDontSave;
            fallbackStats.maxHealth = legacyMaxHealth;
            fallbackStats.comboLimit = legacyComboLimit;
            fallbackStats.stunDuration = legacyStunDuration;
            fallbackStats.chargeSpeed = legacyChargeSpeed;
            fallbackStats.chargeDuration = legacyChargeDuration;
            fallbackStats.lookDistance = legacyLookDistance;
            fallbackStats.chargeDistance = legacyChargeDistance;
            fallbackStats.attackDistance = legacyAttackDistance;
            fallbackStats.attackDamage = legacyAttackDamage;
            fallbackStats.stingDamage = legacyStingDamage;
            fallbackStats.rotationSpeed = legacyRotationSpeed;
            fallbackStats.recoveryDuration = legacyRecoveryDuration;

            Debug.LogWarning($"{name}: ScorpionStatsData reference missing. Using legacy serialized fallback values.", this);
            return fallbackStats;
        }

        ScorpionStatsData LoadConfiguredStatsData()
        {
            if (!string.IsNullOrWhiteSpace(defaultStatsResourcePath))
                return Resources.Load<ScorpionStatsData>(defaultStatsResourcePath);

            return null;
        }
    }
}
