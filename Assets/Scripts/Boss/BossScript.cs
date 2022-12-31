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
        Boss = GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        CurrentHealth = MaxHealth;
        InitialBeat = BeatClock;
        combo = 0;
    }
    public void FixedUpdate()
    {
        Distance = Player.transform.position - Boss.position;
        var DistanceScalar = Mathf.Abs(Distance.magnitude);

        if (combo < comboLimit)
        {
            if (Charge == true)
            {
                ChargeTowardsPlayer();
            }
            else
            {
                if (/*DistanceScalar < 120 &&*/ DistanceScalar > 9)
                {
                    StrideClock -= Time.deltaTime;
                    if (StrideClock > 5)
                    {
                        ChargeTowardsPlayer();
                    }
                    if (StrideClock <= 5)
                    {
                        IdleStop();
                        Charge = false;
                    }
                    if (StrideClock <= 0)
                        StrideClock = 10;

                }
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
            Death();
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
        Instantiate(Explosion, transform.position + new Vector3(0, 1, 0), transform.rotation);
        GameObject.Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            TakeDamage(5);
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
        Jaw1A.enabled = false;
        Jaw2A.enabled = false;
        Jaw1B.enabled = false;
        Jaw2B.enabled = false;
        Sting.enabled = false;

    }

    private void BossRecovered()
    {
        Scorpion.SetBool("Stunned", false);
        combo = 0;
        StunnedClock = 10;
        StunEffect.SetActive(false);
        Charge = true;
        Jaw1A.enabled = true;
        Jaw2A.enabled = true;
        Jaw1B.enabled = true;
        Jaw2B.enabled = true;
        Sting.enabled = true;
    }

}

