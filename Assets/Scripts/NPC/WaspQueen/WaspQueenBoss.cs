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

        readonly List<GameObject> activeSummonedWasps = new List<GameObject>(8);
        readonly List<WaspQueenProjectile> activeProjectiles = new List<WaspQueenProjectile>(4);
        readonly List<WaspQueenPoisonZone> activePoisonZones = new List<WaspQueenPoisonZone>(4);
        readonly float[] lastAudioPlayTimes = new float[11];

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
        Coroutine hitEffectHideRoutine;

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
            TickState();
        }

        void FixedUpdate()
        {
            if (deathHandled || state == WaspQueenState.Inactive)
                return;

            if (state == WaspQueenState.Charge && stateActionExecuted)
            {
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
            else
            {
                // Cosmetic flinch only on surviving hits; the lethal blow plays the death animation instead.
                TriggerAnimator(HitHash);
            }

            return true;
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

                        EnterState(WaspQueenState.Idle, Config != null ? Config.idleDecisionDelay : 0f);
                    });
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
                    EnterState(WaspQueenState.RangedPoisonShot, CurrentPhase().rangedTelegraphDuration);
                    break;
                case WaspQueenAbility.PoisonAoE:
                    cachedAoeTargetPosition = ResolveAoeTargetPosition();
                    EnterState(WaspQueenState.PoisonAoE, CurrentPhase().aoeTelegraphDuration);
                    break;
                case WaspQueenAbility.SummonWasps:
                    EnterState(WaspQueenState.SummonWasps, CurrentPhase().summonTelegraphDuration);
                    break;
                case WaspQueenAbility.Charge:
                    EnterState(WaspQueenState.Charge, CurrentPhase().chargeTelegraphDuration);
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
            TickTelegraphedState(() =>
            {
                FireProjectile();
                rangedCooldownRemaining = CurrentPhase().rangedCooldown;
                EnterRecovery(WaspQueenAbility.RangedPoisonShot, CurrentPhase().rangedRecoveryDuration);
            });
        }

        void TickPoisonAoe()
        {
            FacePlayer();
            TickTelegraphedState(() =>
            {
                SpawnPoisonZone();
                aoeCooldownRemaining = CurrentPhase().aoeCooldown;
                EnterRecovery(WaspQueenAbility.PoisonAoE, CurrentPhase().aoeRecoveryDuration);
            });
        }

        void TickSummon()
        {
            FacePlayer();
            TickTelegraphedState(() =>
            {
                SpawnSummonedWasps();
                summonCooldownRemaining = CurrentPhase().summonCooldown;
                EnterRecovery(WaspQueenAbility.SummonWasps, CurrentPhase().summonRecoveryDuration);
            });
        }

        void TickCharge()
        {
            if (!stateActionExecuted)
            {
                FacePlayer();
                TickTelegraphedState(BeginChargeActive);
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            if (ChargeAttack != null)
                ChargeAttack.EndCharge();

            SetChargeHitboxEnabled(false);
            chargeCooldownRemaining = CurrentPhase().chargeCooldown;
            EnterRecovery(WaspQueenAbility.Charge, CurrentPhase().chargeRecoveryDuration);
        }

        void BeginChargeActive()
        {
            if (ChargeAttack != null && Player != null)
            {
                Vector3 toPlayer = Player.transform.position - transform.position;
                ChargeAttack.BeginCharge(new Vector3(toPlayer.x, 0f, toPlayer.z));
            }

            stateActionExecuted = true;
            stateTimer = CurrentPhase().chargeDuration;
            SetChargeHitboxEnabled(true);
            PlayClip(chargeDashClip, AudioCue.ChargeDash, 0.08f);
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
            WaspQueenProjectile projectile = Instantiate(
                Config.poisonProjectilePrefab,
                origin,
                Quaternion.LookRotation(direction.normalized, Vector3.up));
            projectile.Activate(
                origin,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
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
            WaspQueenPoisonZone poisonZone = Instantiate(Config.poisonZonePrefab, cachedAoeTargetPosition, Quaternion.identity);
            poisonZone.Activate(
                cachedAoeTargetPosition,
                CurrentPhase().aoeRadius,
                0f,
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
                activeSummonedWasps.Add(wasp);
            }
        }

        Transform ResolveWaspSpawnPoint(int index)
        {
            if (WaspSpawnPoints == null || WaspSpawnPoints.Length == 0)
                return null;

            return WaspSpawnPoints[index % WaspSpawnPoints.Length];
        }

        Vector3 ResolveAoeTargetPosition()
        {
            if (Player != null)
                return Player.transform.position;

            if (AoeOrigin != null)
                return AoeOrigin.position;

            return transform.position;
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
                    TriggerAnimator(ChargeHash);
                    PlayClip(chargeTelegraphClip, AudioCue.ChargeTelegraph, 0.08f);
                    SetChargeHitboxEnabled(false);
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
            StopMovement();
            HideHitEffects();
            state = WaspQueenState.Death;
            OnStateEntered(WaspQueenState.Death);
            CleanupRuntimeObjects();
            SpawnDeathPresentation();
            Defeated?.Invoke(this);
            genericDefeated?.Invoke(this);
            gameObject.SetActive(false);
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
            if (Animator != null)
                Animator.SetTrigger(triggerHash);
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
