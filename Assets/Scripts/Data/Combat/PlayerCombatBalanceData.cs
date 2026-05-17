using UnityEngine;

namespace Beavermania.Data.Combat
{
    [CreateAssetMenu(fileName = "PlayerCombatBalanceData", menuName = "Beavermania/Combat/Player Combat Balance Data")]
    public class PlayerCombatBalanceData : ScriptableObject
    {
        public float maxHealth = 1000f;
        public float maxStamina = 100f;
        public int bareHandsMeleeDamage = 50;
        public int bowEquippedMeleeDamage = 50;
        public int hammerMeleeDamage = 700;
        public int armorSetMeleeDamage = 200;
        public int armorSetAirDamage = 200;
        public int bareHandsAirDamage = 20;
        public int rollAttackDamage = 200;
        public float scorpionLightDamage = 15f;
        public float scorpionHeavyDamage = 30f;
        public float shroomHealPerTick = 2f;
        public float appleHealAmount = 500f;
        public float bowShotStaminaCost = 30f;
        public float stoneThrowStaminaCost = 20f;
        [Range(0f, 100f)]
        public float fireBreathHealthCostPercent = 20f;
        public float attackRange = 0.5f;
        public float groundBeat = 0.3f;
        public float airBeat = 0.2f;

        void OnValidate()
        {
            if (maxHealth <= 0f)
                Debug.LogWarning($"{name}: maxHealth must be > 0.", this);

            if (maxStamina <= 0f)
                Debug.LogWarning($"{name}: maxStamina must be > 0.", this);

            if (fireBreathHealthCostPercent < 0f || fireBreathHealthCostPercent > 100f)
                Debug.LogWarning($"{name}: fireBreathHealthCostPercent must be between 0 and 100.", this);
        }
    }
}
