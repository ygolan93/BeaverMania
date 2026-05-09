using System;
using Cinemachine;
using UnityEngine;

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

    [NonSerialized] Behaviour owner;
    [NonSerialized] CinemachineFreeLook freeLook;
    [NonSerialized] Transform cachedTransform;
    [NonSerialized] Camera cachedCamera;
    [NonSerialized] Source cachedSource;

    public void Configure(Behaviour owner, CinemachineFreeLook freeLook)
    {
        this.owner = owner;
        this.freeLook = freeLook;
    }

    public void Invalidate()
    {
        ClearCache();
    }

    public bool TryGetPlanarBasis(out Vector3 forward, out Vector3 right)
    {
        if (!IsCachedValid() && !TryResolve())
        {
            WarnMissingCamera();
            forward = Vector3.zero;
            right = Vector3.zero;
            return false;
        }

        var basisRotation = GetBasisRotation();
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

        if (TryGetBrainOutputCamera(out var brainCamera))
        {
            Cache(brainCamera, Source.BrainCamera);
            return true;
        }

        if (IsCameraValid(Camera.main))
        {
            Cache(Camera.main, Source.MainCamera);
            return true;
        }

        if (IsFreeLookValid())
        {
            Cache(freeLook.transform, null, Source.FreeLookState);
            return true;
        }

        ClearCache();
        return false;
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
                    && cachedTransform == freeLook.transform;
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

    Quaternion GetBasisRotation()
    {
        if (cachedSource == Source.FreeLookState && IsFreeLookValid())
        {
            return freeLook.State.FinalOrientation;
        }

        return cachedTransform.rotation;
    }

    bool IsFreeLookValid()
    {
        return freeLook != null && freeLook.enabled && freeLook.gameObject.activeInHierarchy;
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

    static bool IsCameraValid(Camera candidate)
    {
        return candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy;
    }

    static Vector3 Flatten(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    void WarnMissingCamera()
    {
        BuildSafeLogger.WarnOnce(
            "Behaviour.MissingGameplayCamera",
            "Behaviour could not resolve an enabled gameplay camera for camera-relative movement or aim.",
            owner);
    }
}
