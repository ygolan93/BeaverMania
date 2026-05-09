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
        BuildSafeLogger.ErrorOnce(
            $"{ownerName}.{fieldName}.Required",
            $"Missing required reference '{fieldName}' on {ownerName}; disabling affected component.",
            owner,
            missingField,
            missingTag);

        DisableOwner(owner);
        return false;
    }

    public static bool Optional(Object value, MonoBehaviour owner, string fieldName)
    {
        if (value != null)
        {
            return true;
        }

        string ownerName = OwnerName(owner);
        BuildSafeLogger.WarnOnce(
            $"{ownerName}.{fieldName}.Optional",
            $"Missing optional reference '{fieldName}' on {ownerName}.",
            owner,
            fieldName);
        return false;
    }

    public static bool RequireTaggedComponent<T>(string tag, MonoBehaviour owner, out T component) where T : Component
    {
        component = null;

        GameObject taggedObject = null;
        try
        {
            taggedObject = GameObject.FindGameObjectWithTag(tag);
        }
        catch (UnityException)
        {
            BuildSafeLogger.ErrorOnce(
                $"{OwnerName(owner)}.{tag}.InvalidTag",
                $"Required tag '{tag}' is not defined for {OwnerName(owner)}.",
                owner,
                missingTag: tag);

            DisableOwner(owner);
            return false;
        }

        if (!Require(taggedObject, owner, tag + " tag"))
        {
            return false;
        }

        component = taggedObject.GetComponent<T>();
        if (component != null)
        {
            return true;
        }

        BuildSafeLogger.ErrorOnce(
            $"{OwnerName(owner)}.{tag}.{typeof(T).Name}.Required",
            $"Tagged object '{tag}' is missing required component {typeof(T).Name} for {OwnerName(owner)}; disabling affected component.",
            owner,
            missingField: typeof(T).Name,
            missingTag: tag);

        DisableOwner(owner);
        return false;
    }

    static void DisableOwner(MonoBehaviour owner)
    {
        if (owner != null)
        {
            owner.enabled = false;
        }
    }

    static string OwnerName(MonoBehaviour owner) => owner != null ? owner.GetType().Name : "<null owner>";

}
