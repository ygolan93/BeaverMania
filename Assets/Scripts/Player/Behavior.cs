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
    public Vector3 PlatformVelocity;
    public Carry Load;
    public Quaternion rotGoal;
    public float speed = 5;
    public float steer = 0.12f;
    public float JumpForce = 3;
    public float Walk = 4;
    public float Run = 12;
    float InsertWalk;
    float InsertRun;
    float levitation = 10f;
    float InitiateAir = 0.5f;
    float AnimSpeed = 1;
    public bool neutralAndMoving;
    [SerializeField] int JumpNum;
    int JumpLimit;
    int JumpNumPreserve;
    public Transform Root;
    public Animator Otter;
    public bool grounded;
    public bool OnPlatform = false;
    [Header("Health")]
    float StopHurt = 0;
    public float MaxHealth = 1000;
    public float CurrentHealth;
    public float MaxStamina = 100;
    public float CurrentStamina;
    float StaminaClockInitial = 3f;
    float StaminaClock;
    public Health_Bar_Script HealthBar;
    public bool heal;
    public bool TouchShroom;
    public bool hurt;
    double HealthPercent;
    double StaminaPercent;
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
    int insertGroundAttack;
    bool HammerHeld = false;
    public GameObject ParryShield;
    public bool isParried;
    [SerializeField] GameObject RightHandWeapon;
    [SerializeField] GameObject LeftHandWeapon;

    [Header("Other Objects")]
    public GameObject Seed;
    public GameObject HoneyJar;
    public GameObject PlacedJar;
    public GameObject GoldBrick;
    public GameObject PlacedGold;
    public bool GoldPicked;
    public bool Honeypicked;
    public bool GobletPicked;
    public int GobletPickup = 0;
    public float GobletClock = 10f;
    public string GobletText;
    [Header("Chat")]
    public string Plattering;
    [Header("Audio & Effects")]
    public AudioScript Sound;
    [SerializeField] MusicPlaylist MusicOP;
    float HealQue = 3;
    [SerializeField] ParticleSystem SlideEffect;
    [SerializeField] ParticleSystem HealEffect;
    [SerializeField] GameObject ElectricEffect;
    public ParticleSystem.ShapeModule HealShape;
    [SerializeField] Light HealLight;
    [SerializeField] ParticleSystem HurtEffect;
    [SerializeField] Light HurtLight;
    [Header("UI")]
    public GameObject LooseScreen;
    public GameObject BossBar;
    public bool isAtTrader = false;
    public string LogCount;
    public string DebugText;
    public string StaminaText;
    public string HealingText;
    public string AppleText;
    public int Apple;
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

        if (OBJ.gameObject.CompareTag("Part") || OBJ.gameObject.CompareTag("Seed") || OBJ.gameObject.CompareTag("Apple"))
        {
            Otter.Play("Crouch");
            if (OBJ.gameObject.CompareTag("Seed"))
            {
                NutCount++;
                Destroy(OBJ.gameObject);
            }
            if (OBJ.gameObject.CompareTag("Apple"))
            {
                Apple++;
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
            if (Input.GetKey(KeyCode.LeftControl))
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
                MusicOP.BeatNuts();
                Plattering = ("Why do I hear boss music?");
                ChangeSpeech = 5;
            }
        }
        if (OBJ.gameObject.CompareTag("Honey") && Honeypicked == false)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                HoneyON();
                Destroy(OBJ.gameObject);
            }
        }
        if (OBJ.gameObject.CompareTag("Gold") && GoldPicked == false)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                GoldON();
                Destroy(OBJ.gameObject);
            }
        }
        if (OBJ.gameObject.tag == "Life")
        {
            //if (CurrentHealth < MaxHealth)
            //{
            Plattering = ("Shroom!");
            ChangeSpeech = 1;
            //}
            HealingText = "Checkpoint saved";
            if (CurrentHealth < MaxHealth)
            {
                TakeDamage(-2);
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
            if (Lives > 0)
            {
                transform.position = GM.lastCheckPointPos;
                Lives--;
            }
            if (Lives == 0)
            {
                ActivateLooseMenu();
            }
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
        if (OBJ.gameObject.tag == "Tile")
        {
            grounded = false;
            OnPlatform = false;
        }
    }
    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Outro"))
        {
            MusicOP.OutroSong();
        }
    }
    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Trader"))
        {
            ShowCursor();
            isAtTrader = true;
            FreeLook.m_Orbits[2].m_Radius = 6;
            FreeLook.m_XAxis.m_MaxSpeed = 0;
            FreeLook.m_YAxis.m_MaxSpeed = 0;
            FreeLook.m_LookAt = OBJ.transform;
        }
        if (OBJ.gameObject.CompareTag("What Is this?"))
        {
            Plattering = "Ah shit. what happened here?";
        }

        if (OBJ.gameObject.CompareTag("Arena"))
        {
            BossBar.SetActive(true);
        }



    }
    public void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Trader"))
        {
            isAtTrader = false;
            FreeLook.m_Orbits[2].m_Radius = 4.7F;
            FreeLook.m_XAxis.m_MaxSpeed = 300;
            FreeLook.m_YAxis.m_MaxSpeed = 2;
            FreeLook.m_LookAt = Root;
        }
        if (OBJ.gameObject.CompareTag("Boss"))
        {
            BossBar.SetActive(false);
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
            BeatGrounded = GroundBeat*Otter.speed;
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
        if (isParried == false)
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
        if (isParried == true)
        {
            CurrentStamina -= Damage;
            HealthBar.SetStamina(CurrentStamina);
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
            if (grounded == false || Input.GetKey(KeyCode.LeftShift))
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
        FreeLook.m_LookAt = Root;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void Start()
    {
        HideCursor();
        AimIcon.SetActive(false);
        Player = GetComponent<Rigidbody>();
        GM = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        CurrentHealth = MaxHealth;
        HealthBar.SetMaxHealth(CurrentHealth);
        CurrentStamina = MaxStamina;
        HealthBar.SetMaxStamina(CurrentStamina);
        HealShape = HealEffect.shape;
        RightHandWeapon.SetActive(false);
        LeftHandWeapon.SetActive(false);
        ParryOFF();
        HoneyOFF();
        GoldOFF();
        ElectricEffect.SetActive(false);
        Lives = 3;
        Apple = 0;
        StaminaClock = StaminaClockInitial;
        JumpLimit = JumpNum;
        FallClock = InitialFall;
        InsertWalk = Walk;
        InsertRun = Run;
        insertGroundAttack = GroundAttack;
        LooseScreen.SetActive(false);
        BossBar.SetActive(false);
    }
    public void Update()
    {
        //Update UI text
        if (Plattering != "")
        {
            ChangeSpeech -= Time.deltaTime;
            if (ChangeSpeech <= 0)
            {
                Plattering = "";
            }
        }
        if (isParried == false)
        {
            if (CurrentStamina > MaxStamina)
                CurrentStamina = MaxStamina;


            if (CurrentStamina < MaxStamina && !Input.GetKey(KeyCode.LeftControl))
            {
                if (!Input.GetKey(KeyCode.Mouse1) && isParried == false && !Input.GetKey(KeyCode.Mouse0))
                {
                    StaminaClock -= Time.deltaTime;
                    if (StaminaClock <= 0)
                    {
                        CurrentStamina += 1;
                        HealthBar.SetStamina(CurrentStamina);
                    }
                }
                else
                    StaminaClock = StaminaClockInitial;
            }
            else
                StaminaClock = StaminaClockInitial;
        }

        HealthPercent = System.Math.Round((CurrentHealth / MaxHealth) * 100f, 1);
        DebugText = HealthPercent + "%";
        StaminaPercent = System.Math.Round((CurrentStamina / MaxStamina) * 100f, 1);
        StaminaText = StaminaPercent + "%";
        Wallet = Currency + " Coins";
        SeedText = NutCount + " Nuts (R)";
        AppleText = Apple + " Apples (T)";
        GobletText = GobletPickup + " Goblets (Y)";
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
            if (Player.transform.parent != null)
            {
                Player.transform.parent = null;
            }
            OnPlatform = false;
            Player.velocity = new Vector3(Player.velocity.x, JumpForce, Player.velocity.z);
            Sound.Jump();
            levitation = 10;
            Otter.speed = AnimSpeed;
            JumpNum--;
            JumpNumPreserve = JumpNum;
        }
        if (Input.GetKey(KeyCode.Space) || Input.GetKeyUp(KeyCode.Space))
            JumpNum = JumpNumPreserve;

        //Load Loose screen on death
        if (CurrentHealth <= 0)
        {
            if (Lives > 0)
            {
                RestartCheckpoint();
            }
            if (Lives == 0)
            {
                ActivateLooseMenu();
            }
        }

        if (CurrentStamina <= 0)
        {
            ParryOFF();
            CurrentStamina = 0;
        }

    }
    public void ActivateLooseMenu()
    {
        MusicOP.StopMusic();
        Time.timeScale = 0;
        ShowCursor();
        LooseScreen.SetActive(true);
    }
    public void HideLooseMenu()
    {
        MusicOP.ResumeMusic();
        Time.timeScale = 1;
        HideCursor();
        LooseScreen.SetActive(false);
    }
    public void RestartCheckpoint()
    {
        HealthBar.SetHealth(MaxHealth);
        CurrentHealth = MaxHealth;
        transform.position = GM.lastCheckPointPos;
        Lives--;
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GobletON()
    {
        GobletPicked = true;
        GobletPickup--;
        AnimSpeed = 3;
        Walk = 7;
        Run = 18;
        JumpLimit = 5;
        GroundAttack = 180;
        GroundBeat = GroundBeat/5;
        AirBeat =  AirBeat/5;
        ElectricEffect.SetActive(true);     
    }
    public void GobletOFF()
    {
        GobletPicked = false;
        AnimSpeed = 1;
        Walk = InsertWalk;
        Run = InsertRun;
        GroundAttack = insertGroundAttack;
        JumpLimit = 3;
        GroundBeat = 5 * GroundBeat;
        AirBeat = 5 * AirBeat;
        ElectricEffect.SetActive(false);
        GobletClock = 10F;

    }

    public void ParryON()
    {
        Otter.SetBool("Parry", true);
        ParryShield.SetActive(true);
        isParried = true;
    }
    public void ParryOFF()
    {
        Otter.SetBool("Parry", false);
        ParryShield.SetActive(false);
        isParried = false;
    }
    public void HoneyON()
    {
        Honeypicked = true;
        HoneyJar.SetActive(true);
    }
    public void HoneyOFF()
    {
        Honeypicked = false;
        HoneyJar.SetActive(false);
    }
    public void GoldON()
    {
        GoldPicked = true;
        GoldBrick.SetActive(true);
    }
    public void GoldOFF()
    {
        GoldPicked = false;
        GoldBrick.SetActive(false);
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

        //Switch off hurt and heal effects automaticly
        if (hurt == true || heal == true)
        {
            StopHurt += Time.deltaTime;
            if (StopHurt >= 0.15f)
            {
                hurt = false;
                heal = false;
            }
        }

        if (CurrentHealth >= MaxHealth)
        {
            heal = false;
        }


        //Basic movement setup
        if (!Input.GetKey(KeyCode.LeftControl)) //Crouch action stops all movement on ground
        {
            HealQue = 3;
            //heal = false;
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
        //Crouch action - Place Logs
        if (Input.GetKey(KeyCode.LeftControl) && grounded == true)
        {
            speed = 0;
            if (HammerHeld == false)
                Otter.SetBool("crouch", true);
            if (HammerHeld == true)
            {
                if (CurrentStamina > 0)
                    ParryON();
                if (CurrentStamina <= 0)
                {
                    Otter.SetBool("crouch", true);
                }
            }
            if (Honeypicked == true)
            {
                HoneyOFF();
                Instantiate(PlacedJar, AttackPoint.position - new Vector3(0, 0.5f, 0.3f), Quaternion.identity);
            }
            if (GoldPicked==true)
            {
                GoldOFF();
                Instantiate(PlacedGold, AttackPoint.position-new Vector3(0,0.5f,0.3f), Quaternion.identity);
            }

        }
        else
        {
            Otter.SetBool("crouch", false);
            ParryOFF();
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
            Otter.speed = AnimSpeed;
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
            rotGoal = Quaternion.LookRotation(new Vector3(Player.velocity.x, 0, Player.velocity.z));
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
            //Sound.Heal();
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

        if (isAtTrader == false)
        {
            //Melee action
            if (Input.GetKey(KeyCode.Mouse0) && CurrentStamina > 0)
            {
                CurrentStamina -= 0.1f;
                HealthBar.SetStamina(CurrentStamina);
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
                Otter.speed = AnimSpeed;
            }

        }



        //Stoning action
        if (Input.GetKey(KeyCode.Mouse1) && CurrentStamina > 0)
        {
            Otter.SetBool("fight", false);
            //Otter.SetBool("crouch", true);
            //Player.velocity = new Vector3(0, Player.velocity.y, 0);
            Stone.SetActive(true);
            AimIcon.SetActive(true);
            if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {
                rotGoal = Quaternion.LookRotation(XZForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse1) && Stone.active && CurrentStamina > 0)
        {
            Otter.Play("Throw");
            //Otter.SetBool("crouch", false);
            Instantiate(Ball, AttackPoint.position, Quaternion.identity);
            CurrentStamina -= 20;
            HealthBar.SetStamina(CurrentStamina);
        }
        if (!Input.GetKey(KeyCode.Mouse1))
        {
            Stone.SetActive(false);
            AimIcon.SetActive(false);
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

        //Eat Apple
        if (Input.GetKeyUp(KeyCode.T) && Apple > 0)
        {
            if (MaxHealth - CurrentHealth > 500)
            {
                TakeDamage(-500);
                HealthBar.SetHealth(CurrentHealth);
            }
            else
            {
                CurrentHealth = MaxHealth;
                HealthBar.SetMaxHealth(MaxHealth);
            }
            Apple--;
        }

        //Use Goblet
        if (Input.GetKeyUp(KeyCode.Y) && GobletPickup > 0)
        {
            GobletON();
        }
        if (GobletPicked==true)
        {
            CurrentStamina = MaxStamina;
            GobletClock -= Time.deltaTime;
            HealingText = "Boost time: " + Math.Round(GobletClock);
            if (GobletClock <= 0)
                GobletOFF();
        }

    }

}

