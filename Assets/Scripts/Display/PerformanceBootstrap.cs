using UnityEngine;

namespace Beavermania.Display
{
    public static class PerformanceBootstrap
    {
        const int TargetFrameRate = 60;
        const int StandaloneMediumQuality = 1;
        const float MaxShadowDistance = 30f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyPerformancePolicy()
        {
            Application.targetFrameRate = TargetFrameRate;

#if !UNITY_EDITOR
            if (QualitySettings.GetQualityLevel() > StandaloneMediumQuality)
                QualitySettings.SetQualityLevel(StandaloneMediumQuality, applyExpensiveChanges: true);

            QualitySettings.realtimeReflectionProbes = false;

            if (QualitySettings.shadowDistance > MaxShadowDistance)
                QualitySettings.shadowDistance = MaxShadowDistance;

            if (QualitySettings.antiAliasing > 0)
                QualitySettings.antiAliasing = 0;

            ApplyMediumQualityGpuTuning();
#endif
        }

        static void ApplyMediumQualityGpuTuning()
        {
            if (QualitySettings.GetQualityLevel() != StandaloneMediumQuality)
                return;

            if (QualitySettings.pixelLightCount > 2)
                QualitySettings.pixelLightCount = 2;

            if (QualitySettings.shadowCascades > 1)
                QualitySettings.shadowCascades = 1;

            QualitySettings.softVegetation = false;
        }

#if DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureDevStatsLogger()
        {
            var loggerObject = new GameObject(nameof(PerformanceDevStatsLogger));
            loggerObject.hideFlags = HideFlags.HideAndDontSave;
            loggerObject.AddComponent<PerformanceDevStatsLogger>();
            Object.DontDestroyOnLoad(loggerObject);
        }
#endif
    }

#if DEVELOPMENT_BUILD
    sealed class PerformanceDevStatsLogger : MonoBehaviour
    {
        const float LogIntervalSeconds = 5f;
        const float LowFpsThreshold = 45f;
        const float QualityFallbackSeconds = 4f;
        const float WarmupSeconds = 15f;
        const int FastQualityLevel = 0;

        float logTimer;
        float fpsAccum;
        int fpsSamples;
        float lowFpsTimer;
        bool qualityFallbackApplied;
        float sceneStartTime;

        void Awake()
        {
            sceneStartTime = Time.unscaledTime;
            logTimer = LogIntervalSeconds;
        }

        void Update()
        {
            if (Time.unscaledTime - sceneStartTime < WarmupSeconds)
                return;

            float instantaneousFps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            fpsAccum += instantaneousFps;
            fpsSamples++;

            if (instantaneousFps < LowFpsThreshold)
                lowFpsTimer += Time.unscaledDeltaTime;
            else
                lowFpsTimer = 0f;

#if !UNITY_EDITOR
            TryApplyQualityFallback();
#endif

            logTimer -= Time.unscaledDeltaTime;
            if (logTimer > 0f)
                return;

            logTimer = LogIntervalSeconds;
            float averageFps = fpsSamples > 0 ? fpsAccum / fpsSamples : 0f;
            fpsAccum = 0f;
            fpsSamples = 0;

            if (averageFps < LowFpsThreshold)
                LogLowFpsDetails(averageFps);
            else
                Debug.Log($"[Perf] avg FPS~{averageFps:F0} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} shadowDist={QualitySettings.shadowDistance}");
        }

        void TryApplyQualityFallback()
        {
            if (qualityFallbackApplied)
                return;

            if (lowFpsTimer < QualityFallbackSeconds)
                return;

            if (QualitySettings.GetQualityLevel() <= FastQualityLevel)
                return;

            qualityFallbackApplied = true;
            QualitySettings.SetQualityLevel(FastQualityLevel, applyExpensiveChanges: true);
            Debug.LogWarning($"[Perf] sustained FPS below {LowFpsThreshold}; falling back to {QualitySettings.names[QualitySettings.GetQualityLevel()]} quality.");
        }

        void LogLowFpsDetails(float averageFps)
        {
#if UNITY_EDITOR
            Debug.Log(
                $"[Perf] low FPS avg~{averageFps:F0} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} " +
                $"batches={UnityEditor.UnityStats.batches} drawCalls={UnityEditor.UnityStats.drawCalls} " +
                $"tris={UnityEditor.UnityStats.triangles} setPass={UnityEditor.UnityStats.setPassCalls} " +
                $"shadowCasters={UnityEditor.UnityStats.shadowCasters}");
#else
            Debug.LogWarning(
                $"[Perf] low FPS avg~{averageFps:F0} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} shadowDist={QualitySettings.shadowDistance}");
#endif
        }
    }
#endif
}
