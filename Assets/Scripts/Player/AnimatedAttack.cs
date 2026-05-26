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
                if (enemy == null)
                    continue;

                if (enemy.name != null)
                    Debug.Log("Hit " + enemy.name);

                if (TryApplyInterfaceDamage(enemy, Damage))
                    continue;

                switch (enemy.tag)
                {
                    case "NPC":
                        {
                            var wasp = enemy.gameObject.GetComponent<NPC_Basic>();
                            if (wasp != null)
                                wasp.TakeDamage(Damage);
                            break;
                        }
                    case "Hive":
                        {
                            var hive = enemy.gameObject.GetComponent<Static_Hive>();
                            if (hive != null)
                                hive.TakeDamage(Damage);
                            break;
                        }
                    case "Scorpion":
                        {
                            var scorpion = enemy.gameObject.GetComponent<ScorpionScript>();
                            if (scorpion != null)
                                scorpion.TakeDamage(Damage);
                            break;
                        }
                }
            }
        }

        bool TryApplyInterfaceDamage(Collider enemy, int damage)
        {
            if (enemy == null)
                return false;

            IEnemyDamageReceiver damageReceiver = enemy.GetComponentInParent<IEnemyDamageReceiver>();
            return damageReceiver != null && damageReceiver.ReceiveDamage(damage, EnemyDamageType.Normal, transform);
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
        /// <summary>
        /// Hurricane Kick damage window (animation event). Weapon-independent skill; bow does not block air kick hits.
        /// </summary>
        public void AirAttack()
        {
            ApplyHurricaneKickDamage();
        }

        void ApplyHurricaneKickDamage()
        {
            if (Player == null || Sphere == null)
                return;

            if (Player.ArmorEquipped)
                CauseDamage(Sphere.position + new Vector3(0, 0.5f, 0), 4f, ArmorSetAirDamage);
            else
                CauseDamage(Sphere.position, 2.5f, BareHandsAirDamage);
        }

        public void ShieldParryON()
        {
            if (Player != null)
                Player.SetShieldParryAnimator(true);
        }

        public void ShieldParryOFF()
        {
            if (Player != null)
                Player.SetShieldParryAnimator(false);
        }

        public void SetGlowActive(bool active)
        {
            if (GlowEffect == null)
                return;

            if (!active)
            {
                StopFireBreathPresentation();
                return;
            }

            GlowEffect.SetActive(true);
        }

        public void StopFireBreathPresentation()
        {
            if (GlowEffect != null)
            {
                ParticleSystem[] particleSystems = GlowEffect.GetComponentsInChildren<ParticleSystem>(true);
                for (var i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null)
                        particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                TrailRenderer[] trails = GlowEffect.GetComponentsInChildren<TrailRenderer>(true);
                for (var i = 0; i < trails.Length; i++)
                {
                    if (trails[i] != null)
                        trails[i].Clear();
                }

                Light[] lights = GlowEffect.GetComponentsInChildren<Light>(true);
                for (var i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null)
                        lights[i].enabled = false;
                }

                GlowEffect.SetActive(false);
            }
        }

        public bool IsFinisherGlowActive()
        {
            return GlowEffect != null && GlowEffect.activeSelf;
        }

        public void TurnOnGlow()
        {
            if (Player != null && Player.IsSwordAndShieldEquipped() && !Player.CanActivateFireBreath())
            {
                Player.StopFireBreathEffects();
                return;
            }

            if (Player != null)
                Player.OnSwordFinisherAnimationStarted();

            SetGlowActive(true);
        }

        public void TurnOffGlow()
        {
            if (Player != null)
                Player.OnSwordFinisherAnimationEnded();
            else
                SetGlowActive(false);
        }
    }
}
