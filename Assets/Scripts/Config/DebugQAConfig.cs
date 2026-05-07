using UnityEngine;

[CreateAssetMenu(fileName = "DebugQAConfig", menuName = "BeaverMania/Config/Debug QA Config")]
public class DebugQAConfig : ScriptableObject
{
    public bool enableRuntimeReferenceValidation = true;
    public bool logPrefabConfigApplication;
    public bool disableCombatDamage;
}
