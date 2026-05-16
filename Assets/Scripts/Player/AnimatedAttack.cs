using System.Collections;
using System.Collections.Generic;
using Beavermania.NPC;
using Beavermania.Objects;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Player.Combat
{

    public class AnimatedAttack : MonoBehaviour
    {
        [SerializeField] BeaverPlayer Player;
        [SerializeField] Transform AttackPoint;
        [SerializeField] Transform Sphere;
        [SerializeField] GameObject GlowEffect;
        [SerializeField] LayerMask enemyLayers;

        const int FallbackBareHandsMeleeDamage = 50;
        const int FallbackBowMeleeDamage = 50;
        const int FallbackHammerMeleeDamage = 700;
        const int FallbackArmorSetMeleeDamage = 200;
        const int FallbackArmorSetAirDamage = 200;
        const int FallbackBareHandsAirDamage = 20;
        const int FallbackRollAttackDamage = 200;

        private void Start()
        {
            Player = transform.parent.GetComponent<BeaverPlayer>();
            if (GlowEffect != null)
                GlowEffect.SetActive(false);
        }

        int BareHandsMeleeDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.bareHandsMeleeDamage
                : FallbackBareHandsMeleeDamage;

        int BowMeleeDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.bowEquippedMeleeDamage
                : FallbackBowMeleeDamage;

        int HammerMeleeDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.hammerMeleeDamage
                : FallbackHammerMeleeDamage;

        int ArmorSetMeleeDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.armorSetMeleeDamage
                : FallbackArmorSetMeleeDamage;

        int ArmorSetAirDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.armorSetAirDamage
                : FallbackArmorSetAirDamage;

        int BareHandsAirDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.bareHandsAirDamage
                : FallbackBareHandsAirDamage;

        int RollAttackDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.rollAttackDamage
                : FallbackRollAttackDamage;

        public void CauseDamage(Vector3 origin, float range, int Damage)
        {
            Collider[] hitEnemies = Physics.OverlapSphere(origin, range, enemyLayers);
            foreach (Collider enemy in hitEnemies)
            {
                if (enemy.name != null)
                {
                    Debug.Log("Hit " + enemy.name);
                }
                switch (enemy.tag)
                {
                    case "NPC":
                        {
                            var Wasp = enemy.gameObject.GetComponent<NPC_Basic>();
                            Wasp.TakeDamage(Damage);
                            break;
                        }
                    case "Hive":
                        {
                            var Hive = enemy.gameObject.GetComponent<Static_Hive>();
                            Hive.TakeDamage(Damage);
                            break;
                        }
                    case "Scorpion":
                        {
                            var Scorpion = enemy.gameObject.GetComponent<ScorpionScript>();
                            Scorpion.TakeDamage(Damage);
                            break;
                        }
                }
            }
        }


        public void RollAttack()
        {
            CauseDamage(AttackPoint.position, 1.5f, RollAttackDamage);
        }

        public void GroundAttack()
        {
            var arsenal = Player.GetComponent<BeaverPlayer>().Arsenal;
            var weapon = Player.GetComponent<BeaverPlayer>().arsenalBrowser;
            switch (arsenal[weapon])
            {
                case "Bare Hands":
                    {
                        CauseDamage(AttackPoint.position, 0.7f, BareHandsMeleeDamage);
                        break;
                    }
                case "Bow":
                    {
                        CauseDamage(AttackPoint.position, 1f, BowMeleeDamage);
                        break;
                    }
                case "Hammers":
                    {
                        CauseDamage(AttackPoint.position, 2f, HammerMeleeDamage);
                        break;
                    }
                case "ArmorSet":
                    {
                        var feetPos = Sphere.position + new Vector3(0, 0.5f, 0);
                        CauseDamage(feetPos, 4f, ArmorSetMeleeDamage);
                        break;
                    }
            }

        }
        public void AirAttack()
        {
            var arsenal = Player.GetComponent<BeaverPlayer>().Arsenal;
            var weapon = Player.GetComponent<BeaverPlayer>().arsenalBrowser;
            switch (arsenal[weapon])
            {
                case "Bare Hands":
                    {
                        CauseDamage(Sphere.position, 2.5f, BareHandsAirDamage);
                        break;
                    }
                case "Hammers":
                    {
                        CauseDamage(Sphere.position, 2.5f, BareHandsAirDamage);
                        break;
                    }
                case "ArmorSet":
                    {
                        CauseDamage(Sphere.position+new Vector3(0,0.5f,0), 4f, ArmorSetAirDamage);
                        break;
                    }
            }
        }

        public void ShieldParryON()
        {
            Player.ParryON();
        }
        public void ShieldParryOFF()
        {
            Player.ParryOFF();
        }

        public void TurnOnGlow()
        {
            if (GlowEffect != null)
                GlowEffect.SetActive(true);
        }

        public void TurnOffGlow()
        {
            if (GlowEffect != null)
                GlowEffect.SetActive(false);
        }
    }
}
