using UnityEngine;

[CreateAssetMenu(fileName = "HazardDamageConfig", menuName = "BeaverMania/Config/Hazard Damage Config")]
public class HazardDamageConfig : ScriptableObject
{
    [Min(0f)] public float scorpionJawClampDamage = 15f;
    [Min(0f)] public float scorpionStingDamage = 30f;
    [Min(0f)] public float scorpionParryChipDamage = 6f;
    [Min(0)] public int scorpionParryCounterDamage = 10;
}
