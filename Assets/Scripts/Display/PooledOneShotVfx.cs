using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Beavermania.Display
{
    public sealed class PooledOneShotVfx : MonoBehaviour
    {
        static readonly Dictionary<GameObject, ObjectPool<PooledOneShotVfx>> Pools = new Dictionary<GameObject, ObjectPool<PooledOneShotVfx>>();

        ObjectPool<PooledOneShotVfx> pool;
        ParticleSystem[] particleSystems;
        float cachedLifetime;
        Coroutine returnRoutine;
        bool released;
        bool suppressStopCallback;

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                Pools.Add(prefab, pool);
            }

            var vfx = pool.Get();
            vfx.Spawn(position, rotation, prefab.transform.localScale);
            return vfx.gameObject;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (prefab == null)
                return null;

            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                Pools.Add(prefab, pool);
            }

            var vfx = pool.Get();
            vfx.Spawn(position, rotation, scale);
            return vfx.gameObject;
        }

        static ObjectPool<PooledOneShotVfx> CreatePool(GameObject prefab)
        {
            ObjectPool<PooledOneShotVfx> pool = null;
            pool = new ObjectPool<PooledOneShotVfx>(
                () => CreateInstance(prefab, pool),
                vfx =>
                {
                    vfx.released = false;
                    vfx.StopReturnRoutine();
                    vfx.StopAndClear();
                    vfx.gameObject.SetActive(true);
                },
                vfx =>
                {
                    vfx.released = true;
                    vfx.StopReturnRoutine();
                    vfx.StopAndClear();
                    vfx.gameObject.SetActive(false);
                },
                vfx => Destroy(vfx.gameObject),
                true);

            return pool;
        }

        static PooledOneShotVfx CreateInstance(GameObject prefab, ObjectPool<PooledOneShotVfx> pool)
        {
            var instance = Instantiate(prefab);
            var vfx = instance.GetComponent<PooledOneShotVfx>();
            if (vfx == null)
                vfx = instance.AddComponent<PooledOneShotVfx>();

            vfx.Bind(pool);
            instance.SetActive(false);
            return vfx;
        }

        void Bind(ObjectPool<PooledOneShotVfx> sourcePool)
        {
            pool = sourcePool;
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            cachedLifetime = GetLifetime(particleSystems);

            var rootParticles = GetComponent<ParticleSystem>();
            if (rootParticles != null)
            {
                var main = rootParticles.main;
                main.stopAction = ParticleSystemStopAction.Callback;
            }
        }

        void Spawn(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;

            for (var i = 0; i < particleSystems.Length; i++)
                particleSystems[i].Play(true);

            if (cachedLifetime > 0)
                returnRoutine = StartCoroutine(ReturnAfterLifetime(cachedLifetime));
        }

        void OnParticleSystemStopped()
        {
            if (!suppressStopCallback)
                Release();
        }

        IEnumerator ReturnAfterLifetime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            Release();
        }

        void Release()
        {
            if (released || pool == null)
                return;

            pool.Release(this);
        }

        void StopReturnRoutine()
        {
            if (returnRoutine == null)
                return;

            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        void StopAndClear()
        {
            suppressStopCallback = true;
            for (var i = 0; i < particleSystems.Length; i++)
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            suppressStopCallback = false;
        }

        static float GetLifetime(ParticleSystem[] systems)
        {
            var lifetime = 0f;
            for (var i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                if (main.loop)
                    continue;

                lifetime = Mathf.Max(lifetime, Max(main.startDelay) + main.duration + Max(main.startLifetime));
            }

            return lifetime;
        }

        static float Max(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return curve.constantMax;
                case ParticleSystemCurveMode.Curve:
                    return MaxCurve(curve.curve) * curve.curveMultiplier;
                case ParticleSystemCurveMode.TwoCurves:
                    return MaxCurve(curve.curveMax) * curve.curveMultiplier;
                default:
                    return 0f;
            }
        }

        static float MaxCurve(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return 0f;

            var max = 0f;
            for (var i = 0; i < curve.length; i++)
                max = Mathf.Max(max, curve.keys[i].value);

            return max;
        }
    }
}
