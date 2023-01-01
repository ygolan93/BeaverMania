using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossScript : MonoBehaviour
{
    Rigidbody Boss;
    [SerializeField] Animator Scorpion;
    public NPC_Health BossHealth;
    public int CurrentHealth;
    public int MaxHealth = 2000;
    public GameObject BossHPBar;
    public GameObject HitEffect;
    public GameObject Explosion;
    public GameObject StunEffect;
    public NPC_Audio Sound;
    public Behavior Player;
    Vector3 Distance;
    public Quaternion rotGoal;
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

    private void Start()
    {
        Boss = gameObject.GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        CurrentHealth = MaxHealth;
        InitialBeat = BeatClock;
        combo = 0;
        Explosion.SetActive(false);
    }
    public void FixedUpdate()
    {
        Distance = Player.transform.position - Boss.position;
        var DistanceScalar = Mathf.Abs(Distance.magnitude);
        if (Charge==true)
        {
            if (combo < comboLimit)
            {
                ChargeTowardsPlayer();

                if (DistanceScalar < 100 && DistanceScalar > 9)
                {
                    StrideClock -= Time.deltaTime;
                    if (StrideClock > 5)
                    {
                        ChargeTowardsPlayer();
                    }
                    if (StrideClock <= 5)
                    {
                        IdleStop();
                        //Charge = false;
                    }
                    if (StrideClock <= 0)
                        StrideClock = 10;

                }

                if (DistanceScalar < 9 && DistanceScalar > 8)
                {
                    StopAndAttack();
                }
                if (DistanceScalar < 8)
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
            BossStunned();
            StunnedClock -= Time.deltaTime;
            if (StunnedClock <= 0)
            {
                BossRecovered();
            }
        }

        if (CurrentHealth <= 0)
        {
            Death();
        }
    }

    private void ChargeTowardsPlayer()
    {
        Boss.velocity = new Vector3(Distance.x, 0, Distance.z).normalized*10+new Vector3(0,Boss.velocity.y,0);
        Scorpion.SetBool("Walk", true);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }

    private void IdleStop()
    {
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
        Boss.velocity = new Vector3(0, Boss.velocity.y, 0);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);

    }
    private void StopAndAttack()
    {
        Boss.velocity = new Vector3(0, Boss.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", true);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);


    }

    private void Reverse()
    {
        Boss.velocity = new Vector3(-Distance.x, 0, -Distance.z).normalized * 5 + new Vector3(0, Boss.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", true);
        Scorpion.SetBool("Attack", false);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }

    public void TakeDamage(int Damage)
    {
        //Charge = true;
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
        GameObject.Destroy(gameObject);
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

    private void BossStunned()
    {
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Attack", false);
        Scorpion.SetBool("Stunned",true);
        StunEffect.SetActive(true);
        Charge = false;
    }

    private void BossRecovered()
    {
        Scorpion.SetBool("Stunned", false);
        combo = 0;
        StunnedClock = 10;
        StunEffect.SetActive(false);
        Charge = true;
    }

}

