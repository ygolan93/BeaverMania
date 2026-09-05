using System.Collections.Generic;
using Beavermania.Data.Combat;
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
        [SerializeField] bool enableHitDebugLogs;

        const int FallbackRollAttackDamage = 200;
        const int FallbackBareHandsGroundDamage = 50;
        const int FallbackBareHandsAirDamage = 20;
        const float FallbackBareHandsGroundRadius = 0.7f;
        const float FallbackBareHandsAirRadius = 2.5f;

        static readonly Collider[] s_hitBuffer = new Collider[32];
        static readonly HashSet<int> s_routedReceiverIds = new HashSet<int>();

        private void Start()
        {
            Player = transform.parent.GetComponent<BeaverPlayer>();
            if (GlowEffect != null)
                GlowEffect.SetActive(false);
        }

        int RollAttackDamage =>
            Player != null && Player.CombatBalance != null
                ? Player.CombatBalance.rollAttackDamage
                : FallbackRollAttackDamage;

        WeaponData ResolveEquippedWeaponData()
        {
            return Player != null ? Player.EquippedWeaponData : null;
        }

        public void CauseDamage(Vector3 origin, float range, int Damage)
        {
            CauseDamage(origin, range, Damage, PlayerAttackKind.Unspecified);
        }

        void CauseDamage(Vector3 origin, float range, int damage, PlayerAttackKind attackKind)
        {
            s_routedReceiverIds.Clear();
            int count = Physics.OverlapSphereNonAlloc(origin, range, s_hitBuffer, enemyLayers);
            for (int i = 0; i < count; i++)
            {
                Collider enemy = s_hitBuffer[i];
                if (enemy == null)
                    continue;

                if (enableHitDebugLogs && enemy.name != null)
                    Debug.Log("Hit " + enemy.name);

                if (TryApplyInterfaceDamage(enemy, damage, attackKind))
                    continue;

                switch (enemy.tag)
                {
                    case "NPC":
                        {
                            var wasp = enemy.gameObject.GetComponent<NPC_Basic>();
                            if (wasp != null)
                                wasp.TakeDamage(damage);
                            break;
                        }
                    case "Hive":
                        {
                            var hive = enemy.gameObject.GetComponent<Static_Hive>();
                            if (hive != null)
                                hive.TakeDamage(damage);
                            break;
                        }
                    case "Scorpion":
                        {
                            var scorpion = enemy.gameObject.GetComponent<ScorpionScript>();
                            if (scorpion != null)
                                scorpion.ReceivePlayerAttack(damage, attackKind, EnemyDamageType.Normal, transform);
                            break;
                        }
                }
            }
        }

        bool TryApplyInterfaceDamage(Collider enemy, int damage, PlayerAttackKind attackKind)
        {
            if (enemy == null)
                return false;

            IEnemyDamageReceiver damageReceiver = enemy.GetComponentInParent<IEnemyDamageReceiver>();
            if (damageReceiver == null)
                return false;

            if (!TryClaimReceiverForThisSwing(damageReceiver))
                return true;

            return TryRouteInterfaceDamage(damageReceiver, damage, attackKind);
        }

        /// <summary>
        /// A boss overlaps the swing sphere with several colliders (jaws, sting, body), all resolving to the
        /// same receiver. One animation event must reach that receiver once, so the first collider claims it
        /// for the remainder of this <see cref="CauseDamage"/> call and later duplicates are reported as
        /// already owned. The claim set is per-swing state on a single-threaded call path, so reusing it
        /// keeps the hit loop allocation-free.
        /// </summary>
        static bool TryClaimReceiverForThisSwing(IEnemyDamageReceiver damageReceiver)
        {
            return s_routedReceiverIds.Add(GetDamageReceiverId(damageReceiver));
        }

        static int GetDamageReceiverId(IEnemyDamageReceiver damageReceiver)
        {
            return damageReceiver is Component component ? component.GetInstanceID() : damageReceiver.GetHashCode();
        }

        /// <summary>
        /// Reports whether the receiver owned the hit, not whether health was reduced. The receiver remains
        /// responsible for rejecting invalid or terminal-state damage, so the legacy tag fallback must not
        /// damage the same enemy a second time.
        /// </summary>
        bool TryRouteInterfaceDamage(
            IEnemyDamageReceiver damageReceiver,
            int damage,
            PlayerAttackKind attackKind)
        {
            if (damageReceiver == null)
                return false;

            if (damageReceiver is IPlayerAttackReceiver playerAttackReceiver)
                playerAttackReceiver.ReceivePlayerAttack(damage, attackKind, EnemyDamageType.Normal, transform);
            else
                damageReceiver.ReceiveDamage(damage, EnemyDamageType.Normal, transform);

            return true;
        }

        public void RollAttack()
        {
            CauseDamage(AttackPoint.position, 1.5f, RollAttackDamage, PlayerAttackKind.Unspecified);
        }

        public void GroundAttack()
        {
            WeaponData weaponData = ResolveEquippedWeaponData();
            if (weaponData == null)
            {
                CauseDamage(
                    AttackPoint.position,
                    FallbackBareHandsGroundRadius,
                    FallbackBareHandsGroundDamage,
                    PlayerAttackKind.BareHands);
                return;
            }

            Vector3 origin = weaponData.category == WeaponCategory.ArmorSet && Sphere != null
                ? Sphere.position + Vector3.up * weaponData.groundAttackOriginYOffset
                : AttackPoint.position + Vector3.up * weaponData.groundAttackOriginYOffset;

            PlayerAttackKind attackKind = weaponData.category == WeaponCategory.ArmorSet
                ? PlayerAttackKind.SwordSwing
                : weaponData.category == WeaponCategory.BareHands
                    ? PlayerAttackKind.BareHands
                    : PlayerAttackKind.Unspecified;
            CauseDamage(origin, weaponData.groundAttackRadius, weaponData.groundMeleeDamage, attackKind);
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

            WeaponData weaponData = ResolveEquippedWeaponData();
            if (weaponData != null && weaponData.category == WeaponCategory.ArmorSet)
            {
                Vector3 origin = Sphere.position + Vector3.up * weaponData.groundAttackOriginYOffset;
                CauseDamage(
                    origin,
                    weaponData.airAttackRadius,
                    weaponData.airMeleeDamage,
                    PlayerAttackKind.HurricaneSword);
                return;
            }

            int airDamage = weaponData != null ? weaponData.airMeleeDamage : FallbackBareHandsAirDamage;
            float airRadius = weaponData != null ? weaponData.airAttackRadius : FallbackBareHandsAirRadius;
            CauseDamage(Sphere.position, airRadius, airDamage, PlayerAttackKind.HurricaneKick);
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
