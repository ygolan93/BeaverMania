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
        const int OverlapBufferSize = 24;

        static readonly Collider[] OverlapBuffer = new Collider[OverlapBufferSize];
        static readonly int[] ProcessedTargetIds = new int[OverlapBufferSize];

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
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                range,
                OverlapBuffer,
                enemyLayers,
                QueryTriggerInteraction.Collide);

            int processedCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                Collider enemy = OverlapBuffer[i];
                if (enemy == null)
                    continue;

                int dedupeKey = ResolveDamageDedupeKey(enemy);
                if (WasTargetAlreadyProcessed(dedupeKey, processedCount))
                    continue;

                ProcessedTargetIds[processedCount] = dedupeKey;
                processedCount++;

                if (enableHitDebugLogs && enemy.name != null)
                    Debug.Log("Hit " + enemy.name);

                if (TryApplyInterfaceDamage(enemy, Damage))
                    continue;

                switch (enemy.tag)
                {
                    case "NPC":
                        {
                            if (!enemy.TryGetComponent(out NPC_Basic wasp))
                                wasp = enemy.GetComponentInParent<NPC_Basic>();
                            if (wasp != null)
                                wasp.TakeDamage(Damage);
                            break;
                        }
                    case "Hive":
                        {
                            if (!enemy.TryGetComponent(out Static_Hive hive))
                                hive = enemy.GetComponentInParent<Static_Hive>();
                            if (hive != null)
                                hive.TakeDamage(Damage);
                            break;
                        }
                    case "Scorpion":
                        {
                            if (!enemy.TryGetComponent(out ScorpionScript scorpion))
                                scorpion = enemy.GetComponentInParent<ScorpionScript>();
                            if (scorpion != null)
                                scorpion.TakeDamage(Damage);
                            break;
                        }
                }
            }
        }

        static int ResolveDamageDedupeKey(Collider enemy)
        {
            IEnemyDamageReceiver damageReceiver = enemy.GetComponentInParent<IEnemyDamageReceiver>();
            if (damageReceiver is Component receiverComponent)
                return receiverComponent.GetInstanceID();

            Transform root = enemy.transform.root;
            return root != null ? root.GetInstanceID() : enemy.GetInstanceID();
        }

        static bool WasTargetAlreadyProcessed(int dedupeKey, int processedCount)
        {
            for (int i = 0; i < processedCount; i++)
            {
                if (ProcessedTargetIds[i] == dedupeKey)
                    return true;
            }

            return false;
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
            WeaponData weaponData = ResolveEquippedWeaponData();
            if (weaponData == null)
            {
                CauseDamage(
                    AttackPoint.position,
                    FallbackBareHandsGroundRadius,
                    FallbackBareHandsGroundDamage);
                return;
            }

            Vector3 origin = weaponData.category == WeaponCategory.ArmorSet && Sphere != null
                ? Sphere.position + Vector3.up * weaponData.groundAttackOriginYOffset
                : AttackPoint.position + Vector3.up * weaponData.groundAttackOriginYOffset;

            CauseDamage(origin, weaponData.groundAttackRadius, weaponData.groundMeleeDamage);
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
                CauseDamage(origin, weaponData.airAttackRadius, weaponData.airMeleeDamage);
                return;
            }

            int airDamage = weaponData != null ? weaponData.airMeleeDamage : FallbackBareHandsAirDamage;
            float airRadius = weaponData != null ? weaponData.airAttackRadius : FallbackBareHandsAirRadius;
            CauseDamage(Sphere.position, airRadius, airDamage);
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
