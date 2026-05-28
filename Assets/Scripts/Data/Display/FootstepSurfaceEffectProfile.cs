using System;
using UnityEngine;

namespace Beavermania.Data.Display
{
    public enum FootstepSurfaceType
    {
        Dust,
        Grass,
        Sand,
        Mud,
        Wood,
        Water
    }

    [CreateAssetMenu(fileName = "FootstepSurfaceEffectProfile", menuName = "Beavermania/Display/Footstep Surface Effect Profile")]
    public sealed class FootstepSurfaceEffectProfile : ScriptableObject
    {
        [SerializeField] SurfaceRule[] rules;
        [SerializeField] SurfaceRule fallback = SurfaceRule.CreateDefault(FootstepSurfaceType.Dust);

        public SurfaceRule Resolve(RaycastHit hit)
        {
            if (rules != null)
            {
                for (int i = 0; i < rules.Length; i++)
                {
                    if (rules[i].Matches(hit))
                        return rules[i];
                }
            }

            return fallback.IsConfigured ? fallback : SurfaceRule.CreateDefault(FootstepSurfaceType.Dust);
        }

        void OnValidate()
        {
            if (!fallback.IsConfigured)
                fallback = SurfaceRule.CreateDefault(FootstepSurfaceType.Dust);
        }

        [Serializable]
        public struct SurfaceRule
        {
            [SerializeField] FootstepSurfaceType surfaceType;
            [SerializeField] Color color;
            [SerializeField] string[] tags;
            [SerializeField] string[] materialNameContains;
            [SerializeField] string[] physicsMaterialNameContains;
            [SerializeField] LayerMask layers;
            [SerializeField] GameObject effectPrefab;
            [SerializeField] float scale;

            public FootstepSurfaceType SurfaceType => surfaceType;
            public Color Color => color;
            public GameObject EffectPrefab => effectPrefab;
            public float Scale => scale > 0f ? scale : 1f;
            public bool IsConfigured => color.a > 0f || effectPrefab != null;

            public bool Matches(RaycastHit hit)
            {
                Collider hitCollider = hit.collider;
                if (hitCollider == null)
                    return false;

                if (MatchesLayer(hitCollider.gameObject.layer))
                    return true;

                if (MatchesTag(hitCollider.tag))
                    return true;

                if (MatchesName(hitCollider.sharedMaterial != null ? hitCollider.sharedMaterial.name : null, physicsMaterialNameContains))
                    return true;

                var renderer = hitCollider.GetComponent<Renderer>();
                if (renderer != null && MatchesName(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : null, materialNameContains))
                    return true;

                return false;
            }

            public static SurfaceRule CreateDefault(FootstepSurfaceType type)
            {
                Color resolvedColor;
                switch (type)
                {
                    case FootstepSurfaceType.Grass:
                        resolvedColor = new Color(0.34f, 0.65f, 0.28f, 0.72f);
                        break;
                    case FootstepSurfaceType.Sand:
                        resolvedColor = new Color(0.88f, 0.72f, 0.42f, 0.68f);
                        break;
                    case FootstepSurfaceType.Mud:
                        resolvedColor = new Color(0.28f, 0.16f, 0.08f, 0.7f);
                        break;
                    case FootstepSurfaceType.Wood:
                        resolvedColor = new Color(0.48f, 0.28f, 0.12f, 0.62f);
                        break;
                    case FootstepSurfaceType.Water:
                        resolvedColor = new Color(0.35f, 0.7f, 1f, 0.6f);
                        break;
                    default:
                        resolvedColor = new Color(0.68f, 0.58f, 0.44f, 0.65f);
                        break;
                }

                return new SurfaceRule
                {
                    surfaceType = type,
                    color = resolvedColor,
                    scale = 1f
                };
            }

            static bool MatchesName(string candidate, string[] fragments)
            {
                if (string.IsNullOrWhiteSpace(candidate) || fragments == null)
                    return false;

                for (int i = 0; i < fragments.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(fragments[i])
                        && candidate.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            bool MatchesTag(string candidate)
            {
                if (string.IsNullOrWhiteSpace(candidate) || tags == null)
                    return false;

                for (int i = 0; i < tags.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(tags[i])
                        && string.Equals(candidate, tags[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool MatchesLayer(int layer)
            {
                return layers.value != 0 && (layers.value & (1 << layer)) != 0;
            }
        }
    }
}
