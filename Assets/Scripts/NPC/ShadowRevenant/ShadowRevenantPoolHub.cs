using System.Collections.Generic;
using Beavermania.Data.NPC;
using UnityEngine;
using UnityEngine.Pool;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantPoolHub : MonoBehaviour
    {
        [SerializeField] ShadowRevenantConfig config;
        [SerializeField] Transform poolRoot;

        ObjectPool<ShadowRevenantProjectile> projectilePool;
        ObjectPool<ShadowRevenantDreadFogZone> fogPool;
        ObjectPool<ShadowRevenantShadeMinion> shadePool;

        public int ActiveShadeCount => shadePool != null ? shadePool.CountActive : 0;

        public void Initialize(ShadowRevenantConfig revenantConfig)
        {
            if (config == null)
                config = revenantConfig;

            EnsurePools();
        }

        void Awake()
        {
            EnsurePools();
        }

        public ShadowRevenantProjectile SpawnProjectile(
            Vector3 position,
            Quaternion rotation,
            Vector3 direction,
            IShadowRevenantTarget target)
        {
            EnsurePools();
            if (projectilePool == null || config == null || projectilePool.CountActive >= Mathf.Max(1, config.projectileMaxActive))
                return null;

            ShadowRevenantProjectile projectile = projectilePool.Get();
            projectile.Activate(
                position,
                rotation,
                direction,
                target,
                config.projectileDamage,
                config.projectileSpeed,
                config.projectileLifetime);
            return projectile;
        }

        public ShadowRevenantDreadFogZone SpawnFog(Vector3 position, IShadowRevenantTarget target)
        {
            EnsurePools();
            if (fogPool == null || config == null || fogPool.CountActive >= Mathf.Max(1, config.fogMaxActive))
                return null;

            ShadowRevenantDreadFogZone fog = fogPool.Get();
            fog.Activate(
                position,
                config.fogRadius,
                config.fogDuration,
                config.fogDamagePerTick,
                config.fogTickInterval,
                config.fogSlowPercent,
                target);
            return fog;
        }

        public ShadowRevenantShadeMinion SpawnShade(Vector3 position, IShadowRevenantTarget target)
        {
            EnsurePools();
            if (shadePool == null || config == null || shadePool.CountActive >= Mathf.Max(1, config.shadeMaxActive))
                return null;

            ShadowRevenantShadeMinion shade = shadePool.Get();
            shade.Activate(
                position,
                target,
                config.shadeMoveSpeed,
                config.shadeDamage,
                config.shadeDamageCooldown,
                config.shadeLifetime);
            return shade;
        }

        void EnsurePools()
        {
            if (config == null)
                return;

            if (poolRoot == null)
                poolRoot = transform;

            if (projectilePool == null && config.projectilePrefab != null)
            {
                projectilePool = CreatePool(
                    config.projectilePrefab,
                    Mathf.Max(1, config.projectilePrewarmCount),
                    Mathf.Max(1, config.projectileMaxActive));
                Prewarm(projectilePool, Mathf.Max(0, config.projectilePrewarmCount));
            }

            if (fogPool == null && config.fogPrefab != null)
            {
                fogPool = CreatePool(
                    config.fogPrefab,
                    Mathf.Max(1, config.fogPrewarmCount),
                    Mathf.Max(1, config.fogMaxActive));
                Prewarm(fogPool, Mathf.Max(0, config.fogPrewarmCount));
            }

            if (shadePool == null && config.shadeMinionPrefab != null)
            {
                shadePool = CreatePool(
                    config.shadeMinionPrefab,
                    Mathf.Max(1, config.shadePrewarmCount),
                    Mathf.Max(1, config.shadeMaxActive));
                Prewarm(shadePool, Mathf.Max(0, config.shadePrewarmCount));
            }
        }

        ObjectPool<T> CreatePool<T>(T prefab, int defaultCapacity, int maxSize) where T : Component, IShadowRevenantPooledItem
        {
            ObjectPool<T> pool = null;
            pool = new ObjectPool<T>(
                () => CreateInstance(prefab, pool),
                item =>
                {
                    if (item != null)
                        item.gameObject.SetActive(true);
                },
                item =>
                {
                    if (item == null)
                        return;

                    item.gameObject.SetActive(false);
                },
                item =>
                {
                    if (item != null)
                        Destroy(item.gameObject);
                },
                true,
                defaultCapacity,
                maxSize);

            return pool;
        }

        T CreateInstance<T>(T prefab, ObjectPool<T> pool) where T : Component, IShadowRevenantPooledItem
        {
            T instance = Instantiate(prefab, poolRoot);
            switch (instance)
            {
                case ShadowRevenantProjectile projectile:
                    projectile.Bind(projectilePool.Release);
                    break;
                case ShadowRevenantDreadFogZone fog:
                    fog.Bind(fogPool.Release);
                    break;
                case ShadowRevenantShadeMinion shade:
                    shade.Bind(shadePool.Release);
                    break;
            }

            instance.gameObject.SetActive(false);
            return instance;
        }

        void Prewarm<T>(ObjectPool<T> pool, int count) where T : Component, IShadowRevenantPooledItem
        {
            if (pool == null || count <= 0)
                return;

            var items = new List<T>(count);
            for (var i = 0; i < count; i++)
                items.Add(pool.Get());

            for (var i = 0; i < items.Count; i++)
                pool.Release(items[i]);
        }
    }
}
