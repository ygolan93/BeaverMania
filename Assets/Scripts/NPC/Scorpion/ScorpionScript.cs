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
    public class ScorpionScript : MonoBehaviour, IEnemyDamageReceiver
    {
        const float LookRotationEpsilon = 0.0001f;
        const float ReverseSpeed = 5f;
        const float HitEffectDuration = 0.1f;
        const int BridgeComboOverride = 10;
        const int DefaultAttackDamage = 15;
        const int DefaultStingDamage = 30;
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
        bool deathHandled;

        public ScorpionState State => currentState;
        public ScorpionStatsData StatsData => ActiveStats;
        public int MaxHealth => ActiveStats.maxHealth;
        public int comboLimit => ActiveStats.comboLimit;
        public float StunnedClock => currentState == ScorpionState.Stunned ? stateTimer : 0f;
        public float chargeSpeed => ActiveStats.chargeSpeed;
        public float chargeDuration => ActiveStats.chargeDuration;
        public float chargeClock => minimumChargeTimeRemaining;
        public float lookDistance => ActiveStats.lookDistance;
        public float chargeDistance => ActiveStats.chargeDistance;
        public float attackDistance => ActiveStats.attackDistance;
        public int AttackDamageAmount => ActiveStats.attackDamage;
        public int StingDamageAmount => ActiveStats.stingDamage;
        public float rotationSpeed => ActiveStats.rotationSpeed;
        public float recoveryDuration => ActiveStats.recoveryDuration;
        bool IsAggressive => combo >= Mathf.Max(0, comboLimit - 5);
        float ReverseDistanceThreshold => Mathf.Max(0f, chargeDistance - 10f);
        ScorpionStatsData ActiveStats => statsData != null ? statsData : ResolveFallbackStats();

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

            RefreshTargetContext();

            if (CurrentHealth <= 0)
            {
                Death();
                return;
            }

            if (combo >= comboLimit && currentState != ScorpionState.Stunned && currentState != ScorpionState.Recovered)
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
            ApplyLookPresentation();
            if (!HasCombatTargetInRange())
            {
                EnterState(ScorpionState.Idle);
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

        void TickChargeWindup()
        {
            ApplyLookPresentation();
            if (!HasCombatTargetInRange())
            {
                EnterState(ScorpionState.Idle);
                return;
            }

            if (currentDistance > chargeDistance)
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

        void TickAttack()
        {
            ApplyAttackPresentation();
            if (!HasCombatTargetInRange())
            {
                ResetChargeCycle();
                EnterState(ScorpionState.Idle);
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
            if (!HasCombatTargetInRange())
            {
                EnterState(ScorpionState.Idle);
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
            if (deathHandled || CurrentHealth <= 0 || damage <= 0)
                return false;

            transform.rotation = rotGoal;
            SetEffectActive(HitEffect, true);
            hitEffectTimer = HitEffectDuration;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            combo++;
            Sound?.Beat();
            if (BossHealth != null)
                BossHealth.SetNPCHealth(CurrentHealth);

            boostCharge?.RegisterHit(damage);

            if (CurrentHealth <= 0)
                Death();

            return true;
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

            gameObject.SetActive(false);
        }

        void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject.CompareTag("Strike"))
            {
                Death();
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
        }

        void ResolvePlayerReferences()
        {
            if (Player == null)
                Player = FindObjectOfType<BeaverPlayer>();

            if (boostCharge == null && Player != null)
                boostCharge = Player.BoostCharge != null ? Player.BoostCharge : Player.GetComponent<BoostChargeController>();

            targetTransform = Player != null ? Player.transform : null;
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

            currentState = nextState;
            state = nextState.ToString();

            switch (nextState)
            {
                case ScorpionState.ChargeWindup:
                    minimumChargeTimeRemaining = chargeDuration;
                    stateTimer = Time.fixedDeltaTime;
                    break;
                case ScorpionState.Stunned:
                    stateTimer = ActiveStats.stunDuration;
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
            Scorpion.SetBool("Stunned", stunned);
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
