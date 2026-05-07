using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NPC_Basic : MonoBehaviour, IDamageable
{
    [Header("Config")]
    [SerializeField] EnemyAIConfig aiConfig;

    [Header("Body and animation")]
    public Rigidbody NPC;
    [SerializeField] Animator Wasp;
    [Header("Movement")]
    GameObject PlayerTarget;
    Behaviour PlayerHealth;
    GameObject AnotherWasp;
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
    public Vector3 currentPos;
    public bool floating;
    public float floatSpeed = 1.0f;
    public float floatDistance = 1.0f;
    public float maxTiltAngle = 10.0f;
    [Header("Health and damage")]
    public int hit2stun;
    public int combo = 0;
    public int Damage2Player = 1;
    public int MaxHealth = 2000;
    public int CurrentHealth;
    public NPC_Health NPCHealthBar;
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
        ApplyConfig();
        SpawnPos = transform.position;
        Wasp = GetComponent<Animator>();
        CurrentHealth = MaxHealth;
        PlayerTarget = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth = PlayerTarget != null ? PlayerTarget.GetComponent<Behaviour>() : null;

        if (!ValidateReferences())
        {
            return;
        }

        HitEffect.SetActive(false);
        RandoMovement();
        NPC.velocity = Vector3.forward;
        BuzzSource.SetActive(true);
    }

    bool ValidateReferences()
    {
        bool valid = RuntimeReferenceValidator.Require(NPC, this, nameof(NPC)) &
            RuntimeReferenceValidator.Require(Wasp, this, nameof(Wasp)) &
            RuntimeReferenceValidator.Require(PlayerTarget, this, "Player tag") &
            RuntimeReferenceValidator.Require(PlayerHealth, this, nameof(PlayerHealth)) &
            RuntimeReferenceValidator.Require(NPCHealthBar, this, nameof(NPCHealthBar)) &
            RuntimeReferenceValidator.Require(HitEffect, this, nameof(HitEffect)) &
            RuntimeReferenceValidator.Require(SlashEffect, this, nameof(SlashEffect)) &
            RuntimeReferenceValidator.Require(Explosion, this, nameof(Explosion)) &
            RuntimeReferenceValidator.Require(Sound, this, nameof(Sound)) &
            RuntimeReferenceValidator.Require(BuzzSource, this, nameof(BuzzSource)) &
            RuntimeReferenceValidator.Require(Body, this, nameof(Body)) &
            RuntimeReferenceValidator.Require(Head, this, nameof(Head)) &
            RuntimeReferenceValidator.Require(Wing, this, nameof(Wing)) &
            RuntimeReferenceValidator.Require(Leg, this, nameof(Leg)) &
            RuntimeReferenceValidator.Require(Reward, this, nameof(Reward));

        return valid;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        AnotherWasp = GameObject.FindGameObjectWithTag("NPC");
        Vector3 Distance = PlayerTarget.transform.position - transform.position;
        PlayerDistance = Distance.magnitude;
        ChangeNav -= Time.deltaTime;
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
                rotGoal = Quaternion.LookRotation(NPC.velocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
                if (Distance.magnitude >= AggroDistance)
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

                if (PlayerDistance < AggroDistance)
                {
                    if (Contact == false)
                    {
                        BuzzSource.SetActive(true);
                        ChargeClock = 0.7f;
                        Physics.IgnoreCollision(AnotherWasp.GetComponent<Collider>(), transform.GetComponent<Collider>());
                        NPC.velocity = (Distance.normalized * AttackSpeed);
                        Wasp.SetBool("Sting", true);
                        ChangeNav = 0;
                    }
                    if (Contact == true)
                    {
                        Wasp.SetBool("Sting", false);
                        NPC.AddForce(new Vector3(-Distance.x, 0.01f, -Distance.z).normalized * 0.1f);
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

        if (!Input.GetKey(KeyCode.Mouse0) || PlayerDistance > 3)
        {
            HitEffect.SetActive(false);
            SlashEffect.SetActive(false);
        }
    }
    private void LateUpdate()
    { 
        if (!Input.GetKey(KeyCode.Mouse0)||Distance.magnitude>6)
        {
            Wasp.SetBool("Beat", false);
        }
        if (CurrentHealth <= 0)
        {
            Death();
        }
    }

    private void Death()
    {
        PlayerHealth.Plattering = ("HA! gotcha");
        PlayerHealth.ChangeSpeech = 1;
        Instantiate(Explosion, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Body, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Head, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Wing, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Wing, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation);
        Instantiate(Reward, transform.position + new Vector3(0, 7, 0), transform.rotation);
        Instantiate(Reward, transform.position + new Vector3(0, 7, 0), transform.rotation);
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
            if (OBJ.gameObject.GetComponent<Behaviour>().isParried== true)
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
        if ((transform.position - SpawnPos).magnitude > LeashDistance)
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
        Recovery = RecoverySeconds;
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
            if (CombatDebugGate.AllowsDamage(PlayerHealth.gameObject))
            {
                PlayerHealth.TakeDamage(Damage2Player);
            }
        }
        Wasp.SetBool("Sting", false);
        if (PlayerHealth.isParried == true)
        {
            PlayerHealth.TakeDamage(0.01f);
            TakeDamage(1);
            combo = 3;
        }

    }

    float AggroDistance => aiConfig != null ? aiConfig.aggroDistance : 50f;
    float LeashDistance => aiConfig != null ? aiConfig.leashDistance : 30f;
    float RecoverySeconds => aiConfig != null ? aiConfig.recoverySeconds : 10f;

    void ApplyConfig()
    {
        if (aiConfig == null)
        {
            BuildSafeLogger.WarnOnce(nameof(NPC_Basic) + ".MissingConfig", "Missing enemy AI config; using prefab values.", this, nameof(aiConfig));
            return;
        }

        MaxHealth = aiConfig.maxHealth;
        hit2stun = aiConfig.hitToStun;
        Damage2Player = aiConfig.damageToPlayer;
        AttackSpeed = aiConfig.attackSpeed;
        Recovery = aiConfig.recoverySeconds;
    }

    public void RandoMovement()
    {
        BuzzSource.SetActive(true);
        if ((SpawnPos - transform.position).magnitude < LeashDistance)
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

    public void TakeDamage(int Damage) => TakeDamage(new DamageEvent { Amount = Damage, Source = null, Point = transform.position, Type = DamageType.Generic, CanStun = false });

    public void TakeDamage(DamageEvent damageEvent)
    {
        BuzzSource.SetActive(false);
        rotGoal = Quaternion.LookRotation(Distance);
        transform.rotation = rotGoal;
        NPC.useGravity = true;
        Wasp.SetBool("Beat", true);
        Wasp.SetBool("Sting", false);
        CurrentHealth -= Mathf.RoundToInt(damageEvent.Amount);
        var playerArsenal = PlayerTarget.GetComponent<Behaviour>().Arsenal;
        var currentWeapon = PlayerTarget.GetComponent<Behaviour>().arsenalBrowser;
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
                    //var playerAnimator = PlayerTarget.GetComponent<Behaviour>().Otter;
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
        if (damageEvent.CanStun && (damageEvent.Type == DamageType.Projectile || damageEvent.Type == DamageType.Fire))
        {
            combo += hit2stun;
        }
        NPCHealthBar.SetNPCHealth(CurrentHealth);
    }
}
