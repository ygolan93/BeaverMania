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
    public int combo = 0;
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
    public float a;
    public float b;
    public bool Charge = false;
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
        Distance = Player.transform.position- Boss.position;
        var DistanceScalar = Mathf.Abs(Distance.magnitude);

        if (combo < 5)
        {
            if (DistanceScalar < 70 && DistanceScalar > 9)
            {
                StrideClock -= Time.deltaTime;
                if (StrideClock > 5)
                {
                    ChargeTowardsPlayer();
                }
                if (StrideClock<=5)
                {
                    IdleStop();
                }
                if (StrideClock <= 0)
                    StrideClock = 20;
                
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
        //else
        //RandoMovement();

        if (!Input.GetKey(KeyCode.Mouse0) || !Input.GetKey(KeyCode.Mouse1))
        {
            HitEffect.SetActive(false);
        }

        if (combo>=5)
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

    //private void RandoMovement()
    //{
    //    StrideClock -= Time.deltaTime;

    //    if (StrideClock <= 0)
    //    {
    //        a = Random.Range(-1, 2) * Random.value;
    //        b = Random.Range(-1, 2) * Random.value;
    //        StrideClock = 7f;
    //    }
    //    Vector3 Wander = new Vector3(a, 0, b);
    //    if (Wander.magnitude > 0)
    //        Scorpion.SetBool("Walk", true);
    //    else
    //        Scorpion.SetBool("Walk", false);
    //    Boss.velocity = Wander.normalized * 10 + new Vector3(0, Boss.velocity.y, 0);
    //    rotGoal = Quaternion.LookRotation(Wander);
    //    Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.1f);

    //}
    public void TakeDamage(int Damage)
    {
        Charge = true;
        transform.rotation = rotGoal;
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

    //private void OnTriggerStay(Collider OBJ)
    //{
    //    if (OBJ.gameObject.CompareTag("Player"))
    //    {
    //        Charge = true;
    //    }

    //}
    //private void OnTriggerExit(Collider OBJ)
    //{
    //    if (OBJ.gameObject.CompareTag("Player"))
    //    {
    //        Charge = false;
    //    }

    //}
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
    }

    private void BossRecovered()
    {
        Scorpion.SetBool("Stunned", false);
        combo = 0;
        StunnedClock = 10;
        StunEffect.SetActive(false);
    }

}

