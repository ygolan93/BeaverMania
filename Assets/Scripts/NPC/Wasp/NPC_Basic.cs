using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NPC_Basic : MonoBehaviour
{
    [Header("Body and animation")]
    public Rigidbody NPC;
    [SerializeField] Animator Wasp;

    [Header("Movement")]
    [SerializeField] bool Move = true;
    GameObject PlayerTarget;
    Behavior PlayerHealth;
    GameObject AnotherWasp;
    public Vector3 Distance;
    public Quaternion rotGoal;
    float steer = 0.5f;
    public float AttackSpeed = 7f;
    public float PlayerDistance;
    float ChangeNav = 5f;
    public float Recovery = 10f;
    float ChargeClock = 0.7f;
    bool Contact = false;
    float a;
    float b;
    float c;
    Vector3 SpawnPos;

    [Header("Health and damage")]
    public int combo = 0;
    public int Damage2Player = 1;
    public int MaxHealth = 2000;
    public int CurrentHealth;
    public NPC_Health NPCHealthBar;
    public GameObject HitEffect;
    public GameObject Explosion;


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
        PlayerHealth = PlayerTarget.GetComponent<Behavior>();
        HitEffect.SetActive(false);
        RandoMovement();
        if (Move==true)
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
        if (combo < 3)
        {
            if (Move == true)
            {
                rotGoal = Quaternion.LookRotation(NPC.velocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            }
            if (Distance.magnitude >= 50)
            {
                if (ChangeNav <= 0)
                {
                    Wasp.SetBool("Sting", false);
                    if (Move == true)
                    {
                        RandoMovement();
                    }
                }
                if (Move == true)
                {
                    TurnBack();
                }
            }

            if (PlayerDistance < 50)
            {
                if (Contact == false)
                {
                    ChargeClock = 0.7f;
                    Physics.IgnoreCollision(AnotherWasp.GetComponent<Collider>(), GetComponent<Collider>());
                    if (Move==true)
                    NPC.velocity = (Distance.normalized * 50f);

                    Wasp.SetBool("Sting", true);
                    ChangeNav = 0;

                }
                if (Contact == true)
                {
                    if (Move == true)
                    {
                        NPC.AddForce(new Vector3(-Distance.x, 0.01f, -Distance.z).normalized * 0.1f);
                        transform.rotation = (Quaternion.LookRotation(Distance));
                    }
                    ChargeClock -= Time.deltaTime;
                    if (ChargeClock <= 0)
                        Contact = false;
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
        }

        if (Move==false)
        {
            transform.position = SpawnPos;
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
        Sound.Ahh();
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
            if (combo < 3)
            {
                Sting();
            }
            Contact = true;
        }
        if (OBJ.gameObject.CompareTag("Strike"))
        {
            Death();
        }
        if (OBJ.gameObject.CompareTag("Danagge"))
        {
            Wasp.SetBool("Beat", true);
            TakeDamage(1);
        }
    }


    void TurnBack()
    {
        if ((transform.position - SpawnPos).magnitude > 30)
        {
            NPC.velocity = (SpawnPos - transform.position).normalized * 10;
        }
    }

    void Stunned()
    {
        //Sound.StopBuzzing();
        NPC.constraints = RigidbodyConstraints.None;
        NPC.useGravity = true;
        Wasp.SetBool("Stunned", true);
        Wasp.SetBool("Sting", false);
    }

    void Recovered()
    {
        //Sound.Buzz();
        Wasp.SetBool("Stunned", false);
        Recovery = 10f;
        NPC.constraints = RigidbodyConstraints.FreezeRotation;
        NPC.useGravity = false;
    }
    public void Sting()
    {
        Recovered();
        if (PlayerHealth.isParried == false)
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

    private void RandoMovement()
    {
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
        HitEffect.SetActive(true);
        Wasp.SetBool("Sting", false);
        CurrentHealth -= Damage;
        Sound.Beat();
        combo++;
        NPCHealthBar.SetNPCHealth(CurrentHealth);


    }
}
