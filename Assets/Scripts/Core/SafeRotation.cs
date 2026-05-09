using UnityEngine;

public static class SafeRotation
{
    const float MinSqrMagnitude = 0.000001f;

    public static bool TryLookRotation(Vector3 forward, out Quaternion rotation)
    {
        if (!IsFinite(forward) || forward.sqrMagnitude <= MinSqrMagnitude)
        {
            rotation = Quaternion.identity;
            return false;
        }

        rotation = Quaternion.LookRotation(forward);
        return true;
    }

    public static bool TryPlanarLookRotation(Vector3 forward, out Quaternion rotation)
    {
        forward.y = 0f;
        return TryLookRotation(forward, out rotation);
    }

    public static bool IsFinite(Vector3 vector)
    {
        return IsFinite(vector.x) && IsFinite(vector.y) && IsFinite(vector.z);
    }

    static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static Quaternion LookRotationOrCurrent(Vector3 forward, Quaternion current)
    {
        Quaternion rotation;
        return TryLookRotation(forward, out rotation) ? rotation : current;
    }

    public static Quaternion PlanarLookRotationOrCurrent(Vector3 forward, Quaternion current)
    {
        Quaternion rotation;
        return TryPlanarLookRotation(forward, out rotation) ? rotation : current;
    }

    public static Quaternion LookRotationOrIdentity(Vector3 forward)
    {
        Quaternion rotation;
        return TryLookRotation(forward, out rotation) ? rotation : Quaternion.identity;
    }
}
