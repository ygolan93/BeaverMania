using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorpionScript : MonoBehaviour
{
    [Header("General Stats")]
    Rigidbody RBScorpion;
    [SerializeField] Animator Scorpion;
    //GameObject AnotherScorpion;
    public NPC_Health BossHealth;
    public int CurrentHealth;
    public int MaxHealth = 2000;
    public Behaviour Player;
    public GameObject[] drops;

    [Header("Combat Factors")]
    public int combo = 0;
    public int comboLimit;
    public float StunnedClock = 10f;
    float initialStun;
    public bool isAttacking = false;
    public float chargeSpeed;
    float paceUp;
    [SerializeField] float chargeClock;
    float resetCharge;
    public Collider Jaw1A;
    public Collider Jaw1B;
    public Collider Jaw2A;
    public Collider Jaw2B;
    public Collider Sting;
    public string state = "Idle";

    [Header("Mobility")]
    public float lookDistance;
    public float chargeDistance;
    public float attackDistance;
    public float currentDistance;
    Vector3 Distance;
    public Quaternion rotGoal;

    [Header("Effects & Sound")]
    public GameObject HitEffect;
    public GameObject Explosion;
    public GameObject StunEffect;
    public NPC_Audio Sound;



    private void Start()
    {
        RBScorpion = gameObject.GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        //AnotherScorpion = GameObject.FindGameObjectWithTag("Scorpion");
        Physics.IgnoreLayerCollision(10, 10);

        CurrentHealth = MaxHealth;
        Explosion.SetActive(false);
        HitEffect.SetActive(false);
        paceUp = chargeSpeed + 2;
        resetCharge = chargeClock;
        initialStun = StunnedClock;
        combo = 0;
    }

    public void FixedUpdate()
    {
        Distance = Player.transform.position - RBScorpion.position;
        currentDistance = Mathf.Abs(Distance.magnitude);
        switch (state)
        {
            case "Idle":
                {
                    Idle();
                    break;
                }
            case "Look":
                {
                    LookAtPlayer();
                    break;
                }
            case "Charge":
                {
                    var speed = chargeSpeed;
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        speed = paceUp;
                        Scorpion.speed = 1.05f;
                    }
                    else
                    {
                        speed = chargeSpeed;
                        Scorpion.speed = 1;
                    }

                    Charge(speed);
                    break;
                }
            case "Stop":
                {
                    StopAndAttack();
                    break;
                }
            case "Reverse":
                {
                    Reverse();
                    break;
                }
            case "Stunned":
                {
                    Stunned();
                    break;
                }
            case "Recovered":
                {
                    Recovered();
                    break;
                }
        }


    }

    private void Update()
    {
        HitEffect.SetActive(false);
        if (combo >= comboLimit)
        {
            Stunned();
            StunnedClock -= Time.deltaTime;
            if (StunnedClock <= 0)
            {
                Recovered();
            }
        }
        if (CurrentHealth <= 0)
        {
            Death();
        }
        if (combo<comboLimit)
        {
            if (combo==1)
            {
                state = "Look";
            }
            if (combo==3)
            {
                state = "Charge";
            }
            if (currentDistance > lookDistance)
            {
                state = "Idle";
            }
            if(currentDistance <= lookDistance)
            {
                if (combo<comboLimit-5)
                {
                    if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
                    {
                        if (currentDistance<chargeDistance-10)
                        {
                            state = "Reverse";
                        }
                        else
                        {
                            state = "Look";
                        }
                        
                    }
                    else
                    {
                        if (currentDistance > chargeDistance)
                        {
                            state = "Look";
                        }
                        if (currentDistance <= chargeDistance)
                        {

                            if (chargeClock > 0)
                            {
                                state = "Charge";
                                chargeClock -= Time.deltaTime;
                            }
                            if (chargeClock <= 0)
                            {
                                if (currentDistance > attackDistance)
                                {
                                    state = "Charge";
                                }
                                else
                                {
                                    state = "Stop";

                                    if (Input.GetKeyUp(KeyCode.Mouse0) || Input.GetKeyUp(KeyCode.Mouse1))
                                    {
                                        chargeClock = resetCharge;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    state = "Charge";
                }
      
            }
            

        }
        if (combo >= comboLimit)
        {
            state = "Stunned";
            StunnedClock -= Time.deltaTime;
            if (StunnedClock <= 0)
            {
                state = "Recovered";
            }
        }


    }

    private void Idle()
    {
        isAttacking = false;
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        rotGoal = transform.rotation;
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }
    private void LookAtPlayer()
    {
        isAttacking = false;
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        if (transform.rotation != rotGoal)
        {
            Scorpion.SetBool("Walk", true);
            Scorpion.SetBool("Backwards", false);
            Scorpion.SetBool("Attack", false);
        }
        else
        {
            Scorpion.SetBool("Walk", false);
            Scorpion.SetBool("Backwards", false);
            Scorpion.SetBool("Attack", false);
        }
    }
    public void Charge(float speed)
    {
        isAttacking = true;
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        RBScorpion.velocity = new Vector3(Distance.x, 0, Distance.z).normalized * speed + new Vector3(0, RBScorpion.velocity.y, 0);

        HitEffect.SetActive(false);
        Scorpion.SetBool("Walk", true);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
    }
    private void StopAndAttack()
    {
        isAttacking = true;
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", true);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }
    private void Reverse()
    {
        isAttacking = true;
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        RBScorpion.velocity = new Vector3(-Distance.x, 0, -Distance.z).normalized * 5 + new Vector3(0, RBScorpion.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", true);
        Scorpion.SetBool("Attack", false);
    }

    public void TakeDamage(int Damage)
    {
        transform.rotation = rotGoal;
        HitEffect.SetActive(true);
        CurrentHealth -= Damage;
        combo++;
        Sound.Beat();
        BossHealth.SetNPCHealth(CurrentHealth);
    }
    private void Stunned()
    {
        isAttacking = false;
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Attack", false);
        Scorpion.SetBool("Stunned", true);
        StunEffect.SetActive(true);
    }
    private void Recovered()
    {
        isAttacking = false;
        Scorpion.SetBool("Stunned", false);
        combo = 0;
        StunnedClock = 10;
        StunEffect.SetActive(false);
    }
    private void Death()
    {
        isAttacking = false;
        Explosion.SetActive(true);
        Explosion.transform.parent = null;
        foreach (var item in drops)
        {
            Instantiate(item, gameObject.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
        }
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Strike"))
        {
            Death();
        }
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            TakeDamage(5);
        }
        if (OBJ.gameObject.CompareTag("Bridge"))
        {
            var Tree = OBJ.gameObject.GetComponent<LogSpawner>();
            Tree.DestroyTree(OBJ.transform);
            TakeDamage(10);
            combo = 10;
        }
    }



}

