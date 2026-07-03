using System.Collections;
using System.Collections.Generic;
using Beavermania.Data.Display;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beavermania.Display
{
    [DisallowMultipleComponent]
    public sealed class FootstepVfxEmitter : MonoBehaviour
    {
        const int DefaultPoolSize = 8;
        const string UrpParticleUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";
        const string LegacyParticleShaderName = "Particles/Standard Unlit";
        const string DefaultFootstepMaterialPath = "Assets/Materials/VFX/M_FootstepDust_Cartoon.mat";

        static Material s_runtimeFallbackMaterial;
        static readonly RaycastHit[] s_footstepHits = new RaycastHit[8];

        [SerializeField] FootstepSurfaceEffectProfile surfaceProfile;
        [SerializeField] Material footstepParticleMaterial;
        [SerializeField] Transform raycastOrigin;
        [SerializeField] LayerMask groundLayers = ~0;
        [SerializeField] float raycastDistance = 2.5f;
        [SerializeField] float minStepInterval = 0.08f;
        [SerializeField] float originHeight = 0.35f;
        [SerializeField] int poolSize = DefaultPoolSize;

        readonly Queue<ParticleSystem> proceduralPool = new();
        float nextAllowedStepTime;
        WaitForSeconds cachedReturnWait;

        void Awake()
        {
            EnsureFootstepMaterialAssigned();
            PrewarmPool(Mathf.Min(2, poolSize));
        }

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
            int count = Physics.RaycastNonAlloc(start, Vector3.down, s_footstepHits, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (count == 0) { hit = default; rule = default; return false; }

            int closestIndex = -1;
            float closestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = s_footstepHits[i].collider;
                if (hitCollider == null || IsOwnedCollider(hitCollider))
                    continue;
                if (s_footstepHits[i].distance < closestDist)
                {
                    closestDist = s_footstepHits[i].distance;
                    closestIndex = i;
                }
            }

            if (closestIndex < 0) { hit = default; rule = default; return false; }

            hit = s_footstepHits[closestIndex];
            rule = surfaceProfile != null
                ? surfaceProfile.Resolve(hit)
                : ResolveDefaultSurface(hit);
            return true;
        }

        bool IsOwnedCollider(Collider collider)
        {
            Transform hitTransform = collider.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform);
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

                string physicsMaterialName = hitCollider.sharedMaterial != null ? hitCollider.sharedMaterial.name : null;
                string rendererMaterialName = ResolveRendererMaterialName(hitCollider);
                if (Contains(physicsMaterialName, "wood") || Contains(rendererMaterialName, "wood"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Wood);
                if (Contains(physicsMaterialName, "sand") || Contains(rendererMaterialName, "sand"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Sand);
                if (Contains(physicsMaterialName, "mud") || Contains(rendererMaterialName, "mud"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Mud);
                if (Contains(physicsMaterialName, "water") || Contains(rendererMaterialName, "water"))
                    return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Water);
            }

            return FootstepSurfaceEffectProfile.SurfaceRule.CreateDefault(FootstepSurfaceType.Dust);
        }

        void PrewarmPool(int count)
        {
            for (int i = 0; i < count && poolSize > 0; i++)
            {
                ParticleSystem particles = CreateProceduralParticleSystem();
                if (particles == null)
                    break;

                particles.gameObject.SetActive(false);
                proceduralPool.Enqueue(particles);
            }
        }

        ParticleSystem GetProceduralParticleSystem()
        {
            if (proceduralPool.Count > 0)
                return proceduralPool.Dequeue();

            if (poolSize <= 0)
                return null;

            ParticleSystem particles = CreateProceduralParticleSystem();
            if (particles != null)
                poolSize--;

            return particles;
        }

        ParticleSystem CreateProceduralParticleSystem()
        {
            var instance = new GameObject("FootstepDustVfx");
            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
            var particles = instance.AddComponent<ParticleSystem>();
            ConfigureParticles(particles);
            return particles;
        }

        IEnumerator ReturnWhenComplete(ParticleSystem particles)
        {
            if (particles == null)
                yield break;

            if (cachedReturnWait == null)
                cachedReturnWait = new WaitForSeconds(GetLifetime(particles));

            yield return cachedReturnWait;

            if (particles == null)
                yield break;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.gameObject.SetActive(false);
            proceduralPool.Enqueue(particles);
        }

        void ConfigureParticles(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
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
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

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

            ApplyParticleRendererMaterial(particles);
        }

        void ApplyParticleRendererMaterial(ParticleSystem particles)
        {
            Material material = ResolveFootstepMaterial();
            if (material == null)
                return;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
                return;

            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material;
        }

        void EnsureFootstepMaterialAssigned()
        {
            if (footstepParticleMaterial != null)
                return;

#if UNITY_EDITOR
            footstepParticleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(DefaultFootstepMaterialPath);
#endif
        }

        Material ResolveFootstepMaterial()
        {
            if (footstepParticleMaterial != null)
                return footstepParticleMaterial;

            if (s_runtimeFallbackMaterial != null)
                return s_runtimeFallbackMaterial;

            Shader shader = Shader.Find(UrpParticleUnlitShaderName);
            if (shader == null)
                shader = Shader.Find(LegacyParticleShaderName);
            if (shader == null)
                return null;

            var dustColor = new Color(0.75f, 0.62f, 0.42f, 0.4f);
            s_runtimeFallbackMaterial = new Material(shader);
            if (s_runtimeFallbackMaterial.HasProperty("_BaseColor"))
                s_runtimeFallbackMaterial.SetColor("_BaseColor", dustColor);
            else
                s_runtimeFallbackMaterial.color = dustColor;

            return s_runtimeFallbackMaterial;
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

        static string ResolveRendererMaterialName(Collider hitCollider)
        {
            if (hitCollider == null)
                return null;

            Renderer renderer = hitCollider.GetComponent<Renderer>();
            if (renderer == null)
                renderer = hitCollider.GetComponentInParent<Renderer>();

            return renderer != null && renderer.sharedMaterial != null
                ? renderer.sharedMaterial.name
                : null;
        }
    }
}
