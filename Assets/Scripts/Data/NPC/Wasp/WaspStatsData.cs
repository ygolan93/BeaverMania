using UnityEngine;

namespace Beavermania.Data.NPC
{
    [CreateAssetMenu(fileName = "WaspStatsData", menuName = "Beavermania/NPC/Wasp Stats Data")]
    public sealed class WaspStatsData : ScriptableObject
    {
        [Min(1)] public int maxHealth = 1000;
        [Min(0)] public int damageToPlayer = 15;
        [Min(1)] public int hitsToStun = 10;
        [Min(0f)] public float stunRecovery = 10f;
        [Min(0f)] public float floatSpeed = 1f;
        [Min(0f)] public float floatDistance = 1f;
        [Min(0f)] public float maxTiltAngle = 7f;
        [Min(0)] public int weaponHitDamage = 15;
        [Min(0)] public int parryDamage = 20;

        void OnValidate()
        {
            if (hitsToStun < 1)
                Debug.LogWarning($"{name}: hitsToStun should be >= 1.", this);
        }
    }
}
