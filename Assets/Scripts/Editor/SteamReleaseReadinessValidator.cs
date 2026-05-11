using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SteamReleaseReadinessValidator
{
    const string MenuPath = "Tools/Beavermania/Validate Steam Release Readiness";
    const string LoggingConfigAssetPath = "Assets/Config/LoggingConfig.asset";
    const string LoggingConfigResourcePath = "RuntimeConfig/LoggingConfig";

    static readonly string[] DevelopmentDiagnosticFiles =
    {
        "Assets/Scripts/Core/DevelopmentRuntimeDiagnostics.cs",
        "Assets/Scripts/Debug/DebugBootstrapper.cs",
        "Assets/Scripts/Debug/DebugDamageTrigger.cs",
        "Assets/Scripts/Debug/DebugCheckpointTeleport.cs",
        "Assets/Scripts/Debug/DebugSceneResetShortcut.cs",
        "Assets/Scripts/Debug/FpsDisplay.cs"
    };

    [MenuItem(MenuPath)]
    public static void ValidateSteamReleaseReadiness()
    {
        var report = new StringBuilder();
        var warningCount = 0;

        report.AppendLine("Steam release readiness validation (audit only; no project/build settings are modified).");
        AppendBuildFlagAudit(report, ref warningCount);
        AppendDisplayAudit(report, ref warningCount);
        AppendQualityAudit(report, ref warningCount);
        AppendRenderPipelineAudit(report, ref warningCount);
        AppendLoggingAudit(report, ref warningCount);
        AppendDevelopmentDiagnosticsAudit(report, ref warningCount);
        AppendManualChecks(report);

        if (warningCount > 0)
        {
            Debug.LogWarning(report.ToString());
        }
        else
        {
            Debug.Log(report.ToString());
        }
    }

    static void AppendBuildFlagAudit(StringBuilder report, ref int warningCount)
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

        report.AppendLine("Build flags:");
        report.AppendLine("- active build target: " + buildTarget + " / " + buildTargetGroup);
        report.AppendLine("- development build: " + EditorUserBuildSettings.development);
        report.AppendLine("- script debugging: " + EditorUserBuildSettings.allowDebugging);
        report.AppendLine("- autoconnect profiler: " + GetEditorUserBuildBool("connectProfiler"));
        report.AppendLine("- deep profiling: " + GetEditorUserBuildBool("buildWithDeepProfilingSupport"));
        report.AppendLine("- scripting define symbols: " + (string.IsNullOrEmpty(symbols) ? "<none>" : symbols));

        AddWarningIf(EditorUserBuildSettings.development, report, ref warningCount, "Development Build must be off for Steam release candidates.");
        AddWarningIf(EditorUserBuildSettings.allowDebugging, report, ref warningCount, "Script Debugging must be off for Steam release candidates.");
        AddWarningIf(ContainsSymbol(symbols, "DEVELOPMENT_BUILD"), report, ref warningCount, "DEVELOPMENT_BUILD is present in scripting define symbols; remove release diagnostics override.");
    }

    static void AppendDisplayAudit(StringBuilder report, ref int warningCount)
    {
        report.AppendLine("Display defaults:");
        report.AppendLine("- fullscreen mode: " + PlayerSettings.fullScreenMode);
        report.AppendLine("- default native resolution: " + PlayerSettings.defaultIsNativeResolution);
        report.AppendLine("- default resolution: " + PlayerSettings.defaultScreenWidth + "x" + PlayerSettings.defaultScreenHeight);
        report.AppendLine("- resizable window: " + PlayerSettings.resizableWindow);
        report.AppendLine("- allow fullscreen switch: " + PlayerSettings.allowFullscreenSwitch);
        report.AppendLine("- run in background: " + PlayerSettings.runInBackground);

        AddWarningIf(!PlayerSettings.defaultIsNativeResolution && (PlayerSettings.defaultScreenWidth < 1280 || PlayerSettings.defaultScreenHeight < 720), report, ref warningCount, "Default resolution is below 1280x720; manually confirm Steam first-launch UX.");
    }

    static void AppendQualityAudit(StringBuilder report, ref int warningCount)
    {
        var qualityLevel = QualitySettings.GetQualityLevel();
        var qualityNames = QualitySettings.names;
        var qualityName = qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "<unknown>";

        report.AppendLine("Quality / frame pacing:");
        report.AppendLine("- active quality: " + qualityLevel + " / " + qualityName);
        report.AppendLine("- vSyncCount: " + QualitySettings.vSyncCount);
        report.AppendLine("- Application.targetFrameRate: " + Application.targetFrameRate);
        report.AppendLine("- shadows: " + QualitySettings.shadows);
        report.AppendLine("- shadow resolution: " + QualitySettings.shadowResolution);
        report.AppendLine("- shadow cascades: " + QualitySettings.shadowCascades);
        report.AppendLine("- shadow distance: " + QualitySettings.shadowDistance);

        AddWarningIf(QualitySettings.vSyncCount == 0 && Application.targetFrameRate <= 0, report, ref warningCount, "vSync is off and target frame rate is uncapped/default; confirm intentional frame pacing.");
        AddWarningIf(QualitySettings.shadowDistance <= 0f || QualitySettings.shadowCascades <= 0, report, ref warningCount, "Shadows/cascades need manual scene validation for release quality.");
    }

    static void AppendRenderPipelineAudit(StringBuilder report, ref int warningCount)
    {
        var graphicsAsset = GraphicsSettings.renderPipelineAsset;
        var qualityAsset = QualitySettings.renderPipeline;
        var activeAsset = qualityAsset != null ? qualityAsset : graphicsAsset;

        report.AppendLine("Render pipeline:");
        report.AppendLine("- GraphicsSettings pipeline: " + AssetName(graphicsAsset));
        report.AppendLine("- active QualitySettings pipeline: " + AssetName(qualityAsset));
        report.AppendLine("- effective pipeline: " + AssetName(activeAsset));

        AddWarningIf(activeAsset == null, report, ref warningCount, "No scriptable render pipeline asset is active.");
        AddWarningIf(activeAsset != null && !IsUniversalRenderPipelineAsset(activeAsset), report, ref warningCount, "Effective render pipeline asset does not appear to be URP.");
    }

    static void AppendLoggingAudit(StringBuilder report, ref int warningCount)
    {
        var assetConfig = AssetDatabase.LoadAssetAtPath<LoggingConfig>(LoggingConfigAssetPath);
        var resourceConfig = Resources.Load<LoggingConfig>(LoggingConfigResourcePath);
        var loggingRelevant = EditorUserBuildSettings.development || EditorUserBuildSettings.allowDebugging;

        report.AppendLine("Logging:");
        report.AppendLine("- LoggingConfig asset: " + AssetName(assetConfig));
        report.AppendLine("- LoggingConfig Resources fallback: " + AssetName(resourceConfig));
        report.AppendLine("- BuildSafeLogger release path: calls compiled out unless UNITY_EDITOR or DEVELOPMENT_BUILD is defined.");

        AddWarningIf(loggingRelevant && assetConfig == null && resourceConfig == null, report, ref warningCount, "Development/editor logging is relevant but no LoggingConfig asset or Resources fallback was found.");
    }

    static void AppendDevelopmentDiagnosticsAudit(StringBuilder report, ref int warningCount)
    {
        var diagnosticsIncluded = EditorUserBuildSettings.development || EditorUserBuildSettings.allowDebugging;

        report.AppendLine("Development diagnostics:");
        report.AppendLine("- included in current player build flags: " + diagnosticsIncluded);
        foreach (var file in DevelopmentDiagnosticFiles.Where(File.Exists).OrderBy(path => path, StringComparer.Ordinal))
        {
            report.AppendLine("- guarded file present: " + file);
        }

        AddWarningIf(diagnosticsIncluded, report, ref warningCount, "Development-only diagnostics will be available with current build flags; disable for Steam release.");
    }

    static void AppendManualChecks(StringBuilder report)
    {
        report.AppendLine("Manual Steam release checks:");
        report.AppendLine("- Confirm fullscreen/windowed defaults and target resolution on clean install.");
        report.AppendLine("- Confirm vSync / target frame-rate behavior on target hardware.");
        report.AppendLine("- Confirm active quality level, URP asset, shadow distance, and cascades in representative gameplay.");
        report.AppendLine("- Confirm non-development player has no diagnostic UI or log spam.");
        report.AppendLine("- Confirm Steam overlay opens/closes without input, focus, fullscreen, or resolution regressions.");
    }

    static void AddWarningIf(bool condition, StringBuilder report, ref int warningCount, string message)
    {
        if (!condition)
        {
            return;
        }

        warningCount++;
        report.AppendLine("WARN: " + message);
    }

    static bool ContainsSymbol(string symbols, string symbol)
    {
        return !string.IsNullOrEmpty(symbols) && symbols.Split(';').Any(value => string.Equals(value.Trim(), symbol, StringComparison.Ordinal));
    }

    static string GetEditorUserBuildBool(string propertyName)
    {
        var property = typeof(EditorUserBuildSettings).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        return property != null && property.PropertyType == typeof(bool) ? property.GetValue(null, null).ToString() : "<unavailable>";
    }

    static bool IsUniversalRenderPipelineAsset(RenderPipelineAsset asset)
    {
        return asset != null && asset.GetType().FullName.Contains("UniversalRenderPipelineAsset");
    }

    static string AssetName(UnityEngine.Object asset)
    {
        return asset != null ? asset.name + " (" + AssetDatabase.GetAssetPath(asset) + ")" : "<null>";
    }
}
