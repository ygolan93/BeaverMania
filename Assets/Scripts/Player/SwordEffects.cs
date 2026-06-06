using System.Collections.Generic;
using Beavermania.Data.Combat;
using Beavermania.Player;
using UnityEngine;

namespace Beavermania.Player.Combat
{
    public class SwordEffects : MonoBehaviour
    {
        const string SwordGlareChildName = "SwordGlare";

        readonly List<GameObject> activeFireBreathPresentationObjects = new List<GameObject>();

        Rigidbody Player;
        [SerializeField] GameObject FireSwordTrail;
        [SerializeField] GameObject FireBreath;
        [SerializeField] Transform Sword;
        [SerializeField] Transform AttackPoint;
        [SerializeField] GameObject SwordCopter;
        [SerializeField] GameObject SwordPlainTrail;
        [SerializeField] GameObject swordGlare;

        bool loggedMissingSwordGlare;
        bool lastSwordGlareActiveState;
        bool hasLastSwordGlareActiveState;

        void Awake()
        {
            CacheSwordGlareReference();
        }

        void Start()
        {
            Player = transform.parent.GetComponent<Rigidbody>();
            CacheSwordGlareReference();
        }

        void OnEnable()
        {
            hasLastSwordGlareActiveState = false;
        }

        void CacheSwordGlareReference()
        {
            if (swordGlare != null || Sword == null)
                return;

            Transform glareTransform = Sword.Find(SwordGlareChildName);
            if (glareTransform != null)
                swordGlare = glareTransform.gameObject;
        }

        public void FireSword()
        {
            BeaverPlayerBehaviour owner = ResolveOwner();
            if (owner == null)
                return;

            WeaponData equippedWeapon = owner.EquippedWeaponData;
            if (equippedWeapon == null || !equippedWeapon.supportsFireBreath)
            {
                owner.StopFireBreathAttackPresentation();
                owner.UpdateSwordGlareAvailability();
                return;
            }

            if (FireBreath == null || AttackPoint == null || Sword == null)
            {
                owner.StopFireBreathAttackPresentation();
                owner.UpdateSwordGlareAvailability();
                return;
            }

            if (!FireBreath.TryGetComponent(out Projectile fireBreathPrefab))
            {
                owner.StopFireBreathAttackPresentation();
                owner.UpdateSwordGlareAvailability();
                return;
            }

            owner.TryActivateFireBreath(fireBreathPrefab, AttackPoint);
        }

        public void SetSwordGlareActive(bool active)
        {
            if (!TryResolveSwordGlare(out GameObject glare))
                return;

            if (hasLastSwordGlareActiveState && lastSwordGlareActiveState == active && glare.activeSelf == active)
                return;

            hasLastSwordGlareActiveState = true;
            lastSwordGlareActiveState = active;

            if (active)
            {
                if (Sword != null && !Sword.gameObject.activeInHierarchy)
                    return;

                glare.SetActive(true);
                PlayPresentationOnObject(glare);
                return;
            }

            StopPresentationOnObject(glare);
            glare.SetActive(false);
        }

        public void OnFireBreathActivated()
        {
            SpawnFireSwordTrail();
        }

        public void StopFireBreathTrailPresentation()
        {
            for (var i = activeFireBreathPresentationObjects.Count - 1; i >= 0; i--)
            {
                GameObject presentationObject = activeFireBreathPresentationObjects[i];
                if (presentationObject == null)
                    continue;

                StopPresentationOnObject(presentationObject);
                Destroy(presentationObject);
            }

            activeFireBreathPresentationObjects.Clear();
        }

        public void StopFireBreathPresentation()
        {
            StopFireBreathTrailPresentation();
        }

        bool TryResolveSwordGlare(out GameObject glare)
        {
            glare = swordGlare;
            if (glare == null)
                CacheSwordGlareReference();

            glare = swordGlare;
            if (glare != null)
                return true;

            if (!loggedMissingSwordGlare)
            {
                Debug.LogWarning(
                    $"{nameof(SwordEffects)}: {SwordGlareChildName} child was not found under sword transform '{Sword?.name}'. FireBreath glare control skipped.",
                    this);
                loggedMissingSwordGlare = true;
            }

            return false;
        }

        void SpawnFireSwordTrail()
        {
            if (FireSwordTrail == null || Sword == null)
                return;

            var fireSwordTrail = Instantiate(FireSwordTrail, Sword.position, Sword.rotation);
            fireSwordTrail.transform.SetParent(Sword, true);
            activeFireBreathPresentationObjects.Add(fireSwordTrail);
        }

        static void PlayPresentationOnObject(GameObject presentationObject)
        {
            ParticleSystem[] particleSystems = presentationObject.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] == null)
                    continue;

                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystems[i].Play(true);
            }

            TrailRenderer[] trails = presentationObject.GetComponentsInChildren<TrailRenderer>(true);
            for (var i = 0; i < trails.Length; i++)
            {
                if (trails[i] == null)
                    continue;

                trails[i].emitting = true;
            }

            Light[] lights = presentationObject.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].enabled = true;
            }
        }

        static void StopPresentationOnObject(GameObject presentationObject)
        {
            ParticleSystem[] particleSystems = presentationObject.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null)
                    particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            TrailRenderer[] trails = presentationObject.GetComponentsInChildren<TrailRenderer>(true);
            for (var i = 0; i < trails.Length; i++)
            {
                if (trails[i] != null)
                {
                    trails[i].emitting = false;
                    trails[i].Clear();
                }
            }

            Light[] lights = presentationObject.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].enabled = false;
            }
        }

        BeaverPlayerBehaviour ResolveOwner()
        {
            return transform.parent != null
                ? transform.parent.GetComponent<BeaverPlayerBehaviour>()
                : null;
        }

        public void GreatSwing()
        {
            BeaverPlayerBehaviour owner = ResolveOwner();
            if (owner?.EquippedWeaponData == null || owner.EquippedWeaponData.category != WeaponCategory.ArmorSet)
                return;

            var greatTrail = Instantiate(SwordCopter, Sword.position, Sword.rotation);
            greatTrail.transform.parent = Sword;
        }

        public void SmallSwing()
        {
            BeaverPlayerBehaviour owner = ResolveOwner();
            if (owner?.EquippedWeaponData == null || owner.EquippedWeaponData.category != WeaponCategory.ArmorSet)
                return;

            var smallTrail = Instantiate(SwordPlainTrail, Sword.position, Sword.rotation);
            smallTrail.transform.parent = Sword;
        }
    }
}
