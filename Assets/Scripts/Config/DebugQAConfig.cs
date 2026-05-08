using UnityEngine;

[CreateAssetMenu(fileName = "DebugQAConfig", menuName = "BeaverMania/Config/Debug QA Config")]
public class DebugQAConfig : ScriptableObject
{
    public bool enableRuntimeReferenceValidation = true;
    public bool logPrefabConfigApplication;
    public bool disableCombatDamage;

    [Header("Debug QA Runtime")]
    public bool enableDebugBootstrapper;
    public bool enableFpsDisplay = true;
    public bool enableSceneResetShortcut = true;
    public bool enableCheckpointTeleport = true;
    public bool enableDamageTrigger = true;

    [Header("Shortcuts")]
    public KeyCode sceneResetKey = KeyCode.F5;
    public KeyCode checkpointTeleportKey = KeyCode.F6;
    public KeyCode damageTriggerKey = KeyCode.F7;

    [Header("Tuning")]
    [Min(1)] public int fpsFontSize = 16;
    [Min(0f)] public float debugDamageAmount = 100f;
    public Vector3 checkpointTeleportOffset = Vector3.up;
}
