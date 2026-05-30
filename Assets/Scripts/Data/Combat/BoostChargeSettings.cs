using UnityEngine;

namespace Beavermania.Data.Combat
{
    [CreateAssetMenu(fileName = "BoostChargeSettings", menuName = "Beavermania/Combat/Boost Charge Settings")]
    public sealed class BoostChargeSettings : ScriptableObject
    {
        [Header("Meter")]
        [Min(1f)] public float maxCharge = 100f;
        [Min(1f)] public float readyThreshold = 100f;

        [Header("Combat gain")]
        [Min(0f)] public float chargePerHit = 8f;
        [Min(0f)] public float chargePerKill = 35f;
        [Min(1)] public int comboHitsForBonus = 5;
        [Min(0f)] public float comboBonusCharge = 20f;

        [Header("Decay")]
        [Min(0f)] public float outOfCombatDelay = 6f;
        [Min(0f)] public float decayPerSecond = 4f;
        [Min(0f)] public float chargeLostOnPlayerDamage = 30f;
    }
}
