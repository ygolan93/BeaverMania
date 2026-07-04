using Beavermania.Display;
using Beavermania.Player;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class WaspQueenProjectile : MonoBehaviour
    {
        const float DirectionEpsilon = 0.0001f;

        [SerializeField] Rigidbody body;
        [SerializeField] Collider projectileCollider;
        [SerializeField] GameObject impactVfxPrefab;

        System.Action<WaspQueenProjectile> releaseToPool;
        BeaverPlayerBehaviour player;
        float damage;
        float lifetimeRemaining;
        bool released = true;

        public bool IsActive => !released;

        public void Bind(System.Action<WaspQueenProjectile> release) { releaseToPool = release; }

        public void Activate(
            Vector3 position,
            Quaternion rotation,
            Vector3 direction,
            BeaverPlayerBehaviour playerTarget,
            float projectileDamage,
            float projectileSpeed,
            float projectileLifetime,
            GameObject overrideImpactVfx = null)
        {
            CacheReferences();
            transform.SetPositionAndRotation(position, rotation);
            released = false;
            player = playerTarget;
            damage = Mathf.Max(0f, projectileDamage);
            lifetimeRemaining = Mathf.Max(0.05f, projectileLifetime);
            impactVfxPrefab = overrideImpactVfx != null ? overrideImpactVfx : impactVfxPrefab;

            gameObject.SetActive(true);

            if (projectileCollider != null)
                projectileCollider.enabled = true;

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = false;
                body.velocity = (direction.sqrMagnitude > DirectionEpsilon ? direction.normalized : transform.forward)
                    * Mathf.Max(0f, projectileSpeed);
                body.angularVelocity = Vector3.zero;
            }
        }

        void Awake()
        {
            CacheReferences();
        }

        void Update()
        {
            if (released)
                return;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
                Deactivate();
        }

        void OnTriggerEnter(Collider other)
        {
            TryResolveHit(other);
        }

        void OnCollisionEnter(Collision collision)
        {
            TryResolveHit(collision != null ? collision.collider : null);
        }

        public void Deactivate()
        {
            if (released)
                return;

            released = true;

            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            if (projectileCollider != null)
                projectileCollider.enabled = false;

            if (releaseToPool != null)
            {
                gameObject.SetActive(false);
                releaseToPool(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void TryResolveHit(Collider other)
        {
            if (released || other == null)
                return;

            if (PlayerHit(other))
            {
                if (player != null && !player.Rolling && !player.isParried)
                    player.TakeDamage(damage);

                SpawnImpactVfx();
                Deactivate();
                return;
            }

            if (other.isTrigger)
                return;

            SpawnImpactVfx();
            Deactivate();
        }

        bool PlayerHit(Collider other)
        {
            if (player == null)
                return false;

            return other.GetComponentInParent<BeaverPlayerBehaviour>() == player
                || other.transform.IsChildOf(player.transform);
        }

        void SpawnImpactVfx()
        {
            if (impactVfxPrefab != null)
                PooledOneShotVfx.Spawn(impactVfxPrefab, transform.position, transform.rotation);
        }

        void CacheReferences()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();

            if (projectileCollider == null)
                projectileCollider = GetComponent<Collider>();
        }
    }
}
