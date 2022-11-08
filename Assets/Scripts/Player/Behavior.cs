using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Behavior : MonoBehaviour
{
    //Movement and animation
    public Rigidbody Player;
    public Carry Load;
    public Quaternion rotGoal;
    public float speed = 5;
    public float steer = 0.9f;
    public float JumpForce = 3;
    public float Walk = 4;
    public float Run = 12;
    float levitation = 10f;
    float InitiateAir = 0.5f;

    //float AirClock;
    public bool neutralAndMoving;
    [SerializeField] int JumpLimit = 2;
    public Animator Otter;
    public bool grounded;

    //Health
    public float MaxHealth = 1000;
    public float CurrentHealth;
    public Health_Bar_Script HealthBar;
    public bool heal;
    public bool TouchShroom;
    public bool hurt;
    double HealthPercent;
    public int Lives;
    public GameObject ICON_1;
    public GameObject ICON_2;
    public GameObject ICON_3;

    //Combat
    public float Beat = 0;
    public Projectile Ball;
    public Transform AttackPoint;
    public Transform Sphere;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public float GroundBeat = 0.3f;
    float BeatGrounded = 0.4f;
    public float AirBeat = 0.2f;
    float BeatAir = 0.2f;
    float FallClock;
    float Throw = 0;
    public int GroundAttack = 30;
    [SerializeField] GameObject RightHandWeapon;
    [SerializeField] GameObject LeftHandWeapon;

    //Audio & effects
    public AudioScript Sound;
    float HealQue = 3;
    [SerializeField] ParticleSystem SlideEffect;
    [SerializeField] ParticleSystem HealEffect;
    public ParticleSystem.ShapeModule HealShape;

    [SerializeField] Light HealLight;
    [SerializeField] ParticleSystem HurtEffect;
    [SerializeField] Light HurtLight;

    //UI
    public string LogCount;
    public string DebugText;
    public string HealingText;
    public int Currency = 0;
    public string Wallet;
    public GameMaster GM;
    public void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.name == "Isle2")
        {
            DebugText = ("Congrats! You've finished the demo level");
        }
        if (OBJ.gameObject.CompareTag("Part"))
        {
            Otter.Play("Crouch");
        }

        if (OBJ.gameObject.CompareTag("Life"))
        {
            GM.lastCheckPointPos = transform.position;
            HealingText = "Checkpoint saved";
        }

        if (OBJ.gameObject.tag == "Isle" || OBJ.gameObject.CompareTag("Bridge"))
        {
            if (Player.velocity.magnitude > 8 && !Input.anyKey)
            {
                Otter.Play("Roll");
            }

        }
        if (OBJ.gameObject.CompareTag("Coin"))
        {
            Sound.Coin();
        }

    }
    public void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Isle" || OBJ.gameObject.CompareTag("Bridge"))
        {
            JumpLimit = 2;
            grounded = true;
        }

        if (OBJ.gameObject.CompareTag("Weapon"))
        {
            HealingText = "Pick  it up!";
            if (Input.GetKey(KeyCode.LeftControl))
            {
                RightHandWeapon.SetActive(true);
                LeftHandWeapon.SetActive(true);
                GroundAttack = 250;
                Destroy(OBJ.gameObject);
            }
        }

        if (OBJ.gameObject.tag == "Life")
        {
            if (CurrentHealth < MaxHealth)
            {
                TakeDamage(-10);
                TouchShroom = true;
            }
            if (CurrentHealth >= MaxHealth)
                TouchShroom = false;
        }
        if (OBJ.gameObject.tag == "Strike")
        {
            DebugText = "You Can't Swim!";
            transform.position = GM.lastCheckPointPos;
            Lives--;
            if (Lives == 0)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        }
    }
    public void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Isle") || OBJ.gameObject.CompareTag("Bridge"))
        {
            grounded = false;
        }
        if (OBJ.gameObject.tag == "Life")
        {
            TouchShroom = false;

        }
    }
    public void Attack()
    {
        if (grounded == true)
        {
            Collider[] hitEnemies = Physics.OverlapSphere(AttackPoint.position, attackRange, enemyLayers);

            foreach (Collider enemy in hitEnemies)
            {
                if (enemy.CompareTag("NPC"))
                {
                    Debug.Log("Hit " + enemy.name);
                    enemy.GetComponent<NPC_Basic>().TakeDamage(GroundAttack);

                }
            }
            BeatGrounded = GroundBeat;
        }
        if (grounded == false)
        {
            Collider[] hitEnemies = Physics.OverlapSphere(Sphere.position, attackRange, enemyLayers);

            foreach (Collider enemy in hitEnemies)
            {
                if (enemy.CompareTag("NPC"))
                {
                    Debug.Log("Hit " + enemy.name);
                    enemy.GetComponent<NPC_Basic>().TakeDamage(40);
                }

            }
            BeatAir = AirBeat * Otter.speed;
        }
    }

    public void TakeDamage(float Damage)
    {
        CurrentHealth -= Damage;
        HealthBar.SetHealth(CurrentHealth);
        if (Damage > 0)
        {
            hurt = true;
            heal = false;
        }
        if (Damage < 0)
        {
            hurt = false;
            heal = true;
        }
    }
    public void PlayerMove(Vector3 Direction)
    {
        rotGoal = Quaternion.LookRotation(new Vector3(Player.velocity.x,0,Player.velocity.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
        Otter.SetBool("walk", true);
        if (grounded == true)
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.anyKey)
            {
                Otter.SetBool("run", true);
                speed = Run;
                steer = 0.15f;
            }
            else
            {
                Otter.SetBool("run", false);
                speed = Walk;
                steer = 0.1f;
            }
            Player.velocity = (Direction.normalized * speed) + new Vector3(0, Player.velocity.y, 0);
        }
        if (grounded == false)
            Player.AddForce(Direction.normalized*5);
    }

    public void Start()
    {
        //Player.velocity = new Vector3(0, 0, 20);
        Player = GetComponent<Rigidbody>();
        GM = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        CurrentHealth = MaxHealth;
        HealthBar.SetMaxHealth(CurrentHealth);
        HealShape = HealEffect.shape;
        RightHandWeapon.SetActive(false);
        LeftHandWeapon.SetActive(false);
        Lives = 3;
    }
    public void Update()
    {
        //Update UI text
        HealthPercent = System.Math.Round((CurrentHealth / MaxHealth) * 100f, 1);
        DebugText = HealthPercent + "%";
        Wallet = Currency + " Coins";
        if (Lives == 3)
        {
            ICON_1.SetActive(true);
            ICON_2.SetActive(true);
            ICON_3.SetActive(true);
        }
        if (Lives == 2)
        {
            ICON_1.SetActive(false);
            ICON_2.SetActive(true);
            ICON_3.SetActive(true);
        }
        if (Lives == 1)
        {
            ICON_1.SetActive(false);
            ICON_2.SetActive(false);
            ICON_3.SetActive(true);
        }



        //Lock and hide mouse icon
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //Jump action
        if (Input.GetKeyDown(KeyCode.Space) && JumpLimit > 0)
        {
            Otter.SetBool("midair", true);
            Player.velocity = new Vector3(Player.velocity.x, JumpForce, Player.velocity.z);
            Sound.Jump();
            JumpLimit--;
            levitation = 10;
            Otter.speed = 1;
        }

        //Reload scene on death
        if (CurrentHealth <= 0)
        {
            transform.position = GM.lastCheckPointPos;
            Lives--;
            if (Lives == 0)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

    }


    [System.Obsolete]
    public void FixedUpdate()
    {
        //Camera vectors setup
        Vector3 cameraRelativeForward = Camera.main.transform.TransformDirection(Vector3.forward);
        Vector3 cameraRelativeBack = Camera.main.transform.TransformDirection(Vector3.back);
        Vector3 cameraRelativeRight = Camera.main.transform.TransformDirection(Vector3.right);
        Vector3 cameraRelativeLeft = Camera.main.transform.TransformDirection(Vector3.left);

        //Horizontal camera vectors
        Vector3 XZForward = new Vector3(cameraRelativeForward.x, 0, cameraRelativeForward.z);
        Vector3 XZBack = new Vector3(cameraRelativeBack.x, 0, cameraRelativeBack.z);
        Vector3 XZRight = new Vector3(cameraRelativeRight.x, 0, cameraRelativeRight.z);
        Vector3 XZLeft = new Vector3(cameraRelativeLeft.x, 0, cameraRelativeLeft.z);


        //Basic movement setup
        if (!Input.GetKey(KeyCode.LeftControl)) //Crouch action stops all movement on ground
        {
            HealQue = 3;
            heal = false;
            if (Input.GetKey(KeyCode.W))
            {
                PlayerMove(XZForward);
            }
            if (Input.GetKey(KeyCode.S))
            {
                PlayerMove(XZBack);
            }
            if (Input.GetKey(KeyCode.D))
            {
                PlayerMove(XZRight);
            }
            if (Input.GetKey(KeyCode.A))
            {
                PlayerMove(XZLeft);
            }
            if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
            {
                PlayerMove(XZForward + XZRight);
            }
            if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))
            {
                PlayerMove(XZForward + XZLeft);
            }
            if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D))
            {
                PlayerMove(XZBack + XZRight);
            }
            if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A))
            {
                PlayerMove(XZBack + XZLeft);
            }
        }
        //Crouch action
        if (Input.GetKey(KeyCode.LeftControl) && grounded == true)
        {
            speed = 0;
            Otter.SetBool("crouch", true);
            if (Input.GetKey(KeyCode.Mouse0) && CurrentHealth < MaxHealth)
            {
                var HealTime = System.Math.Round((HealQue));
                HealingText = "Healing in: " + HealTime;
                HealQue -= Time.deltaTime;
                if (HealQue <= 0)
                {
                    HealingText = "";
                    TakeDamage(-0.2f);
                }
            }
            else
            {
                heal = false;
            }
        }
        else
        {
            Otter.SetBool("crouch", false);
            HealingText = "";
        }

        //Aurborne and landing sequence animations conditioning
        //+ Player's slide and full-break conditioning on ground
        if (grounded == false)
        {
            FallClock -= Time.deltaTime;
            if (FallClock <= 0)
            {
                Otter.SetBool("midair", true);
            }
        }
        if (grounded == true)
        {
            FallClock = 0.08f;
            Otter.SetBool("midair", false);
            levitation = 10;
            Otter.speed = 1;
            if (!Input.anyKey && Player.velocity.magnitude >= 6)
            {
                neutralAndMoving = true;
                if (Player.velocity.magnitude < 6)
                    Player.velocity = new Vector3(0, Player.velocity.y, 0);
            }
            else
            {
                neutralAndMoving = false;
            }
        }
        else
        {
            neutralAndMoving = false;
        }

        if (neutralAndMoving == true)
        {
            Otter.SetBool("moving", true);
            SlideEffect.enableEmission = true;
        }
        if (neutralAndMoving == false)
        {
            Otter.SetBool("moving", false);
            SlideEffect.enableEmission = false;
        }

        //Heal and hurt effects conditioning
        if (heal == true)
        {
            Sound.Heal();
            HealShape.radius = 5;
            HealShape.radiusSpeed = 1;
            HealEffect.emissionRate = 30f;
            HealLight.intensity = (1f);
            HurtEffect.enableEmission = false;
            HealEffect.enableEmission = true;
            HurtLight.enabled = false;
            HealLight.enabled = true;
        }
        if (hurt == true)
        {
            HurtEffect.enableEmission = true;
            HealEffect.enableEmission = false;
            HurtLight.enabled = true;
            HealLight.enabled = false;
        }
        if (heal == false && hurt == false && TouchShroom == false)
        {
            HurtEffect.enableEmission = false;
            HealEffect.enableEmission = false;
            HurtLight.enabled = false;
            HealLight.enabled = false;
        }
        if (TouchShroom == true)
        {
            Sound.Heal();
            HealShape.radius = 10;
            HealShape.radiusSpeed = 3;
            HealEffect.emissionRate = 70f;
            HealLight.intensity = (10f);
            HurtEffect.enableEmission = false;
            HealEffect.enableEmission = true;
            HurtLight.enabled = false;
            HealLight.enabled = true;
        }

        //Melee action
        if (Input.GetKey(KeyCode.Mouse0))
        {

            Otter.SetBool("fight", true); //Airkick leveitation
            if (grounded == false)
            {
                BeatAir -= Time.deltaTime;
                if (BeatAir <= 0)
                {
                    Attack();
                }
                InitiateAir -= Time.deltaTime;
                if (InitiateAir <= 0)
                {
                    if (Otter.speed > 0.4)
                    {
                        Player.AddForce(0, levitation, 0);
                        levitation -= 0.05f;
                        Otter.speed -= 0.005f;
                    }
                    else
                    {
                        Otter.SetBool("fight", false);
                    }
                }
            }
            if (grounded == true)
            {
                BeatGrounded -= Time.deltaTime;
                if (BeatGrounded <= 0)
                {
                    Attack();
                }
            }

        }
        else
        {
            Otter.SetBool("fight", false);
            InitiateAir = 0.5f;
            GroundAttack = 30;
            Beat = 0;
            Otter.speed = 1;
        }

        //Stoning action
        if (Input.GetKey(KeyCode.F)&& grounded==true)
        {
            HealingText = "(<X>)";
            if (!Input.GetKey(KeyCode.S)&& !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {
                rotGoal = Quaternion.LookRotation(XZForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            }
            Throw -= Time.deltaTime;
            if (Throw <= 0)
            {
                Otter.Play("Throw");
                Instantiate(Ball, AttackPoint.position, Quaternion.identity);
                Throw = 0.4f;
            }

        }
        //Draw logs action
        if (Input.GetKey(KeyCode.Mouse1))
        {
            TakeDamage(10);
        }
        if (!Input.GetKey(KeyCode.Mouse1))
        {
            hurt = false;
        }
    }

    public void LateUpdate()
    {
        //Stop movement animations by default
        Otter.SetBool("walk", false);
        Otter.SetBool("run", false);
        Otter.SetBool("midair", false);
    }
}
