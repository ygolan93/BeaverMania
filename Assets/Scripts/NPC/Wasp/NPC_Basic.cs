using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NPC_Basic : MonoBehaviour
{
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
    [SerializeField] GameObject DamageEffect;

    [Header("Sound")]
    public NPC_Audio Sound;

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
        CurrentHealth = MaxHealth;
        PlayerTarget = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth = PlayerTarget.GetComponent<Behaviour>();
        HitEffect.SetActive(false);
        RandoMovement();
        NPC.velocity = Vector3.forward;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        //Sound.Buzz();
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
                if (Distance.magnitude >= 50)
                {
                    if (ChangeNav <= 0)
                    {
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
                        ChargeClock = 0.7f;
                        Physics.IgnoreCollision(AnotherWasp.GetComponent<Collider>(), transform.GetComponent<Collider>());
                        NPC.velocity = (Distance.normalized * 50f);
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
            DamageEffect.SetActive(false);
        }
    }
    private void LateUpdate()
    {
        Wasp.SetBool("Beat", false);
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
        //Sound.StopBuzzing();
        floating = false;
        NPC.constraints = RigidbodyConstraints.None;
        NPC.useGravity = true;
        Wasp.SetBool("Stunned", true);
        Wasp.SetBool("Sting", false);
    }

    void Recovered()
    {
        //Sound.Buzz();
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
        //floating = true;
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
        //Sound.StopBuzzing();
        rotGoal = Quaternion.LookRotation(Distance);
        transform.rotation = rotGoal;
        NPC.useGravity = true;
        Wasp.SetBool("Beat", true);
        DamageEffect.SetActive(true);
        Wasp.SetBool("Sting", false);
        CurrentHealth -= Damage;
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
                    SlashEffect.SetActive(true);
                    Sound.HeavySwordDamage();
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
        NPCHealthBar.SetNPCHealth(CurrentHealth);


    }
}
