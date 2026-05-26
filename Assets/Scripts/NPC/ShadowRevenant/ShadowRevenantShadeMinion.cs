using System;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantShadeMinion : MonoBehaviour, IShadowRevenantPooledItem
    {
        const float DirectionEpsilon = 0.0001f;

        [SerializeField] Rigidbody shadeRigidbody;
        [SerializeField] Collider damageCollider;
        [SerializeField] Animator animator;

        Action<ShadowRevenantShadeMinion> releaseToPool;
        IShadowRevenantTarget target;
        float moveSpeed;
        float damage;
        float damageCooldown;
        float nextDamageTime;
        float lifetimeRemaining;
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
        }

        public void Activate(
            Vector3 position,
            IShadowRevenantTarget minionTarget,
            float speed,
            float contactDamage,
            float contactDamageCooldown,
            float lifetime)
        {
            CacheReferences();
            released = false;
            target = minionTarget;
            moveSpeed = Mathf.Max(0f, speed);
            damage = Mathf.Max(0f, contactDamage);
            damageCooldown = Mathf.Max(0.05f, contactDamageCooldown);
            nextDamageTime = 0f;
            lifetimeRemaining = Mathf.Max(0.05f, lifetime);
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
                DeactivateToPool();
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

        public void DeactivateToPool()
        {
            if (released)
                return;

            released = true;
            target = null;
            lifetimeRemaining = 0f;

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
