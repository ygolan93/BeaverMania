using System;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantProjectile : MonoBehaviour, IShadowRevenantPooledItem
    {
        const float DirectionEpsilon = 0.0001f;

        [SerializeField] Rigidbody projectileRigidbody;
        [SerializeField] Collider projectileCollider;
        [SerializeField] GameObject impactVfxPrefab;

        Action<ShadowRevenantProjectile> releaseToPool;
        IShadowRevenantTarget target;
        float damage;
        float lifetimeRemaining;
        bool released = true;

        public bool IsPoolActive => !released;

        public void Bind(Action<ShadowRevenantProjectile> releaseAction)
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
            if (projectileRigidbody == null)
                projectileRigidbody = GetComponent<Rigidbody>();

            if (projectileCollider == null)
                projectileCollider = GetComponent<Collider>();
        }

        public void Activate(
            Vector3 position,
            Quaternion rotation,
            Vector3 direction,
            IShadowRevenantTarget projectileTarget,
            float projectileDamage,
            float speed,
            float lifetime)
        {
            CacheReferences();
            released = false;
            target = projectileTarget;
            damage = Mathf.Max(0f, projectileDamage);
            lifetimeRemaining = Mathf.Max(0.05f, lifetime);
            transform.SetPositionAndRotation(position, rotation);

            if (projectileCollider != null)
                projectileCollider.enabled = true;

            Vector3 launchDirection = direction.sqrMagnitude > DirectionEpsilon ? direction.normalized : transform.forward;
            if (projectileRigidbody != null)
            {
                projectileRigidbody.isKinematic = false;
                projectileRigidbody.useGravity = false;
                projectileRigidbody.velocity = launchDirection * Mathf.Max(0f, speed);
                projectileRigidbody.angularVelocity = Vector3.zero;
            }
        }

        void Update()
        {
            if (released)
                return;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
                DeactivateToPool();
        }

        void OnTriggerEnter(Collider other)
        {
            TryHit(other);
        }

        void OnCollisionEnter(Collision collision)
        {
            TryHit(collision != null ? collision.collider : null);
        }

        void TryHit(Collider other)
        {
            if (released || other == null)
                return;

            IShadowRevenantTarget hitTarget = ResolveTarget(other);
            if (hitTarget == null || !hitTarget.CanReceiveShadowDamage)
                return;

            if (hitTarget.IsParrying)
            {
                if (impactVfxPrefab != null)
                    Beavermania.Display.PooledOneShotVfx.Spawn(impactVfxPrefab, transform.position, transform.rotation);

                DeactivateToPool();
                return;
            }

            hitTarget.ReceiveShadowDamage(damage);
            if (impactVfxPrefab != null)
                Beavermania.Display.PooledOneShotVfx.Spawn(impactVfxPrefab, transform.position, transform.rotation);

            DeactivateToPool();
        }

        IShadowRevenantTarget ResolveTarget(Collider other)
        {
            IShadowRevenantTarget candidate = other.GetComponentInParent<IShadowRevenantTarget>();
            if (candidate != null)
                return candidate;

            return target != null && target.TargetTransform != null && other.transform.IsChildOf(target.TargetTransform)
                ? target
                : null;
        }

        public void DeactivateToPool()
        {
            if (released)
                return;

            released = true;
            target = null;
            lifetimeRemaining = 0f;
            damage = 0f;

            if (projectileRigidbody != null)
            {
                projectileRigidbody.velocity = Vector3.zero;
                projectileRigidbody.angularVelocity = Vector3.zero;
                projectileRigidbody.isKinematic = true;
            }

            if (projectileCollider != null)
                projectileCollider.enabled = false;

            if (releaseToPool != null)
                releaseToPool(this);
            else
                gameObject.SetActive(false);
        }
    }
}
