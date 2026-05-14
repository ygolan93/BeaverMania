using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

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

            var debris = pool.Get();
            debris.Spawn(position, rotation, prefab.transform.localScale, false);
            return debris.gameObject;
        }

        static ObjectPool<PooledDeathDebris> CreatePool(GameObject prefab)
        {
            ObjectPool<PooledDeathDebris> pool = null;
            pool = new ObjectPool<PooledDeathDebris>(
                () => CreateInstance(prefab, pool),
                debris =>
                {
                    debris.released = false;
                    debris.StopReturnRoutine();
                    debris.gameObject.SetActive(true);
                },
                debris =>
                {
                    debris.released = true;
                    debris.StopReturnRoutine();
                    debris.ResetRuntimeState(debris.transform.position, debris.transform.rotation, debris.defaultScale);
                    debris.gameObject.SetActive(false);
                },
                debris => Destroy(debris.gameObject),
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
            overflowInstance = isOverflowInstance;
            released = false;
            ResetRuntimeState(position, rotation, scale);
            returnRoutine = StartCoroutine(ReturnAfterLifetime(safeLifetime));
        }

        void ResetRuntimeState(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;

            for (var i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].velocity = Vector3.zero;
                rigidbodies[i].angularVelocity = Vector3.zero;
            }

            for (var i = 0; i < colliders.Length; i++)
                colliders[i].enabled = true;

            for (var i = 0; i < lifetimeScripts.Length; i++)
            {
                lifetimeScripts[i].time = lifetimeDurations[i];
                lifetimeScripts[i].enabled = false;
            }

            DisableSelfDestroyScripts();
        }

        void DisableSelfDestroyScripts()
        {
            for (var i = 0; i < selfDestroyScripts.Length; i++)
                selfDestroyScripts[i].enabled = false;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (released)
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

            if (released)
                return true;

            ApplyDestroySelfForPool(destroy);
            return true;
        }

        void ApplyDestroySelfForPool(global::Destroy destroy)
        {
            if (destroy.effect != null)
                Object.Instantiate(destroy.effect, destroy.transform.position, Quaternion.identity);

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
                fragmentColliders[i].enabled = false;
        }

        IEnumerator ReturnAfterLifetime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            returnRoutine = null;
            Release();
        }

        void Release()
        {
            if (released)
                return;

            released = true;

            if (pool != null && !overflowInstance)
            {
                pool.Release(this);
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
    }
}
