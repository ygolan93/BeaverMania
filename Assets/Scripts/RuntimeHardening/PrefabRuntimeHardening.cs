using UnityEngine;

[DisallowMultipleComponent]
public class PrefabRuntimeHardening : MonoBehaviour
{
    [SerializeField] DebugQAConfig debugConfig;
    [SerializeField] Component[] requiredComponents;
    [SerializeField] GameObject[] requiredObjects;

    void Awake()
    {
        if (debugConfig != null && !debugConfig.enableRuntimeReferenceValidation)
        {
            return;
        }

        for (int i = 0; i < requiredComponents.Length; i++)
        {
            RuntimeReferenceValidator.Require(requiredComponents[i], this, nameof(requiredComponents) + "[" + i + "]");
        }

        for (int i = 0; i < requiredObjects.Length; i++)
        {
            RuntimeReferenceValidator.Require(requiredObjects[i], this, nameof(requiredObjects) + "[" + i + "]");
        }
    }
}
