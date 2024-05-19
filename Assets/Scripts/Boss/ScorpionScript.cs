using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorpionScript : MonoBehaviour
{
    [Header("Boss Stats")]
    Rigidbody RBScorpion;
    [SerializeField] Animator Scorpion;
    public NPC_Health BossHealth;
    public int CurrentHealth;
    public int MaxHealth = 2000;
    public Behaviour Player;
    public bool isAttacking;
    GameObject AnotherScorpion;
    Vector3 Distance;
    [SerializeField] float maxDistanceScalar;
    [SerializeField] float minDistanceScalar;
    public Quaternion rotGoal;
    public float chargeSpeed;
    public float StrideClock = 20f;
    public float BeatClock = 10f;
    public float StunnedClock = 10f;
    float InitialBeat;
    public bool Charge = false;
    public int combo = 0;
    public int comboLimit;
    public Collider Jaw1A;
    public Collider Jaw1B;
    public Collider Jaw2A;
    public Collider Jaw2B;
    public Collider Sting;

    [Header("Boss UI")]
    public GameObject BossHPBar;
    public GameObject BossPanel;

    [Header("Effects & Sound")]
    public GameObject HitEffect;
    public GameObject Explosion;
    public GameObject StunEffect;
    public NPC_Audio Sound;



    private void Start()
    {
        RBScorpion = gameObject.GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        CurrentHealth = MaxHealth;
        InitialBeat = BeatClock;
        combo = 0;
        Explosion.SetActive(false);
    }
    public void FixedUpdate()
    {
        if (Charge==false)
        {
            isAttacking = false;
        }
        if (Charge==true)
        {
            AnotherScorpion = GameObject.FindGameObjectWithTag("Scorpion");
            Distance = Player.transform.position - RBScorpion.position;
            var DistanceScalar = Mathf.Abs(Distance.magnitude);
            isAttacking = true;
            rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
            RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
            if (combo < comboLimit)
            {
                ChargeTowardsPlayer();
                if (DistanceScalar < maxDistanceScalar && DistanceScalar > minDistanceScalar)
                {
                    if (Input.GetKey(KeyCode.Mouse1)|| Input.GetKey(KeyCode.Mouse0))
                    {
                        Reverse();
                    }
                    else
                    {
                        StrideClock -= Time.deltaTime;
                        if (StrideClock > 5)
                        {
                            Charge = true;
                        }
                        if (StrideClock <= 5)
                        {
                            IdleStop();
                        }
                        if (StrideClock <= 0)
                            StrideClock = 10;
                    }


                }

                if (DistanceScalar < minDistanceScalar)
                {
                    StopAndAttack();
                }
                if (DistanceScalar < minDistanceScalar-1)
                {
                    Reverse();
                }
            }
        }
        if (Charge==false)
        {
            IdleStop();
        }
    
        if (!Input.GetKey(KeyCode.Mouse0) || !Input.GetKey(KeyCode.Mouse1))
        {
            HitEffect.SetActive(false);
        }

        if (combo>= comboLimit)
        {
            ScorpionStunned();
            StunnedClock -= Time.deltaTime;
            if (StunnedClock <= 0)
            {
                ScorpionRecovered();
            }
        }

        if (CurrentHealth <= 0)
        {
            Death();
        }
    }

    public void InitiateCharge()
    {
        Charge = true;
        BossHPBar.SetActive(true);
    }
    private void ChargeTowardsPlayer()
    {
        Physics.IgnoreCollision(AnotherScorpion.GetComponent<Collider>(), transform.GetComponent<Collider>());
        RBScorpion.velocity = new Vector3(Distance.x, 0, Distance.z).normalized*chargeSpeed+new Vector3(0, RBScorpion.velocity.y,0);
        Scorpion.SetBool("Walk", true);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
    }

    private void IdleStop()
    {
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);

    }
    private void StopAndAttack()
    {
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", true);
        isAttacking = true;
        //Sound.Sting();
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);


    }

    private void Reverse()
    {
        RBScorpion.velocity = new Vector3(-Distance.x, 0, -Distance.z).normalized * 5 + new Vector3(0, RBScorpion.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", true);
        Scorpion.SetBool("Attack", false);
        //Sound.Sting();
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }

    public void TakeDamage(int Damage)
    {
        Charge = true;
        //transform.rotation = rotGoal;
        HitEffect.SetActive(true);
        CurrentHealth -= Damage;
        Sound.Beat();

        BossHealth.SetNPCHealth(CurrentHealth);
    }
    private void Death()
    {
        BossHPBar.SetActive(false);
        Explosion.SetActive(true);
        Explosion.transform.parent = null;
        //Destroy(gameObject);
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            TakeDamage(5);
        }
        if (OBJ.gameObject.CompareTag("Bridge"))
        {
            var Tree = OBJ.gameObject.GetComponent<LogSpawner>();
            Tree.DestroyTree();
            TakeDamage(10);
            combo = 10;
        }
    }

    private void ScorpionStunned()
    {
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Attack", false);
        Scorpion.SetBool("Stunned",true);
        StunEffect.SetActive(true);
        Charge = false;
    }

    private void ScorpionRecovered()
    {
        Scorpion.SetBool("Stunned", false);
        combo = 0;
        StunnedClock = 10;
        StunEffect.SetActive(false);
        Charge = true;
    }
}

