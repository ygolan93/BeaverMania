using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAIConfig", menuName = "BeaverMania/Config/Enemy AI Config")]
public class EnemyAIConfig : ScriptableObject
{
    [Header("Wasp")]
    [Min(0)] public int maxHealth = 2000;
    [Min(0)] public int hitToStun = 3;
    [Min(0)] public int damageToPlayer = 1;
    [Min(0f)] public float attackSpeed = 7f;
    [Min(0f)] public float recoverySeconds = 10f;
    [Min(0f)] public float aggroDistance = 50f;
    [Min(0f)] public float leashDistance = 30f;
}
