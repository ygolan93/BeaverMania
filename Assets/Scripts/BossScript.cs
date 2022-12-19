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
    public Behavior PlayerHealth;
    Vector3 Distance;
    public Quaternion rotGoal;
    public float StrideClock = 7f;
    public float BeatClock = 10f;
    float InitialBeat;
    public float a;
    public float b;
    bool Charge = false;
    private void Start()
    {
        Boss = GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth = Player.GetComponent<Behavior>();
        CurrentHealth = MaxHealth;
        InitialBeat = BeatClock;
    }
    private void FixedUpdate()
    {
        if (Charge == true)
        {
            Distance = Player.transform.position - Boss.transform.position;
            AttackPlayer();
        }

        if (CurrentHealth <= 0)
        {
            Death();
        }

        if (!Input.GetKey(KeyCode.Mouse0) || Charge == false)
        {
            HitEffect.SetActive(false);
            RandoMovement();
        }
    }

    private void AttackPlayer()
    {
        Boss.velocity = new Vector3(Distance.x, Boss.velocity.y, Distance.z);
        Scorpion.SetBool("Walk", true);
        rotGoal = Quaternion.LookRotation(Distance);
        Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
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
        Scorpion.SetBool("Walk", true);
        Boss.velocity = new Vector3(a, 0, b).normalized * 3 + new Vector3(a, Boss.velocity.y, b);
        rotGoal = Quaternion.LookRotation(Boss.velocity);
        Boss.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);

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

    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            Charge = true;
        }

    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            Charge = false;
        }

    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            TakeDamage(5);
        }
    }

}
