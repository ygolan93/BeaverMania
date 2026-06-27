using System;
using System.Collections;
using System.Collections.Generic;
using Beavermania.Display;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Beavermania.NPC
{
    public sealed class PooledDeathDebris : MonoBehaviour
    {
        const int DefaultPoolCapacity = 16;
        const int MaxActiveInstances = 128;
        const float DefaultLifetime = 8f;

        struct DebrisFragment
        {
            public Transform Root;
            public GameObject Effect;
            public bool SaveAfterKill;
            public Collider[] Colliders;
        }

        static readonly Dictionary<GameObject, ObjectPool<PooledDeathDebris>> Pools = new Dictionary<GameObject, ObjectPool<PooledDeathDebris>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SubscribeSceneClear()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearAllPools();
        }

        static void OnSceneUnloaded(Scene scene)
        {
            ClearAllPools();
        }

        public static void ClearAllPools()
        {
            foreach (var entry in Pools)
            {
                try
                {
                    entry.Value?.Clear();
                }
                catch (Exception)
                {
                    // Pool may reference instances destroyed during scene unload.
                }
            }

            Pools.Clear();
        }

        ObjectPool<PooledDeathDebris> pool;
        Rigidbody[] rigidbodies;
        Collider[] colliders;
        EffectObject[] lifetimeScripts;
        DebrisFragment[] fragments;
        float[] lifetimeDurations;
        Vector3 defaultScale;
        float safeLifetime;
        Coroutine returnRoutine;
        bool released;
        bool overflowInstance;

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, -1f);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetimeOverride)
        {
            if (prefab == null)
                return null;

            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                Pools.Add(prefab, pool);
            }

            if (pool.CountActive >= MaxActiveInstances)
                return SpawnOverflow(prefab, position, rotation, lifetimeOverride);

            var debris = GetAliveFromPool(pool, prefab);
            if (debris == null)
                return null;

            debris.Spawn(position, rotation, prefab.transform.localScale, false, lifetimeOverride);
            return debris.gameObject;
        }

        static PooledDeathDebris GetAliveFromPool(ObjectPool<PooledDeathDebris> sourcePool, GameObject prefab)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                PooledDeathDebris debris = null;
                try
                {
                    debris = sourcePool.Get();
                }
                catch (MissingReferenceException)
                {
                    // Stale reference in pool stack after external destroy or scene unload.
                }

                if (debris != null && debris.IsAlive())
                    return debris;

                if (Pools.TryGetValue(prefab, out var existing))
                {
                    try
                    {
                        existing.Clear();
                    }
                    catch
                    {
                        // ignored
                    }

                    Pools.Remove(prefab);
                }

                sourcePool = CreatePool(prefab);
                Pools[prefab] = sourcePool;
            }

            return null;
        }

        static ObjectPool<PooledDeathDebris> CreatePool(GameObject prefab)
        {
            ObjectPool<PooledDeathDebris> pool = null;
            pool = new ObjectPool<PooledDeathDebris>(
                () => CreateInstance(prefab, pool),
                debris =>
                {
                    if (debris == null || !debris.IsAlive())
                        return;

                    debris.released = false;
                    debris.StopReturnRoutine();
                    debris.gameObject.SetActive(true);
                },
                debris =>
                {
                    if (debris == null || !debris.IsAlive())
                        return;

                    debris.released = true;
                    debris.StopReturnRoutine();
                    debris.ResetRuntimeState(debris.transform.position, debris.transform.rotation, debris.defaultScale);
                    debris.gameObject.SetActive(false);
                },
                debris =>
                {
                    if (debris != null && debris.IsAlive())
                        Destroy(debris.gameObject);
                },
                true,
                DefaultPoolCapacity,
                MaxActiveInstances);

            return pool;
        }

        static PooledDeathDebris CreateInstance(GameObject prefab, ObjectPool<PooledDeathDebris> pool)
        {
            var instance = Instantiate(prefab);
            instance.SetActive(false);

            var debris = instance.GetComponent<PooledDeathDebris>();
            if (debris == null)
                debris = instance.AddComponent<PooledDeathDebris>();

            debris.Bind(pool, prefab.transform.localScale, false);
            return debris;
        }

        static GameObject SpawnOverflow(GameObject prefab, Vector3 position, Quaternion rotation, float lifetimeOverride)
        {
            var instance = Instantiate(prefab, position, rotation);
            var debris = instance.GetComponent<PooledDeathDebris>();
            if (debris == null)
                debris = instance.AddComponent<PooledDeathDebris>();

            debris.Bind(null, prefab.transform.localScale, true);
            debris.Spawn(position, rotation, prefab.transform.localScale, true, lifetimeOverride);
            return instance;
        }

        void Bind(ObjectPool<PooledDeathDebris> sourcePool, Vector3 scale, bool isOverflowInstance)
        {
            pool = sourcePool;
            defaultScale = scale;
            overflowInstance = isOverflowInstance;
            rigidbodies = GetComponentsInChildren<Rigidbody>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            lifetimeScripts = GetComponentsInChildren<EffectObject>(true);
            lifetimeDurations = new float[lifetimeScripts.Length];
            safeLifetime = DefaultLifetime;

            for (var i = 0; i < lifetimeScripts.Length; i++)
            {
                lifetimeDurations[i] = lifetimeScripts[i].time;
                safeLifetime = Mathf.Max(safeLifetime, lifetimeDurations[i]);
                lifetimeScripts[i].enabled = false;
            }

            CacheAndRemoveLegacyDestroyComponents();
        }

        void CacheAndRemoveLegacyDestroyComponents()
        {
            var destroyComponents = GetComponentsInChildren<global::Destroy>(true);
            fragments = new DebrisFragment[destroyComponents.Length];

            for (var i = 0; i < destroyComponents.Length; i++)
            {
                var destroy = destroyComponents[i];
                if (destroy == null)
                    continue;

                fragments[i] = new DebrisFragment
                {
                    Root = destroy.transform,
                    Effect = destroy.effect,
                    SaveAfterKill = destroy.saveAfterKill,
                    Colliders = destroy.GetComponentsInChildren<Collider>(true)
                };

                Destroy(destroy);
            }
        }

        void Spawn(Vector3 position, Quaternion rotation, Vector3 scale, bool isOverflowInstance, float lifetimeOverride = -1f)
        {
            if (!IsAlive())
                return;

            overflowInstance = isOverflowInstance;
            released = false;
            ResetRuntimeState(position, rotation, scale);
            float lifetime = lifetimeOverride > 0f ? lifetimeOverride : safeLifetime;
            returnRoutine = StartCoroutine(ReturnAfterLifetime(lifetime));
        }

        void ResetRuntimeState(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsAlive())
                return;

            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;

            for (var i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] == null)
                    continue;

                rigidbodies[i].velocity = Vector3.zero;
                rigidbodies[i].angularVelocity = Vector3.zero;
            }

            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;

                colliders[i].enabled = true;
            }

            for (var i = 0; i < lifetimeScripts.Length; i++)
            {
                if (lifetimeScripts[i] == null)
                    continue;

                lifetimeScripts[i].time = lifetimeDurations[i];
                lifetimeScripts[i].enabled = false;
            }

            if (fragments != null)
            {
                for (var i = 0; i < fragments.Length; i++)
                {
                    if (fragments[i].Root != null && fragments[i].Root != transform)
                        fragments[i].Root.gameObject.SetActive(true);

                    var fragmentColliders = fragments[i].Colliders;
                    if (fragmentColliders == null)
                        continue;

                    for (var c = 0; c < fragmentColliders.Length; c++)
                    {
                        if (fragmentColliders[c] != null)
                            fragmentColliders[c].enabled = true;
                    }
                }
            }
        }

        void OnDisable()
        {
            StopReturnRoutine();
        }

        void OnDestroy()
        {
            released = true;
            StopReturnRoutine();
            pool = null;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsAlive() || released)
                return;

            if (!collision.gameObject.CompareTag("Player"))
                return;

            if (!TryResolveFragmentForCollision(collision, out var fragmentIndex))
                return;

            ApplyFragmentDestroy(fragmentIndex);
        }

        bool TryResolveFragmentForCollision(Collision collision, out int fragmentIndex)
        {
            fragmentIndex = -1;

            if (fragments == null || fragments.Length == 0)
                return false;

            if (collision.contactCount > 0)
            {
                var hitCollider = collision.GetContact(0).thisCollider;
                if (hitCollider != null)
                {
                    for (var i = 0; i < fragments.Length; i++)
                    {
                        var fragmentColliders = fragments[i].Colliders;
                        if (fragmentColliders == null)
                            continue;

                        for (var c = 0; c < fragmentColliders.Length; c++)
                        {
                            if (fragmentColliders[c] == hitCollider)
                            {
                                fragmentIndex = i;
                                return true;
                            }
                        }
                    }
                }
            }

            fragmentIndex = 0;
            return true;
        }

        public bool HandleLegacyDestroy(global::Destroy destroy)
        {
            if (destroy == null)
                return false;

            if (!IsAlive() || released)
                return true;

            if (destroy.gameObject == gameObject)
            {
                StopReturnRoutine();
                Release();
                return true;
            }

            destroy.gameObject.SetActive(false);
            return true;
        }

        void ApplyFragmentDestroy(int fragmentIndex)
        {
            if (!IsAlive() || released || fragments == null)
                return;

            if (fragmentIndex < 0 || fragmentIndex >= fragments.Length)
                return;

            var fragment = fragments[fragmentIndex];
            if (fragment.Root == null)
                return;

            if (fragment.Effect != null)
                PooledOneShotVfx.Spawn(fragment.Effect, fragment.Root.position, Quaternion.identity);

            if (fragment.SaveAfterKill)
            {
                fragment.Root.gameObject.SetActive(false);
                if (fragment.Root == transform)
                {
                    StopReturnRoutine();
                    Release();
                }

                return;
            }

            StopReturnRoutine();
            if (fragment.Root == transform)
            {
                Release();
                return;
            }

            fragment.Root.gameObject.SetActive(false);
            if (fragment.Colliders == null)
                return;

            for (var i = 0; i < fragment.Colliders.Length; i++)
            {
                if (fragment.Colliders[i] != null)
                    fragment.Colliders[i].enabled = false;
            }
        }

        IEnumerator ReturnAfterLifetime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            returnRoutine = null;

            if (!IsAlive())
                yield break;

            Release();
        }

        void Release()
        {
            if (!IsAlive() || released)
                return;

            released = true;

            if (pool != null && !overflowInstance)
            {
                var activePool = pool;
                pool = null;

                try
                {
                    activePool.Release(this);
                }
                catch (MissingReferenceException)
                {
                    // Instance was destroyed during scene unload; pool is reset on next scene load.
                }

                return;
            }

            Destroy(gameObject);
        }

        void StopReturnRoutine()
        {
            if (returnRoutine == null)
                return;

            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        bool IsAlive()
        {
            return this != null && gameObject != null;
        }
    }
}
