using System.Collections;
using System.Collections.Generic;
using Beavermania.Data.Display;
using UnityEngine;

namespace Beavermania.Display
{
    [DisallowMultipleComponent]
    public sealed class FootstepVfxEmitter : MonoBehaviour
    {
        const int DefaultPoolSize = 8;

        [SerializeField] FootstepSurfaceEffectProfile surfaceProfile;
        [SerializeField] Transform raycastOrigin;
        [SerializeField] LayerMask groundLayers = ~0;
        [SerializeField] float raycastDistance = 1.45f;
        [SerializeField] float minStepInterval = 0.08f;
        [SerializeField] float originHeight = 0.35f;
        [SerializeField] int poolSize = DefaultPoolSize;

        readonly Queue<ParticleSystem> proceduralPool = new();
        float nextAllowedStepTime;

        public void PlayStep()
        {
            if (Time.time < nextAllowedStepTime)
                return;

            if (!TryResolveSurface(out RaycastHit hit, out FootstepSurfaceEffectProfile.SurfaceRule rule))
                return;

            nextAllowedStepTime = Time.time + Mathf.Max(0f, minStepInterval);

            if (rule.EffectPrefab != null)
            {
                PooledOneShotVfx.Spawn(rule.EffectPrefab, hit.point, Quaternion.LookRotation(hit.normal), Vector3.one * rule.Scale);
                return;
            }

            PlayProceduralBurst(hit.point, hit.normal, rule);
        }

        void PlayProceduralBurst(Vector3 position, Vector3 normal, FootstepSurfaceEffectProfile.SurfaceRule rule)
        {
            ParticleSystem particles = GetProceduralParticleSystem();
            if (particles == null)
                return;

            particles.transform.SetPositionAndRotation(position + normal * 0.025f, Quaternion.LookRotation(normal));
            particles.transform.localScale = Vector3.one * rule.Scale;
            ApplyParticleColor(particles, rule.Color);
            particles.gameObject.SetActive(true);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
            StartCoroutine(ReturnWhenComplete(particles));
        }

        bool TryResolveSurface(out RaycastHit hit, out FootstepSurfaceEffectProfile.SurfaceRule rule)
        {
            Transform origin = raycastOrigin != null ? raycastOrigin : transform;
            Vector3 start = origin.position + Vector3.up * originHeight;
            if (!Physics.Raycast(start, Vector3.down, out hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                rule = default;
                return false;
            }

            rule = surfaceProfile != null
                ? surfaceProfile.Resolve(hit)
                : ResolveDefaultSurface(hit);
            return true;
        }

        FootstepSurfaceEffectProfile.SurfaceRule ResolveDefaultSurface(RaycastHit hit)
        {
            Collider hitCollider = hit.collider;
            if (hitCollider != null)
            {
                string tag = hitCollider.tag;
                if (string.Equals(tag, "Bridge", System.StringComparison.OrdinalIgnoreCase))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Wood);
                if (string.Equals(tag, "Isle", System.StringComparison.OrdinalIgnoreCase))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Grass);
                if (string.Equals(tag, "Tile", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(tag, "stairs", System.StringComparison.OrdinalIgnoreCase))
                {
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Dust);
                }

                string materialName = hitCollider.sharedMaterial != null ? hitCollider.sharedMaterial.name : null;
                if (Contains(materialName, "wood"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Wood);
                if (Contains(materialName, "sand"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Sand);
                if (Contains(materialName, "mud"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Mud);
                if (Contains(materialName, "water"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Water);
            }

            return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Dust);
        }

        ParticleSystem GetProceduralParticleSystem()
        {
            if (proceduralPool.Count > 0)
                return proceduralPool.Dequeue();

            if (poolSize <= 0)
                return null;

            var instance = new GameObject("FootstepDustVfx", typeof(ParticleSystem));
            instance.transform.SetParent(transform, false);
            var particles = instance.GetComponent<ParticleSystem>();
            ConfigureParticles(particles);
            instance.SetActive(false);
            poolSize--;
            return particles;
        }

        IEnumerator ReturnWhenComplete(ParticleSystem particles)
        {
            if (particles == null)
                yield break;

            float lifetime = GetLifetime(particles);
            yield return new WaitForSeconds(lifetime);

            if (particles == null)
                yield break;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.gameObject.SetActive(false);
            proceduralPool.Enqueue(particles);
        }

        static void ConfigureParticles(ParticleSystem particles)
        {
            var main = particles.main;
            main.duration = 0.28f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 18;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;
            shape.arc = 360f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);

            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(0.7f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });
        }

        static void ApplyParticleColor(ParticleSystem particles, Color color)
        {
            var main = particles.main;
            main.startColor = color;
        }

        static float GetLifetime(ParticleSystem particles)
        {
            var main = particles.main;
            return main.duration + Max(main.startLifetime);
        }

        static float Max(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return curve.constantMax;
                default:
                    return 0.4f;
            }
        }

        static bool Contains(string value, string fragment)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
