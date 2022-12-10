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
    public AudioSource Sound;
    GameObject Player;
    Behavior PlayerHealth;
    Vector3 SpawnPos;
    Vector3 Distance;
    public Quaternion rotGoal;
    public float StrideClock = 7f;
    public float BeatClock = 10f;
    float InitialBeat;
    public float a;
    public float b;
    bool Hit=false;
    float speed;
    private void Start()
    {
        Boss = GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth = Player.GetComponent<Behavior>();
        SpawnPos = Boss.position;
        CurrentHealth = MaxHealth;
        InitialBeat = BeatClock;
    }
    private void FixedUpdate()
    {
        if (Hit==true)
        {
            if (CurrentHealth > MaxHealth * 0.75f)
            {
                Scorpion.speed = 0.8f;
                AttackPlayer(1.5f, 0, 20);
            }
            if (CurrentHealth <= MaxHealth * 0.75f)
            {
                Scorpion.speed = 2;
                AttackPlayer(3f, 9, 20);
            }
            StrideClock -= Time.deltaTime;
            if (StrideClock<=0)
            {
                Hit = false;
            }
        }
        if (Hit == false)
        {
            RandoMovement();
            Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
            Distance = Player.transform.position - Boss.position;
        }
        


        if (CurrentHealth <= 0)
        {
            Death();
        }

        if (!Input.GetKey(KeyCode.Mouse0) || Distance.magnitude > 3)
        {
            HitEffect.SetActive(false);
        }
    }

    private void AttackPlayer(float AttackSpeed, float AttackTime, float AttackDistance)
    {
        Scorpion.SetBool("Walk", true);
        if (Distance.magnitude <= AttackDistance || Hit == true)
        {
            rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
            Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.5f);
            BeatClock -= Time.deltaTime;
            if (BeatClock <= AttackTime)
            {
                Boss.velocity = new Vector3(Distance.x * AttackSpeed, Boss.velocity.y, Distance.z * AttackSpeed);
            }
            else
            {
                Scorpion.SetBool("Walk", false);
                Boss.velocity = new Vector3(0, Boss.velocity.y, 0);
            }
        }
        else
        {
            rotGoal = Quaternion.LookRotation(new Vector3(Boss.velocity.x, 0, Boss.velocity.z));
            RandoMovement();
        }
    }

    private void RandoMovement()
    {
        StrideClock -= Time.deltaTime;

        if (StrideClock <= 0)
        {
            a = Random.Range(-1, 2) * Random.value;
            b = Random.Range(-1, 2) * Random.value;
            StrideClock = 7f;
        }
        Scorpion.speed = 0.4f;
        Scorpion.SetBool("Walk", true);
        Boss.velocity = new Vector3(a, 0, b).normalized * 3 + new Vector3(a, Boss.velocity.y, b);
    }
    public void TakeDamage(int Damage)
    {
        transform.rotation = rotGoal;
        HitEffect.SetActive(true);
        CurrentHealth -= Damage;
        Sound.Play();
        combo++;
        BossHealth.SetNPCHealth(CurrentHealth);
    }
    private void Death()
    {
        Instantiate(Explosion, transform.position + new Vector3(0, 1, 0), transform.rotation);
        GameObject.Destroy(gameObject);
    }
    private void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject==Player)
        {
            PlayerHealth.TakeDamage(50);
            //Hit = false;
            BeatClock = InitialBeat;
        }
    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            Hit = true;
        }
    }

}
