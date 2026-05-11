using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SteamReleaseReadinessValidator
{
    const string MenuPath = "Tools/Beavermania/Validate Steam Release Readiness";
    const string LoggingConfigAssetPath = "Assets/Config/LoggingConfig.asset";
    const string QualitySettingsAssetPath = "ProjectSettings/QualitySettings.asset";
    const string SteamQualityPlatform = "Standalone";
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
        var steamQuality = LoadSteamQualityAuditSnapshot(report, ref warningCount);
        AppendQualityAudit(report, ref warningCount, steamQuality);
        AppendRenderPipelineAudit(report, ref warningCount, steamQuality);
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

    static void AppendQualityAudit(StringBuilder report, ref int warningCount, QualityAuditSnapshot steamQuality)
    {
        report.AppendLine("Quality / frame pacing:");
        report.AppendLine("- Steam platform quality source: " + steamQuality.SourceDescription);
        report.AppendLine("- Steam platform quality: " + steamQuality.Level + " / " + steamQuality.Name);
        report.AppendLine("- editor active quality: " + QualitySettings.GetQualityLevel() + " / " + GetCurrentQualityName());
        report.AppendLine("- vSyncCount: " + steamQuality.VSyncCount);
        report.AppendLine("- Application.targetFrameRate: " + Application.targetFrameRate);
        report.AppendLine("- shadows: " + steamQuality.Shadows);
        report.AppendLine("- shadow resolution: " + steamQuality.ShadowResolution);
        report.AppendLine("- shadow cascades: " + steamQuality.ShadowCascades);
        report.AppendLine("- shadow distance: " + steamQuality.ShadowDistance);

        AddWarningIf(steamQuality.VSyncCount == 0 && Application.targetFrameRate <= 0, report, ref warningCount, "vSync is off and target frame rate is uncapped/default; confirm intentional frame pacing.");
        AddWarningIf(steamQuality.ShadowDistance <= 0f || steamQuality.ShadowCascades <= 0, report, ref warningCount, "Shadows/cascades need manual scene validation for release quality.");
    }

    static void AppendRenderPipelineAudit(StringBuilder report, ref int warningCount, QualityAuditSnapshot steamQuality)
    {
        var graphicsAsset = GraphicsSettings.renderPipelineAsset;
        var qualityAsset = steamQuality.RenderPipelineAsset;
        var activeAsset = qualityAsset != null ? qualityAsset : graphicsAsset;

        report.AppendLine("Render pipeline:");
        report.AppendLine("- GraphicsSettings pipeline: " + AssetName(graphicsAsset));
        report.AppendLine("- Steam quality pipeline: " + steamQuality.RenderPipelineDescription);
        report.AppendLine("- effective Steam player pipeline: " + AssetName(activeAsset));

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

    static QualityAuditSnapshot LoadSteamQualityAuditSnapshot(StringBuilder report, ref int warningCount)
    {
        if (TryReadSteamQualityFromProjectSettings(out var snapshot))
        {
            return snapshot;
        }

        AddWarningIf(true, report, ref warningCount, "Could not read Standalone default quality from ProjectSettings/QualitySettings.asset; falling back to the editor's active quality level.");
        return QualityAuditSnapshot.FromCurrentQuality("editor active quality fallback");
    }

    static bool TryReadSteamQualityFromProjectSettings(out QualityAuditSnapshot snapshot)
    {
        snapshot = null;

        if (!File.Exists(QualitySettingsAssetPath))
        {
            return false;
        }

        var qualities = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>();
        var defaults = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
        System.Collections.Generic.Dictionary<string, string> currentQuality = null;
        var readingQualities = false;
        var readingDefaults = false;

        foreach (var line in File.ReadAllLines(QualitySettingsAssetPath))
        {
            if (line == "  m_QualitySettings:")
            {
                readingQualities = true;
                readingDefaults = false;
                continue;
            }

            if (line == "  m_PerPlatformDefaultQuality:")
            {
                readingQualities = false;
                readingDefaults = true;
                currentQuality = null;
                continue;
            }

            if (readingQualities && line.StartsWith("  - ", StringComparison.Ordinal))
            {
                currentQuality = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
                qualities.Add(currentQuality);
                ReadYamlKeyValue(line.Substring(4), currentQuality);
                continue;
            }

            if (readingQualities && currentQuality != null && line.StartsWith("    ", StringComparison.Ordinal))
            {
                ReadYamlKeyValue(line.Substring(4), currentQuality);
                continue;
            }

            if (readingDefaults)
            {
                if (!line.StartsWith("    ", StringComparison.Ordinal))
                {
                    readingDefaults = false;
                    continue;
                }

                var trimmed = line.Trim();
                var separator = trimmed.IndexOf(':');
                if (separator > 0 && int.TryParse(trimmed.Substring(separator + 1).Trim(), out var defaultQuality))
                {
                    defaults[trimmed.Substring(0, separator)] = defaultQuality;
                }
            }
        }

        if (!defaults.TryGetValue(SteamQualityPlatform, out var qualityLevel) || qualityLevel < 0 || qualityLevel >= qualities.Count)
        {
            return false;
        }

        var quality = qualities[qualityLevel];
        snapshot = new QualityAuditSnapshot
        {
            Level = qualityLevel,
            Name = ReadString(quality, "name", "<unknown>"),
            VSyncCount = ReadInt(quality, "vSyncCount", QualitySettings.vSyncCount),
            Shadows = (ShadowQuality)ReadInt(quality, "shadows", (int)QualitySettings.shadows),
            ShadowResolution = (ShadowResolution)ReadInt(quality, "shadowResolution", (int)QualitySettings.shadowResolution),
            ShadowCascades = ReadInt(quality, "shadowCascades", QualitySettings.shadowCascades),
            ShadowDistance = ReadFloat(quality, "shadowDistance", QualitySettings.shadowDistance),
            RenderPipelineAsset = ReadRenderPipelineAsset(quality, out var renderPipelineDescription),
            RenderPipelineDescription = renderPipelineDescription,
            SourceDescription = SteamQualityPlatform + " default from " + QualitySettingsAssetPath
        };
        return true;
    }

    static void ReadYamlKeyValue(string text, System.Collections.Generic.Dictionary<string, string> values)
    {
        var separator = text.IndexOf(':');
        if (separator <= 0)
        {
            return;
        }

        values[text.Substring(0, separator).Trim()] = text.Substring(separator + 1).Trim();
    }

    static string ReadString(System.Collections.Generic.Dictionary<string, string> values, string key, string fallback)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }

    static int ReadInt(System.Collections.Generic.Dictionary<string, string> values, string key, int fallback)
    {
        return values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    static float ReadFloat(System.Collections.Generic.Dictionary<string, string> values, string key, float fallback)
    {
        return values.TryGetValue(key, out var value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    static RenderPipelineAsset ReadRenderPipelineAsset(System.Collections.Generic.Dictionary<string, string> values, out string description)
    {
        description = "<null>";
        if (!values.TryGetValue("customRenderPipeline", out var value) || string.IsNullOrEmpty(value) || value.Contains("fileID: 0"))
        {
            return null;
        }

        var guidMatch = Regex.Match(value, @"guid: ([0-9a-fA-F]+)");
        if (!guidMatch.Success)
        {
            description = value;
            return null;
        }

        var path = AssetDatabase.GUIDToAssetPath(guidMatch.Groups[1].Value);
        var asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
        description = asset != null ? AssetName(asset) : "<missing asset: " + value + ">";
        return asset;
    }

    static string GetCurrentQualityName()
    {
        var qualityLevel = QualitySettings.GetQualityLevel();
        var qualityNames = QualitySettings.names;
        return qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "<unknown>";
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

    sealed class QualityAuditSnapshot
    {
        public int Level;
        public string Name;
        public int VSyncCount;
        public ShadowQuality Shadows;
        public ShadowResolution ShadowResolution;
        public int ShadowCascades;
        public float ShadowDistance;
        public RenderPipelineAsset RenderPipelineAsset;
        public string RenderPipelineDescription;
        public string SourceDescription;

        public static QualityAuditSnapshot FromCurrentQuality(string sourceDescription)
        {
            return new QualityAuditSnapshot
            {
                Level = QualitySettings.GetQualityLevel(),
                Name = GetCurrentQualityName(),
                VSyncCount = QualitySettings.vSyncCount,
                Shadows = QualitySettings.shadows,
                ShadowResolution = QualitySettings.shadowResolution,
                ShadowCascades = QualitySettings.shadowCascades,
                ShadowDistance = QualitySettings.shadowDistance,
                RenderPipelineAsset = QualitySettings.renderPipeline,
                RenderPipelineDescription = AssetName(QualitySettings.renderPipeline),
                SourceDescription = sourceDescription
            };
        }
    }
}
