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
        bool isTag = fieldName != null && fieldName.EndsWith(" tag");
        string missingTag = isTag ? fieldName.Substring(0, fieldName.Length - 4) : null;
        string missingField = isTag ? null : fieldName;
        BuildSafeLogger.WarnOnce(
            $"{ownerName}.{fieldName}",
            $"Missing required reference '{fieldName}' on {ownerName}.",
            owner,
            missingField,
            missingTag);

        if (owner != null)
        {
            owner.enabled = false;
        }

        return false;
    }
}
