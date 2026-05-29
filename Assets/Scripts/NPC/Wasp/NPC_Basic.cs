using System.Collections;
using System.Collections.Generic;
using Beavermania.Core.Input;
using Beavermania.Display;
using Beavermania.Objects;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

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
        public float Recovery = 10f;
        float ChargeClock = 0.7f;
        bool Contact = false;
        float a;
        float b;
        float c;
        //public WaspCourse course;
        Vector3 SpawnPos;
        bool deathHandled;
        EnemyHealthBarVisibility healthBarVisibility;
        Coroutine hitEffectHideRoutine;

        [Header("Floating")]
        public Vector3 currentPos;
        public bool floating;
        public float floatSpeed = 1.0f;
        public float floatDistance = 1.0f;
        public float maxTiltAngle = 10.0f;

        [Header("Health and Damage")]
        public int hit2stun;
        public int combo = 0;
        public int Damage2Player = 1;
        public int MaxHealth = 2000;
        public int CurrentHealth;
        public NPC_Health NPCHealthBar;

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
        // Start is called before the first frame update    
        public void Start()
        {
            SpawnPos = transform.position;
            Wasp = GetComponent<Animator>();
            npcCollider = GetComponent<Collider>();
            CurrentHealth = MaxHealth;
            PlayerTarget = GameObject.FindGameObjectWithTag("Player");
            PlayerHealth = PlayerTarget.GetComponent<BeaverPlayer>();
            if (NPCHealthBar != null)
                NPCHealthBar.SetMaxNPCHealth(MaxHealth);
            healthBarVisibility = GetComponent<EnemyHealthBarVisibility>();
            if (healthBarVisibility == null)
                healthBarVisibility = gameObject.AddComponent<EnemyHealthBarVisibility>();
            HitEffect.SetActive(false);
            RandoMovement();
            NPC.velocity = Vector3.forward;
            BuzzSource.SetActive(true);
        }

        // Update is called once per frame
        public void FixedUpdate()
        {
            AnotherWaspLookupTimer -= Time.deltaTime;
            Vector3 Distance = PlayerTarget.transform.position - transform.position;
            PlayerDistance = Distance.magnitude;
            ChangeNav -= Time.deltaTime;

            bool throttleFarAi = PlayerDistance > FarAiUpdateDistance && (Time.frameCount & 1) != 0;
            if (throttleFarAi)
            {
                if (!PlayerInputReader.IsPrimaryHeld() || PlayerDistance > 3)
                {
                    HitEffect.SetActive(false);
                    SlashEffect.SetActive(false);
                }
                return;
            }

            if (combo < hit2stun)
            {
                if (floating == true)
                {
                    Wasp.SetBool("Sting", false);
                    currentPos = transform.position;
                    FloatOnAir(currentPos);
                }
                else
                {
                    if (NPC.velocity.sqrMagnitude > LookRotationEpsilon)
                        rotGoal = Quaternion.LookRotation(NPC.velocity);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
                    if (Distance.magnitude >= 50)
                    {
                        if (ChangeNav <= 0)
                        {
                            BuzzSource.SetActive(true);
                            Wasp.SetBool("Sting", false);
                            RandoMovement();
                        }
                        else
                        {
                            TurnBack();
                        }
                    }

                    if (PlayerDistance < 50)
                    {
                        if (Contact == false)
                        {
                            BuzzSource.SetActive(true);
                            ChargeClock = 0.7f;
                            RefreshAnotherWaspCollider();
                            if (AnotherWaspCollider != null && npcCollider != null)
                            {
                                Physics.IgnoreCollision(AnotherWaspCollider, npcCollider);
                            }
                            NPC.velocity = (Distance.normalized * 50f);
                            Wasp.SetBool("Sting", true);
                            ChangeNav = 0;
                        }
                        if (Contact == true)
                        {
                            Wasp.SetBool("Sting", false);
                            NPC.AddForce(new Vector3(-Distance.x, 0.01f, -Distance.z).normalized * 0.1f);
                            if (Distance.sqrMagnitude > LookRotationEpsilon)
                                transform.rotation = (Quaternion.LookRotation(Distance));
                            ChargeClock -= Time.deltaTime;
                            if (ChargeClock <= 0)
                                Contact = false;
                        }
                    
                    }
                }

            }
            else
            {
                Stunned();
                Recovery -= Time.deltaTime;
                if (Recovery <= 0)
                {
                    Recovered();
                    combo = 0;
                }
            }

            if (!PlayerInputReader.IsPrimaryHeld() || PlayerDistance > 3)
            {
                HitEffect.SetActive(false);
                SlashEffect.SetActive(false);
            }
        }
        private void LateUpdate()
        { 
            if (!PlayerInputReader.IsPrimaryHeld() || Distance.magnitude > 6)
            {
                Wasp.SetBool("Beat", false);
            }
            if (CurrentHealth <= 0)
            {
                Death();
            }
        }

        private void RefreshAnotherWaspCollider()
        {
            if (AnotherWaspCollider != null && AnotherWasp != null && AnotherWasp != gameObject && AnotherWaspCollider.gameObject == AnotherWasp)
            {
                return;
            }

            if (AnotherWaspLookupTimer > 0)
            {
                return;
            }

            AnotherWaspLookupTimer = AnotherWaspLookupCooldown;
            AnotherWasp = null;
            AnotherWaspCollider = null;

            GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
            foreach (GameObject npc in npcs)
            {
                if (npc == null || npc == gameObject)
                {
                    continue;
                }

                Collider collider = npc.GetComponent<Collider>();
                if (collider == null)
                {
                    continue;
                }

                AnotherWasp = npc;
                AnotherWaspCollider = collider;
                return;
            }
        }

        private void Death()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            if (PlayerHealth != null && PlayerHealth.BoostCharge != null)
                PlayerHealth.BoostCharge.RegisterKill();
            PlayerHealth.Plattering = "Gotcha!";
            PlayerHealth.ChangeSpeech = 0.55f;
            PooledOneShotVfx.Spawn(Explosion, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Body, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Head, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Wing, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Wing, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
            PooledDeathDebris.Spawn(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
            CurrencySpin.ConfigureEnemyDrop(Instantiate(Reward, transform.position + new Vector3(0, 7, 0), transform.rotation));
            CurrencySpin.ConfigureEnemyDrop(Instantiate(Reward, transform.position + new Vector3(0, 7, 0), transform.rotation));
            GameObject.Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject == PlayerTarget)
            {
                if (combo < hit2stun)
                {
                    Sting();
                }
                Contact = true;
                if (PlayerHealth.isParried== true)
                {
                    TakeDamage(20);
                    combo += 10;
                }
            }
            if (OBJ.gameObject.CompareTag("Strike"))
            {
                Death();
            }
            if (OBJ.gameObject.CompareTag("Damage"))
            {
                Wasp.SetBool("Beat", true);
                TakeDamage(15);
                Sound.Beat();
                combo = hit2stun;
            }
            if (OBJ.gameObject.CompareTag("Isle"))
            {
                Contact = true;
                NPC.velocity += new Vector3(Random.Range(-1, 1), 1, Random.Range(-1,1));
            }
        }
        public void TurnBack()
        {
            BuzzSource.SetActive(true);
            Wasp.SetBool("Sting", false);
            if ((transform.position - SpawnPos).magnitude > 30)
            {
                NPC.velocity = (SpawnPos - transform.position).normalized * 10;
            }
        }


        void FloatOnAir(Vector3 initialPosition)
        {
            StartCoroutine(FloatObject(initialPosition));
        }
        private IEnumerator FloatObject(Vector3 initialPosition)
        {
            float direction = -1.0f;
            while (true)
            {
                float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatDistance+1;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                float tiltAngle = direction * maxTiltAngle * Mathf.Sin(Time.time * floatSpeed);
                transform.rotation = Quaternion.Euler(tiltAngle, transform.rotation.eulerAngles.y, 0);
                if (newY >= initialPosition.y + floatDistance || newY <= initialPosition.y - floatDistance)
                {
                    direction *= 1.0f;
                }

                yield return null;
            }
        }
       public void Stunned()
        {
            BuzzSource.SetActive(false);
            floating = false;
            NPC.constraints = RigidbodyConstraints.None;
            NPC.useGravity = true;
            Wasp.SetBool("Stunned", true);
            Wasp.SetBool("Sting", false);
        }

        void Recovered()
        {
            BuzzSource.SetActive(true);
            floating = false;
            Wasp.SetBool("Stunned", false);
            Recovery = 10f;
            NPC.constraints = RigidbodyConstraints.FreezeRotation;
            NPC.useGravity = false;
        }
        public void Sting()
        {
            floating = false;
            Recovered();
            if (PlayerHealth.isParried == false && PlayerHealth.Rolling == false)
            {
                Sound.Sting();
                PlayerHealth.TakeDamage(Damage2Player);
            }
            Wasp.SetBool("Sting", false);
            if (PlayerHealth.isParried == true)
            {
                PlayerHealth.TakeDamage(0.01f);
                TakeDamage(1);
                combo = 3;
            }

        }

        public void RandoMovement()
        {
            BuzzSource.SetActive(true);
            if ((SpawnPos - transform.position).magnitude < 20)
            {
                Recovered();
                a = Random.Range(-1, 1);
                b = Random.Range(-0.1f, 0.1f);
                c = Random.Range(-1, 1);
                NPC.velocity = new Vector3(a, b, c);
                ChangeNav = 5f;
            }
            else 
                NPC.velocity = (SpawnPos - transform.position).normalized * 5;
        }

        public void TakeDamage(int Damage)
        {
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
            var playerArsenal = PlayerHealth.Arsenal;
            var currentWeapon = PlayerHealth.arsenalBrowser;
            switch (playerArsenal[currentWeapon])
            {
                case "Bare Hands":
                    {
                        HitEffect.SetActive(true);
                        Sound.Beat();
                        break;
                    }
                case "Hammers":
                    {
                        HitEffect.SetActive(true);
                        Sound.Beat();
                        break;
                    }
                case "Bow":
                    {
                        HitEffect.SetActive(true);
                        Sound.Beat();
                        break;
                    }
                case "ArmorSet":
                    {
                        SlashEffect.SetActive(true);
                        Sound.LiteSwordDamage();
                        //var playerAnimator = PlayerTarget.GetComponent<BeaverPlayer>().Otter;
                        //var currentClip = playerAnimator.GetComponent<AnimationClip>().ToString();
                        //if (currentClip=="Sword1" || currentClip == "Sword2")
                        //{
                        //    HitEffect.SetActive(true);
                        //    Sound.LiteSwordDamage();
                        //}
                        //if(currentClip=="NewSwordJump")
                        //{
                        //    Sound.HeavySwordDamage();
                        //    //Sound.Beat();
                        //}
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

        void ShowHitEffectBriefly()
        {
            if (HitEffect == null)
                return;

            HitEffect.SetActive(true);
            if (hitEffectHideRoutine != null)
                StopCoroutine(hitEffectHideRoutine);
            hitEffectHideRoutine = StartCoroutine(HideHitEffectAfterDelay());
        }

        IEnumerator HideHitEffectAfterDelay()
        {
            yield return new WaitForSeconds(HitEffectVisibleDuration);
            if (HitEffect != null)
                HitEffect.SetActive(false);
            hitEffectHideRoutine = null;
        }
    }
}
