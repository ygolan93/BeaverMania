using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantDreadFogZone : MonoBehaviour, IShadowRevenantPooledItem
    {
        const float DefaultCylinderDiameterAtUnitScale = 1f;

        struct TargetTick
        {
            public IShadowRevenantTarget Target;
            public float NextDamageTime;
        }

        [SerializeField] SphereCollider fogCollider;
        [SerializeField] Transform telegraphRoot;
        [SerializeField] Transform activeVisualRoot;

        readonly List<TargetTick> trackedTargets = new List<TargetTick>(4);
        ParticleSystem[] childParticleSystems;
        Action<ShadowRevenantDreadFogZone> releaseToPool;
        float damagePerTick;
        float tickInterval;
        float slowPercent;
        float lifetimeRemaining;
        float poolLifetimeRemaining;
        float fadeOutDuration;
        float fadeRemaining;
        float currentRadius;
        float activeVisualHeight = 0.03f;
        float visualScaleMultiplier = 1f;
        Vector3 activeVisualBaseScale = Vector3.one;
        bool released = true;
        bool damagePhaseActive;
        bool fadingOut;

        public bool IsPoolActive => !released;
        public bool IsDamagePhaseActive => IsPoolActive && damagePhaseActive;
        public float CurrentRadius => currentRadius;
        public Vector3 ZoneCenter => transform.position;

        public void Bind(Action<ShadowRevenantDreadFogZone> releaseAction)
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
            if (fogCollider == null)
                fogCollider = GetComponent<SphereCollider>();

            if (activeVisualRoot == null)
                activeVisualRoot = transform.Find("ActiveHazard");

            if (telegraphRoot == null)
                telegraphRoot = transform.Find("TelegraphRing");

            if (activeVisualRoot == null)
                activeVisualRoot = transform.Find("visualRoot");

            if (childParticleSystems == null || childParticleSystems.Length == 0)
                childParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public void Activate(
            Vector3 position,
            float radius,
            float duration,
            float tickDamage,
            float damageInterval,
            float fogSlowPercent,
            IShadowRevenantTarget initialTarget,
            float scaleMultiplier = 1f,
            float fadeOutTime = 0f,
            float maxPoolLifetime = 0f)
        {
            float totalLifetime = maxPoolLifetime > 0f
                ? maxPoolLifetime
                : duration + fadeOutTime;
            BeginTelegraph(position, radius, scaleMultiplier, totalLifetime);
            BeginDamagePhase(duration, tickDamage, damageInterval, fogSlowPercent, initialTarget, fadeOutTime);
        }

        public void BeginTelegraph(Vector3 position, float radius, float scaleMultiplier, float maxPoolLifetime)
        {
            CacheReferences();
            released = false;
            damagePhaseActive = false;
            fadingOut = false;
            fadeRemaining = 0f;
            transform.position = position;
            trackedTargets.Clear();
            currentRadius = Mathf.Max(0.1f, radius);
            visualScaleMultiplier = Mathf.Max(0.1f, scaleMultiplier);
            lifetimeRemaining = 0f;
            poolLifetimeRemaining = Mathf.Max(0.1f, maxPoolLifetime);

            if (fogCollider != null)
            {
                fogCollider.isTrigger = true;
                fogCollider.radius = currentRadius;
                fogCollider.enabled = false;
            }

            SetVisualActive(telegraphRoot, true, currentRadius, 0.04f);
            SetVisualActive(activeVisualRoot, false, currentRadius, activeVisualHeight);
        }

        public void BeginDamagePhase(
            float duration,
            float tickDamage,
            float damageInterval,
            float fogSlowPercent,
            IShadowRevenantTarget initialTarget,
            float fadeOutTime = 0f)
        {
            if (released)
                return;

            damagePhaseActive = true;
            fadingOut = false;
            fadeRemaining = 0f;
            fadeOutDuration = Mathf.Max(0f, fadeOutTime);
            damagePerTick = Mathf.Max(0f, tickDamage);
            tickInterval = Mathf.Max(0.05f, damageInterval);
            slowPercent = Mathf.Clamp01(fogSlowPercent);
            lifetimeRemaining = Mathf.Max(0.05f, duration);

            if (fogCollider != null)
            {
                fogCollider.radius = currentRadius;
                fogCollider.enabled = true;
            }

            SetVisualActive(telegraphRoot, false, currentRadius, 0.04f);
            SetVisualActive(activeVisualRoot, true, currentRadius, activeVisualHeight);

            if (initialTarget != null)
                TrackTarget(initialTarget, Time.time);
        }

        void SetVisualActive(Transform root, bool active, float radius, float height)
        {
            if (root == null)
                return;

            root.gameObject.SetActive(active);
            if (!active)
                return;

            float diameterScale = (radius * 2f / DefaultCylinderDiameterAtUnitScale) * visualScaleMultiplier;
            root.localScale = new Vector3(diameterScale, height, diameterScale);
            if (root == activeVisualRoot)
                activeVisualBaseScale = root.localScale;
        }

        void Update()
        {
            if (released)
                return;

            poolLifetimeRemaining -= Time.deltaTime;
            if (poolLifetimeRemaining <= 0f)
            {
                DeactivateToPool();
                return;
            }

            if (fadingOut)
            {
                TickFadeOut();
                return;
            }

            if (!damagePhaseActive)
                return;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                if (fadeOutDuration > 0f)
                    BeginFadeOut();
                else
                    DeactivateToPool();
            }
        }

        void BeginFadeOut()
        {
            damagePhaseActive = false;
            fadingOut = true;
            fadeRemaining = fadeOutDuration;
            trackedTargets.Clear();

            if (fogCollider != null)
                fogCollider.enabled = false;

            SetVisualActive(telegraphRoot, false, currentRadius, 0.04f);
            if (activeVisualRoot != null)
            {
                activeVisualRoot.gameObject.SetActive(true);
                activeVisualBaseScale = activeVisualRoot.localScale;
            }
        }

        void TickFadeOut()
        {
            fadeRemaining -= Time.deltaTime;
            if (activeVisualRoot != null && fadeOutDuration > 0f)
            {
                float t = Mathf.Clamp01(fadeRemaining / fadeOutDuration);
                activeVisualRoot.localScale = activeVisualBaseScale * t;
            }

            if (fadeRemaining <= 0f)
                DeactivateToPool();
        }

        void OnTriggerStay(Collider other)
        {
            if (released || !damagePhaseActive || other == null)
                return;

            IShadowRevenantTarget hitTarget = other.GetComponentInParent<IShadowRevenantTarget>();
            if (hitTarget == null || !hitTarget.CanReceiveShadowDamage)
                return;

            float now = Time.time;
            int index = TrackTarget(hitTarget, now);
            if (index < 0 || trackedTargets[index].NextDamageTime > now)
                return;

            hitTarget.ReceiveShadowDamage(damagePerTick);
            hitTarget.TryApplyDreadFogSlow(slowPercent, tickInterval);

            TargetTick tick = trackedTargets[index];
            tick.NextDamageTime = now + tickInterval;
            trackedTargets[index] = tick;
        }

        int TrackTarget(IShadowRevenantTarget hitTarget, float now)
        {
            for (var i = 0; i < trackedTargets.Count; i++)
            {
                if (trackedTargets[i].Target == hitTarget)
                    return i;
            }

            trackedTargets.Add(new TargetTick
            {
                Target = hitTarget,
                NextDamageTime = now
            });
            return trackedTargets.Count - 1;
        }

        void StopChildParticles()
        {
            if (childParticleSystems == null)
                return;

            for (var i = 0; i < childParticleSystems.Length; i++)
            {
                ParticleSystem ps = childParticleSystems[i];
                if (ps == null)
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
            }
        }

        public void DeactivateToPool()
        {
            if (released)
                return;

            released = true;
            damagePhaseActive = false;
            fadingOut = false;
            trackedTargets.Clear();
            lifetimeRemaining = 0f;
            poolLifetimeRemaining = 0f;
            fadeRemaining = 0f;

            if (fogCollider != null)
                fogCollider.enabled = false;

            SetVisualActive(telegraphRoot, false, currentRadius, 0.04f);
            SetVisualActive(activeVisualRoot, false, currentRadius, activeVisualHeight);
            StopChildParticles();

            if (releaseToPool != null)
                releaseToPool(this);
            else
                gameObject.SetActive(false);
        }
    }
}
