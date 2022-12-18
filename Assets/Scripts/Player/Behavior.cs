using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Behavior : MonoBehaviour
{
    [Header("Movement and animation")]
    public Rigidbody Player;
    Rigidbody Ground;
    public Vector3 PlatformVelocity;
    public Carry Load;
    public Quaternion rotGoal;
    public float speed = 5;
    public float steer = 0.12f;
    public float JumpForce = 3;
    public float Walk = 4;
    public float Run = 12;
    float levitation = 10f;
    float InitiateAir = 0.5f;
    bool StandOnIsle;
    public bool neutralAndMoving;
    [SerializeField] int JumpNum;
    int JumpLimit;
    int JumpNumPreserve;
    public Transform Root;
    public Animator Otter;
    public bool grounded;
    public bool OnPlatform = false;
    [Header("Health")]
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

    [Header("Combat")]
    public GameObject Stone;
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
    [SerializeField] float InitialFall = 0.2f;
    public int GroundAttack = 30;
    bool StoneHeld = false;
    bool HammerHeld = false;
    [SerializeField] GameObject RightHandWeapon;
    [SerializeField] GameObject LeftHandWeapon;

    [Header("Other Objects")]
    public GameObject Seed;
    public int GobletPickup = 0;
    public string GobletText;
    [Header("Chat")]
    public string Plattering;
    [Header("Audio & Effects")]
    bool isPlaying = false;
    public AudioScript Sound;
    [SerializeField] GameObject MusicOP;
    float HealQue = 3;
    [SerializeField] ParticleSystem SlideEffect;
    [SerializeField] ParticleSystem HealEffect;
    public ParticleSystem.ShapeModule HealShape;
    [SerializeField] Light HealLight;
    [SerializeField] ParticleSystem HurtEffect;
    [SerializeField] Light HurtLight;
    DoorShut Door;
    [Header("UI")]
    public string LogCount;
    public string DebugText;
    public string HealingText;
    public int Currency = 0;
    public int NutCount;
    public string SeedText;
    public string Wallet;
    public GameMaster GM;
    public GameObject AimIcon;
    bool IsCursorOn;
    public CinemachineFreeLook FreeLook;
    public float ChangeSpeech = 1F;
    public void OnCollisionEnter(Collision OBJ)
    {
    
        if (OBJ.gameObject.CompareTag("Part") || OBJ.gameObject.CompareTag("Seed"))
        {
            Otter.Play("Crouch");
            if (OBJ.gameObject.CompareTag("Seed"))
            {
                NutCount++;
                Destroy(OBJ.gameObject);
            }
        }

        if (OBJ.gameObject.CompareTag("Life"))
        {
            GM.lastCheckPointPos = transform.position;
        }

        if (OBJ.gameObject.tag == "Isle" || OBJ.gameObject.CompareTag("Bridge"))
        {
            FallClock = InitialFall;
        }
        if (OBJ.gameObject.CompareTag("Coin"))
        {
            Sound.Coin();
        }
        if (OBJ.gameObject.CompareTag("GobletKey"))
        {
            Sound.Coin();
            GobletPickup++;
            Destroy(OBJ.gameObject);
        }

    }
    public void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Isle" || OBJ.gameObject.CompareTag("Bridge") || OBJ.gameObject.CompareTag("Tile"))
        {
            grounded = true;
        }
        if (OBJ.gameObject.CompareTag("Isle"))
        {
            StandOnIsle = true;
        }
        if (OBJ.gameObject.tag == "Tile")
        {
            var OBJVelocity = OBJ.transform.GetComponent<Rigidbody>();
            PlatformVelocity = OBJVelocity.velocity;
            OnPlatform = true;
            
        }
        if (OBJ.gameObject.CompareTag("Weapon"))
        {
            Plattering = ("Hm, What's this?");
            ChangeSpeech = 1;
            if (Input.GetKey(KeyCode.LeftControl))
            {
                RightHandWeapon.SetActive(true);
                LeftHandWeapon.SetActive(true);
                HammerHeld = true;
                Destroy(OBJ.gameObject);
                Plattering = ("Why do I hear boss music?");
                ChangeSpeech = 5;
            }
        }
        if (OBJ.gameObject.tag == "Life")
        {
            if (CurrentHealth < MaxHealth)
            {
                Plattering = ("Shroom!");
                ChangeSpeech = 1;
            }
            HealingText = "Checkpoint saved";
            if (CurrentHealth < MaxHealth)
            {
                TakeDamage(-10);
                TouchShroom = true;
            }
            if (CurrentHealth >= MaxHealth)
                TouchShroom = false;
        }
        if (OBJ.gameObject.CompareTag("NPC"))
        {
            Plattering = "Get off me ya nasty bastards!";
            ChangeSpeech = 3;
        }
        if (OBJ.gameObject.tag == "Strike")
        {
            HealingText = "You Can't Swim!";
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
            StandOnIsle = false;
        }
        if (OBJ.gameObject.tag == "Life")
        {
            //Plattering = ("That's what i'm talking about");
            //ChangeSpeech = 1;
            TouchShroom = false;
        }
        if (OBJ.gameObject.tag == "Tile")
        {
            grounded = false;
            OnPlatform = false;
        }
    }
    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Music"))
        {
            MusicOP.SetActive(true);
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
                if (enemy.CompareTag("Boss"))
                {
                    Debug.Log("Hit " + enemy.name);
                    enemy.GetComponent<BossScript>().TakeDamage(GroundAttack);
                }
                if (enemy.CompareTag("Hive"))
                {
                    Debug.Log("Hit " + enemy.name);
                    enemy.GetComponent<Static_Hive>().TakeDamage(GroundAttack);
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
                if (enemy.CompareTag("Boss"))
                {
                    Debug.Log("Hit " + enemy.name);
                    enemy.GetComponent<BossScript>().TakeDamage(40);
                }
                if (enemy.CompareTag("Hive"))
                {
                    Debug.Log("Hit " + enemy.name);
                    enemy.GetComponent<Static_Hive>().TakeDamage(40);
                }

            }
            BeatAir = AirBeat * Otter.speed;
        }
    }

    //Player Character talks nonesense

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
        rotGoal = Quaternion.LookRotation(new Vector3(Direction.x, 0, Direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
        Otter.SetBool("walk", true);
        if (OnPlatform == false)
        {
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
                Player.AddForce(Direction.normalized * 5);
        }
        if (OnPlatform == true)
        {
            //if (grounded == true)
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
                Player.velocity = (Direction.normalized * speed) + PlatformVelocity;

            }
            //if (grounded == false)
            //    Player.AddForce(Direction.normalized * 5);
        }
    }
    public void ShowCursor()
    {
        //show mouse icon
        IsCursorOn = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void HideCursor()
    {
        //Lock and hide mouse icon
        IsCursorOn = false;
        FreeLook.m_LookAt=Root;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void Start()
    {
        AimIcon.SetActive(false);
        Player = GetComponent<Rigidbody>();
        GM = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        Door = GameObject.FindGameObjectWithTag("Door").GetComponent<DoorShut>();
        CurrentHealth = MaxHealth;
        HealthBar.SetMaxHealth(CurrentHealth);
        HealShape = HealEffect.shape;
        RightHandWeapon.SetActive(false);
        LeftHandWeapon.SetActive(false);
        Lives = 3;
        JumpLimit = JumpNum;
        FallClock = InitialFall;
    }
    public void Update()
    {
        HideCursor();
        //Update UI text
        if (Plattering != "")
        {
            ChangeSpeech -= Time.deltaTime;
            if (ChangeSpeech <= 0)
            {
                Plattering = "";
            }
        }
        HealthPercent = System.Math.Round((CurrentHealth / MaxHealth) * 100f, 1);
        DebugText = HealthPercent + "%";
        Wallet = Currency + " Coins";
        SeedText = NutCount + " Nuts";
        GobletText = GobletPickup + "/"+Door.GobletNum +" Goblets";
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

        if (HammerHeld == true)
        {
            GroundAttack = 450;
        }


        //Jump action
        if (Input.GetKeyDown(KeyCode.Space) && JumpNum > 0)
        {
            if (Player.transform.parent!=null)
            {
                Player.transform.parent = null;
            }
            OnPlatform = false;
            Player.velocity = new Vector3(Player.velocity.x, JumpForce, Player.velocity.z);
            Sound.Jump();
            levitation = 10;
            Otter.speed = 1;
            JumpNum--;
            JumpNumPreserve = JumpNum;
        }
        if (Input.GetKey(KeyCode.Space) || Input.GetKeyUp(KeyCode.Space))
            JumpNum = JumpNumPreserve;

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


        Otter.SetBool("walk", false);
        Otter.SetBool("run", false);

        Otter.SetBool("midair", false);

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
            if (StandOnIsle == true && Load.i == 0 && Input.GetKey(KeyCode.Mouse0))
            {
                StoneHeld = true;
                Stone.SetActive(true);
            }
            speed = 0;
            Otter.SetBool("crouch", true);
            if (Input.GetKey(KeyCode.H) && CurrentHealth < MaxHealth)
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
            if (JumpNum < JumpLimit)
            {
                Otter.SetBool("midair", true);
            }
            if (JumpNum == JumpLimit)
            {
                Otter.SetBool("midair", false);
                FallClock -= Time.deltaTime;
                if (FallClock <= 0)
                {
                    Otter.SetBool("midair", true);
                }
            }
        }
        if (grounded == true)
        {
            JumpNum = JumpLimit;
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
            rotGoal = Quaternion.LookRotation(new Vector3(Player.velocity.x,0,Player.velocity.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.5f);
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
        if (Input.GetKey(KeyCode.Mouse0) && StoneHeld == false && IsCursorOn==false)
        {
            Otter.SetBool("fight", true); //Airkick leveitation
            if (grounded == false)
            {
                //Plattering = "Copter time!";
                //ChangeSpeech = 1f;
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
        if (Input.GetKey(KeyCode.Mouse0) && StoneHeld == true)
        {
            Otter.SetBool("fight", false);
            Otter.SetBool("crouch", false);
            AimIcon.SetActive(true);
            if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {
                rotGoal = Quaternion.LookRotation(XZForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse0) && StoneHeld == true)
        {
            Otter.Play("Throw");
            Instantiate(Ball, AttackPoint.position, Quaternion.identity);
            Stone.SetActive(false);
            StoneHeld = false;
            AimIcon.SetActive(false);
        }
        //Draw logs action
        if (Input.GetKey(KeyCode.Mouse1) && !Input.GetKey(KeyCode.LeftControl))
        {
            TakeDamage(10);
        }
        if (!Input.GetKey(KeyCode.Mouse1))
        {
            hurt = false;
        }

        //Plant Seed action
        if (Input.GetKeyDown(KeyCode.R) && NutCount > 0)
        {
            Otter.Play("Crouch");
            Instantiate(Seed, AttackPoint.position, Quaternion.identity);
            NutCount--;
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            Otter.SetBool("Crouch", false);
        }
    }

}
