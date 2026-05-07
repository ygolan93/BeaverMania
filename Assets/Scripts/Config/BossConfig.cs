using UnityEngine;

[CreateAssetMenu(fileName = "BossConfig", menuName = "BeaverMania/Config/Boss Config")]
public class BossConfig : ScriptableObject
{
    [Min(1)] public int maxHealth = 2000;
    [Min(1)] public int comboLimit = 10;
    [Min(0f)] public float stunSeconds = 10f;
    [Min(0f)] public float chargeSpeed = 7f;
    [Min(0f)] public float chargeClock = 1f;
    [Min(0f)] public float lookDistance = 40f;
    [Min(0f)] public float chargeDistance = 30f;
    [Min(0f)] public float attackDistance = 10f;
}
