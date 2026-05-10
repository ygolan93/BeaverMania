using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RenderingReadinessValidator
{
    const string MenuPath = "Tools/Beavermania/Validate Rendering Readiness";

    [MenuItem(MenuPath)]
    public static void ValidateRenderingReadiness()
    {
        var report = new StringBuilder();
        var warningCount = 0;

        report.AppendLine("Rendering readiness validation (checks only; no scene/project mutation).");
        ValidateActiveSceneCameras(report, ref warningCount);
        ValidateRenderPipeline(report, ref warningCount);
        AppendQualityAudit(report);
        AppendReleaseDisplayAudit(report);
        AppendManualValidationSteps(report);

        if (warningCount > 0)
        {
            Debug.LogWarning(report.ToString());
        }
        else
        {
            Debug.Log(report.ToString());
        }
    }

    static void ValidateActiveSceneCameras(StringBuilder report, ref int warningCount)
    {
        var scene = SceneManager.GetActiveScene();
        report.AppendLine("Active scene: " + (scene.IsValid() ? scene.path : "<invalid>"));

        if (!PlayerCameraReference.TryGetActiveGameplayCamera(out var gameplayCamera))
        {
            warningCount++;
            report.AppendLine("WARN: No valid gameplay camera from CinemachineBrain.OutputCamera or enabled MainCamera.");
        }
        else
        {
            report.AppendLine("Gameplay camera: " + GetPath(gameplayCamera.transform));
        }

        int mainCameraCount = PlayerCameraReference.CountActiveMainCameras();
        report.AppendLine("Enabled MainCamera-tagged cameras: " + mainCameraCount);
        if (mainCameraCount > 1)
        {
            warningCount++;
            report.AppendLine("WARN: Multiple enabled MainCamera-tagged cameras can destabilize Camera.main lookup.");
        }
    }

    static void ValidateRenderPipeline(StringBuilder report, ref int warningCount)
    {
        PlayerCameraReference.HasRenderPipelineMismatch(out var graphicsAsset, out var qualityAsset);
        var activeAsset = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.renderPipelineAsset;

        report.AppendLine("Graphics render pipeline asset: " + AssetName(graphicsAsset));
        report.AppendLine("Active quality render pipeline asset: " + AssetName(qualityAsset));
        if (graphicsAsset != qualityAsset)
        {
            warningCount++;
            report.AppendLine("WARN: GraphicsSettings and active QualitySettings render pipeline assets differ.");
        }

        if (!IsUniversalRenderPipelineAsset(activeAsset))
        {
            warningCount++;
            report.AppendLine("WARN: Active render pipeline asset does not appear to be URP.");
        }
    }

    static void AppendQualityAudit(StringBuilder report)
    {
        int qualityLevel = QualitySettings.GetQualityLevel();
        string[] qualityNames = QualitySettings.names;
        string qualityName = qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "<unknown>";

        report.AppendLine("Quality audit (no automatic changes):");
        report.AppendLine("- active quality: " + qualityLevel + " / " + qualityName);
        report.AppendLine("- shadows: " + QualitySettings.shadows);
        report.AppendLine("- shadow resolution: " + QualitySettings.shadowResolution);
        report.AppendLine("- shadow cascades: " + QualitySettings.shadowCascades);
        report.AppendLine("- shadow distance: " + QualitySettings.shadowDistance);
        report.AppendLine("- vSyncCount: " + QualitySettings.vSyncCount);
    }

    static void AppendReleaseDisplayAudit(StringBuilder report)
    {
        report.AppendLine("Steam/release display defaults audit (no automatic changes):");
        report.AppendLine("- fullscreen mode: " + PlayerSettings.fullScreenMode);
        report.AppendLine("- default native resolution: " + PlayerSettings.defaultIsNativeResolution);
        report.AppendLine("- default resolution: " + PlayerSettings.defaultScreenWidth + "x" + PlayerSettings.defaultScreenHeight);
        report.AppendLine("- allow fullscreen switch: " + PlayerSettings.allowFullscreenSwitch);
        report.AppendLine("- run in background: " + PlayerSettings.runInBackground);
    }

    static void AppendManualValidationSteps(StringBuilder report)
    {
        report.AppendLine("Manual target-hardware validation required:");
        report.AppendLine("- Confirm camera-relative projectile direction after scene reload/player respawn.");
        report.AppendLine("- Confirm waypoint marker tracks/clamps and hides when target is behind camera.");
        report.AppendLine("- Confirm menu-to-level load emits no null camera errors.");
        report.AppendLine("- Confirm URP asset is active in player build.");
        report.AppendLine("- Confirm shadows/lights and fullscreen/resolution match Steam release expectations.");
    }

    static bool IsUniversalRenderPipelineAsset(RenderPipelineAsset asset)
    {
        return asset != null && asset.GetType().FullName.Contains("UniversalRenderPipelineAsset");
    }

    static string AssetName(Object asset)
    {
        return asset != null ? asset.name + " (" + AssetDatabase.GetAssetPath(asset) + ")" : "<null>";
    }

    static string GetPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
