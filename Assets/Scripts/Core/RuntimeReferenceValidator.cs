using UnityEngine;

public static class RuntimeReferenceValidator
{
    public static bool Require(Object value, MonoBehaviour owner, string fieldName)
    {
        if (value != null)
        {
            return true;
        }

        string ownerName = owner != null ? owner.GetType().Name : "<null owner>";
        BuildSafeLogger.WarnOnce(
            $"{ownerName}.{fieldName}",
            $"Missing required reference '{fieldName}' on {ownerName}.",
            owner);

        if (owner != null)
        {
            owner.enabled = false;
        }

        return false;
    }
}
