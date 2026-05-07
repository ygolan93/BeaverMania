using UnityEngine;

[DisallowMultipleComponent]
public class CombatDebugGate : MonoBehaviour
{
    [SerializeField] DebugQAConfig debugConfig;

    public bool DamageEnabled => debugConfig == null || !debugConfig.disableCombatDamage;

    public static bool AllowsDamage(GameObject target)
    {
        var gate = target != null ? target.GetComponentInParent<CombatDebugGate>() : null;
        return gate == null || gate.DamageEnabled;
    }
}
