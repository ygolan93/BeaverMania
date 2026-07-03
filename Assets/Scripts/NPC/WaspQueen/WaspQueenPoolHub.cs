using System.Collections.Generic;
using Beavermania.Data.NPC;
using UnityEngine;
using UnityEngine.Pool;

namespace Beavermania.NPC
{
    public sealed class WaspQueenPoolHub : MonoBehaviour
    {
        [SerializeField] WaspQueenConfig config;
        [SerializeField] Transform poolRoot;
        [SerializeField] bool enableDebugLogs;

        ObjectPool<WaspQueenProjectile> projectilePool;
        ObjectPool<WaspQueenPoisonZone> poisonZonePool;

        readonly List<WaspQueenProjectile> activeProjectiles = new List<WaspQueenProjectile>(8);
        readonly List<WaspQueenPoisonZone> activePoisonZones = new List<WaspQueenPoisonZone>(4);

        public void Initialize(WaspQueenConfig queenConfig)
        {
            if (config == null)
                config = queenConfig;

            EnsurePools();
        }

        void Awake()
        {
            EnsurePools();
        }

        public WaspQueenProjectile SpawnProjectile(Vector3 position, Quaternion rotation)
        {
            EnsurePools();
            if (projectilePool == null)
            {
                LogPoolUnavailable("projectile");
                return null;
            }

            WaspQueenProjectile projectile = projectilePool.Get();
            if (!activeProjectiles.Contains(projectile))
                activeProjectiles.Add(projectile);
            return projectile;
        }

        public WaspQueenPoisonZone SpawnPoisonZone(Vector3 position)
        {
            EnsurePools();
            if (poisonZonePool == null)
            {
                LogPoolUnavailable("poisonZone");
                return null;
            }

            WaspQueenPoisonZone zone = poisonZonePool.Get();
            if (!activePoisonZones.Contains(zone))
                activePoisonZones.Add(zone);
            return zone;
        }

        public void ReleaseAllActive()
        {
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                if (activeProjectiles[i] != null)
                    activeProjectiles[i].Deactivate();
            }
            activeProjectiles.Clear();

            for (int i = activePoisonZones.Count - 1; i >= 0; i--)
            {
                if (activePoisonZones[i] != null)
                    activePoisonZones[i].Deactivate();
            }
            activePoisonZones.Clear();
        }

        void EnsurePools()
        {
            if (config == null)
                return;

            if (poolRoot == null)
                poolRoot = transform;

            if (projectilePool == null && config.poisonProjectilePrefab != null)
            {
                projectilePool = CreatePool(config.poisonProjectilePrefab, 4, Mathf.Max(1, config.maxActiveProjectiles * 2));
                Prewarm(projectilePool, 2);
            }

            if (poisonZonePool == null && config.poisonZonePrefab != null)
            {
                poisonZonePool = CreatePool(config.poisonZonePrefab, 2, Mathf.Max(1, config.maxActivePoisonZones * 2));
                Prewarm(poisonZonePool, 1);
            }
        }

        ObjectPool<T> CreatePool<T>(T prefab, int defaultCapacity, int maxSize) where T : Component
        {
            ObjectPool<T> pool = null;
            pool = new ObjectPool<T>(
                () => CreateInstance(prefab),
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

        T CreateInstance<T>(T prefab) where T : Component
        {
            T instance = Instantiate(prefab, poolRoot);

            switch (instance)
            {
                case WaspQueenProjectile projectile:
                    projectile.Bind(ReleaseProjectile);
                    break;
                case WaspQueenPoisonZone zone:
                    zone.Bind(ReleasePoisonZone);
                    break;
            }

            instance.gameObject.SetActive(false);
            return instance;
        }

        void ReleaseProjectile(WaspQueenProjectile projectile)
        {
            activeProjectiles.Remove(projectile);
            projectilePool?.Release(projectile);
        }

        void ReleasePoisonZone(WaspQueenPoisonZone zone)
        {
            activePoisonZones.Remove(zone);
            poisonZonePool?.Release(zone);
        }

        void Prewarm<T>(ObjectPool<T> pool, int count) where T : Component
        {
            if (pool == null || count <= 0)
                return;

            var items = new List<T>(count);
            for (int i = 0; i < count; i++)
                items.Add(pool.Get());

            for (int i = 0; i < items.Count; i++)
                pool.Release(items[i]);
        }

        void LogPoolUnavailable(string poolName)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[WaspQueenPoolHub] {poolName} pool unavailable.", this);
        }
    }
}
