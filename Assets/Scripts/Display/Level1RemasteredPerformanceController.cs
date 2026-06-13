using System;
using Beavermania.Data.Display;
using UnityEngine;

namespace Beavermania.Display
{
    public sealed class Level1RemasteredPerformanceController : MonoBehaviour
    {
        [SerializeField] PerformanceBudgetProfile profile;
        [SerializeField] Terrain[] cachedTerrains;
        [SerializeField] GameObject perfCanopyClusterHigh;
        [SerializeField] GameObject perfCanopyClusterProxy;

        FrameBudgetGovernor governor;
        RuntimeTier[] runtimeTiers = Array.Empty<RuntimeTier>();
        TerrainSnapshot[] terrainSnapshots = Array.Empty<TerrainSnapshot>();
        bool missingConfigurationLogged;
        bool globalSnapshotCaptured;
        bool clusterSnapshotCaptured;
        bool hasValidConfiguration;
        int currentAppliedTierIndex = -1;
        GlobalQualitySnapshot globalSnapshot;
        ClusterSnapshot clusterSnapshot;

        public int CurrentTier => currentAppliedTierIndex >= 0
            ? currentAppliedTierIndex
            : governor != null
                ? governor.CurrentTier
                : 0;

        void Awake()
        {
            EnsureRuntimeCacheSynchronized();
        }

        void OnEnable()
        {
            EnsureRuntimeCacheSynchronized();
            CaptureGlobalSnapshot();
            CaptureClusterSnapshot();

            hasValidConfiguration = TryValidateConfiguration(out string failureReason);
            if (!hasValidConfiguration)
            {
                LogMissingConfigurationOnce(failureReason);
                return;
            }

            governor.Reset();
            ApplyTier(governor.CurrentTier);
        }

        void Update()
        {
            if (!hasValidConfiguration || governor == null)
                return;

            governor.RecordFrame(Time.unscaledDeltaTime);
            if (governor.TryStepTier(out int nextTier))
                ApplyTier(nextTier);
        }

        void OnDisable()
        {
            RestoreSnapshots();
        }

        void OnDestroy()
        {
            RestoreSnapshots();
        }

        void EnsureRuntimeCacheSynchronized()
        {
            cachedTerrains ??= Array.Empty<Terrain>();

            if (terrainSnapshots.Length != cachedTerrains.Length)
                terrainSnapshots = new TerrainSnapshot[cachedTerrains.Length];

            if (profile == null || profile.TierCount < 1)
            {
                governor = null;
                runtimeTiers = Array.Empty<RuntimeTier>();
                return;
            }

            if (governor == null || runtimeTiers.Length != profile.TierCount)
            {
                governor = BuildGovernor();
                runtimeTiers = BuildRuntimeTiers();
            }
        }

        FrameBudgetGovernor BuildGovernor()
        {
            if (profile == null || profile.TierCount < 1)
                return null;

            return new FrameBudgetGovernor(
                profile.StartingTierIndex,
                0,
                profile.TierCount - 1,
                profile.RollingWindowSize,
                profile.DegradeAverageFrameTimeMs,
                profile.DegradeConsecutiveFrames,
                profile.RecoverAverageFrameTimeMs,
                profile.RecoverConsecutiveFrames,
                profile.TierChangeCooldownSeconds);
        }

        RuntimeTier[] BuildRuntimeTiers()
        {
            if (profile == null || profile.TierCount < 1)
                return Array.Empty<RuntimeTier>();

            var tiers = new RuntimeTier[profile.TierCount];
            for (int index = 0; index < profile.TierCount; index++)
            {
                if (!profile.TryGetTier(index, out PerformanceBudgetProfile.TierDefinition definition))
                    return Array.Empty<RuntimeTier>();

                int qualityLevelIndex = ResolveQualityLevelIndex(definition.QualityLevelName);
                if (qualityLevelIndex < 0)
                    return Array.Empty<RuntimeTier>();

                tiers[index] = new RuntimeTier(definition, qualityLevelIndex);
            }

            return tiers;
        }

        bool TryValidateConfiguration(out string failureReason)
        {
            if (profile == null)
            {
                failureReason = "Profile is not assigned.";
                return false;
            }

            if (profile.TierCount != 4)
            {
                failureReason = $"Expected 4 tiers but found {profile.TierCount}.";
                return false;
            }

            if (governor == null || runtimeTiers.Length != profile.TierCount)
            {
                failureReason = "Runtime tier data could not be built.";
                return false;
            }

            if (perfCanopyClusterHigh == null || perfCanopyClusterProxy == null)
            {
                failureReason = "Canopy cluster roots are not assigned.";
                return false;
            }

            if (cachedTerrains == null || cachedTerrains.Length == 0)
            {
                failureReason = "Cached terrain list is empty.";
                return false;
            }

            for (int index = 0; index < cachedTerrains.Length; index++)
            {
                if (cachedTerrains[index] == null)
                {
                    failureReason = $"Cached terrain at index {index} is missing.";
                    return false;
                }
            }

            failureReason = null;
            return true;
        }

        void ApplyTier(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= runtimeTiers.Length)
                return;

            RuntimeTier tier = runtimeTiers[tierIndex];
            QualitySettings.SetQualityLevel(tier.QualityLevelIndex, true);
            QualitySettings.shadowDistance = tier.ShadowDistance;

            if (perfCanopyClusterHigh != null)
                perfCanopyClusterHigh.SetActive(tier.HighClusterEnabled);

            if (perfCanopyClusterProxy != null)
                perfCanopyClusterProxy.SetActive(tier.ProxyClusterEnabled);

            for (int index = 0; index < cachedTerrains.Length; index++)
            {
                Terrain terrain = cachedTerrains[index];
                terrain.treeDistance = tier.TreeDistance;
                terrain.treeBillboardDistance = tier.BillboardDistance;
                terrain.treeMaximumFullLODCount = tier.TreeMaximumFullLodCount;
                terrain.detailObjectDistance = tier.DetailObjectDistance;
                terrain.detailObjectDensity = tier.DetailObjectDensity;
                terrain.heightmapPixelError = tier.HeightmapPixelError;
                terrain.basemapDistance = tier.SplatMapDistance;
            }

            currentAppliedTierIndex = tierIndex;
        }

        void CaptureGlobalSnapshot()
        {
            if (globalSnapshotCaptured)
                return;

            globalSnapshot = new GlobalQualitySnapshot(
                QualitySettings.GetQualityLevel(),
                QualitySettings.shadowDistance);
            globalSnapshotCaptured = true;

            for (int index = 0; index < cachedTerrains.Length; index++)
            {
                Terrain terrain = cachedTerrains[index];
                if (terrain == null)
                    continue;

                terrainSnapshots[index] = new TerrainSnapshot(terrain);
            }
        }

        void CaptureClusterSnapshot()
        {
            if (clusterSnapshotCaptured)
                return;

            clusterSnapshot = new ClusterSnapshot(
                perfCanopyClusterHigh != null && perfCanopyClusterHigh.activeSelf,
                perfCanopyClusterProxy != null && perfCanopyClusterProxy.activeSelf);
            clusterSnapshotCaptured = true;
        }

        void RestoreSnapshots()
        {
            hasValidConfiguration = false;
            currentAppliedTierIndex = -1;
            bool shouldRestoreTerrainSnapshots = globalSnapshotCaptured;

            if (globalSnapshotCaptured)
            {
                QualitySettings.SetQualityLevel(globalSnapshot.QualityLevelIndex, true);
                QualitySettings.shadowDistance = globalSnapshot.ShadowDistance;
                globalSnapshotCaptured = false;
            }

            if (clusterSnapshotCaptured)
            {
                if (perfCanopyClusterHigh != null)
                    perfCanopyClusterHigh.SetActive(clusterSnapshot.HighClusterActive);

                if (perfCanopyClusterProxy != null)
                    perfCanopyClusterProxy.SetActive(clusterSnapshot.ProxyClusterActive);

                clusterSnapshotCaptured = false;
            }

            if (!shouldRestoreTerrainSnapshots)
                return;

            for (int index = 0; index < cachedTerrains.Length; index++)
            {
                Terrain terrain = cachedTerrains[index];
                if (terrain == null)
                    continue;

                terrainSnapshots[index].ApplyTo(terrain);
            }
        }

        void LogMissingConfigurationOnce(string failureReason)
        {
            if (missingConfigurationLogged)
                return;

            missingConfigurationLogged = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"{nameof(Level1RemasteredPerformanceController)} disabled: {failureReason}", this);
#endif
        }

        static int ResolveQualityLevelIndex(string qualityLevelName)
        {
            if (string.IsNullOrWhiteSpace(qualityLevelName))
                return -1;

            string[] qualityLevelNames = QualitySettings.names;
            for (int index = 0; index < qualityLevelNames.Length; index++)
            {
                if (string.Equals(qualityLevelNames[index], qualityLevelName, StringComparison.Ordinal))
                    return index;
            }

            return -1;
        }

        readonly struct RuntimeTier
        {
            public RuntimeTier(PerformanceBudgetProfile.TierDefinition definition, int qualityLevelIndex)
            {
                QualityLevelIndex = qualityLevelIndex;
                HighClusterEnabled = definition.HighClusterEnabled;
                ProxyClusterEnabled = definition.ProxyClusterEnabled;
                TreeDistance = definition.TreeDistance;
                BillboardDistance = definition.BillboardDistance;
                TreeMaximumFullLodCount = definition.TreeMaximumFullLodCount;
                DetailObjectDistance = definition.DetailObjectDistance;
                DetailObjectDensity = definition.DetailObjectDensity;
                HeightmapPixelError = definition.HeightmapPixelError;
                SplatMapDistance = definition.SplatMapDistance;
                ShadowDistance = definition.ShadowDistance;
            }

            public int QualityLevelIndex { get; }
            public bool HighClusterEnabled { get; }
            public bool ProxyClusterEnabled { get; }
            public float TreeDistance { get; }
            public float BillboardDistance { get; }
            public int TreeMaximumFullLodCount { get; }
            public float DetailObjectDistance { get; }
            public float DetailObjectDensity { get; }
            public float HeightmapPixelError { get; }
            public float SplatMapDistance { get; }
            public float ShadowDistance { get; }
        }

        readonly struct GlobalQualitySnapshot
        {
            public GlobalQualitySnapshot(int qualityLevelIndex, float shadowDistance)
            {
                QualityLevelIndex = qualityLevelIndex;
                ShadowDistance = shadowDistance;
            }

            public int QualityLevelIndex { get; }
            public float ShadowDistance { get; }
        }

        readonly struct ClusterSnapshot
        {
            public ClusterSnapshot(bool highClusterActive, bool proxyClusterActive)
            {
                HighClusterActive = highClusterActive;
                ProxyClusterActive = proxyClusterActive;
            }

            public bool HighClusterActive { get; }
            public bool ProxyClusterActive { get; }
        }

        struct TerrainSnapshot
        {
            readonly float treeDistance;
            readonly float billboardDistance;
            readonly int treeMaximumFullLodCount;
            readonly float detailObjectDistance;
            readonly float detailObjectDensity;
            readonly float heightmapPixelError;
            readonly float splatMapDistance;

            public TerrainSnapshot(Terrain terrain)
            {
                treeDistance = terrain.treeDistance;
                billboardDistance = terrain.treeBillboardDistance;
                treeMaximumFullLodCount = terrain.treeMaximumFullLODCount;
                detailObjectDistance = terrain.detailObjectDistance;
                detailObjectDensity = terrain.detailObjectDensity;
                heightmapPixelError = terrain.heightmapPixelError;
                splatMapDistance = terrain.basemapDistance;
            }

            public void ApplyTo(Terrain terrain)
            {
                terrain.treeDistance = treeDistance;
                terrain.treeBillboardDistance = billboardDistance;
                terrain.treeMaximumFullLODCount = treeMaximumFullLodCount;
                terrain.detailObjectDistance = detailObjectDistance;
                terrain.detailObjectDensity = detailObjectDensity;
                terrain.heightmapPixelError = heightmapPixelError;
                terrain.basemapDistance = splatMapDistance;
            }
        }
    }
}
