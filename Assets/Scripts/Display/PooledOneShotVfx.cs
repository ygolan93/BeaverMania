using System;
using System.Collections;
using System.Collections.Generic;
using Beavermania.Audio;
using Beavermania.NPC;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Beavermania.Display
{
    public sealed class PooledOneShotVfx : MonoBehaviour
    {
        const int DefaultPoolCapacity = 8;
        const int MaxActiveInstances = 64;
        const string BalloonVfxChannel = "vfx.balloon";
        const float BalloonVfxMinInterval = 0.18f;
        const float DefaultVfxMinInterval = 0.08f;

        static readonly Dictionary<GameObject, ObjectPool<PooledOneShotVfx>> Pools = new Dictionary<GameObject, ObjectPool<PooledOneShotVfx>>();

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

        ObjectPool<PooledOneShotVfx> pool;
        ParticleSystem[] particleSystems;
        AudioSource[] audioSources;
        float[] defaultVolumes;
        float[] defaultPitches;
        bool[] defaultPlayOnAwake;
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

            if (pool.CountActive >= MaxActiveInstances)
                return SpawnOverflow(prefab, position, rotation, prefab.transform.localScale);

            var vfx = GetAliveFromPool(pool, prefab);
            if (vfx == null)
                return null;

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

            if (pool.CountActive >= MaxActiveInstances)
                return SpawnOverflow(prefab, position, rotation, scale);

            var vfx = GetAliveFromPool(pool, prefab);
            if (vfx == null)
                return null;

            vfx.Spawn(position, rotation, scale);
            return vfx.gameObject;
        }

        static PooledOneShotVfx GetAliveFromPool(ObjectPool<PooledOneShotVfx> sourcePool, GameObject prefab)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var vfx = sourcePool.Get();
                if (vfx != null && vfx.IsAlive())
                    return vfx;

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

        static ObjectPool<PooledOneShotVfx> CreatePool(GameObject prefab)
        {
            ObjectPool<PooledOneShotVfx> pool = null;
            pool = new ObjectPool<PooledOneShotVfx>(
                () => CreateInstance(prefab, pool),
                vfx =>
                {
                    if (vfx == null || vfx.gameObject == null)
                        return;

                    vfx.released = false;
                    vfx.suppressStopCallback = false;
                    vfx.StopReturnRoutine();
                    vfx.StopAndClear();
                    vfx.gameObject.SetActive(true);
                },
                vfx =>
                {
                    if (vfx == null || vfx.gameObject == null)
                        return;

                    vfx.released = true;
                    vfx.suppressStopCallback = true;
                    vfx.StopReturnRoutine();
                    vfx.StopAndClear();
                    vfx.gameObject.SetActive(false);
                },
                vfx =>
                {
                    if (vfx != null && vfx.gameObject != null)
                        Destroy(vfx.gameObject);
                },
                true,
                DefaultPoolCapacity,
                MaxActiveInstances);

            return pool;
        }

        static GameObject SpawnOverflow(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var instance = Instantiate(prefab, position, rotation);
            instance.transform.localScale = scale;
            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            var audioSources = instance.GetComponentsInChildren<AudioSource>(true);
            EnsureSfxRoutes(audioSources);
            var lifetime = Mathf.Max(GetLifetime(particleSystems), GetLifetime(audioSources));

            Replay(particleSystems);
            var playOnAwake = new bool[audioSources.Length];
            for (var i = 0; i < audioSources.Length; i++)
                playOnAwake[i] = audioSources[i] != null && audioSources[i].playOnAwake;
            ReplayAudioOneShot(audioSources, playOnAwake);
            Destroy(instance, lifetime);
            return instance;
#else
            return null;
#endif
        }

        static PooledOneShotVfx CreateInstance(GameObject prefab, ObjectPool<PooledOneShotVfx> pool)
        {
            var instance = Instantiate(prefab);
            DisableSelfDestroyComponents(instance);

            var vfx = instance.GetComponent<PooledOneShotVfx>();
            if (vfx == null)
                vfx = instance.AddComponent<PooledOneShotVfx>();

            vfx.Bind(pool);
            instance.SetActive(false);
            return vfx;
        }

        static void DisableSelfDestroyComponents(GameObject instance)
        {
            var effectObjects = instance.GetComponentsInChildren<EffectObject>(true);
            for (var i = 0; i < effectObjects.Length; i++)
            {
                effectObjects[i].enabled = false;
                Destroy(effectObjects[i]);
            }
        }

        void Bind(ObjectPool<PooledOneShotVfx> sourcePool)
        {
            pool = sourcePool;
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            audioSources = GetComponentsInChildren<AudioSource>(true);
            EnsureSfxRoutes(audioSources);
            defaultVolumes = new float[audioSources.Length];
            defaultPitches = new float[audioSources.Length];
            defaultPlayOnAwake = new bool[audioSources.Length];

            for (var i = 0; i < audioSources.Length; i++)
            {
                defaultVolumes[i] = audioSources[i].volume;
                defaultPitches[i] = audioSources[i].pitch;
                defaultPlayOnAwake[i] = audioSources[i].playOnAwake;
            }

            cachedLifetime = Mathf.Max(GetLifetime(particleSystems), GetLifetime(audioSources, defaultPlayOnAwake));

            var rootParticles = GetComponent<ParticleSystem>();
            if (rootParticles != null)
            {
                var main = rootParticles.main;
                main.stopAction = ParticleSystemStopAction.Callback;
            }
        }

        void Spawn(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsAlive())
                return;

            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            EnsureSfxRoutes(audioSources);

            Replay(particleSystems);
            ResetAudioSources();
            ReplayAudioOneShot(audioSources, defaultPlayOnAwake);

            returnRoutine = StartCoroutine(ReturnAfterLifetime(cachedLifetime));
        }

        void OnDisable()
        {
            suppressStopCallback = true;
            StopReturnRoutine();
        }

        void OnDestroy()
        {
            released = true;
            suppressStopCallback = true;
            StopReturnRoutine();
            pool = null;
        }

        void OnParticleSystemStopped()
        {
            if (!IsAlive() || released || suppressStopCallback || returnRoutine != null)
                return;

            Release();
        }

        IEnumerator ReturnAfterLifetime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            returnRoutine = null;

            if (!IsAlive())
                yield break;

            Release();
        }

        bool IsAlive()
        {
            return this != null && gameObject != null;
        }

        void Release()
        {
            if (!IsAlive() || released)
                return;

            if (pool == null)
                return;

            released = true;
            suppressStopCallback = true;
            StopReturnRoutine();

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
            if (!IsAlive())
                return;

            suppressStopCallback = true;
            for (var i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] == null)
                    continue;

                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ResetAudioSources();
            suppressStopCallback = false;
        }

        void ResetAudioSources()
        {
            if (audioSources == null)
                return;

            for (var i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] == null)
                    continue;

                audioSources[i].Stop();
                audioSources[i].volume = defaultVolumes[i];
                audioSources[i].pitch = defaultPitches[i];
                audioSources[i].playOnAwake = defaultPlayOnAwake[i];
            }
        }

        static void Replay(ParticleSystem[] systems)
        {
            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;

                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }
        }

        static void ReplayAudioOneShot(AudioSource[] sources, bool[] playOnAwake)
        {
            AudioSource primary = null;
            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null)
                    continue;

                sources[i].Stop();
                if (!AudioSourceRouting.EnsureRoute(sources[i], AudioSourceRoute.Sfx))
                    continue;

                if (!playOnAwake[i] || !sources[i].isActiveAndEnabled || sources[i].clip == null)
                    continue;

                if (primary == null)
                    primary = sources[i];
                else
                    sources[i].playOnAwake = false;
            }

            if (primary == null)
                return;

            var clip = primary.clip;
            GameplayAudio.TryPlayOneShot(
                primary,
                clip,
                ResolveVfxAudioChannel(clip),
                ResolveVfxMinInterval(clip),
                primary.volume,
                primary.pitch);
        }

        static void EnsureSfxRoutes(AudioSource[] sources)
        {
            if (sources == null)
                return;

            for (var i = 0; i < sources.Length; i++)
                AudioSourceRouting.EnsureRoute(sources[i], AudioSourceRoute.Sfx);
        }

        static string ResolveVfxAudioChannel(AudioClip clip)
        {
            if (clip == null)
                return string.Empty;

            return string.Equals(clip.name, "Balloon", StringComparison.Ordinal)
                ? BalloonVfxChannel
                : "vfx." + clip.name;
        }

        static float ResolveVfxMinInterval(AudioClip clip)
        {
            return clip != null && string.Equals(clip.name, "Balloon", StringComparison.Ordinal)
                ? BalloonVfxMinInterval
                : DefaultVfxMinInterval;
        }

        static float GetLifetime(ParticleSystem[] systems)
        {
            var lifetime = 0f;
            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;

                var main = systems[i].main;
                if (main.loop)
                    continue;

                lifetime = Mathf.Max(lifetime, Max(main.startDelay) + main.duration + Max(main.startLifetime));
            }

            return lifetime;
        }

        static float GetLifetime(AudioSource[] sources)
        {
            var lifetime = 0f;
            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null || !sources[i].playOnAwake || sources[i].loop || sources[i].clip == null)
                    continue;

                lifetime = Mathf.Max(lifetime, sources[i].clip.length / Mathf.Max(Mathf.Abs(sources[i].pitch), 0.01f));
            }

            return lifetime;
        }

        static float GetLifetime(AudioSource[] sources, bool[] playOnAwake)
        {
            var lifetime = 0f;
            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null || !playOnAwake[i] || sources[i].loop || sources[i].clip == null)
                    continue;

                lifetime = Mathf.Max(lifetime, sources[i].clip.length / Mathf.Max(Mathf.Abs(sources[i].pitch), 0.01f));
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
