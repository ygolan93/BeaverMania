using UnityEngine;

public static class SafeRotation
{
    const float MinSqrMagnitude = 0.000001f;

    public static bool TryLookRotation(Vector3 forward, out Quaternion rotation)
    {
        if (forward.sqrMagnitude <= MinSqrMagnitude)
        {
            rotation = Quaternion.identity;
            return false;
        }

        rotation = Quaternion.LookRotation(forward);
        return true;
    }

    public static Quaternion LookRotationOrCurrent(Vector3 forward, Quaternion current)
    {
        Quaternion rotation;
        return TryLookRotation(forward, out rotation) ? rotation : current;
    }

    public static Quaternion LookRotationOrIdentity(Vector3 forward)
    {
        Quaternion rotation;
        return TryLookRotation(forward, out rotation) ? rotation : Quaternion.identity;
    }
}
