using System;
using Beavermania.Data.NPC;
using Beavermania.Display;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantShadeMinion : MonoBehaviour, IShadowRevenantPooledItem, IEnemyDamageReceiver
    {
        const float DirectionEpsilon = 0.0001f;
        const float HitFlashDuration = 0.12f;
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        enum ShadeMoveState
        {
            Orbit,
            Approach,
            Retreat
        }

        [SerializeField] Rigidbody shadeRigidbody;
        [SerializeField] Collider damageCollider;
        [SerializeField] Animator animator;
        [SerializeField] Renderer[] eyeRenderers;
        [SerializeField] bool enableDebugLogs;

        Action<ShadowRevenantShadeMinion> releaseToPool;
        IShadowRevenantTarget target;
        ShadowRevenantConfig behaviorConfig;
        GameObject hitVfxPrefab;
        GameObject deathVfxPrefab;
        ShadowRevenantShadeAudio shadeAudio;
        MaterialPropertyBlock eyePropertyBlock;
        float moveSpeed;
        float damage;
        float damageCooldown;
        float nextDamageTime;
        float lifetimeRemaining;
        float hitFlashRemaining;
        Color[] defaultEyeEmission;
        int currentHealth;
        int maxHealth;
        bool released = true;
        ShadeMoveState moveState = ShadeMoveState.Orbit;
        float moveStateTimer;
        float nextApproachTime;
        float orbitAngleOffset;
        int orbitDirectionSign = 1;

        public bool IsPoolActive => !released;

        public void Bind(Action<ShadowRevenantShadeMinion> releaseAction)
        {
            releaseToPool = releaseAction;
            CacheReferences();
        }

        void Awake()
        {
            CacheReferences();
        }

        void CacheReferences()
        {
            if (shadeRigidbody == null)
                shadeRigidbody = GetComponent<Rigidbody>();

            if (damageCollider == null)
                damageCollider = GetComponent<Collider>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (eyeRenderers == null || eyeRenderers.Length == 0)
            {
                var left = transform.Find("EyeLeft");
                var right = transform.Find("EyeRight");
                if (left != null && right != null)
                    eyeRenderers = new[] { left.GetComponent<Renderer>(), right.GetComponent<Renderer>() };
            }

            if (eyePropertyBlock == null)
                eyePropertyBlock = new MaterialPropertyBlock();

            if (shadeAudio == null)
            {
                shadeAudio = GetComponent<ShadowRevenantShadeAudio>();
                if (shadeAudio == null)
                {
                    if (GetComponent<AudioSource>() == null)
                        gameObject.AddComponent<AudioSource>();

                    shadeAudio = gameObject.AddComponent<ShadowRevenantShadeAudio>();
                }
            }

            CacheDefaultEyeEmission();
        }

        void CacheDefaultEyeEmission()
        {
            if (eyeRenderers == null || eyeRenderers.Length == 0)
                return;

            if (defaultEyeEmission != null && defaultEyeEmission.Length == eyeRenderers.Length)
                return;

            defaultEyeEmission = new Color[eyeRenderers.Length];
            for (var i = 0; i < eyeRenderers.Length; i++)
            {
                Renderer renderer = eyeRenderers[i];
                if (renderer == null || renderer.sharedMaterial == null)
                    continue;

                if (renderer.sharedMaterial.HasProperty(EmissionColorId))
                    defaultEyeEmission[i] = renderer.sharedMaterial.GetColor(EmissionColorId);
            }
        }

        public void Activate(
            Vector3 position,
            IShadowRevenantTarget minionTarget,
            ShadowRevenantConfig config,
            int spawnIndex,
            GameObject hitVfx,
            GameObject deathVfx,
            ShadowRevenantAudioProfile audioProfile,
            float attackSfxCooldown)
        {
            CacheReferences();
            released = false;
            target = minionTarget;
            behaviorConfig = config;
            moveSpeed = config != null ? Mathf.Max(0f, config.shadeMoveSpeed) : 0f;
            damage = config != null ? Mathf.Max(0f, config.shadeDamage) : 0f;
            damageCooldown = config != null ? Mathf.Max(0.05f, config.shadeDamageCooldown) : 0.05f;
            nextDamageTime = 0f;
            lifetimeRemaining = config != null ? Mathf.Max(0.05f, config.shadeLifetime) : 0.05f;
            maxHealth = config != null ? Mathf.Max(1, config.shadeMaxHealth) : 1;
            currentHealth = maxHealth;
            hitVfxPrefab = hitVfx;
            deathVfxPrefab = deathVfx;
            hitFlashRemaining = 0f;
            moveState = ShadeMoveState.Orbit;
            moveStateTimer = 0f;
            ScheduleNextApproach(UnityEngine.Random.Range(0.75f, config != null ? config.shadeApproachInterval : 2f));

            int summonSlots = config != null ? Mathf.Max(1, config.summonCount) : 1;
            orbitAngleOffset = spawnIndex * (Mathf.PI * 2f / summonSlots);
            orbitDirectionSign = spawnIndex % 2 == 0 ? 1 : -1;

            transform.position = ResolveHoverPosition(position);

            if (damageCollider != null)
                damageCollider.enabled = true;

            if (shadeRigidbody != null)
            {
                shadeRigidbody.isKinematic = false;
                shadeRigidbody.velocity = Vector3.zero;
                shadeRigidbody.angularVelocity = Vector3.zero;
            }

            ConfigureShadeAudio(audioProfile, attackSfxCooldown);
            shadeAudio?.PlaySpawn();
        }

        void ConfigureShadeAudio(ShadowRevenantAudioProfile audioProfile, float attackSfxCooldown)
        {
            if (shadeAudio == null)
                shadeAudio = GetComponent<ShadowRevenantShadeAudio>();

            if (shadeAudio == null || audioProfile == null)
                return;

            shadeAudio.Configure(
                audioProfile.shadeSpawn,
                audioProfile.shadeAttack,
                audioProfile.shadeHit,
                audioProfile.shadeDeath,
                audioProfile.shadeOrbitLoop,
                audioProfile.shadeApproachMove,
                attackSfxCooldown,
                behaviorConfig != null ? behaviorConfig.shadeOrbitSfxInterval : 2.4f);
        }

        void Update()
        {
            if (released)
                return;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f || target == null || target.TargetTransform == null)
            {
                DeactivateToPool();
                return;
            }

            if (hitFlashRemaining > 0f)
            {
                hitFlashRemaining -= Time.deltaTime;
                if (hitFlashRemaining <= 0f)
                    RestoreEyeEmission();
            }
        }

        void FixedUpdate()
        {
            if (released || target == null || target.TargetTransform == null || behaviorConfig == null)
                return;

            Vector3 playerPosition = target.TargetTransform.position;
            Vector3 toPlayer = playerPosition - transform.position;
            Vector3 horizontalToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float flatDistance = horizontalToPlayer.magnitude;

            UpdateMoveState();

            Vector3 moveDirection = ResolveMoveDirection(playerPosition, horizontalToPlayer, flatDistance);
            float speed = ResolveMoveSpeed(flatDistance);
            ApplyMovement(moveDirection, speed);
            UpdateOrbitAudio();
        }

        void UpdateOrbitAudio()
        {
            if (moveState != ShadeMoveState.Orbit || shadeAudio == null)
                return;

            shadeAudio.PlayOrbitLoop();
        }

        void UpdateMoveState()
        {
            switch (moveState)
            {
                case ShadeMoveState.Approach:
                    moveStateTimer -= Time.fixedDeltaTime;
                    if (moveStateTimer <= 0f)
                        ReturnToOrbit();
                    break;

                case ShadeMoveState.Retreat:
                    moveStateTimer -= Time.fixedDeltaTime;
                    if (moveStateTimer <= 0f)
                        ReturnToOrbit();
                    break;

                default:
                    if (Time.time >= nextApproachTime && ShouldBeginApproach())
                        BeginApproach();
                    else if (Time.time >= nextApproachTime)
                        ScheduleNextApproach();
                    break;
            }
        }

        void BeginApproach()
        {
            moveState = ShadeMoveState.Approach;
            moveStateTimer = behaviorConfig != null ? behaviorConfig.shadeMaxApproachDuration : 8f;
            shadeAudio?.PlayApproachMove();
        }

        void ReturnToOrbit()
        {
            moveState = ShadeMoveState.Orbit;
            moveStateTimer = 0f;
            ScheduleNextApproach();
        }

        void ScheduleNextApproach(float delayOverride = -1f)
        {
            if (behaviorConfig == null)
            {
                nextApproachTime = Time.time + 2f;
                return;
            }

            if (delayOverride >= 0f)
            {
                nextApproachTime = Time.time + delayOverride;
                return;
            }

            float baseInterval = behaviorConfig.shadeApproachInterval;
            float variance = behaviorConfig.shadeApproachIntervalVariance;
            float min = Mathf.Max(0.5f, baseInterval - variance);
            float max = baseInterval + variance;
            nextApproachTime = Time.time + UnityEngine.Random.Range(min, max);
        }

        bool ShouldBeginApproach()
        {
            if (behaviorConfig == null)
                return false;

            return UnityEngine.Random.value <= behaviorConfig.shadeApproachChance;
        }

        Vector3 ResolveMoveDirection(Vector3 playerPosition, Vector3 horizontalToPlayer, float flatDistance)
        {
            if (horizontalToPlayer.sqrMagnitude <= DirectionEpsilon)
                return Vector3.zero;

            switch (moveState)
            {
                case ShadeMoveState.Approach:
                    return horizontalToPlayer.normalized;

                case ShadeMoveState.Retreat:
                    return -horizontalToPlayer.normalized;

                default:
                    float orbitRadius = Mathf.Max(0.5f, behaviorConfig.shadeOrbitRadius);
                    float angle = orbitAngleOffset + Time.time * moveSpeed * 0.12f * orbitDirectionSign;
                    Vector3 orbitOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
                    Vector3 orbitTarget = playerPosition + orbitOffset;
                    Vector3 toOrbitSlot = orbitTarget - transform.position;
                    toOrbitSlot.y = 0f;

                    Vector3 tangent = Vector3.Cross(Vector3.up, horizontalToPlayer.normalized) * orbitDirectionSign;
                    if (toOrbitSlot.sqrMagnitude > DirectionEpsilon)
                    {
                        float slotWeight = flatDistance > orbitRadius ? 0.55f : 0.35f;
                        return (tangent + toOrbitSlot.normalized * slotWeight).normalized;
                    }

                    return tangent.normalized;
            }
        }

        float ResolveMoveSpeed(float flatDistance)
        {
            switch (moveState)
            {
                case ShadeMoveState.Approach:
                    return moveSpeed * behaviorConfig.shadeApproachSpeedMultiplier;
                case ShadeMoveState.Retreat:
                    return behaviorConfig.shadeRetreatSpeed;
                default:
                    return moveSpeed;
            }
        }

        void ApplyMovement(Vector3 moveDirection, float speed)
        {
            Vector3 resolvedHover = ResolveHoverPosition(transform.position);
            float targetY = resolvedHover.y;

            if (shadeRigidbody != null)
            {
                Vector3 velocity = moveDirection.sqrMagnitude > DirectionEpsilon
                    ? moveDirection * speed
                    : Vector3.zero;
                shadeRigidbody.velocity = new Vector3(velocity.x, 0f, velocity.z);
                shadeRigidbody.angularVelocity = Vector3.zero;

                float nextY = behaviorConfig.verticalSnapSpeed > 0f
                    ? Mathf.MoveTowards(transform.position.y, targetY, behaviorConfig.verticalSnapSpeed * Time.fixedDeltaTime)
                    : targetY;
                shadeRigidbody.MovePosition(new Vector3(transform.position.x, nextY, transform.position.z));

                if (moveDirection.sqrMagnitude > DirectionEpsilon)
                    transform.rotation = Quaternion.LookRotation(moveDirection);
            }
            else
            {
                transform.position += moveDirection * (speed * Time.fixedDeltaTime);
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                if (moveDirection.sqrMagnitude > DirectionEpsilon)
                    transform.rotation = Quaternion.LookRotation(moveDirection);
            }
        }

        void BeginRetreat()
        {
            moveState = ShadeMoveState.Retreat;
            moveStateTimer = behaviorConfig != null ? behaviorConfig.shadeRetreatDuration : 0.85f;
        }

        Vector3 ResolveHoverPosition(Vector3 desiredWorldPosition)
        {
            if (behaviorConfig == null)
                return desiredWorldPosition;

            float startHeight = behaviorConfig.groundCheckStartHeight > 0f
                ? behaviorConfig.groundCheckStartHeight
                : 12f;
            float checkDistance = behaviorConfig.groundCheckDistance > 0f
                ? behaviorConfig.groundCheckDistance
                : 24f;
            Vector3 rayOrigin = desiredWorldPosition + Vector3.up * startHeight;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    checkDistance,
                    behaviorConfig.groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                float finalY = hit.point.y + behaviorConfig.shadeHoverHeight;
                return new Vector3(desiredWorldPosition.x, finalY, desiredWorldPosition.z);
            }

            return new Vector3(desiredWorldPosition.x, desiredWorldPosition.y, desiredWorldPosition.z);
        }

        void OnTriggerStay(Collider other)
        {
            TryDamage(other);
        }

        void OnCollisionStay(Collision collision)
        {
            TryDamage(collision != null ? collision.collider : null);
        }

        void TryDamage(Collider other)
        {
            if (released || other == null || Time.time < nextDamageTime)
                return;

            IShadowRevenantTarget hitTarget = other.GetComponentInParent<IShadowRevenantTarget>();
            if (hitTarget == null || !hitTarget.CanReceiveShadowDamage)
                return;

            hitTarget.ReceiveShadowDamage(damage);
            nextDamageTime = Time.time + damageCooldown;
            shadeAudio?.PlayAttack();
            BeginRetreat();
        }

        public bool ReceiveDamage(int amount, EnemyDamageType damageType, Transform source)
        {
            if (released || amount <= 0)
                return false;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            SpawnHitVfx();
            FlashEyes();
            shadeAudio?.PlayHit();

            if (currentHealth <= 0)
            {
                SpawnDeathVfx();
                shadeAudio?.PlayDeath();
                DeactivateToPool();
            }

            return true;
        }

        void SpawnHitVfx()
        {
            if (hitVfxPrefab != null)
                PooledOneShotVfx.Spawn(hitVfxPrefab, transform.position + Vector3.up * 0.35f, Quaternion.identity);
        }

        void SpawnDeathVfx()
        {
            if (deathVfxPrefab != null)
                PooledOneShotVfx.Spawn(deathVfxPrefab, transform.position + Vector3.up * 0.25f, Quaternion.identity);
        }

        void FlashEyes()
        {
            if (eyeRenderers == null || eyeRenderers.Length == 0)
                return;

            hitFlashRemaining = HitFlashDuration;
            Color flash = new Color(0.6f, 1.4f, 0.7f);
            for (var i = 0; i < eyeRenderers.Length; i++)
            {
                Renderer renderer = eyeRenderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(eyePropertyBlock);
                eyePropertyBlock.SetColor(EmissionColorId, flash);
                renderer.SetPropertyBlock(eyePropertyBlock);
            }
        }

        void RestoreEyeEmission()
        {
            if (eyeRenderers == null || defaultEyeEmission == null)
                return;

            for (var i = 0; i < eyeRenderers.Length; i++)
            {
                Renderer renderer = eyeRenderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(eyePropertyBlock);
                eyePropertyBlock.SetColor(EmissionColorId, defaultEyeEmission[i]);
                renderer.SetPropertyBlock(eyePropertyBlock);
            }
        }

        public void DeactivateToPool()
        {
            if (released)
                return;

            released = true;
            target = null;
            behaviorConfig = null;
            lifetimeRemaining = 0f;
            hitFlashRemaining = 0f;
            RestoreEyeEmission();
            shadeAudio?.StopAndReset();

            if (shadeRigidbody != null)
            {
                shadeRigidbody.velocity = Vector3.zero;
                shadeRigidbody.angularVelocity = Vector3.zero;
                shadeRigidbody.isKinematic = true;
            }

            if (damageCollider != null)
                damageCollider.enabled = false;

            if (releaseToPool != null)
                releaseToPool(this);
            else
                gameObject.SetActive(false);
        }
    }
}
