using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BearNPC : MonoBehaviour
{
    Rigidbody Bear;
    public NPC_Health BearHealth;
    public int CurrentHealth;
    public int MaxHealth = 2000;
    public int combo = 0;
    public GameObject HitEffect;
    public GameObject Explosion;
    public NPC_Audio Sound;
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
        Bear = GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth = Player.GetComponent<Behavior>();
        SpawnPos = Bear.position;
        CurrentHealth = MaxHealth;
        InitialBeat = BeatClock;
    }
    private void FixedUpdate()
    {
        Bear.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.2f);
        Distance = Player.transform.position - Bear.position;
        if (CurrentHealth > MaxHealth * 0.75f)
        {
            AttackPlayer(8, 0, 20);
        }
        if (CurrentHealth <= MaxHealth * 0.75f)
        {
            AttackPlayer(15, 9, 20);
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
        if (Distance.magnitude <= AttackDistance || Hit == true)
        {
            rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
            BeatClock -= Time.deltaTime;
            if (BeatClock <= AttackTime)
            {
                Bear.velocity = new Vector3(Distance.x * AttackSpeed, Bear.velocity.y, Distance.z * AttackSpeed);
            }
            else
            {
                Bear.velocity = new Vector3(0, Bear.velocity.y, 0);
            }
        }
        else
        {
            rotGoal = Quaternion.LookRotation(new Vector3(Bear.velocity.x, 0, Bear.velocity.z));
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
        Bear.velocity = new Vector3(a, 0, b).normalized * 3 + new Vector3(a, Bear.velocity.y, b);
    }
    public void TakeDamage(int Damage)
    {
        rotGoal = Quaternion.LookRotation(Distance);
        transform.rotation = rotGoal;
        Bear.useGravity = true;
        //Wasp.SetBool("Beat", true);
        HitEffect.SetActive(true);
        //Wasp.SetBool("Sting", false);
        CurrentHealth -= Damage;
        Sound.Beat();
        combo++;
        BearHealth.SetNPCHealth(CurrentHealth);
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
            Hit = false;
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
