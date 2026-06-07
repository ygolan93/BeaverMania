using System.Collections;
using Beavermania.Core.Input;
using Beavermania.Data.NPC;
using Beavermania.Display;
using Beavermania.Objects;
using Beavermania.Player;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beavermania.NPC
{
    public class NPC_Basic : MonoBehaviour
    {
        const float LookRotationEpsilon = 0.0001f;
        const float AnotherWaspLookupCooldown = 1.0f;
        const float FarAiUpdateDistance = 40f;
        const float HitEffectVisibleDuration = 0.32f;

        [Header("Core References")]
        public Rigidbody NPC;
        [SerializeField] Animator Wasp;
        Collider npcCollider;
        GameObject AnotherWasp;
        Collider AnotherWaspCollider;
        float AnotherWaspLookupTimer;

        [Header("Player References")]
        GameObject PlayerTarget;
        BeaverPlayer PlayerHealth;

        [Header("Movement")]
        public Vector3 Distance;
        public Quaternion rotGoal;
        readonly float steer = 0.5f;
        public float AttackSpeed = 7f;
        public float PlayerDistance;
        float ChangeNav = 5f;
        float ChargeClock = 0.7f;
        bool Contact = false;
        float a;
        float b;
        float c;
        Vector3 SpawnPos;
        bool deathHandled;
        EnemyHealthBarVisibility healthBarVisibility;
        Coroutine hitEffectHideRoutine;
        Coroutine floatRoutine;

        [Header("Floating")]
        public Vector3 currentPos;
        public bool floating;

        [Header("Stats")]
        [SerializeField] WaspStatsData statsData;
        [SerializeField] string defaultStatsResourcePath;

        [Header("Health and Damage")]
        public int combo = 0;
        public int CurrentHealth;
        public NPC_Health NPCHealthBar;

        [Header("Legacy Stats Fallback")]
        [FormerlySerializedAs("floatSpeed")]
        [SerializeField] float legacyFloatSpeed = 1f;
        [FormerlySerializedAs("floatDistance")]
        [SerializeField] float legacyFloatDistance = 1f;
        [FormerlySerializedAs("maxTiltAngle")]
        [SerializeField] float legacyMaxTiltAngle = 10f;
        [FormerlySerializedAs("hit2stun")]
        [SerializeField] int legacyHitsToStun = 10;
        [FormerlySerializedAs("Damage2Player")]
        [SerializeField] int legacyDamageToPlayer = 1;
        [FormerlySerializedAs("MaxHealth")]
        [SerializeField] int legacyMaxHealth = 2000;
        [FormerlySerializedAs("Recovery")]
        [SerializeField] float legacyStunRecovery = 10f;

        WaspStatsData fallbackStats;
        float stunRecoveryClock;

        public int MaxHealth => ActiveStats.maxHealth;
        public int hit2stun => ActiveStats.hitsToStun;
        public int Damage2Player => ActiveStats.damageToPlayer;
        float floatSpeed => ActiveStats.floatSpeed;
        float floatDistance => ActiveStats.floatDistance;
        float maxTiltAngle => ActiveStats.maxTiltAngle;
        WaspStatsData ActiveStats => statsData != null ? statsData : ResolveFallbackStats();

        [Header("Effects")]
        public GameObject HitEffect;
        public GameObject SlashEffect;
        public GameObject Explosion;

        [Header("Sound")]
        public NPC_Audio Sound;
        [SerializeField] GameObject BuzzSource;

        [Header("On Death")]
        public GameObject Body;
        public GameObject Head;
        public GameObject Wing;
        public GameObject Leg;
        public GameObject Reward;

        void OnEnable()
        {
            ActiveWaspRegistry.Register(this);
        }

        void OnDisable()
        {
            ActiveWaspRegistry.Unregister(this);
            StopFloatRoutine();
        }

        public void Start()
        {
            SpawnPos = transform.position;
            Wasp = GetComponent<Animator>();
            npcCollider = GetComponent<Collider>();
            CurrentHealth = MaxHealth;
            stunRecoveryClock = ActiveStats.stunRecovery;
            PlayerHealth = PlayerReferenceCache.GetPlayer();
            if (PlayerHealth != null)
                PlayerTarget = PlayerHealth.gameObject;
            if (PlayerTarget == null)
                PlayerTarget = GameObject.FindGameObjectWithTag("Player");
            if (NPCHealthBar != null)
                NPCHealthBar.SetMaxNPCHealth(MaxHealth);
            healthBarVisibility = GetComponent<EnemyHealthBarVisibility>();
            if (healthBarVisibility == null)
                healthBarVisibility = gameObject.AddComponent<EnemyHealthBarVisibility>();
            SetCombatEffectsActive(false);
            RandoMovement();
            NPC.velocity = Vector3.forward;
            if (BuzzSource != null)
                BuzzSource.SetActive(true);
        }

        public void FixedUpdate()
        {
            if (deathHandled || CurrentHealth <= 0 || PlayerTarget == null)
                return;

            AnotherWaspLookupTimer -= Time.deltaTime;
            Vector3 distanceToPlayer = PlayerTarget.transform.position - transform.position;
            Distance = distanceToPlayer;
            PlayerDistance = distanceToPlayer.magnitude;
            ChangeNav -= Time.deltaTime;

            bool throttleFarAi = PlayerDistance > FarAiUpdateDistance && (Time.frameCount & 1) != 0;
            if (throttleFarAi)
            {
                if (!PlayerInputReader.IsPrimaryHeld() || PlayerDistance > 3f)
                    SetCombatEffectsActive(false);

                return;
            }

            if (combo < hit2stun)
            {
                if (floating)
                {
                    Wasp.SetBool("Sting", false);
                    currentPos = transform.position;
                    FloatOnAir(currentPos);
                }
                else
                {
                    StopFloatRoutine();

                    if (NPC.velocity.sqrMagnitude > LookRotationEpsilon)
                        rotGoal = Quaternion.LookRotation(NPC.velocity);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
                    if (distanceToPlayer.magnitude >= 50f)
                    {
                        if (ChangeNav <= 0f)
                        {
                            if (BuzzSource != null)
                                BuzzSource.SetActive(true);
                            Wasp.SetBool("Sting", false);
                            RandoMovement();
                        }
                        else
                        {
                            TurnBack();
                        }
                    }

                    if (PlayerDistance < 50f)
                    {
                        if (!Contact)
                        {
                            if (BuzzSource != null)
                                BuzzSource.SetActive(true);
                            ChargeClock = 0.7f;
                            RefreshAnotherWaspCollider();
                            if (AnotherWaspCollider != null && npcCollider != null)
                                Physics.IgnoreCollision(AnotherWaspCollider, npcCollider);

                            NPC.velocity = distanceToPlayer.normalized * 50f;
                            Wasp.SetBool("Sting", true);
                            ChangeNav = 0f;
                        }
                        else
                        {
                            Wasp.SetBool("Sting", false);
                            NPC.AddForce(new Vector3(-distanceToPlayer.x, 0.01f, -distanceToPlayer.z).normalized * 0.1f);
                            if (distanceToPlayer.sqrMagnitude > LookRotationEpsilon)
                                transform.rotation = Quaternion.LookRotation(distanceToPlayer);
                            ChargeClock -= Time.deltaTime;
                            if (ChargeClock <= 0f)
                                Contact = false;
                        }
                    }
                }
            }
            else
            {
                StopFloatRoutine();
                Stunned();
                stunRecoveryClock -= Time.deltaTime;
                if (stunRecoveryClock <= 0f)
                {
                    Recovered();
                    combo = 0;
                }
            }

            if (!PlayerInputReader.IsPrimaryHeld() || PlayerDistance > 3f)
                SetCombatEffectsActive(false);
        }

        void LateUpdate()
        {
            if (deathHandled)
                return;

            if (!PlayerInputReader.IsPrimaryHeld() || Distance.sqrMagnitude > 36f)
                Wasp.SetBool("Beat", false);

            if (CurrentHealth <= 0)
                Death();
        }

        void RefreshAnotherWaspCollider()
        {
            if (AnotherWaspCollider != null && AnotherWasp != null && AnotherWasp != gameObject && AnotherWaspCollider.gameObject == AnotherWasp)
                return;

            if (AnotherWaspLookupTimer > 0f)
                return;

            AnotherWaspLookupTimer = AnotherWaspLookupCooldown;
            AnotherWasp = null;
            AnotherWaspCollider = null;

            if (ActiveWaspRegistry.TryFindNearestOtherWaspCollider(this, out Collider nearestCollider, out GameObject nearestWasp))
            {
                AnotherWasp = nearestWasp;
                AnotherWaspCollider = nearestCollider;
            }
        }

        void Death()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            StopFloatRoutine();
            ActiveWaspRegistry.Unregister(this);

            if (PlayerHealth != null && PlayerHealth.BoostCharge != null)
                PlayerHealth.BoostCharge.RegisterKill();

            if (PlayerHealth != null)
            {
                PlayerHealth.Plattering = "Gotcha!";
                PlayerHealth.ChangeSpeech = 0.55f;
            }

            PooledOneShotVfx.Spawn(Explosion, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Body, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Head, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Wing, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Wing, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0f, 1f, 0f), transform.rotation);
            CurrencySpin.ConfigureEnemyDrop(Instantiate(Reward, transform.position + new Vector3(0f, 7f, 0f), transform.rotation));
            CurrencySpin.ConfigureEnemyDrop(Instantiate(Reward, transform.position + new Vector3(0f, 7f, 0f), transform.rotation));
            Destroy(gameObject);
        }

        void OnCollisionEnter(Collision OBJ)
        {
            if (deathHandled)
                return;

            if (OBJ.gameObject == PlayerTarget)
            {
                if (combo < hit2stun)
                    Sting();

                Contact = true;
                if (PlayerHealth != null && PlayerHealth.isParried)
                {
                    TakeDamage(ActiveStats.parryDamage);
                    combo += 10;
                }
            }

            if (OBJ.gameObject.CompareTag("Strike"))
            {
                Death();
                return;
            }

            if (OBJ.gameObject.CompareTag("Damage"))
            {
                Wasp.SetBool("Beat", true);
                TakeDamage(ActiveStats.weaponHitDamage);
                if (Sound != null)
                    Sound.Beat();
                combo = hit2stun;
            }

            if (OBJ.gameObject.CompareTag("Isle"))
            {
                Contact = true;
                NPC.velocity += new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f));
            }
        }

        public void TurnBack()
        {
            if (BuzzSource != null)
                BuzzSource.SetActive(true);
            Wasp.SetBool("Sting", false);
            if ((transform.position - SpawnPos).magnitude > 30f)
                NPC.velocity = (SpawnPos - transform.position).normalized * 10f;
        }

        void FloatOnAir(Vector3 initialPosition)
        {
            if (floatRoutine != null)
                return;

            floatRoutine = StartCoroutine(FloatObject(initialPosition));
        }

        void StopFloatRoutine()
        {
            if (floatRoutine == null)
                return;

            StopCoroutine(floatRoutine);
            floatRoutine = null;
        }

        IEnumerator FloatObject(Vector3 initialPosition)
        {
            float direction = -1f;
            while (floating && !deathHandled)
            {
                float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatDistance + 1f;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                float tiltAngle = direction * maxTiltAngle * Mathf.Sin(Time.time * floatSpeed);
                transform.rotation = Quaternion.Euler(tiltAngle, transform.rotation.eulerAngles.y, 0f);
                if (newY >= initialPosition.y + floatDistance || newY <= initialPosition.y - floatDistance)
                    direction *= 1f;

                yield return null;
            }

            floatRoutine = null;
        }

        public void Stunned()
        {
            StopFloatRoutine();
            if (BuzzSource != null)
                BuzzSource.SetActive(false);
            floating = false;
            NPC.constraints = RigidbodyConstraints.None;
            NPC.useGravity = true;
            Wasp.SetBool("Stunned", true);
            Wasp.SetBool("Sting", false);
        }

        void Recovered()
        {
            StopFloatRoutine();
            if (BuzzSource != null)
                BuzzSource.SetActive(true);
            floating = false;
            Wasp.SetBool("Stunned", false);
            stunRecoveryClock = ActiveStats.stunRecovery;
            NPC.constraints = RigidbodyConstraints.FreezeRotation;
            NPC.useGravity = false;
        }

        public void Sting()
        {
            floating = false;
            Recovered();
            if (PlayerHealth != null && !PlayerHealth.isParried && !PlayerHealth.Rolling)
            {
                if (Sound != null)
                    Sound.Sting();
                PlayerHealth.TakeDamage(Damage2Player);
            }

            Wasp.SetBool("Sting", false);
            if (PlayerHealth != null && PlayerHealth.isParried)
            {
                PlayerHealth.TakeDamage(0.01f);
                TakeDamage(1);
                combo = 3;
            }
        }

        public void RandoMovement()
        {
            if (BuzzSource != null)
                BuzzSource.SetActive(true);
            if ((SpawnPos - transform.position).magnitude < 20f)
            {
                Recovered();
                a = Random.Range(-1f, 1f);
                b = Random.Range(-0.1f, 0.1f);
                c = Random.Range(-1f, 1f);
                NPC.velocity = new Vector3(a, b, c);
                ChangeNav = 5f;
            }
            else
            {
                NPC.velocity = (SpawnPos - transform.position).normalized * 5f;
            }
        }

        public void TakeDamage(int Damage)
        {
            if (deathHandled || Damage <= 0)
                return;

            if (BuzzSource != null)
                BuzzSource.SetActive(false);

            Vector3 towardPlayer = PlayerTarget != null ? PlayerTarget.transform.position - transform.position : Distance;
            if (towardPlayer.sqrMagnitude > LookRotationEpsilon)
            {
                rotGoal = Quaternion.LookRotation(towardPlayer);
                transform.rotation = rotGoal;
            }

            NPC.useGravity = true;
            Wasp.SetBool("Beat", true);
            Wasp.SetBool("Sting", false);
            CurrentHealth -= Damage;

            if (PlayerHealth != null)
            {
                var playerArsenal = PlayerHealth.Arsenal;
                var currentWeapon = PlayerHealth.arsenalBrowser;
                switch (playerArsenal[currentWeapon])
                {
                    case "Bare Hands":
                    case "Hammers":
                    case "Bow":
                        SetHitEffectActive(true);
                        if (Sound != null)
                            Sound.Beat();
                        break;
                    case "ArmorSet":
                        SetSlashEffectActive(true);
                        if (Sound != null)
                            Sound.LiteSwordDamage();
                        break;
                }
            }

            combo++;
            if (NPCHealthBar != null)
                NPCHealthBar.SetNPCHealth(CurrentHealth);

            if (healthBarVisibility != null)
                healthBarVisibility.NotifyDamaged();

            ShowHitEffectBriefly();

            if (PlayerHealth != null && PlayerHealth.BoostCharge != null)
                PlayerHealth.BoostCharge.RegisterHit(Damage);
        }

        void SetCombatEffectsActive(bool active)
        {
            SetHitEffectActive(active);
            SetSlashEffectActive(active);
        }

        void SetHitEffectActive(bool active)
        {
            if (HitEffect != null && HitEffect.activeSelf != active)
                HitEffect.SetActive(active);
        }

        void SetSlashEffectActive(bool active)
        {
            if (SlashEffect != null && SlashEffect.activeSelf != active)
                SlashEffect.SetActive(active);
        }

        void ShowHitEffectBriefly()
        {
            if (HitEffect == null)
                return;

            SetHitEffectActive(true);
            if (hitEffectHideRoutine != null)
                StopCoroutine(hitEffectHideRoutine);
            hitEffectHideRoutine = StartCoroutine(HideHitEffectAfterDelay());
        }

        IEnumerator HideHitEffectAfterDelay()
        {
            yield return new WaitForSeconds(HitEffectVisibleDuration);
            if (HitEffect != null)
                SetHitEffectActive(false);
            hitEffectHideRoutine = null;
        }

        WaspStatsData ResolveFallbackStats()
        {
            if (fallbackStats != null)
                return fallbackStats;

            WaspStatsData loadedStats = LoadConfiguredStatsData();
            if (loadedStats != null)
            {
                fallbackStats = loadedStats;
                return fallbackStats;
            }

            fallbackStats = ScriptableObject.CreateInstance<WaspStatsData>();
            fallbackStats.hideFlags = HideFlags.HideAndDontSave;
            fallbackStats.maxHealth = legacyMaxHealth;
            fallbackStats.damageToPlayer = legacyDamageToPlayer;
            fallbackStats.hitsToStun = legacyHitsToStun;
            fallbackStats.stunRecovery = legacyStunRecovery;
            fallbackStats.floatSpeed = legacyFloatSpeed;
            fallbackStats.floatDistance = legacyFloatDistance;
            fallbackStats.maxTiltAngle = legacyMaxTiltAngle;
            fallbackStats.weaponHitDamage = 15;
            fallbackStats.parryDamage = 20;

            Debug.LogWarning($"{name}: WaspStatsData reference missing. Using legacy serialized fallback values.", this);
            return fallbackStats;
        }

        WaspStatsData LoadConfiguredStatsData()
        {
            if (!string.IsNullOrWhiteSpace(defaultStatsResourcePath))
                return Resources.Load<WaspStatsData>(defaultStatsResourcePath);

            return null;
        }
    }
}
