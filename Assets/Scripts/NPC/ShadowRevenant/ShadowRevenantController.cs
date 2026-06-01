using Beavermania.Data.NPC;
using Beavermania.Display;
using Beavermania.Objects;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantController : MonoBehaviour, IEnemyDamageReceiver, ILightBreakReactive
    {
        const float DirectionEpsilon = 0.0001f;

        static readonly int PhasedHash = Animator.StringToHash("Phased");
        static readonly int AttackHash = Animator.StringToHash("Attack");
        static readonly int StaggerHash = Animator.StringToHash("Stagger");
        static readonly int SummonHash = Animator.StringToHash("Summon");
        static readonly int DeadHash = Animator.StringToHash("Dead");

        [SerializeField] ShadowRevenantConfig config;
        [SerializeField] Transform targetTransform;
        [SerializeField] Transform projectileMuzzle;
        [SerializeField] Transform fogSpawnAnchor;
        [SerializeField] Transform[] shadeSpawnPoints;
        [SerializeField] Rigidbody body;
        [SerializeField] Animator animator;
        [SerializeField] NPC_Health healthBar;
        [SerializeField] ShadowRevenantPoolHub poolHub;
        [SerializeField] Collider[] phaseDisabledColliders;
        [SerializeField] Light projectileMuzzleGlow;
        [SerializeField] GameObject[] summonSigilVisuals;
        [SerializeField] Transform bossVisualRoot;
        [SerializeField] Color healthBarLightBreakFillColor = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField] bool enableDebugLogs;

        Image healthBarFillImage;
        Color healthBarDefaultFillColor = Color.white;
        bool healthBarFillColorCached;

        enum StrafeMode
        {
            Orbit = 0,
            TowardTarget = 1,
            AwayFromFog = 2
        }

        IShadowRevenantTarget target;
        ShadowRevenantState state = ShadowRevenantState.Dormant;
        ShadowRevenantState previousState;
        ShadowRevenantDreadFogZone pendingFogCast;
        StrafeMode strafeMode = StrafeMode.Orbit;
        int currentHealth;
        float stateTimer;
        float projectileCooldownRemaining;
        float fogCooldownRemaining;
        float summonCooldownRemaining;
        float phaseCooldownRemaining;
        float lightBrokenRemaining;
        bool deathHandled;
        bool teleportApplied;
        float spawnAnchorY;
        EnemyHealthBarVisibility healthBarVisibility;

        public ShadowRevenantState State => state;
        public int CurrentHealth => currentHealth;

        void Awake()
        {
            CacheReferences();
            ConfigureRigidbodyForHover();
            if (poolHub != null)
                poolHub.Initialize(config);
        }

        void Start()
        {
            spawnAnchorY = transform.position.y;
            ResolveTarget();
            ResetHealth();
            EnterState(ShadowRevenantState.Dormant, 0f);
            MaintainHoverHeight();
            spawnAnchorY = transform.position.y;
        }

        void OnEnable()
        {
            deathHandled = false;
        }

        void Update()
        {
            if (state == ShadowRevenantState.Dead || deathHandled)
                return;

            TickCooldowns();
            TickState();
        }

        void FixedUpdate()
        {
            if (state == ShadowRevenantState.Dead || deathHandled)
                return;

            if (state == ShadowRevenantState.Strafe)
                StrafeAroundTarget();

            MaintainHoverHeight();
        }

        void CacheReferences()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (poolHub == null)
                poolHub = GetComponent<ShadowRevenantPoolHub>();

            if (projectileMuzzleGlow == null && projectileMuzzle != null)
                projectileMuzzleGlow = projectileMuzzle.GetComponentInChildren<Light>(true);

            healthBarVisibility = GetComponent<EnemyHealthBarVisibility>();
            if (healthBarVisibility == null)
                healthBarVisibility = gameObject.AddComponent<EnemyHealthBarVisibility>();

            CacheHealthBarFillColor();

            if (bossVisualRoot == null)
            {
                Transform visual = transform.Find("Visual");
                if (visual != null)
                    bossVisualRoot = visual;
            }
        }

        void CacheHealthBarFillColor()
        {
            if (healthBarFillColorCached || healthBar == null || healthBar.NPCslider == null)
                return;

            Transform fillRect = healthBar.NPCslider.fillRect;
            if (fillRect == null)
                return;

            healthBarFillImage = fillRect.GetComponent<Image>();
            if (healthBarFillImage == null)
                return;

            healthBarDefaultFillColor = healthBarFillImage.color;
            healthBarFillColorCached = true;
        }

        void SetHealthBarLightBreakAccent(bool enabled)
        {
            CacheHealthBarFillColor();
            if (healthBarFillImage == null)
                return;

            healthBarFillImage.color = enabled ? healthBarLightBreakFillColor : healthBarDefaultFillColor;
        }

        void ResetHealth()
        {
            currentHealth = config != null ? Mathf.Max(1, config.maxHealth) : 1;
            if (healthBar != null)
                healthBar.SetMaxNPCHealth(currentHealth);
        }

        void ResolveTarget()
        {
            if (targetTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    targetTransform = playerObject.transform;
            }

            target = targetTransform != null ? targetTransform.GetComponentInParent<IShadowRevenantTarget>() : null;
            if (target == null && targetTransform != null)
                target = targetTransform.GetComponentInChildren<IShadowRevenantTarget>();
        }

        void TickCooldowns()
        {
            projectileCooldownRemaining = Mathf.Max(0f, projectileCooldownRemaining - Time.deltaTime);
            fogCooldownRemaining = Mathf.Max(0f, fogCooldownRemaining - Time.deltaTime);
            summonCooldownRemaining = Mathf.Max(0f, summonCooldownRemaining - Time.deltaTime);
            phaseCooldownRemaining = Mathf.Max(0f, phaseCooldownRemaining - Time.deltaTime);

            if (lightBrokenRemaining > 0f)
                lightBrokenRemaining = Mathf.Max(0f, lightBrokenRemaining - Time.deltaTime);
        }

        void TickState()
        {
            stateTimer -= Time.deltaTime;

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            if (config == null || target == null || target.TargetTransform == null)
                return;

            switch (state)
            {
                case ShadowRevenantState.Dormant:
                    TickDormant();
                    break;
                case ShadowRevenantState.Idle:
                case ShadowRevenantState.Strafe:
                    TickDecisionState();
                    break;
                case ShadowRevenantState.ProjectileWindup:
                    TickProjectileWindup();
                    break;
                case ShadowRevenantState.ProjectileRecover:
                    TickRecover(ShadowRevenantState.Strafe);
                    break;
                case ShadowRevenantState.FogWindup:
                    TickFogWindup();
                    break;
                case ShadowRevenantState.FogRecover:
                    TickRecover(ShadowRevenantState.Strafe);
                    break;
                case ShadowRevenantState.SummonWindup:
                    TickSummonWindup();
                    break;
                case ShadowRevenantState.SummonRecover:
                    TickRecover(ShadowRevenantState.Strafe);
                    break;
                case ShadowRevenantState.PhaseShiftEnter:
                    TickPhaseShiftEnter();
                    break;
                case ShadowRevenantState.Phased:
                    TickPhased();
                    break;
                case ShadowRevenantState.LightBroken:
                case ShadowRevenantState.Staggered:
                    TickLightBroken();
                    break;
                case ShadowRevenantState.Teleport:
                    EnterState(ShadowRevenantState.Phased, config.phaseDuration);
                    break;
            }
        }

        void TickDormant()
        {
            if (DistanceToTarget() <= config.aggroRange)
                EnterState(ShadowRevenantState.Idle, 0.5f);
        }

        void TickDecisionState()
        {
            FaceTarget();

            float distance = DistanceToTarget();
            if (distance > config.leashRange)
            {
                EnterState(ShadowRevenantState.Dormant, 0f);
                return;
            }

            strafeMode = ResolveStrafeMode(distance);

            if (TryStartPhase(distance))
                return;

            if (poolHub != null && poolHub.ActiveShadeCount > 0)
            {
                if (TryStartFog(distance))
                    return;

                if (TryStartProjectile(distance))
                    return;
            }
            else
            {
                if (TryStartSummon())
                    return;

                if (TryStartFog(distance))
                    return;

                if (TryStartProjectile(distance))
                    return;
            }

            if (state != ShadowRevenantState.Strafe)
                EnterState(ShadowRevenantState.Strafe, 0f);
        }

        StrafeMode ResolveStrafeMode(float distance)
        {
            if (poolHub != null && target != null && poolHub.IsTargetInsideActiveFog(target)
                && IsBossNearActiveFogCenter())
            {
                return StrafeMode.AwayFromFog;
            }

            if (poolHub != null && poolHub.ActiveShadeCount > 0 && distance <= config.closeRange)
                return StrafeMode.AwayFromFog;

            if (distance > config.mediumRange)
                return StrafeMode.TowardTarget;

            return StrafeMode.Orbit;
        }

        bool IsBossNearActiveFogCenter()
        {
            if (poolHub == null || config == null || target == null || target.TargetTransform == null)
                return false;

            if (!poolHub.IsTargetInsideActiveFog(target))
                return false;

            Vector3 delta = transform.position - target.TargetTransform.position;
            delta.y = 0f;
            float maxDistance = config.fogRadius * 1.25f;
            return delta.sqrMagnitude <= maxDistance * maxDistance;
        }

        bool TryStartPhase(float distance)
        {
            if (phaseCooldownRemaining > 0f || lightBrokenRemaining > 0f)
                return false;

            bool inCloseRange = distance <= config.closeRange;
            if (config.preferPhaseWhenClose && inCloseRange && distance <= config.phaseTriggerRange)
            {
                EnterState(ShadowRevenantState.PhaseShiftEnter, config.phaseWindup);
                phaseCooldownRemaining = config.phaseCooldown;
                return true;
            }

            if (!inCloseRange || distance > config.phaseTriggerRange)
                return false;

            EnterState(ShadowRevenantState.PhaseShiftEnter, config.phaseWindup);
            phaseCooldownRemaining = config.phaseCooldown;
            return true;
        }

        bool TryStartProjectile(float distance)
        {
            if (projectileCooldownRemaining > 0f || distance > config.projectileRange)
                return false;

            if (distance < config.closeRange * 0.75f)
                return false;

            EnterState(ShadowRevenantState.ProjectileWindup, config.projectileWindup);
            return true;
        }

        bool TryStartFog(float distance)
        {
            if (fogCooldownRemaining > 0f || distance > config.fogRange)
                return false;

            if (distance < config.closeRange * 0.5f)
                return false;

            if (poolHub != null)
            {
                if (poolHub.ActiveFogCount >= config.fogMaxActive)
                    return false;

                if (target != null && poolHub.IsTargetInsideActiveFog(target))
                    return false;
            }

            if (distance < config.closeRange || distance > config.mediumRange * 1.35f)
                return false;

            EnterState(ShadowRevenantState.FogWindup, config.fogWindup);
            return true;
        }

        bool TryStartSummon()
        {
            if (summonCooldownRemaining > 0f || poolHub == null || config.maxActiveMinions <= 0)
                return false;

            if (poolHub.ActiveShadeCount >= config.maxActiveMinions)
                return false;

            EnterState(ShadowRevenantState.SummonWindup, config.summonWindup);
            return true;
        }

        void TickProjectileWindup()
        {
            FaceTarget();
            if (stateTimer > 0f)
                return;

            FireProjectile();
            projectileCooldownRemaining = config.projectileCooldown;
            EnterState(ShadowRevenantState.ProjectileRecover, config.projectileRecover);
        }

        void TickFogWindup()
        {
            FaceTarget();
            if (stateTimer > 0f)
                return;

            ActivatePendingFogDamage();
            fogCooldownRemaining = config.fogCooldown;
            EnterState(ShadowRevenantState.FogRecover, config.fogRecover);
        }

        void TickSummonWindup()
        {
            FaceTarget();
            if (stateTimer > 0f)
                return;

            SpawnShades();
            summonCooldownRemaining = config.summonCooldown;
            EnterState(ShadowRevenantState.SummonRecover, config.summonRecover);
        }

        void TickPhaseShiftEnter()
        {
            FaceTarget();
            if (stateTimer > 0f)
                return;

            EnterState(ShadowRevenantState.Teleport, 0f);
        }

        void TickPhased()
        {
            FaceTarget();
            if (!teleportApplied)
            {
                teleportApplied = true;
                transform.position = ResolveTeleportPosition();
                SpawnVfx(config.phaseVfxPrefab, transform.position);
            }

            if (stateTimer <= 0f)
                EnterState(ShadowRevenantState.Strafe, 0f);
        }

        void TickLightBroken()
        {
            FaceTarget();
            if (stateTimer <= 0f || lightBrokenRemaining <= 0f)
                EnterState(ShadowRevenantState.Strafe, 0f);
        }

        void TickRecover(ShadowRevenantState nextState)
        {
            FaceTarget();
            if (stateTimer <= 0f)
                EnterState(nextState, 0f);
        }

        void FireProjectile()
        {
            if (poolHub == null || target == null || target.TargetTransform == null)
                return;

            Vector3 origin = projectileMuzzle != null ? projectileMuzzle.position : transform.position + Vector3.up;
            Vector3 aimPoint = target.TargetTransform.position + Vector3.up;
            Vector3 direction = aimPoint - origin;
            if (direction.sqrMagnitude <= DirectionEpsilon)
                direction = transform.forward;

            Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            poolHub.SpawnProjectile(origin, rotation, direction, target);
        }

        void BeginFogTelegraph()
        {
            if (poolHub == null)
                return;

            Vector3 position = ResolveFogSpawnPosition();
            pendingFogCast = poolHub.SpawnFogTelegraph(position);
            if (pendingFogCast == null && enableDebugLogs)
                Debug.LogWarning("[ShadowRevenant] Fog telegraph spawn failed (pool exhausted).", this);
        }

        void ActivatePendingFogDamage()
        {
            if (pendingFogCast == null || config == null)
                return;

            pendingFogCast.BeginDamagePhase(
                config.fogDuration,
                config.fogDamagePerTick,
                config.fogTickInterval,
                config.fogSlowPercent,
                target,
                config.fogFadeOutTime);
            pendingFogCast = null;
        }

        Vector3 ResolveFogSpawnPosition()
        {
            if (fogSpawnAnchor != null)
                return fogSpawnAnchor.position;

            if (target != null && target.TargetTransform != null)
                return target.TargetTransform.position;

            return transform.position;
        }

        void SpawnShades()
        {
            if (poolHub == null || target == null)
                return;

            int count = Mathf.Min(config.summonCount, config.maxActiveMinions - poolHub.ActiveShadeCount);
            for (var i = 0; i < count; i++)
            {
                Transform spawnPoint = ResolveShadeSpawnPoint(i);
                Vector3 position = spawnPoint != null
                    ? spawnPoint.position
                    : transform.position + transform.right * (i - count * 0.5f);
                poolHub.SpawnShade(position, target);
            }
        }

        Transform ResolveShadeSpawnPoint(int index)
        {
            if (shadeSpawnPoints == null || shadeSpawnPoints.Length == 0)
                return null;

            return shadeSpawnPoints[index % shadeSpawnPoints.Length];
        }

        Vector3 ResolveTeleportPosition()
        {
            if (target == null || target.TargetTransform == null)
                return transform.position;

            Vector3 targetPosition = target.TargetTransform.position;
            int attempts = Mathf.Max(1, config.teleportValidationAttempts);
            for (var i = 0; i < attempts; i++)
            {
                Vector2 circle = Random.insideUnitCircle.normalized;
                if (circle.sqrMagnitude <= DirectionEpsilon)
                    circle = Vector2.right;

                float radius = Random.Range(config.teleportMinRadius, config.teleportMaxRadius);
                Vector3 candidate = targetPosition + new Vector3(circle.x, 0f, circle.y) * radius;
                Vector3 rayOrigin = candidate + Vector3.up * Mathf.Max(0.1f, config.teleportRaycastHeight);

                if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, config.teleportRaycastHeight * 2f, config.teleportGroundMask, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 grounded = hit.point;
                if (config.teleportObstructionMask.value != 0
                    && Physics.CheckSphere(grounded + Vector3.up, config.teleportClearanceRadius, config.teleportObstructionMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                return ResolveHoverPosition(grounded);
            }

            return ResolveHoverPosition(transform.position);
        }

        void StrafeAroundTarget()
        {
            if (config == null || body == null || target == null || target.TargetTransform == null)
                return;

            Vector3 toTarget = target.TargetTransform.position - transform.position;
            Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontal.sqrMagnitude <= DirectionEpsilon)
                return;

            Vector3 moveDirection;
            switch (strafeMode)
            {
                case StrafeMode.TowardTarget:
                    moveDirection = horizontal.normalized;
                    break;
                case StrafeMode.AwayFromFog:
                    moveDirection = -horizontal.normalized;
                    break;
                default:
                    moveDirection = Vector3.Cross(Vector3.up, horizontal.normalized);
                    break;
            }

            Vector3 nextPosition = transform.position + moveDirection * (config.strafeSpeed * Time.fixedDeltaTime);

            if (body.isKinematic)
                body.MovePosition(ResolveHoverPosition(nextPosition));
            else
                body.velocity = new Vector3(moveDirection.x * config.strafeSpeed, 0f, moveDirection.z * config.strafeSpeed);
        }

        void ConfigureRigidbodyForHover()
        {
            if (body == null)
                return;

            bool useGravity = config != null && config.usePhysicsGravity;
            body.useGravity = useGravity;
            body.isKinematic = !useGravity;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        LayerMask ResolveGroundMask()
        {
            if (config == null)
                return Physics.DefaultRaycastLayers;

            return config.groundMask.value != 0 ? config.groundMask : config.teleportGroundMask;
        }

        float ResolveGroundCheckStartHeight()
        {
            if (config == null)
                return 12f;

            return config.groundCheckStartHeight > 0f
                ? config.groundCheckStartHeight
                : Mathf.Max(0.1f, config.teleportRaycastHeight);
        }

        float ResolveGroundCheckDistance()
        {
            if (config == null)
                return 24f;

            return config.groundCheckDistance > 0f
                ? config.groundCheckDistance
                : Mathf.Max(0.1f, config.teleportRaycastHeight) * 2f;
        }

        Vector3 ResolveHoverPosition(Vector3 desiredWorldPosition)
        {
            if (config == null)
                return desiredWorldPosition;

            float startHeight = ResolveGroundCheckStartHeight();
            float checkDistance = ResolveGroundCheckDistance();
            Vector3 rayOrigin = desiredWorldPosition + Vector3.up * startHeight;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    checkDistance,
                    ResolveGroundMask(),
                    QueryTriggerInteraction.Ignore))
            {
                float finalY = hit.point.y + config.hoverHeight;
                return new Vector3(desiredWorldPosition.x, finalY, desiredWorldPosition.z);
            }

            float fallbackY = Mathf.Max(desiredWorldPosition.y, spawnAnchorY);
            return new Vector3(desiredWorldPosition.x, fallbackY, desiredWorldPosition.z);
        }

        void MaintainHoverHeight()
        {
            if (config == null || deathHandled)
                return;

            Vector3 resolved = ResolveHoverPosition(transform.position);
            Vector3 nextPosition = resolved;

            if (config.verticalSnapSpeed > 0f)
            {
                float y = Mathf.MoveTowards(
                    transform.position.y,
                    resolved.y,
                    config.verticalSnapSpeed * Time.fixedDeltaTime);
                nextPosition = new Vector3(transform.position.x, y, transform.position.z);
            }

            if (body != null && body.isKinematic)
                body.MovePosition(nextPosition);
            else
                transform.position = nextPosition;
        }

        void FaceTarget()
        {
            if (config == null || target == null || target.TargetTransform == null)
                return;

            Vector3 toTarget = target.TargetTransform.position - transform.position;
            Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontal.sqrMagnitude <= DirectionEpsilon)
                return;

            Quaternion desired = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, config.faceTurnSpeed * Time.deltaTime);
        }

        float DistanceToTarget()
        {
            if (target == null || target.TargetTransform == null)
                return float.MaxValue;

            return Vector3.Distance(transform.position, target.TargetTransform.position);
        }

        public bool ReceiveDamage(int damage, EnemyDamageType damageType, Transform source)
        {
            if (deathHandled || currentHealth <= 0 || config == null)
                return false;

            bool isLightBreakDamage = damageType == EnemyDamageType.Light || damageType == EnemyDamageType.Fire;
            if (isLightBreakDamage)
                ApplyLightBreak(config.lightBreakVulnerableDuration, source);

            float multiplier = ResolveDamageMultiplier(isLightBreakDamage);
            int resolvedDamage = Mathf.RoundToInt(Mathf.Max(0, damage) * multiplier);
            if (resolvedDamage <= 0)
                return false;

            currentHealth = Mathf.Max(0, currentHealth - resolvedDamage);
            if (healthBar != null)
                healthBar.SetNPCHealth(currentHealth);

            healthBarVisibility?.NotifyDamaged();

            SpawnVfx(config.hitVfxPrefab, transform.position + Vector3.up);
            if (currentHealth <= 0)
                Die();

            return true;
        }

        float ResolveDamageMultiplier(bool isLightBreakDamage)
        {
            if (lightBrokenRemaining > 0f)
                return config.lightBrokenDamageMultiplier;

            if (state == ShadowRevenantState.Phased || state == ShadowRevenantState.PhaseShiftEnter || state == ShadowRevenantState.Teleport)
                return isLightBreakDamage ? config.lightBrokenDamageMultiplier : config.phasedDamageMultiplier;

            return config.normalDamageMultiplier;
        }

        public bool ApplyLightBreak(float duration, Transform source)
        {
            if (deathHandled || config == null)
                return false;

            lightBrokenRemaining = Mathf.Max(duration, config.lightBreakStaggerSeconds);
            EnterState(ShadowRevenantState.LightBroken, lightBrokenRemaining);
            SpawnVfx(config.lightBreakVfxPrefab, transform.position + Vector3.up);
            return true;
        }

        void EnterState(ShadowRevenantState nextState, float duration)
        {
            OnExitState(state);
            previousState = state;
            state = nextState;
            stateTimer = Mathf.Max(0f, duration);
            OnEnterState(nextState);

            bool phased = nextState == ShadowRevenantState.PhaseShiftEnter
                || nextState == ShadowRevenantState.Phased
                || nextState == ShadowRevenantState.Teleport;
            SetPhaseCollision(!phased);

            if (animator != null)
            {
                animator.SetBool(PhasedHash, phased);
                if (nextState == ShadowRevenantState.ProjectileWindup || nextState == ShadowRevenantState.FogWindup)
                    animator.SetTrigger(AttackHash);
                if (nextState == ShadowRevenantState.SummonWindup)
                    animator.SetTrigger(SummonHash);
                if (nextState == ShadowRevenantState.LightBroken || nextState == ShadowRevenantState.Staggered)
                    animator.SetTrigger(StaggerHash);
            }

            if (nextState != ShadowRevenantState.Phased)
                teleportApplied = false;
        }

        void OnEnterState(ShadowRevenantState entered)
        {
            switch (entered)
            {
                case ShadowRevenantState.FogWindup:
                    BeginFogTelegraph();
                    break;
                case ShadowRevenantState.ProjectileWindup:
                    SetProjectileMuzzleGlow(true);
                    break;
                case ShadowRevenantState.SummonWindup:
                    SetSummonSigilsVisible(true);
                    break;
                case ShadowRevenantState.PhaseShiftEnter:
                    SpawnVfx(config != null ? config.phaseVfxPrefab : null, transform.position + Vector3.up * 0.5f);
                    break;
                case ShadowRevenantState.LightBroken:
                    SetHealthBarLightBreakAccent(true);
                    break;
            }
        }

        void OnExitState(ShadowRevenantState exited)
        {
            switch (exited)
            {
                case ShadowRevenantState.LightBroken:
                    SetHealthBarLightBreakAccent(false);
                    break;
                case ShadowRevenantState.ProjectileWindup:
                    SetProjectileMuzzleGlow(false);
                    break;
                case ShadowRevenantState.SummonWindup:
                    SetSummonSigilsVisible(false);
                    break;
                case ShadowRevenantState.FogWindup:
                    if (pendingFogCast != null && state != ShadowRevenantState.FogRecover)
                    {
                        pendingFogCast.DeactivateToPool();
                        pendingFogCast = null;
                    }
                    break;
            }
        }

        void SetProjectileMuzzleGlow(bool enabled)
        {
            if (projectileMuzzleGlow != null)
                projectileMuzzleGlow.enabled = enabled;
        }

        void SetSummonSigilsVisible(bool visible)
        {
            if (summonSigilVisuals == null)
                return;

            for (var i = 0; i < summonSigilVisuals.Length; i++)
            {
                if (summonSigilVisuals[i] != null)
                    summonSigilVisuals[i].SetActive(visible);
            }
        }

        void SetPhaseCollision(bool enabled)
        {
            if (phaseDisabledColliders == null)
                return;

            for (var i = 0; i < phaseDisabledColliders.Length; i++)
            {
                if (phaseDisabledColliders[i] != null)
                    phaseDisabledColliders[i].enabled = enabled;
            }
        }

        void Die()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            state = ShadowRevenantState.Dead;
            SetPhaseCollision(false);

            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (animator != null)
                animator.SetTrigger(DeadHash);

            if (poolHub != null)
                poolHub.ReleaseAllActiveCombat();

            if (pendingFogCast != null)
            {
                pendingFogCast.DeactivateToPool();
                pendingFogCast = null;
            }

            Vector3 deathPosition = ResolveHoverPosition(transform.position);
            SpawnVfx(config != null ? config.deathVfxPrefab : null, deathPosition + Vector3.up * 1.2f);
            SpawnRemains(deathPosition);
            SpawnDrops();
            HideBossAfterDeath();
        }

        void HideBossAfterDeath()
        {
            if (healthBar != null)
            {
                Canvas barCanvas = healthBar.GetComponentInParent<Canvas>();
                if (barCanvas != null)
                    barCanvas.enabled = false;
            }

            if (bossVisualRoot != null)
                bossVisualRoot.gameObject.SetActive(false);
            else
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                for (var i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].enabled = false;
                }
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }

            if (projectileMuzzleGlow != null)
                projectileMuzzleGlow.enabled = false;

            SetSummonSigilsVisible(false);
        }

        void SpawnRemains(Vector3 groundedPosition)
        {
            if (config == null || config.remainsPrefab == null)
                return;

            GameObject remains = Instantiate(config.remainsPrefab, groundedPosition, Quaternion.identity);
            ShadowRevenantRemains remainsBehaviour = remains.GetComponent<ShadowRevenantRemains>();
            if (remainsBehaviour == null)
                remainsBehaviour = remains.AddComponent<ShadowRevenantRemains>();

            remainsBehaviour.ConfigureLifetime(config.remainsLifetime);
        }

        void SpawnDrops()
        {
            if (config == null || config.deathDropPrefabs == null)
                return;

            for (var i = 0; i < config.deathDropPrefabs.Length; i++)
            {
                GameObject dropPrefab = config.deathDropPrefabs[i];
                if (dropPrefab == null)
                    continue;

                GameObject drop = Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                CurrencySpin.ConfigureEnemyDrop(drop);
            }
        }

        void SpawnVfx(GameObject prefab, Vector3 position)
        {
            if (prefab != null)
                PooledOneShotVfx.Spawn(prefab, position, Quaternion.identity);
        }
    }
}
