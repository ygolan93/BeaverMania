using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantDreadFogZone : MonoBehaviour, IShadowRevenantPooledItem
    {
        struct TargetTick
        {
            public IShadowRevenantTarget Target;
            public float NextDamageTime;
        }

        [SerializeField] SphereCollider fogCollider;
        [SerializeField] Transform visualRoot;

        readonly List<TargetTick> trackedTargets = new List<TargetTick>(4);
        Action<ShadowRevenantDreadFogZone> releaseToPool;
        float damagePerTick;
        float tickInterval;
        float slowPercent;
        float lifetimeRemaining;
        bool released = true;

        public bool IsPoolActive => !released;

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
        }

        public void Activate(
            Vector3 position,
            float radius,
            float duration,
            float tickDamage,
            float damageInterval,
            float fogSlowPercent,
            IShadowRevenantTarget initialTarget)
        {
            CacheReferences();
            released = false;
            transform.position = position;
            trackedTargets.Clear();
            damagePerTick = Mathf.Max(0f, tickDamage);
            tickInterval = Mathf.Max(0.05f, damageInterval);
            slowPercent = Mathf.Clamp01(fogSlowPercent);
            lifetimeRemaining = Mathf.Max(0.05f, duration);

            if (fogCollider != null)
            {
                fogCollider.isTrigger = true;
                fogCollider.radius = Mathf.Max(0.1f, radius);
                fogCollider.enabled = true;
            }

            if (visualRoot != null)
            {
                float diameter = Mathf.Max(0.1f, radius) * 2f;
                visualRoot.localScale = new Vector3(diameter, visualRoot.localScale.y, diameter);
            }

            if (initialTarget != null)
                TrackTarget(initialTarget, Time.time);
        }

        void Update()
        {
            if (released)
                return;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
                DeactivateToPool();
        }

        void OnTriggerStay(Collider other)
        {
            if (released || other == null)
                return;

            IShadowRevenantTarget target = other.GetComponentInParent<IShadowRevenantTarget>();
            if (target == null || !target.CanReceiveShadowDamage)
                return;

            float now = Time.time;
            int index = TrackTarget(target, now);
            if (index < 0 || trackedTargets[index].NextDamageTime > now)
                return;

            target.ReceiveShadowDamage(damagePerTick);
            target.TryApplyDreadFogSlow(slowPercent, tickInterval);

            TargetTick tick = trackedTargets[index];
            tick.NextDamageTime = now + tickInterval;
            trackedTargets[index] = tick;
        }

        int TrackTarget(IShadowRevenantTarget target, float now)
        {
            for (var i = 0; i < trackedTargets.Count; i++)
            {
                if (trackedTargets[i].Target == target)
                    return i;
            }

            trackedTargets.Add(new TargetTick
            {
                Target = target,
                NextDamageTime = now
            });
            return trackedTargets.Count - 1;
        }

        public void DeactivateToPool()
        {
            if (released)
                return;

            released = true;
            trackedTargets.Clear();
            lifetimeRemaining = 0f;

            if (fogCollider != null)
                fogCollider.enabled = false;

            if (releaseToPool != null)
                releaseToPool(this);
            else
                gameObject.SetActive(false);
        }
    }
}
