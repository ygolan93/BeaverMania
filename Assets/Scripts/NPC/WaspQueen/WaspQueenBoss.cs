using System;
using System.Collections;
using System.Collections.Generic;
using Beavermania.Audio;
using Beavermania.Data.NPC;
using Beavermania.Display;
using Beavermania.Player;
using UnityEngine;

namespace Beavermania.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WaspQueenBoss : MonoBehaviour, IEnemyDamageReceiver, IBossVictorySource
    {
        enum AudioCue
        {
            Intro = 0,
            RangedTelegraph = 1,
            RangedFire = 2,
            AoeTelegraph = 3,
            ChargeTelegraph = 4,
            ChargeDash = 5,
            Summon = 6,
            PhaseTransition = 7,
            Hit = 8,
            Death = 9,
            Sting = 10
        }

        static readonly int RangedAttackHash = Animator.StringToHash("RangedAttack");
        static readonly int PoisonAoeHash = Animator.StringToHash("PoisonAoE");
        static readonly int ChargeHash = Animator.StringToHash("Charge");
        static readonly int SummonHash = Animator.StringToHash("Summon");
        static readonly int PhaseTransitionHash = Animator.StringToHash("PhaseTransition");
        static readonly int DieHash = Animator.StringToHash("Die");
        static readonly int StingHash = Animator.StringToHash("Sting");
        static readonly int HitHash = Animator.StringToHash("Hit");
        static readonly int IntroHash = Animator.StringToHash("Intro");
        static readonly Dictionary<int, string> AnimatorTriggerNamesByHash = new Dictionary<int, string>
        {
            { RangedAttackHash, "RangedAttack" },
            { PoisonAoeHash, "PoisonAoE" },
            { ChargeHash, "Charge" },
            { SummonHash, "Summon" },
            { PhaseTransitionHash, "PhaseTransition" },
            { DieHash, "Die" },
            { StingHash, "Sting" },
            { HitHash, "Hit" },
            { IntroHash, "Intro" }
        };

        const float LookRotationEpsilon = 0.0001f;
        const float PlayerResolveRetryDelay = 1f;

        public WaspQueenConfig Config;
        public Rigidbody Body;
        public Animator Animator;
        public NPC_Health HealthBar;
        public BeaverPlayerBehaviour Player;
        public Transform ProjectileSpawnPoint;
        public Transform AoeOrigin;
        public Transform[] WaspSpawnPoints;
        public WaspQueenChargeAttack ChargeAttack;
        public AudioSource AudioSource;
        public Collider ChargeHitbox;
        public bool ActivateOnStart;

        [Header("Audio Clips")]
        [SerializeField] AudioClip introClip;
        [SerializeField] AudioClip rangedTelegraphClip;
        [SerializeField] AudioClip rangedFireClip;
        [SerializeField] AudioClip aoeTelegraphClip;
        [SerializeField] AudioClip chargeTelegraphClip;
        [SerializeField] AudioClip chargeDashClip;
        [SerializeField] AudioClip summonClip;
        [SerializeField] AudioClip phaseTransitionClip;
        [SerializeField] AudioClip hitClip;
        [SerializeField] AudioClip deathClip;
        [SerializeField] AudioClip stingClip;

        [Header("Hit Effects")]
        [SerializeField] GameObject slashEffect;
        [SerializeField] GameObject hitEffect;
        [SerializeField] float hitEffectVisibleDuration = 0.2f;

        [Header("Death")]
        [Tooltip("Seconds to keep the body active so WQ_Death can play before disabling. 0 = use the death clip length.")]
        [SerializeField] float deathDisableDelay = 0f;

        [Header("Flinch")]
        [SerializeField] float minFlinchInterval = 0.5f;
        [SerializeField] bool suppressFlinchDuringAttacks = true;

        [Header("Arena")]
        [Tooltip("Optional arena anchor. If unassigned, the boss captures its spawn position as the arena center.")]
        [SerializeField] Transform arenaCenter;

        [Header("Pooling")]
        [SerializeField] WaspQueenPoolHub poolHub;

        readonly List<GameObject> activeSummonedWasps = new List<GameObject>(8);
        readonly List<WaspQueenProjectile> activeProjectiles = new List<WaspQueenProjectile>(4);
        readonly List<WaspQueenPoisonZone> activePoisonZones = new List<WaspQueenPoisonZone>(4);
        readonly float[] lastAudioPlayTimes = new float[11];
        readonly HashSet<int> missingAnimatorTriggerWarnings = new HashSet<int>();

        EnemyHealthBarVisibility healthBarVisibility;
        Action<IBossVictorySource> genericDefeated;
        WaspQueenState state = WaspQueenState.Inactive;
        WaspQueenAbility previousAbility = WaspQueenAbility.None;
        int repeatedAbilityCount;
        int currentHealth;
        int currentPhaseIndex;
        int pendingPhaseIndex = -1;
        float stateTimer;
        float rangedCooldownRemaining;
        float aoeCooldownRemaining;
        float chargeCooldownRemaining;
        float summonCooldownRemaining;
        float stingCooldownRemaining;
        float nextPlayerResolveTime;
        Vector3 cachedAoeTargetPosition;
        Vector3 stingReturnPosition;
        bool deathHandled;
        bool stateActionExecuted;
        bool explosionSpawned;
        float lastFlinchTime = -999f;
        Vector3 capturedArenaCenter;
        bool arenaCenterCaptured;
        Vector3 repositionTarget;
        readonly RaycastHit[] groundSnapHits = new RaycastHit[8];
        Coroutine hitEffectHideRoutine;
        Coroutine deathRoutine;

        const float ReleaseFallbackExtra = 1.25f;
        const float DefaultDeathClipLength = 1.8f;
        const string ChargeTelegraphStateName = "Charge_Telegraph";
        const string ChargeDashStateName = "Charge_Dash";
        const string ChargeRecoveryStateName = "Charge_Recovery";
        const string IdleStateName = "Idle";
        static readonly int ChargeTelegraphStateHash = Animator.StringToHash(ChargeTelegraphStateName);
        static readonly int ChargeDashStateHash = Animator.StringToHash(ChargeDashStateName);
        static readonly int ChargeRecoveryStateHash = Animator.StringToHash(ChargeRecoveryStateName);
        static readonly int IdleStateHash = Animator.StringToHash(IdleStateName);

        public event Action<WaspQueenBoss> Defeated;
        event Action<IBossVictorySource> IBossVictorySource.Defeated
        {
            add => genericDefeated += value;
            remove => genericDefeated -= value;
        }

        public WaspQueenState State => state;
        public int CurrentPhaseNumber => currentPhaseIndex + 1;
        public int CurrentHealth => currentHealth;
        public Vector3 CurrentChargeDirection => ChargeAttack != null ? ChargeAttack.ChargeDirection : transform.forward;
        public float VictoryDelay => Config != null ? Mathf.Max(0f, Config.victoryDelay) : 0f;

        void Awake()
        {
            CacheReferences();
            ResetCombatState();
            ApplyHealthBarState();
        }

        void Start()
        {
            ResolvePlayerReference(force: true);
            if (ActivateOnStart)
                ActivateBoss();
        }

        void OnDisable()
        {
            CleanupRuntimeObjects();
            SetChargeHitboxEnabled(false);
            if (ChargeAttack != null)
                ChargeAttack.EndCharge();
            HideHitEffects();
        }

        void Update()
        {
            ResolvePlayerReference(force: false);
            PurgeDestroyedSummons();
            PurgeInactiveHazards();
            TickCooldowns();

            if (deathHandled)
                return;

            if (currentHealth <= 0)
            {
                HandleDeath();
                return;
            }

            if (state == WaspQueenState.Inactive)
            {
                if (ShouldAutoActivate())
                    ActivateBoss();

                if (state == WaspQueenState.Inactive)
                    return;
            }

            UpdatePendingPhaseTransition();

            if (IsLeashInterruptible(state) && ShouldLeash())
            {
                EnterState(WaspQueenState.Returning, 0f);
            }

            TickState();
        }

        void FixedUpdate()
        {
            if (deathHandled || state == WaspQueenState.Inactive)
                return;

            if (state == WaspQueenState.Charge && stateActionExecuted)
            {
                if (!IsChargeDashAnimPlaying())
                    return;

                if (ChargeAttack != null)
                {
                    bool blocked = ChargeAttack.TickMovement(
                        Body,
                        CurrentPhase().chargeSpeed,
                        Time.fixedDeltaTime,
                        ResolveHoverPosition,
                        Config != null ? Config.chargeObstructionMask : default);

                    if (blocked && stateTimer > 0.05f)
                        stateTimer = 0f;

                    if (ChargeAttack.TryApplyHit(Player, CurrentPhase().chargeDamage, out _))
                        SetChargeHitboxEnabled(false);
                }

                return;
            }

            if (state == WaspQueenState.StingLunge && stateActionExecuted)
            {
                if (ChargeAttack != null)
                {
                    if (Player != null)
                        ChargeAttack.SteerToward(Player.transform.position, Config != null ? Config.stingHomingStrength : 0f, Time.fixedDeltaTime);

                    bool blocked = ChargeAttack.TickMovement(
                        Body,
                        CurrentPhase().stingSpeed,
                        Time.fixedDeltaTime,
                        ResolveHoverPosition,
                        Config != null ? Config.chargeObstructionMask : default);

                    if (blocked && stateTimer > 0.05f)
                        stateTimer = 0f;

                    if (ChargeAttack.TryApplyHit(Player, CurrentPhase().stingDamage, out _))
                    {
                        SetChargeHitboxEnabled(false);
                        stateTimer = 0f;
                    }
                }

                return;
            }

            if (state == WaspQueenState.StingRetreat)
            {
                if (MoveTowardReturnPosition(Config != null ? Config.stingRetreatSpeed : 18f))
                    stateTimer = 0f;

                return;
            }

            if (state == WaspQueenState.Returning)
            {
                MoveTowardPosition(ArenaCenterPosition(), Config != null ? Config.recenterSpeed : 14f);
                return;
            }

            if (state == WaspQueenState.Reposition)
            {
                MoveTowardPosition(repositionTarget, Config != null ? Config.repositionSpeed : 9f);
                return;
            }

            MaintainHoverHeight();
        }

        public void ActivateBoss()
        {
            if (deathHandled)
                return;

            ResolvePlayerReference(force: true);
            if (state != WaspQueenState.Inactive)
                return;

            ResetCombatState();
            healthBarVisibility?.EnableAlwaysShow();
            EnterState(WaspQueenState.Intro, Config != null ? Config.introDuration : 0f);
        }

        public void DeactivateBossEncounter()
        {
            CleanupRuntimeObjects();
            SetChargeHitboxEnabled(false);
            if (ChargeAttack != null)
                ChargeAttack.EndCharge();
            StopMovement();

            ResetCombatState();
            state = WaspQueenState.Inactive;
            stateTimer = 0f;
            stateActionExecuted = false;
            previousAbility = WaspQueenAbility.None;
            repeatedAbilityCount = 0;
            pendingPhaseIndex = -1;
        }

        public void TakeDamage(int damage)
        {
            ReceiveDamage(damage, EnemyDamageType.Normal, null);
        }

        public bool ReceiveDamage(int damage, EnemyDamageType damageType, Transform source)
        {
            if (deathHandled || currentHealth <= 0 || damage <= 0)
                return false;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            ApplyHealthBarState();
            healthBarVisibility?.NotifyDamaged();
            PlayClip(hitClip, AudioCue.Hit, 0.08f, 0.8f);
            ShowHitEffect();

            if (currentHealth <= 0)
            {
                HandleDeath();
            }
            else if (CanFlinch())
            {
                // Cosmetic flinch only on surviving hits; gated so rapid melee cannot stunlock the boss
                // or interrupt readable attack/telegraph animations. The lethal blow plays Death instead.
                lastFlinchTime = Time.time;
                TriggerAnimator(HitHash);
            }

            return true;
        }

        bool CanFlinch()
        {
            if (deathHandled || currentHealth <= 0)
                return false;

            if (Time.time - lastFlinchTime < minFlinchInterval)
                return false;

            if (!suppressFlinchDuringAttacks)
                return true;

            switch (state)
            {
                case WaspQueenState.Idle:
                case WaspQueenState.Decision:
                case WaspQueenState.Recovery:
                    return true;
                default:
                    return false;
            }
        }

        void TickState()
        {
            switch (state)
            {
                case WaspQueenState.Inactive:
                    return;
                case WaspQueenState.Intro:
                    FacePlayer();
                    TickTimedState(() => EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f));
                    break;
                case WaspQueenState.Idle:
                    FacePlayer();
                    TickTimedState(() => EnterState(WaspQueenState.Decision, 0f));
                    break;
                case WaspQueenState.Decision:
                    ResolveDecision();
                    break;
                case WaspQueenState.RangedPoisonShot:
                    TickRangedAttack();
                    break;
                case WaspQueenState.PoisonAoE:
                    TickPoisonAoe();
                    break;
                case WaspQueenState.SummonWasps:
                    TickSummon();
                    break;
                case WaspQueenState.Charge:
                    TickCharge();
                    break;
                case WaspQueenState.StingLunge:
                    TickStingLunge();
                    break;
                case WaspQueenState.StingRetreat:
                    TickStingRetreat();
                    break;
                case WaspQueenState.PhaseTransition:
                    FacePlayer();
                    TickTimedState(() => EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f));
                    break;
                case WaspQueenState.Recovery:
                    FacePlayer();
                    TickTimedState(() =>
                    {
                        if (pendingPhaseIndex >= 0)
                        {
                            BeginPendingPhaseTransition();
                            return;
                        }

                        if (ShouldReposition())
                        {
                            EnterReposition();
                            return;
                        }

                        EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f);
                    });
                    break;
                case WaspQueenState.Reposition:
                    FacePlayer();
                    TickTimedState(() => EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f));
                    break;
                case WaspQueenState.Returning:
                    TickReturning();
                    break;
                case WaspQueenState.Death:
                    return;
            }
        }

        void ResolveDecision()
        {
            if (pendingPhaseIndex >= 0)
            {
                BeginPendingPhaseTransition();
                return;
            }

            if (Config == null || Player == null)
            {
                EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f);
                return;
            }

            WaspQueenAbility ability = WaspQueenDecisionPlanner.ChooseAbility(
                Config,
                CurrentPhase(),
                new WaspQueenDecisionContext(
                    DistanceToPlayer(),
                    activeSummonedWasps.Count,
                    rangedCooldownRemaining,
                    aoeCooldownRemaining,
                    chargeCooldownRemaining,
                    summonCooldownRemaining,
                    stingCooldownRemaining,
                    previousAbility,
                    repeatedAbilityCount));

            switch (ability)
            {
                case WaspQueenAbility.RangedPoisonShot:
                    EnterState(WaspQueenState.RangedPoisonShot, CurrentPhase().rangedTelegraphDuration + ReleaseFallbackExtra);
                    break;
                case WaspQueenAbility.PoisonAoE:
                    cachedAoeTargetPosition = ResolveAoeTargetPosition();
                    EnterState(WaspQueenState.PoisonAoE, CurrentPhase().aoeTelegraphDuration + ReleaseFallbackExtra);
                    break;
                case WaspQueenAbility.SummonWasps:
                    EnterState(WaspQueenState.SummonWasps, CurrentPhase().summonTelegraphDuration + ReleaseFallbackExtra);
                    break;
                case WaspQueenAbility.Charge:
                    EnterState(WaspQueenState.Charge, CurrentPhase().chargeTelegraphDuration + ReleaseFallbackExtra);
                    break;
                case WaspQueenAbility.StingLunge:
                    EnterState(WaspQueenState.StingLunge, Config.stingTelegraphDuration);
                    break;
                default:
                    EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f);
                    break;
            }
        }

        void TickRangedAttack()
        {
            FacePlayer();
            if (stateActionExecuted)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                ExecuteRangedRelease();
        }

        void TickPoisonAoe()
        {
            FacePlayer();
            if (stateActionExecuted)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                ExecuteAoeRelease();
        }

        void TickSummon()
        {
            FacePlayer();
            if (stateActionExecuted)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                ExecuteSummonRelease();
        }

        // Release points are normally driven by clip animation events (AnimEvent_*); the timer above is a
        // safety fallback so the boss can never soft-lock if an event is missing.
        void ExecuteRangedRelease()
        {
            if (state != WaspQueenState.RangedPoisonShot || stateActionExecuted)
                return;

            stateActionExecuted = true;
            FireProjectile();
            rangedCooldownRemaining = CurrentPhase().rangedCooldown;
            EnterRecovery(WaspQueenAbility.RangedPoisonShot, CurrentPhase().rangedRecoveryDuration);
        }

        void ExecuteAoeRelease()
        {
            if (state != WaspQueenState.PoisonAoE || stateActionExecuted)
                return;

            stateActionExecuted = true;
            SpawnPoisonZone();
            aoeCooldownRemaining = CurrentPhase().aoeCooldown;
            EnterRecovery(WaspQueenAbility.PoisonAoE, CurrentPhase().aoeRecoveryDuration);
        }

        void ExecuteSummonRelease()
        {
            if (state != WaspQueenState.SummonWasps || stateActionExecuted)
                return;

            stateActionExecuted = true;
            SpawnSummonedWasps();
            summonCooldownRemaining = CurrentPhase().summonCooldown;
            EnterRecovery(WaspQueenAbility.SummonWasps, CurrentPhase().summonRecoveryDuration);
        }

        void TickCharge()
        {
            if (!stateActionExecuted)
            {
                FacePlayer();
                EnsureChargeSequenceStarted();

                if (IsChargeDashAnimPlaying())
                {
                    BeginChargeActive();
                    return;
                }

                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    EnsureChargeDashAnimation();
                    BeginChargeActive();
                }

                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            if (ChargeAttack != null)
                ChargeAttack.EndCharge();

            SetChargeHitboxEnabled(false);
            chargeCooldownRemaining = CurrentPhase().chargeCooldown;
            EnsureChargeRecoveryAnimation();
            EnterRecovery(WaspQueenAbility.Charge, CurrentPhase().chargeRecoveryDuration);
        }

        void BeginChargeActive()
        {
            if (stateActionExecuted)
                return;

            if (ChargeAttack != null && Player != null)
            {
                Vector3 toPlayer = Player.transform.position - transform.position;
                ChargeAttack.BeginCharge(new Vector3(toPlayer.x, 0f, toPlayer.z));
            }

            stateActionExecuted = true;
            stateTimer = CurrentPhase().chargeDuration;
            SetChargeHitboxEnabled(true);
            PlayClip(chargeDashClip, AudioCue.ChargeDash, 0.08f);
            EnsureChargeDashAnimation();
        }

        // The Charge trigger only transitions from Idle. If the boss enters charge while another clip is
        // still playing, the trigger can sit unused and the FSM timer would start movement in Idle.
        void EnsureChargeSequenceStarted()
        {
            if (Animator == null || Animator.runtimeAnimatorController == null)
                return;

            if (IsInChargeAnimatorSequence())
                return;

            if (IsIdleAnimPlaying())
            {
                TriggerAnimator(ChargeHash);
                return;
            }

            if (!Animator.HasState(0, ChargeTelegraphStateHash))
            {
                Debug.LogWarning($"WaspQueenBoss: Animator controller has no '{ChargeTelegraphStateName}' state on layer 0; charge may desync.", this);
                return;
            }

            Animator.ResetTrigger(ChargeHash);
            Animator.Play(ChargeTelegraphStateHash, 0, 0f);
        }

        bool IsIdleAnimPlaying()
        {
            if (Animator == null)
                return false;

            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == IdleStateHash)
                return true;

            return Animator.IsInTransition(0) && Animator.GetNextAnimatorStateInfo(0).shortNameHash == IdleStateHash;
        }

        bool IsInChargeAnimatorSequence()
        {
            if (Animator == null)
                return false;

            int currentHash = Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            if (currentHash == ChargeTelegraphStateHash
                || currentHash == ChargeDashStateHash
                || currentHash == ChargeRecoveryStateHash)
            {
                return true;
            }

            if (!Animator.IsInTransition(0))
                return false;

            int nextHash = Animator.GetNextAnimatorStateInfo(0).shortNameHash;
            return nextHash == ChargeTelegraphStateHash
                || nextHash == ChargeDashStateHash
                || nextHash == ChargeRecoveryStateHash;
        }

        bool IsChargeDashAnimPlaying()
        {
            if (Animator == null)
                return true;

            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == ChargeDashStateHash)
                return true;

            return Animator.IsInTransition(0) && Animator.GetNextAnimatorStateInfo(0).shortNameHash == ChargeDashStateHash;
        }

        // Guarantees Charge_Dash is playing before code-driven movement runs. No-op when the autonomous
        // telegraph -> dash chain is already active so animation events keep their timing.
        void EnsureChargeDashAnimation()
        {
            if (Animator == null || Animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("WaspQueenBoss: Animator/controller missing; the code-driven charge dash will move with no Charge_Dash animation.", this);
                return;
            }

            if (IsChargeDashAnimPlaying())
                return;

            if (!Animator.HasState(0, ChargeDashStateHash))
            {
                Debug.LogWarning($"WaspQueenBoss: Animator controller has no '{ChargeDashStateName}' state on layer 0; the charge dash will move with no dash animation.", this);
                return;
            }

            Animator.ResetTrigger(ChargeHash);
            Animator.Play(ChargeDashStateHash, 0, 0f);
        }

        void EnsureChargeRecoveryAnimation()
        {
            if (Animator == null || Animator.runtimeAnimatorController == null)
                return;

            if (Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == ChargeRecoveryStateHash)
                return;

            if (Animator.IsInTransition(0) && Animator.GetNextAnimatorStateInfo(0).shortNameHash == ChargeRecoveryStateHash)
                return;

            if (!Animator.HasState(0, ChargeRecoveryStateHash))
                return;

            Animator.ResetTrigger(ChargeHash);
            Animator.Play(ChargeRecoveryStateHash, 0, 0f);
        }

        void TickStingLunge()
        {
            if (!stateActionExecuted)
            {
                FacePlayer();
                TickTelegraphedState(BeginStingActive);
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            if (ChargeAttack != null)
                ChargeAttack.EndCharge();

            SetChargeHitboxEnabled(false);
            stingCooldownRemaining = CurrentPhase().stingCooldown;
            EnterState(WaspQueenState.StingRetreat, Config != null ? Config.stingRetreatDuration : 0f);
        }

        void TickStingRetreat()
        {
            FacePlayer();
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                EnterRecovery(WaspQueenAbility.StingLunge, Config != null ? Config.stingRecoveryDuration : 0f);
        }

        bool MoveTowardReturnPosition(float speed)
        {
            if (Body == null)
                return true;

            Vector3 current = transform.position;
            Vector3 horizontal = new Vector3(stingReturnPosition.x - current.x, 0f, stingReturnPosition.z - current.z);
            float distance = horizontal.magnitude;
            const float reachThreshold = 0.75f;
            if (distance <= reachThreshold)
            {
                MaintainHoverHeight();
                return true;
            }

            float step = Mathf.Max(0f, speed) * Time.fixedDeltaTime;
            Vector3 next = step >= distance
                ? new Vector3(stingReturnPosition.x, current.y, stingReturnPosition.z)
                : current + horizontal.normalized * step;

            Vector3 resolved = ResolveHoverPosition(next);
            if (Body.isKinematic)
                Body.MovePosition(resolved);
            else
                Body.position = resolved;

            return step >= distance;
        }

        bool MoveTowardPosition(Vector3 target, float speed)
        {
            if (Body == null)
                return true;

            Vector3 current = transform.position;
            Vector3 horizontal = new Vector3(target.x - current.x, 0f, target.z - current.z);
            float distance = horizontal.magnitude;
            const float reachThreshold = 0.75f;
            if (distance <= reachThreshold)
            {
                MaintainHoverHeight();
                return true;
            }

            float step = Mathf.Max(0f, speed) * Time.fixedDeltaTime;
            Vector3 next = step >= distance
                ? new Vector3(target.x, current.y, target.z)
                : current + horizontal.normalized * step;

            Vector3 resolved = ResolveHoverPosition(next);
            if (Body.isKinematic)
                Body.MovePosition(resolved);
            else
                Body.position = resolved;

            return step >= distance;
        }

        Vector3 ArenaCenterPosition()
        {
            return arenaCenter != null ? arenaCenter.position : capturedArenaCenter;
        }

        float DistanceFromArenaCenter()
        {
            Vector3 center = ArenaCenterPosition();
            Vector3 offset = new Vector3(transform.position.x - center.x, 0f, transform.position.z - center.z);
            return offset.magnitude;
        }

        bool IsLeashInterruptible(WaspQueenState current)
        {
            switch (current)
            {
                case WaspQueenState.Idle:
                case WaspQueenState.Decision:
                case WaspQueenState.Recovery:
                case WaspQueenState.Reposition:
                    return true;
                default:
                    return false;
            }
        }

        bool ShouldLeash()
        {
            if (Config == null)
                return false;

            if (Config.leashRange > 0f && DistanceToPlayer() > Config.leashRange)
                return true;

            if (Config.arenaRadius > 0f && DistanceFromArenaCenter() > Config.arenaRadius)
                return true;

            return false;
        }

        void TickReturning()
        {
            FacePlayer();
            bool nearCenter = DistanceFromArenaCenter() <= Mathf.Max(1f, (Config != null ? Config.arenaRadius : 22f) * 0.5f);
            if (!nearCenter)
                return;

            bool playerInRange = Player != null && DistanceToPlayer() <= (Config != null ? Config.reengageRange : 20f);
            if (playerInRange || Player == null)
                EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f);
        }

        bool ShouldReposition()
        {
            if (Config == null)
                return false;

            if (DistanceFromArenaCenter() > Config.arenaRadius * 0.6f)
                return true;

            if (Player != null && DistanceToPlayer() < Config.repositionTooCloseRange)
                return true;

            return UnityEngine.Random.value < Config.repositionChance;
        }

        void EnterReposition()
        {
            repositionTarget = ClampToArena(ResolveRepositionTarget());
            EnterState(WaspQueenState.Reposition, Config != null ? Config.repositionDuration : 0.6f);
        }

        Vector3 ResolveRepositionTarget()
        {
            float step = Config != null ? Config.repositionStep : 4f;

            if (Config != null && DistanceFromArenaCenter() > Config.arenaRadius * 0.6f)
                return ArenaCenterPosition();

            if (Player != null && Config != null && DistanceToPlayer() < Config.repositionTooCloseRange)
            {
                Vector3 away = transform.position - Player.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < LookRotationEpsilon)
                    away = -transform.forward;
                return transform.position + away.normalized * step;
            }

            Vector3 toPlayer = Player != null ? Player.transform.position - transform.position : transform.forward;
            Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
            Vector3 perpendicular = Vector3.Cross(Vector3.up, flatToPlayer.sqrMagnitude > LookRotationEpsilon ? flatToPlayer.normalized : transform.forward);
            float direction = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            return transform.position + perpendicular * direction * step;
        }

        Vector3 ClampToArena(Vector3 position)
        {
            if (Config == null || Config.arenaRadius <= 0f)
                return position;

            Vector3 center = ArenaCenterPosition();
            Vector3 offset = new Vector3(position.x - center.x, 0f, position.z - center.z);
            float maxRadius = Mathf.Max(1f, Config.arenaRadius);
            if (offset.magnitude > maxRadius)
            {
                Vector3 clamped = center + offset.normalized * maxRadius;
                return new Vector3(clamped.x, position.y, clamped.z);
            }

            return position;
        }

        public void AnimEvent_FireProjectile()
        {
            ExecuteRangedRelease();
        }

        public void AnimEvent_ActivateAoE()
        {
            ExecuteAoeRelease();
        }

        public void AnimEvent_SummonWasps()
        {
            ExecuteSummonRelease();
        }

        public void AnimEvent_EnableChargeHitbox()
        {
            if (state != WaspQueenState.Charge || stateActionExecuted)
                return;

            EnsureChargeDashAnimation();
            BeginChargeActive();
        }

        public void AnimEvent_DisableChargeHitbox()
        {
            if (state == WaspQueenState.Charge && stateActionExecuted)
                SetChargeHitboxEnabled(false);
        }

        public void AnimEvent_PhasePulse()
        {
            // Cosmetic phase-pulse hook; gameplay phase change is timer-driven by the PhaseTransition state.
        }

        public void AnimEvent_QueenScream()
        {
            // Optional intro SFX cue; safe no-op if unused.
        }

        public void AnimEvent_ExplodeFragments()
        {
            if (deathHandled)
                SpawnDeathExplosionOnce();
        }

        void BeginStingActive()
        {
            if (ChargeAttack != null && Player != null)
            {
                Vector3 toPlayer = Player.transform.position - transform.position;
                ChargeAttack.BeginCharge(new Vector3(toPlayer.x, 0f, toPlayer.z));
            }

            stateActionExecuted = true;
            stateTimer = Config != null ? Config.stingActiveDuration : 0.9f;
            SetChargeHitboxEnabled(true);
            PlayClip(stingClip, AudioCue.Sting, 0.08f);
        }

        void EnterRecovery(WaspQueenAbility completedAbility, float duration)
        {
            TrackAbilityUsage(completedAbility);
            EnterState(WaspQueenState.Recovery, duration);
        }

        void TrackAbilityUsage(WaspQueenAbility completedAbility)
        {
            if (previousAbility == completedAbility)
                repeatedAbilityCount++;
            else
                repeatedAbilityCount = 0;

            previousAbility = completedAbility;
        }

        void UpdatePendingPhaseTransition()
        {
            if (Config == null || state == WaspQueenState.PhaseTransition || deathHandled)
                return;

            if (currentPhaseIndex == 0 && currentHealth <= Mathf.CeilToInt(Config.maxHealth * Config.phaseTwoHealthThresholdNormalized))
            {
                pendingPhaseIndex = 1;
                return;
            }

            if (currentPhaseIndex == 1 && currentHealth <= Mathf.CeilToInt(Config.maxHealth * Config.phaseThreeHealthThresholdNormalized))
                pendingPhaseIndex = 2;
        }

        void BeginPendingPhaseTransition()
        {
            if (pendingPhaseIndex < 0)
                return;

            currentPhaseIndex = Mathf.Clamp(pendingPhaseIndex, 0, 2);
            pendingPhaseIndex = -1;
            EnterState(WaspQueenState.PhaseTransition, Config != null ? Config.phaseTransitionDuration : 0f);
        }

        void FireProjectile()
        {
            if (Config == null || Config.poisonProjectilePrefab == null)
                return;

            Vector3 origin = ProjectileSpawnPoint != null ? ProjectileSpawnPoint.position : transform.position + Vector3.up;
            Vector3 targetPosition = Player != null ? Player.transform.position + Vector3.up : transform.position + transform.forward * 4f;
            Vector3 direction = targetPosition - origin;
            if (direction.sqrMagnitude <= LookRotationEpsilon)
                direction = transform.forward;

            TrimProjectileCap();
            Quaternion projectileRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            WaspQueenProjectile projectile = poolHub != null
                ? poolHub.SpawnProjectile(origin, projectileRotation)
                : Instantiate(Config.poisonProjectilePrefab, origin, projectileRotation);
            if (projectile == null) return;
            projectile.Activate(
                origin,
                projectileRotation,
                direction,
                Player,
                CurrentPhase().rangedDamage,
                CurrentPhase().projectileSpeed,
                Mathf.Max(1.5f, Config.farRange / Mathf.Max(1f, CurrentPhase().projectileSpeed)));
            activeProjectiles.Add(projectile);
            PlayClip(rangedFireClip, AudioCue.RangedFire, 0.1f);
        }

        void SpawnPoisonZone()
        {
            if (Config == null || Config.poisonZonePrefab == null)
                return;

            TrimPoisonZoneCap();
            WaspQueenPoisonZone poisonZone = poolHub != null
                ? poolHub.SpawnPoisonZone(cachedAoeTargetPosition)
                : Instantiate(Config.poisonZonePrefab, cachedAoeTargetPosition, Quaternion.identity);
            if (poisonZone == null) return;
            poisonZone.Activate(
                cachedAoeTargetPosition,
                CurrentPhase().aoeRadius,
                CurrentPhase().aoeGroundTelegraphTime,
                CurrentPhase().aoeDuration,
                CurrentPhase().aoeDamage,
                CurrentPhase().aoeTickRate,
                Player);
            activePoisonZones.Add(poisonZone);
        }

        void SpawnSummonedWasps()
        {
            if (Config == null || Config.waspPrefab == null)
                return;

            PurgeDestroyedSummons();

            int missingCapacity = Mathf.Max(0, CurrentPhase().maxActiveSummonedWasps - activeSummonedWasps.Count);
            int spawnCount = Mathf.Min(CurrentPhase().waspsPerSummon, missingCapacity);
            for (int index = 0; index < spawnCount; index++)
            {
                Transform spawnPoint = ResolveWaspSpawnPoint(index);
                Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position + transform.right * (index - (spawnCount * 0.5f));
                Vector3 lookDirection = Player != null ? Player.transform.position - position : transform.forward;
                Vector3 horizontalLookDirection = new Vector3(lookDirection.x, 0f, lookDirection.z);
                Quaternion rotation = horizontalLookDirection.sqrMagnitude > LookRotationEpsilon
                    ? Quaternion.LookRotation(horizontalLookDirection.normalized, Vector3.up)
                    : transform.rotation;

                GameObject wasp = Instantiate(Config.waspPrefab, position, rotation);
                ConfigureSummonedWasp(wasp);
                activeSummonedWasps.Add(wasp);
            }
        }

        void ConfigureSummonedWasp(GameObject wasp)
        {
            if (wasp == null)
                return;

            // Marker makes NPC_Basic.Death() spawn a couple of short-lived debris and no currency drop,
            // so the arena does not fill with corpses. World wasps are unaffected.
            if (wasp.GetComponent<BossSummonedWasp>() == null)
                wasp.AddComponent<BossSummonedWasp>();
        }

        Transform ResolveWaspSpawnPoint(int index)
        {
            if (WaspSpawnPoints == null || WaspSpawnPoints.Length == 0)
                return null;

            return WaspSpawnPoints[index % WaspSpawnPoints.Length];
        }

        Vector3 ResolveAoeTargetPosition()
        {
            Vector3 candidate;
            if (Player != null)
                candidate = Player.transform.position;
            else if (AoeOrigin != null)
                candidate = AoeOrigin.position;
            else
                candidate = transform.position;

            return SnapToGround(candidate);
        }

        Vector3 SnapToGround(Vector3 position)
        {
            if (Config == null)
                return position;

            Vector3 rayOrigin = position + Vector3.up * Mathf.Max(0.1f, Config.groundCheckStartHeight);
            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                groundSnapHits,
                Config.groundCheckDistance,
                Config.groundMask,
                QueryTriggerInteraction.Ignore);

            // The ray starts above the player, so it can hit the player's own colliders
            // (Character layer is in the production groundMask). Skip those hits.
            Transform playerTransform = Player != null ? Player.transform : null;
            float closestDistance = float.MaxValue;
            float groundY = 0f;
            bool foundGround = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundSnapHits[i];
                if (playerTransform != null && hit.transform != null && hit.transform.IsChildOf(playerTransform))
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    groundY = hit.point.y;
                    foundGround = true;
                }
            }

            if (foundGround)
                return new Vector3(position.x, groundY + 0.02f, position.z);

            return position;
        }

        void TickCooldowns()
        {
            rangedCooldownRemaining = Mathf.Max(0f, rangedCooldownRemaining - Time.deltaTime);
            aoeCooldownRemaining = Mathf.Max(0f, aoeCooldownRemaining - Time.deltaTime);
            chargeCooldownRemaining = Mathf.Max(0f, chargeCooldownRemaining - Time.deltaTime);
            summonCooldownRemaining = Mathf.Max(0f, summonCooldownRemaining - Time.deltaTime);
            stingCooldownRemaining = Mathf.Max(0f, stingCooldownRemaining - Time.deltaTime);
        }

        void TickTimedState(Action onComplete)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                onComplete?.Invoke();
        }

        void TickTelegraphedState(Action onExecute)
        {
            if (stateActionExecuted)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            stateActionExecuted = true;
            onExecute?.Invoke();
        }

        void EnterState(WaspQueenState nextState, float duration)
        {
            state = nextState;
            stateTimer = Mathf.Max(0f, duration);
            stateActionExecuted = false;
            OnStateEntered(nextState);
        }

        void OnStateEntered(WaspQueenState nextState)
        {
            switch (nextState)
            {
                case WaspQueenState.Intro:
                    TriggerAnimator(IntroHash);
                    PlayClip(introClip, AudioCue.Intro, 0.25f);
                    break;
                case WaspQueenState.RangedPoisonShot:
                    TriggerAnimator(RangedAttackHash);
                    PlayClip(rangedTelegraphClip, AudioCue.RangedTelegraph, 0.08f);
                    break;
                case WaspQueenState.PoisonAoE:
                    TriggerAnimator(PoisonAoeHash);
                    PlayClip(aoeTelegraphClip, AudioCue.AoeTelegraph, 0.08f);
                    break;
                case WaspQueenState.Charge:
                    PlayClip(chargeTelegraphClip, AudioCue.ChargeTelegraph, 0.08f);
                    SetChargeHitboxEnabled(false);
                    EnsureChargeSequenceStarted();
                    break;
                case WaspQueenState.StingLunge:
                    stingReturnPosition = transform.position;
                    TriggerAnimator(StingHash);
                    PlayClip(stingClip, AudioCue.Sting, 0.08f);
                    SetChargeHitboxEnabled(false);
                    break;
                case WaspQueenState.SummonWasps:
                    TriggerAnimator(SummonHash);
                    PlayClip(summonClip, AudioCue.Summon, 0.08f);
                    break;
                case WaspQueenState.PhaseTransition:
                    TriggerAnimator(PhaseTransitionHash);
                    PlayClip(phaseTransitionClip, AudioCue.PhaseTransition, 0.12f);
                    SetChargeHitboxEnabled(false);
                    break;
                case WaspQueenState.Returning:
                    SetChargeHitboxEnabled(false);
                    if (ChargeAttack != null)
                        ChargeAttack.EndCharge();
                    CleanupRuntimeObjects();   // minions disengage / hazards cleared while leashing home
                    break;
                case WaspQueenState.Death:
                    TriggerAnimator(DieHash);
                    PlayClip(deathClip, AudioCue.Death, 0.12f);
                    SetChargeHitboxEnabled(false);
                    break;
            }
        }

        void FacePlayer()
        {
            if (Player == null)
                return;

            Vector3 toPlayer = Player.transform.position - transform.position;
            Vector3 horizontal = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (horizontal.sqrMagnitude <= LookRotationEpsilon)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
        }

        void SetChargeHitboxEnabled(bool enabled)
        {
            if (ChargeHitbox != null)
                ChargeHitbox.enabled = enabled;
        }

        void HandleDeath()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            explosionSpawned = false;
            StopMovement();
            HideHitEffects();
            SetChargeHitboxEnabled(false);
            if (ChargeAttack != null)
                ChargeAttack.EndCharge();

            state = WaspQueenState.Death;
            OnStateEntered(WaspQueenState.Death);   // triggers Die + plays WQ_Death
            CleanupRuntimeObjects();                 // clear summons + hazards immediately
            Defeated?.Invoke(this);
            genericDefeated?.Invoke(this);

            if (isActiveAndEnabled)
            {
                deathRoutine = StartCoroutine(DeathSequence());
            }
            else
            {
                // Cannot run the death animation while disabled; present and disable immediately.
                SpawnDeathExplosionOnce();
                gameObject.SetActive(false);
            }
        }

        IEnumerator DeathSequence()
        {
            float disableDelay = deathDisableDelay > 0f ? deathDisableDelay : DefaultDeathClipLength;
            if (Config != null)
                disableDelay = Mathf.Max(disableDelay, Config.victoryDelay);

            // The explosion normally fires from AnimEvent_ExplodeFragments (~87% of WQ_Death). This is the
            // safety fallback so fragments still spawn once if that event is missing.
            float explosionFallback = Mathf.Max(0f, disableDelay - 0.2f);
            float elapsed = 0f;
            while (elapsed < disableDelay)
            {
                if (!explosionSpawned && elapsed >= explosionFallback)
                    SpawnDeathExplosionOnce();

                elapsed += Time.deltaTime;
                yield return null;
            }

            SpawnDeathExplosionOnce();
            deathRoutine = null;
            gameObject.SetActive(false);
        }

        void SpawnDeathExplosionOnce()
        {
            if (explosionSpawned)
                return;

            explosionSpawned = true;
            SpawnDeathPresentation();
        }

        void CleanupRuntimeObjects()
        {
            for (int index = activeSummonedWasps.Count - 1; index >= 0; index--)
            {
                if (activeSummonedWasps[index] != null)
                    Destroy(activeSummonedWasps[index]);
            }

            activeSummonedWasps.Clear();

            for (int index = activeProjectiles.Count - 1; index >= 0; index--)
            {
                if (activeProjectiles[index] != null)
                    activeProjectiles[index].Deactivate();
            }

            activeProjectiles.Clear();

            for (int index = activePoisonZones.Count - 1; index >= 0; index--)
            {
                if (activePoisonZones[index] != null)
                    activePoisonZones[index].Deactivate();
            }

            activePoisonZones.Clear();
        }

        void SpawnDeathPresentation()
        {
            Vector3 spawnPosition = transform.position + Vector3.up;
            if (Config != null && Config.deathExplosionPrefab != null)
                PooledOneShotVfx.Spawn(Config.deathExplosionPrefab, spawnPosition, transform.rotation);

            if (Config == null || Config.fragmentPrefabs == null)
                return;

            for (int index = 0; index < Config.fragmentPrefabs.Length; index++)
            {
                if (Config.fragmentPrefabs[index] != null)
                    PooledDeathDebris.Spawn(Config.fragmentPrefabs[index], spawnPosition, transform.rotation);
            }
        }

        void PurgeDestroyedSummons()
        {
            for (int index = activeSummonedWasps.Count - 1; index >= 0; index--)
            {
                if (activeSummonedWasps[index] == null)
                    activeSummonedWasps.RemoveAt(index);
            }
        }

        void PurgeInactiveHazards()
        {
            for (int index = activeProjectiles.Count - 1; index >= 0; index--)
            {
                if (activeProjectiles[index] == null || !activeProjectiles[index].IsActive)
                    activeProjectiles.RemoveAt(index);
            }

            for (int index = activePoisonZones.Count - 1; index >= 0; index--)
            {
                if (activePoisonZones[index] == null || !activePoisonZones[index].IsActive)
                    activePoisonZones.RemoveAt(index);
            }
        }

        void TrimProjectileCap()
        {
            PurgeInactiveHazards();
            int maxActive = Config != null ? Mathf.Max(1, Config.maxActiveProjectiles) : 1;
            while (activeProjectiles.Count >= maxActive)
            {
                if (activeProjectiles[0] != null)
                    activeProjectiles[0].Deactivate();

                activeProjectiles.RemoveAt(0);
            }
        }

        void TrimPoisonZoneCap()
        {
            PurgeInactiveHazards();
            int maxActive = Config != null ? Mathf.Max(1, Config.maxActivePoisonZones) : 1;
            while (activePoisonZones.Count >= maxActive)
            {
                if (activePoisonZones[0] != null)
                    activePoisonZones[0].Deactivate();

                activePoisonZones.RemoveAt(0);
            }
        }

        void ResetCombatState()
        {
            currentPhaseIndex = 0;
            pendingPhaseIndex = -1;
            currentHealth = Config != null ? Mathf.Max(1, Config.maxHealth) : 1;
            rangedCooldownRemaining = 0f;
            aoeCooldownRemaining = 0f;
            chargeCooldownRemaining = 0f;
            summonCooldownRemaining = 0f;
            stingCooldownRemaining = 0f;
            previousAbility = WaspQueenAbility.None;
            repeatedAbilityCount = 0;
            stateTimer = 0f;
            stateActionExecuted = false;
            deathHandled = false;
            explosionSpawned = false;
            ApplyHealthBarState();
        }

        void ApplyHealthBarState()
        {
            if (HealthBar == null)
                return;

            HealthBar.SetMaxNPCHealth(Config != null ? Mathf.Max(1, Config.maxHealth) : currentHealth);
            HealthBar.SetNPCHealth(currentHealth);
        }

        void ResolvePlayerReference(bool force)
        {
            if (Player != null)
                return;

            if (!force && Time.time < nextPlayerResolveTime)
                return;

            nextPlayerResolveTime = Time.time + PlayerResolveRetryDelay;
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                Player = playerObject.GetComponent<BeaverPlayerBehaviour>();
        }

        WaspQueenConfig.PhaseSettings CurrentPhase()
        {
            if (Config == null)
                return null;

            switch (currentPhaseIndex)
            {
                case 2:
                    return Config.phase3;
                case 1:
                    return Config.phase2;
                default:
                    return Config.phase1;
            }
        }

        float DistanceToPlayer()
        {
            return Player != null ? Vector3.Distance(transform.position, Player.transform.position) : float.PositiveInfinity;
        }

        Vector3 ResolveHoverPosition(Vector3 desiredPosition)
        {
            if (Config == null)
                return desiredPosition;

            Vector3 rayOrigin = desiredPosition + Vector3.up * Mathf.Max(0.1f, Config.groundCheckStartHeight);
            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    Config.groundCheckDistance,
                    Config.groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return new Vector3(desiredPosition.x, hit.point.y + Config.hoverHeight, desiredPosition.z);
            }

            return desiredPosition;
        }

        void MaintainHoverHeight()
        {
            if (Body == null)
                return;

            Vector3 resolved = ResolveHoverPosition(transform.position);
            if (Body.isKinematic)
                Body.MovePosition(resolved);
            else
                Body.position = resolved;
        }

        bool ShouldAutoActivate()
        {
            if (Config == null || Player == null)
                return false;

            return DistanceToPlayer() <= Mathf.Max(0f, Config.activateRange);
        }

        void StopMovement()
        {
            if (Body == null)
                return;

            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }

        void CacheReferences()
        {
            if (Body == null)
                Body = GetComponent<Rigidbody>();

            if (!arenaCenterCaptured)
            {
                capturedArenaCenter = arenaCenter != null ? arenaCenter.position : transform.position;
                arenaCenterCaptured = true;
            }

            if (Animator == null)
                Animator = GetComponentInChildren<Animator>();

            if (HealthBar == null)
                HealthBar = GetComponent<NPC_Health>();

            if (ChargeAttack == null)
            {
                ChargeAttack = GetComponent<WaspQueenChargeAttack>();
                if (ChargeAttack == null)
                    ChargeAttack = gameObject.AddComponent<WaspQueenChargeAttack>();
            }

            if (AudioSource == null)
            {
                AudioSource = GetComponent<AudioSource>();
                if (AudioSource == null)
                    AudioSource = gameObject.AddComponent<AudioSource>();
            }

            AudioSourceRouting.EnsureRoute(AudioSource, AudioSourceRoute.Enemy);

            healthBarVisibility = GetComponent<EnemyHealthBarVisibility>();
            if (healthBarVisibility == null)
                healthBarVisibility = gameObject.AddComponent<EnemyHealthBarVisibility>();

            if (poolHub == null)
                poolHub = GetComponent<WaspQueenPoolHub>();

            if (poolHub != null)
                poolHub.Initialize(Config);

            HideHitEffects();
        }

        void ShowHitEffect()
        {
            GameObject effect = ResolveHitEffect();
            if (effect == null)
                return;

            if (hitEffectHideRoutine != null)
                StopCoroutine(hitEffectHideRoutine);

            if (slashEffect != null)
                slashEffect.SetActive(false);
            if (hitEffect != null)
                hitEffect.SetActive(false);

            effect.SetActive(true);
            if (isActiveAndEnabled)
                hitEffectHideRoutine = StartCoroutine(HideHitEffectAfterDelay(effect));
        }

        GameObject ResolveHitEffect()
        {
            return IsSwordDamageSource() && slashEffect != null ? slashEffect : hitEffect;
        }

        bool IsSwordDamageSource()
        {
            if (Player == null || Player.Arsenal == null)
                return false;

            int index = Player.arsenalBrowser;
            if (index < 0 || index >= Player.Arsenal.Count)
                return false;

            return Player.Arsenal[index] == "ArmorSet";
        }

        void HideHitEffects()
        {
            if (hitEffectHideRoutine != null)
            {
                StopCoroutine(hitEffectHideRoutine);
                hitEffectHideRoutine = null;
            }

            if (slashEffect != null)
                slashEffect.SetActive(false);
            if (hitEffect != null)
                hitEffect.SetActive(false);
        }

        IEnumerator HideHitEffectAfterDelay(GameObject effect)
        {
            yield return new WaitForSeconds(Mathf.Max(0.02f, hitEffectVisibleDuration));
            if (effect != null)
                effect.SetActive(false);
            hitEffectHideRoutine = null;
        }

        void TriggerAnimator(int triggerHash)
        {
            if (Animator == null)
                return;

            if (!HasAnimatorTrigger(triggerHash))
            {
                if (missingAnimatorTriggerWarnings.Add(triggerHash))
                {
                    Debug.LogWarning(
                        $"WaspQueenBoss: Animator controller missing trigger parameter '{AnimatorTriggerName(triggerHash)}'.",
                        this);
                }

                return;
            }

            Animator.SetTrigger(triggerHash);
        }

        bool HasAnimatorTrigger(int triggerHash)
        {
            if (Animator.runtimeAnimatorController == null)
                return false;

            AnimatorControllerParameter[] parameters = Animator.parameters;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].type == AnimatorControllerParameterType.Trigger
                    && parameters[index].nameHash == triggerHash)
                {
                    return true;
                }
            }

            return false;
        }

        static string AnimatorTriggerName(int triggerHash)
        {
            string triggerName;
            return AnimatorTriggerNamesByHash.TryGetValue(triggerHash, out triggerName)
                ? triggerName
                : triggerHash.ToString();
        }

        void PlayClip(AudioClip clip, AudioCue cue, float minInterval, float volume = 1f)
        {
            if (AudioSource == null || clip == null)
                return;

            int index = (int)cue;
            if (Time.time - lastAudioPlayTimes[index] < minInterval)
                return;

            lastAudioPlayTimes[index] = Time.time;
            AudioSource.PlayOneShot(clip, volume);
        }
    }
}
