using Beavermania.Player;
using UnityEngine;

namespace Beavermania.Player.Combat
{

    public class SwordEffects : MonoBehaviour
    {
        Rigidbody Player;
        [SerializeField] GameObject FireSwordTrail;
        [SerializeField] GameObject FireBreath;
        [SerializeField] Transform Sword;
        [SerializeField] Transform AttackPoint;
        [SerializeField] GameObject SwordCopter;
        [SerializeField] GameObject SwordPlainTrail;

        void Start()
        {
            Player = transform.parent.GetComponent<Rigidbody>();
        }

        public void FireSword()
        {
            if (FireSwordTrail != null && Sword != null)
            {
                var bigSwordTrail = Instantiate(FireSwordTrail, Sword.position, Sword.rotation);
                bigSwordTrail.transform.parent = Sword;
            }

            if (FireBreath == null || AttackPoint == null || Sword == null)
                return;

            if (!FireBreath.TryGetComponent(out Projectile fireBreathPrefab))
                return;

            BeaverPlayerBehaviour owner = transform.parent != null
                ? transform.parent.GetComponent<BeaverPlayerBehaviour>()
                : null;

            if (owner != null
                && owner.TryLaunchFireBreathFromAttackPoint(fireBreathPrefab, AttackPoint, Vector3.zero))
            {
                return;
            }

            if (owner == null)
                return;

            Vector3 fallbackFirePoint = AttackPoint.position;
            if (!owner.TryResolveFireBreathLaunchPose(fallbackFirePoint, out Vector3 spawnPosition, out Vector3 aimDirection, out _))
                return;

            Projectile.Spawn(
                fireBreathPrefab,
                spawnPosition,
                Quaternion.LookRotation(aimDirection, Vector3.up),
                aimDirection,
                owner);
        }

        public void GreatSwing()
        {
            var greatTrail = Instantiate(SwordCopter, Sword.position, Sword.rotation);
            greatTrail.transform.parent = Sword;
        }
        public void SmallSwing()
        {
            var smallTrail = Instantiate(SwordPlainTrail, Sword.position, Sword.rotation);
            smallTrail.transform.parent = Sword;
        }
    }
}
