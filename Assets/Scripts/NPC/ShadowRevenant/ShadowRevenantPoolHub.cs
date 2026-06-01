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
        [SerializeField] bool enableDebugLogs;

        ObjectPool<ShadowRevenantProjectile> projectilePool;
        ObjectPool<ShadowRevenantDreadFogZone> fogPool;
        ObjectPool<ShadowRevenantShadeMinion> shadePool;

        readonly List<ShadowRevenantProjectile> activeProjectiles = new List<ShadowRevenantProjectile>(16);
        readonly List<ShadowRevenantDreadFogZone> activeFogs = new List<ShadowRevenantDreadFogZone>(8);
        readonly List<ShadowRevenantShadeMinion> activeShades = new List<ShadowRevenantShadeMinion>(8);

        public int ActiveShadeCount => activeShades.Count;
        public int ActiveFogCount => activeFogs.Count;

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
            if (projectilePool == null || config == null || activeProjectiles.Count >= Mathf.Max(1, config.projectileMaxActive))
            {
                LogPoolExhausted("projectile");
                return null;
            }

            ShadowRevenantProjectile projectile = projectilePool.Get();
            RegisterProjectile(projectile);
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

        public ShadowRevenantDreadFogZone SpawnFogTelegraph(Vector3 position)
        {
            EnsurePools();
            if (fogPool == null || config == null || activeFogs.Count >= Mathf.Max(1, config.fogMaxActive))
            {
                LogPoolExhausted("fog");
                return null;
            }

            ShadowRevenantDreadFogZone fog = fogPool.Get();
            RegisterFog(fog);
            float maxPoolLifetime = config.fogWindup + config.fogDuration + config.fogFadeOutTime;
            fog.BeginTelegraph(position, config.fogRadius, config.fogVisualScaleMultiplier, maxPoolLifetime);
            return fog;
        }

        public ShadowRevenantDreadFogZone SpawnFog(Vector3 position, IShadowRevenantTarget target)
        {
            ShadowRevenantDreadFogZone fog = SpawnFogTelegraph(position);
            if (fog == null)
                return null;

            fog.BeginDamagePhase(
                config.fogDuration,
                config.fogDamagePerTick,
                config.fogTickInterval,
                config.fogSlowPercent,
                target,
                config.fogFadeOutTime);
            return fog;
        }

        public ShadowRevenantShadeMinion SpawnShade(Vector3 position, IShadowRevenantTarget target)
        {
            EnsurePools();
            if (shadePool == null || config == null || activeShades.Count >= Mathf.Max(1, config.shadeMaxActive))
            {
                LogPoolExhausted("shade");
                return null;
            }

            ShadowRevenantShadeMinion shade = shadePool.Get();
            RegisterShade(shade);
            shade.Activate(
                position,
                target,
                config.shadeMoveSpeed,
                config.shadeDamage,
                config.shadeDamageCooldown,
                config.shadeLifetime,
                config.shadeMaxHealth,
                config.shadeHitVfxPrefab,
                config.shadeDeathVfxPrefab);
            return shade;
        }

        public bool IsTargetInsideActiveFog(IShadowRevenantTarget hitTarget)
        {
            if (hitTarget == null || hitTarget.TargetTransform == null)
                return false;

            Vector3 targetPosition = hitTarget.TargetTransform.position;
            for (var i = 0; i < activeFogs.Count; i++)
            {
                ShadowRevenantDreadFogZone fog = activeFogs[i];
                if (fog == null || !fog.IsDamagePhaseActive)
                    continue;

                Vector3 delta = targetPosition - fog.ZoneCenter;
                delta.y = 0f;
                if (delta.sqrMagnitude <= fog.CurrentRadius * fog.CurrentRadius)
                    return true;
            }

            return false;
        }

        public void ReleaseAllActiveCombat()
        {
            for (var i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                if (activeProjectiles[i] != null)
                    activeProjectiles[i].DeactivateToPool();
            }

            for (var i = activeFogs.Count - 1; i >= 0; i--)
            {
                if (activeFogs[i] != null)
                    activeFogs[i].DeactivateToPool();
            }

            for (var i = activeShades.Count - 1; i >= 0; i--)
            {
                if (activeShades[i] != null)
                    activeShades[i].DeactivateToPool();
            }

            activeProjectiles.Clear();
            activeFogs.Clear();
            activeShades.Clear();
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
                    projectile.Bind(ReleaseProjectile);
                    break;
                case ShadowRevenantDreadFogZone fog:
                    fog.Bind(ReleaseFog);
                    break;
                case ShadowRevenantShadeMinion shade:
                    shade.Bind(ReleaseShade);
                    break;
            }

            instance.gameObject.SetActive(false);
            return instance;
        }

        void RegisterProjectile(ShadowRevenantProjectile projectile)
        {
            if (projectile != null && !activeProjectiles.Contains(projectile))
                activeProjectiles.Add(projectile);
        }

        void RegisterFog(ShadowRevenantDreadFogZone fog)
        {
            if (fog != null && !activeFogs.Contains(fog))
                activeFogs.Add(fog);
        }

        void RegisterShade(ShadowRevenantShadeMinion shade)
        {
            if (shade != null && !activeShades.Contains(shade))
                activeShades.Add(shade);
        }

        void ReleaseProjectile(ShadowRevenantProjectile projectile)
        {
            activeProjectiles.Remove(projectile);
            projectilePool?.Release(projectile);
        }

        void ReleaseFog(ShadowRevenantDreadFogZone fog)
        {
            activeFogs.Remove(fog);
            fogPool?.Release(fog);
        }

        void ReleaseShade(ShadowRevenantShadeMinion shade)
        {
            activeShades.Remove(shade);
            shadePool?.Release(shade);
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

        void LogPoolExhausted(string poolName)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ShadowRevenantPoolHub] {poolName} pool exhausted or unavailable.", this);
        }
    }
}
