using UnityEngine;

namespace Beavermania.Data.Combat
{
    public enum WeaponCategory
    {
        BareHands = 0,
        Hammer = 1,
        Bow = 2,
        ArmorSet = 3,
    }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "Beavermania/Combat/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        public string displayName = "Bare Hands";
        public string legacyArsenalId = "Bare Hands";
        public WeaponCategory category = WeaponCategory.BareHands;

        [Min(0)] public int groundMeleeDamage = 50;
        [Min(0)] public int airMeleeDamage = 20;
        [Min(0f)] public float groundAttackRadius = 0.7f;
        [Min(0f)] public float airAttackRadius = 2.5f;
        [Min(0f)] public float groundAttackOriginYOffset;
        [Min(0f)] public float bowShotStaminaCost;
        [Range(0f, 100f)] public float fireBreathHealthCostPercent;
        public bool supportsFireBreath;
        public bool supportsSwordGlare;

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(legacyArsenalId))
                Debug.LogWarning($"{name}: legacyArsenalId is empty.", this);

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = legacyArsenalId;
        }
    }
}
