using System;
using Beavermania.Display;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantShadeMinion : MonoBehaviour, IShadowRevenantPooledItem, IEnemyDamageReceiver
    {
        const float DirectionEpsilon = 0.0001f;
        const float HitFlashDuration = 0.12f;
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Rigidbody shadeRigidbody;
        [SerializeField] Collider damageCollider;
        [SerializeField] Animator animator;
        [SerializeField] Renderer[] eyeRenderers;
        [SerializeField] bool enableDebugLogs;

        Action<ShadowRevenantShadeMinion> releaseToPool;
        IShadowRevenantTarget target;
        GameObject hitVfxPrefab;
        GameObject deathVfxPrefab;
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
            float speed,
            float contactDamage,
            float contactDamageCooldown,
            float lifetime,
            int health,
            GameObject hitVfx,
            GameObject deathVfx)
        {
            CacheReferences();
            released = false;
            target = minionTarget;
            moveSpeed = Mathf.Max(0f, speed);
            damage = Mathf.Max(0f, contactDamage);
            damageCooldown = Mathf.Max(0.05f, contactDamageCooldown);
            nextDamageTime = 0f;
            lifetimeRemaining = Mathf.Max(0.05f, lifetime);
            maxHealth = Mathf.Max(1, health);
            currentHealth = maxHealth;
            hitVfxPrefab = hitVfx;
            deathVfxPrefab = deathVfx;
            hitFlashRemaining = 0f;
            transform.position = position;

            if (damageCollider != null)
                damageCollider.enabled = true;

            if (shadeRigidbody != null)
            {
                shadeRigidbody.isKinematic = false;
                shadeRigidbody.velocity = Vector3.zero;
                shadeRigidbody.angularVelocity = Vector3.zero;
            }
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
            if (released || target == null || target.TargetTransform == null)
                return;

            Vector3 toTarget = target.TargetTransform.position - transform.position;
            Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontal.sqrMagnitude <= DirectionEpsilon)
                return;

            Vector3 direction = horizontal.normalized;
            if (shadeRigidbody != null)
            {
                Vector3 velocity = direction * moveSpeed;
                shadeRigidbody.velocity = new Vector3(velocity.x, shadeRigidbody.velocity.y, velocity.z);
                shadeRigidbody.MoveRotation(Quaternion.LookRotation(direction));
            }
            else
            {
                transform.position += direction * (moveSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.LookRotation(direction);
            }
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
        }

        public bool ReceiveDamage(int amount, EnemyDamageType damageType, Transform source)
        {
            if (released || amount <= 0)
                return false;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            SpawnHitVfx();
            FlashEyes();

            if (currentHealth <= 0)
            {
                SpawnDeathVfx();
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
            lifetimeRemaining = 0f;
            hitFlashRemaining = 0f;
            RestoreEyeEmission();

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
