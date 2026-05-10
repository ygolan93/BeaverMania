using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public sealed class PlayerCameraReference
{
    const float MinPlanarSqrMagnitude = 0.0001f;

    enum Source
    {
        None,
        SerializedCamera,
        FreeLookState,
        BrainCamera,
        MainCamera
    }

    [SerializeField] Camera camera;

    [NonSerialized] UnityEngine.Object owner;
    [NonSerialized] CinemachineFreeLook freeLook;
    [NonSerialized] Transform cachedTransform;
    [NonSerialized] Camera cachedCamera;
    [NonSerialized] Source cachedSource;

    public void Configure(UnityEngine.Object owner, CinemachineFreeLook freeLook)
    {
        this.owner = owner;
        this.freeLook = freeLook;
    }

    public void Invalidate()
    {
        ClearCache();
    }

    public bool TryGetCamera(out Camera gameplayCamera)
    {
        WarnRenderingReadiness(owner);

        if (IsCachedCameraValid())
        {
            gameplayCamera = cachedCamera;
            return true;
        }

        ClearCache();
        if (TryResolveCamera(out gameplayCamera))
        {
            Cache(gameplayCamera, gameplayCamera == Camera.main ? Source.MainCamera : Source.BrainCamera);
            return true;
        }

        gameplayCamera = null;
        return false;
    }

    public static bool TryGetActiveGameplayCamera(out Camera gameplayCamera, UnityEngine.Object owner = null)
    {
        WarnRenderingReadiness(owner);

        if (TryResolveCamera(out gameplayCamera))
        {
            return true;
        }

        gameplayCamera = null;
        return false;
    }

    public bool TryGetPlanarBasis(out Vector3 forward, out Vector3 right)
    {
        WarnRenderingReadiness(owner);

        if (!IsCachedValid() && !TryResolve())
        {
            WarnMissingCamera();
            forward = Vector3.zero;
            right = Vector3.zero;
            return false;
        }

        Quaternion basisRotation = GetCachedBasisRotation();
        forward = Flatten(basisRotation * Vector3.forward);
        right = Flatten(basisRotation * Vector3.right);
        if (forward.sqrMagnitude < MinPlanarSqrMagnitude || right.sqrMagnitude < MinPlanarSqrMagnitude)
        {
            ClearCache();
            WarnMissingCamera();
            forward = Vector3.zero;
            right = Vector3.zero;
            return false;
        }

        forward.Normalize();
        right.Normalize();
        return true;
    }

    bool TryResolve()
    {
        if (IsCameraValid(camera))
        {
            Cache(camera, Source.SerializedCamera);
            return true;
        }

        if (TryResolveCamera(out var gameplayCamera))
        {
            Cache(gameplayCamera, gameplayCamera == Camera.main ? Source.MainCamera : Source.BrainCamera);
            return true;
        }

        if (IsFreeLookValid())
        {
            Cache(freeLook.VirtualCameraGameObject.transform, null, Source.FreeLookState);
            return true;
        }

        ClearCache();
        return false;
    }

    bool IsCachedCameraValid()
    {
        return cachedCamera != null && IsCameraValid(cachedCamera);
    }

    bool IsCachedValid()
    {
        if (cachedTransform == null)
        {
            return false;
        }

        switch (cachedSource)
        {
            case Source.SerializedCamera:
                return cachedCamera == camera && IsCameraValid(cachedCamera);
            case Source.FreeLookState:
                return !TryGetBrainOutputCamera(out _)
                    && !IsCameraValid(Camera.main)
                    && IsFreeLookValid()
                    && cachedTransform == freeLook.VirtualCameraGameObject.transform;
            case Source.BrainCamera:
            case Source.MainCamera:
                return IsCameraValid(cachedCamera);
            default:
                return false;
        }
    }

    void Cache(Camera sourceCamera, Source source)
    {
        Cache(sourceCamera.transform, sourceCamera, source);
    }

    void Cache(Transform sourceTransform, Camera sourceCamera, Source source)
    {
        cachedTransform = sourceTransform;
        cachedCamera = sourceCamera;
        cachedSource = source;
    }

    void ClearCache()
    {
        cachedTransform = null;
        cachedCamera = null;
        cachedSource = Source.None;
    }

    Quaternion GetCachedBasisRotation()
    {
        if (cachedSource == Source.FreeLookState && freeLook != null)
        {
            return freeLook.State.FinalOrientation;
        }

        return cachedTransform.rotation;
    }

    bool IsFreeLookValid()
    {
        return freeLook != null
            && freeLook.enabled
            && freeLook.VirtualCameraGameObject != null
            && freeLook.VirtualCameraGameObject.activeInHierarchy;
    }

    static bool TryResolveCamera(out Camera gameplayCamera)
    {
        if (TryGetBrainOutputCamera(out gameplayCamera))
        {
            return true;
        }

        gameplayCamera = Camera.main;
        return IsCameraValid(gameplayCamera);
    }

    static bool TryGetBrainOutputCamera(out Camera outputCamera)
    {
        var brain = UnityEngine.Object.FindObjectOfType<CinemachineBrain>();
        if (brain != null && brain.enabled && brain.gameObject.activeInHierarchy && IsCameraValid(brain.OutputCamera))
        {
            outputCamera = brain.OutputCamera;
            return true;
        }

        outputCamera = null;
        return false;
    }

    public static int CountActiveMainCameras()
    {
        int count = 0;
        var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (IsCameraValid(cameras[i]) && cameras[i].CompareTag("MainCamera"))
            {
                count++;
            }
        }

        return count;
    }

    public static bool HasRenderPipelineMismatch(out RenderPipelineAsset graphicsAsset, out RenderPipelineAsset qualityAsset)
    {
        graphicsAsset = GraphicsSettings.renderPipelineAsset;
        qualityAsset = QualitySettings.renderPipeline;
        return graphicsAsset != qualityAsset;
    }

    static bool IsCameraValid(Camera candidate)
    {
        return candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy;
    }

    static Vector3 Flatten(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    static void WarnRenderingReadiness(UnityEngine.Object owner)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!TryResolveCamera(out _))
        {
            BuildSafeLogger.WarnOnce(
                "RenderingReadiness.MissingGameplayCamera",
                "Active scene has no enabled gameplay camera from CinemachineBrain.OutputCamera or MainCamera.",
                owner);
        }

        int mainCameraCount = CountActiveMainCameras();
        if (mainCameraCount > 1)
        {
            BuildSafeLogger.WarnOnce(
                "RenderingReadiness.MultipleMainCameras",
                "Active scene has multiple enabled MainCamera-tagged cameras: " + mainCameraCount,
                owner);
        }

        if (HasRenderPipelineMismatch(out var graphicsAsset, out var qualityAsset))
        {
            BuildSafeLogger.WarnOnce(
                "RenderingReadiness.RenderPipelineMismatch",
                "GraphicsSettings.renderPipelineAsset and active QualitySettings.renderPipeline differ. Graphics=" + AssetName(graphicsAsset) + " Quality=" + AssetName(qualityAsset),
                owner);
        }
#endif
    }

    static string AssetName(UnityEngine.Object asset)
    {
        return asset != null ? asset.name : "<null>";
    }

    void WarnMissingCamera()
    {
        BuildSafeLogger.WarnOnce(
            "Behaviour.MissingGameplayCamera",
            "Behaviour could not resolve an enabled gameplay camera for camera-relative movement or aim.",
            owner);
    }
}
