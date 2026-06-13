using System;
using UnityEngine;

namespace Beavermania.Data.Display
{
    [CreateAssetMenu(fileName = "PerformanceBudgetProfile", menuName = "Beavermania/Display/Performance Budget Profile")]
    public sealed class PerformanceBudgetProfile : ScriptableObject
    {
        [SerializeField] int startingTierIndex = 1;
        [SerializeField] int rollingWindowSize = 60;
        [SerializeField] float degradeAverageFrameTimeMs = 17.2f;
        [SerializeField] int degradeConsecutiveFrames = 45;
        [SerializeField] float recoverAverageFrameTimeMs = 14.9f;
        [SerializeField] int recoverConsecutiveFrames = 300;
        [SerializeField] float tierChangeCooldownSeconds = 1f;
        [SerializeField] TierDefinition[] tiers = Array.Empty<TierDefinition>();

        public int StartingTierIndex => Mathf.Clamp(startingTierIndex, 0, Mathf.Max(0, TierCount - 1));
        public int RollingWindowSize => Mathf.Max(1, rollingWindowSize);
        public float DegradeAverageFrameTimeMs => Mathf.Max(0f, degradeAverageFrameTimeMs);
        public int DegradeConsecutiveFrames => Mathf.Max(1, degradeConsecutiveFrames);
        public float RecoverAverageFrameTimeMs => Mathf.Max(0f, recoverAverageFrameTimeMs);
        public int RecoverConsecutiveFrames => Mathf.Max(1, recoverConsecutiveFrames);
        public float TierChangeCooldownSeconds => Mathf.Max(0f, tierChangeCooldownSeconds);
        public int TierCount => tiers != null ? tiers.Length : 0;

        public bool TryGetTier(int index, out TierDefinition tier)
        {
            if (tiers != null && index >= 0 && index < tiers.Length)
            {
                tier = tiers[index];
                return true;
            }

            tier = default;
            return false;
        }

        void OnValidate()
        {
            if (rollingWindowSize < 1)
                rollingWindowSize = 1;

            if (degradeConsecutiveFrames < 1)
                degradeConsecutiveFrames = 1;

            if (recoverConsecutiveFrames < 1)
                recoverConsecutiveFrames = 1;

            if (tierChangeCooldownSeconds < 0f)
                tierChangeCooldownSeconds = 0f;

            tiers ??= Array.Empty<TierDefinition>();
            startingTierIndex = Mathf.Clamp(startingTierIndex, 0, Mathf.Max(0, tiers.Length - 1));
        }

        [Serializable]
        public struct TierDefinition
        {
            [SerializeField] string label;
            [SerializeField] string qualityLevelName;
            [SerializeField] bool highClusterEnabled;
            [SerializeField] bool proxyClusterEnabled;
            [SerializeField] float treeDistance;
            [SerializeField] float billboardDistance;
            [SerializeField] int treeMaximumFullLodCount;
            [SerializeField] float detailObjectDistance;
            [SerializeField] float detailObjectDensity;
            [SerializeField] float heightmapPixelError;
            [SerializeField] float splatMapDistance;
            [SerializeField] float shadowDistance;

            public string Label => string.IsNullOrWhiteSpace(label) ? qualityLevelName : label;
            public string QualityLevelName => qualityLevelName;
            public bool HighClusterEnabled => highClusterEnabled;
            public bool ProxyClusterEnabled => proxyClusterEnabled;
            public float TreeDistance => Mathf.Max(0f, treeDistance);
            public float BillboardDistance => Mathf.Max(0f, billboardDistance);
            public int TreeMaximumFullLodCount => Mathf.Max(0, treeMaximumFullLodCount);
            public float DetailObjectDistance => Mathf.Max(0f, detailObjectDistance);
            public float DetailObjectDensity => Mathf.Max(0f, detailObjectDensity);
            public float HeightmapPixelError => Mathf.Max(0f, heightmapPixelError);
            public float SplatMapDistance => Mathf.Max(0f, splatMapDistance);
            public float ShadowDistance => Mathf.Max(0f, shadowDistance);
        }
    }
}
