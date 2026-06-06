using System.Collections.Generic;
using Beavermania.Data.Combat;
using UnityEngine;

namespace Beavermania.Player.Combat
{
    [DisallowMultipleComponent]
    public sealed class Weapon : MonoBehaviour
    {
        const string BareHandsResourcePath = "Beavermania/Combat/Weapons/BareHands_Default";
        const string BowResourcePath = "Beavermania/Combat/Weapons/Bow_Default";
        const string HammerResourcePath = "Beavermania/Combat/Weapons/Hammers_Default";
        const string ArmorSetResourcePath = "Beavermania/Combat/Weapons/ArmorSet_Default";

        [SerializeField] WeaponData bareHands;
        [SerializeField] WeaponData hammer;
        [SerializeField] WeaponData bow;
        [SerializeField] WeaponData armorSet;

        readonly List<WeaponData> ownedWeapons = new List<WeaponData>();
        WeaponData equippedWeapon;
        int selectedIndex;

        public WeaponData EquippedWeapon => equippedWeapon;
        public WeaponCategory EquippedCategory => equippedWeapon != null ? equippedWeapon.category : WeaponCategory.BareHands;
        public int SelectedIndex => selectedIndex;
        public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;

        public void BootstrapFromLegacyArsenal(IReadOnlyList<string> legacyIds, int browserIndex)
        {
            EnsureCatalogDefaults();
            ownedWeapons.Clear();

            if (legacyIds != null && legacyIds.Count > 0)
            {
                for (var i = 0; i < legacyIds.Count; i++)
                {
                    WeaponData resolved = ResolveByLegacyId(legacyIds[i]);
                    if (resolved != null && !ownedWeapons.Contains(resolved))
                        ownedWeapons.Add(resolved);
                }
            }

            if (ownedWeapons.Count == 0)
            {
                WeaponData defaultBareHands = ResolveBareHands();
                if (defaultBareHands != null)
                    ownedWeapons.Add(defaultBareHands);
            }

            selectedIndex = ownedWeapons.Count > 0
                ? Mathf.Clamp(browserIndex, 0, ownedWeapons.Count - 1)
                : 0;
            equippedWeapon = ownedWeapons.Count > 0 ? ownedWeapons[selectedIndex] : ResolveBareHands();
        }

        public bool OwnsLegacyId(string legacyId)
        {
            if (string.IsNullOrEmpty(legacyId))
                return false;

            for (var i = 0; i < ownedWeapons.Count; i++)
            {
                WeaponData weapon = ownedWeapons[i];
                if (weapon != null && weapon.legacyArsenalId == legacyId)
                    return true;
            }

            return false;
        }

        public bool TryAddOwned(WeaponData data)
        {
            if (data == null || ownedWeapons.Contains(data))
                return false;

            ownedWeapons.Add(data);
            return true;
        }

        public bool TryCycleNext(int arsenalCounter, out WeaponData nextWeapon, out int newBrowserIndex)
        {
            nextWeapon = equippedWeapon;
            newBrowserIndex = selectedIndex;
            if (arsenalCounter <= 0 || ownedWeapons.Count == 0)
                return false;

            selectedIndex = selectedIndex < arsenalCounter ? selectedIndex + 1 : 0;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, ownedWeapons.Count - 1);
            equippedWeapon = ownedWeapons[selectedIndex];
            nextWeapon = equippedWeapon;
            newBrowserIndex = selectedIndex;
            return true;
        }

        public void Equip(WeaponData data)
        {
            if (data == null)
                return;

            int index = ownedWeapons.IndexOf(data);
            if (index < 0)
            {
                TryAddOwned(data);
                index = ownedWeapons.IndexOf(data);
            }

            if (index < 0)
                return;

            selectedIndex = index;
            equippedWeapon = data;
        }

        public WeaponData ResolveByLegacyId(string legacyId)
        {
            if (string.IsNullOrEmpty(legacyId))
                return null;

            EnsureCatalogDefaults();

            if (bareHands != null && bareHands.legacyArsenalId == legacyId)
                return bareHands;
            if (hammer != null && hammer.legacyArsenalId == legacyId)
                return hammer;
            if (bow != null && bow.legacyArsenalId == legacyId)
                return bow;
            if (armorSet != null && armorSet.legacyArsenalId == legacyId)
                return armorSet;

            return null;
        }

        public WeaponData ResolveBareHands() => bareHands != null ? bareHands : LoadDefault(BareHandsResourcePath);

        public WeaponData ResolveHammer() => hammer != null ? hammer : LoadDefault(HammerResourcePath);

        public WeaponData ResolveBow() => bow != null ? bow : LoadDefault(BowResourcePath);

        public WeaponData ResolveArmorSet() => armorSet != null ? armorSet : LoadDefault(ArmorSetResourcePath);

        void EnsureCatalogDefaults()
        {
            if (bareHands == null)
                bareHands = LoadDefault(BareHandsResourcePath);
            if (hammer == null)
                hammer = LoadDefault(HammerResourcePath);
            if (bow == null)
                bow = LoadDefault(BowResourcePath);
            if (armorSet == null)
                armorSet = LoadDefault(ArmorSetResourcePath);
        }

        static WeaponData LoadDefault(string resourcePath)
        {
            return Resources.Load<WeaponData>(resourcePath);
        }
    }
}
