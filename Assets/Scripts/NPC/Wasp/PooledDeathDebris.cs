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

        static readonly Dictionary<GameObject, ObjectPool<PooledDeathDebris>> Pools = new Dictionary<GameObject, ObjectPool<PooledDeathDebris>>();

        ObjectPool<PooledDeathDebris> pool;
        Rigidbody[] rigidbodies;
        Collider[] colliders;
        EffectObject[] lifetimeScripts;
        global::Destroy[] selfDestroyScripts;
        float[] lifetimeDurations;
        Vector3 defaultScale;
        float safeLifetime;
        Coroutine returnRoutine;
        bool released;
        bool overflowInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SubscribeSceneClear()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
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

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                Pools.Add(prefab, pool);
            }

            if (pool.CountActive >= MaxActiveInstances)
                return SpawnOverflow(prefab, position, rotation);

            var debris = GetAliveFromPool(pool, prefab);
            if (debris == null)
                return SpawnOverflow(prefab, position, rotation);

            debris.Spawn(position, rotation, prefab.transform.localScale, false);
            return debris.gameObject;
        }

        static PooledDeathDebris GetAliveFromPool(ObjectPool<PooledDeathDebris> sourcePool, GameObject prefab)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var debris = sourcePool.Get();
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
                    if (debris != null && debris.gameObject != null)
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
            var debris = instance.GetComponent<PooledDeathDebris>();
            if (debris == null)
                debris = instance.AddComponent<PooledDeathDebris>();

            debris.Bind(pool, prefab.transform.localScale, false);
            instance.SetActive(false);
            return debris;
        }

        static GameObject SpawnOverflow(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = Instantiate(prefab, position, rotation);
            var debris = instance.GetComponent<PooledDeathDebris>();
            if (debris == null)
                debris = instance.AddComponent<PooledDeathDebris>();

            debris.Bind(null, prefab.transform.localScale, true);
            debris.Spawn(position, rotation, prefab.transform.localScale, true);
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
            selfDestroyScripts = GetComponentsInChildren<global::Destroy>(true);
            lifetimeDurations = new float[lifetimeScripts.Length];
            safeLifetime = DefaultLifetime;

            for (var i = 0; i < lifetimeScripts.Length; i++)
            {
                lifetimeDurations[i] = lifetimeScripts[i].time;
                safeLifetime = Mathf.Max(safeLifetime, lifetimeDurations[i]);
                lifetimeScripts[i].enabled = false;
            }

            DisableSelfDestroyScripts();
        }

        void Spawn(Vector3 position, Quaternion rotation, Vector3 scale, bool isOverflowInstance)
        {
            if (!IsAlive())
                return;

            overflowInstance = isOverflowInstance;
            released = false;
            ResetRuntimeState(position, rotation, scale);
            returnRoutine = StartCoroutine(ReturnAfterLifetime(safeLifetime));
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

            DisableSelfDestroyScripts();
        }

        void DisableSelfDestroyScripts()
        {
            for (var i = 0; i < selfDestroyScripts.Length; i++)
            {
                if (selfDestroyScripts[i] == null)
                    continue;

                selfDestroyScripts[i].SetDestroySelfSuppressed(true);
                selfDestroyScripts[i].enabled = false;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsAlive() || released)
                return;

            if (!collision.gameObject.CompareTag("Player"))
                return;

            var destroy = ResolveDestroyForCollision(collision);
            if (destroy == null)
                return;

            ApplyDestroySelfForPool(destroy);
        }

        global::Destroy ResolveDestroyForCollision(Collision collision)
        {
            if (collision.contactCount > 0)
            {
                var hitCollider = collision.GetContact(0).thisCollider;
                var onCollider = hitCollider.GetComponent<global::Destroy>();
                if (onCollider != null)
                    return onCollider;

                var inParent = hitCollider.GetComponentInParent<global::Destroy>();
                if (inParent != null)
                    return inParent;
            }

            return selfDestroyScripts.Length > 0 ? selfDestroyScripts[0] : null;
        }

        public bool HandleLegacyDestroy(global::Destroy destroy)
        {
            if (destroy == null)
                return false;

            if (!IsAlive() || released)
                return true;

            ApplyDestroySelfForPool(destroy);
            return true;
        }

        void ApplyDestroySelfForPool(global::Destroy destroy)
        {
            if (!IsAlive())
                return;

            if (destroy.effect != null)
                PooledOneShotVfx.Spawn(destroy.effect, destroy.transform.position, Quaternion.identity);

            if (destroy.saveAfterKill)
            {
                destroy.gameObject.SetActive(false);
                if (destroy.gameObject == gameObject)
                {
                    StopReturnRoutine();
                    Release();
                }

                return;
            }

            StopReturnRoutine();
            if (destroy.gameObject == gameObject)
            {
                Release();
                return;
            }

            destroy.gameObject.SetActive(false);
            var fragmentColliders = destroy.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < fragmentColliders.Length; i++)
            {
                if (fragmentColliders[i] == null)
                    continue;

                fragmentColliders[i].enabled = false;
            }
        }

        IEnumerator ReturnAfterLifetime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            returnRoutine = null;

            if (!IsAlive() || released)
                yield break;

            Release();
        }

        void Release()
        {
            if (!IsAlive() || released)
                return;

            released = true;
            StopReturnRoutine();

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
