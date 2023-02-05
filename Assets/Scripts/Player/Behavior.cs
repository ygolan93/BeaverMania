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
    float StaminaClockInitial = 0.5f;
    float StaminaClock;
    public Health_Bar_Script HealthBar;
    public bool heal;
    public bool TouchShroom;
    public bool hurt;
    public bool Rolling;
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
    public int GroundAttack = 50;
    public bool HammerHeld;
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
    public float GobletClock = 10f;
    public string GobletText;
    [Header("Chat")]
    public string Plattering;
    [Header("Audio & Effects")]
    public AudioScript Sound;
    //public MusicPlaylist MusicOP;
    float HealQue = 3;
    [SerializeField] ParticleSystem SlideEffect;
    [SerializeField] ParticleSystem HealEffect;
    [SerializeField] GameObject ElectricEffect;
    public ParticleSystem.ShapeModule HealShape;
    [SerializeField] Light HealLight;
    [SerializeField] ParticleSystem HurtEffect;
    [SerializeField] Light HurtLight;
    [SerializeField] GameObject PopUpEffect;
    [SerializeField] GameObject PickUpEffect;
    [SerializeField] GameObject KickWind;
    [SerializeField] GameObject BoxWind;
    [SerializeField] Transform KickEffectPos;
    [Header("UI")]
    public GameObject LooseScreen;
    public BossScript Boss;
    public GameObject BossBar;
    public GameObject BossPanel;
    public GameObject BossContinueButton;
    public bool isAtTrader = false;
    public string LogCount;
    public string DebugText;
    public string StaminaText;
    public string HealingText;
    public string AppleText;
    public int GobletPickup = 0;
    public int Apple;
    public int Currency = 0;
    public int NutCount;
    public string SeedText;
    public string Wallet;
    public GameMaster GM;
    public GameObject AimIcon;
    public CinemachineFreeLook FreeLook;
    public CinemachineFreeLook CamForTraders;
    public CinemachineFreeLook InHouseCam;
    public float ChangeSpeech = 1F;

    public void OnCollisionEnter(Collision OBJ)
    {

        if (OBJ.gameObject.CompareTag("Part") || OBJ.gameObject.CompareTag("Seed") || OBJ.gameObject.CompareTag("Apple") || OBJ.gameObject.CompareTag("GobletKey"))
        {
            Otter.Play("Crouch");
            if (OBJ.gameObject.CompareTag("Seed"))
            {
                NutCount++;
                Destroy(OBJ.gameObject);
            }
            if (OBJ.gameObject.CompareTag("Apple"))
            {
                Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
                Sound.PickUp2();
                Apple++;
                Destroy(OBJ.gameObject);
            }
            if (OBJ.gameObject.CompareTag("GobletKey"))
            {
                Sound.PickUp2();
                GobletPickup++;
                Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
                Destroy(OBJ.gameObject);
            }

        }
        if (OBJ.gameObject.tag == "Isle" || OBJ.gameObject.CompareTag("Bridge"))
        {
            FallClock = InitialFall;
        }
        if (OBJ.gameObject.CompareTag("Coin"))
        {
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
            Sound.Coin();
        }


    }
    public void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Isle" || OBJ.gameObject.CompareTag("Bridge") || OBJ.gameObject.CompareTag("Tile") || OBJ.gameObject.CompareTag("House"))
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
                Plattering = ("Why do I hear Boss music?");
                RightHandWeapon.SetActive(true);
                LeftHandWeapon.SetActive(true);
                HammerHeld = true;
                Destroy(OBJ.gameObject);
                ChangeSpeech = 5;
            }
        }
        if (OBJ.gameObject.CompareTag("Honey") && Honeypicked == false)
        {
            Otter.Play("Crouch");
            HoneyON();
            Destroy(OBJ.gameObject);
        }
        if (OBJ.gameObject.CompareTag("Gold") && GoldPicked == false)
        {
            Otter.Play("Crouch");
            GoldON();
            Destroy(OBJ.gameObject);
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
                transform.position = GM.lastCheckPointPos + new Vector3(1, 1, 1);
                Instantiate(PopUpEffect, transform.position, Quaternion.identity);
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
        if (OBJ.gameObject.CompareTag("Isle") || OBJ.gameObject.CompareTag("Bridge") || OBJ.gameObject.CompareTag("House"))
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
    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.tag == "Life")
        {
            GM.lastCheckPointPos = OBJ.transform.position;
            Plattering = ("Shroom!");
            ChangeSpeech = 1;
            HealingText = "Checkpoint saved";
            if (CurrentHealth < MaxHealth)
            {
                TakeDamage(-2);
                TouchShroom = true;
            }
            if (CurrentHealth >= MaxHealth)
                TouchShroom = false;
        }
        if (OBJ.gameObject.CompareTag("Trader"))
        {
            ShowCursor();
            isAtTrader = true;
            FreeLook.enabled = false;
            CamForTraders.enabled = true;
            CamForTraders.m_Orbits[1].m_Radius = 6;
            CamForTraders.m_XAxis.m_MaxSpeed = 0.1f;
            CamForTraders.m_YAxis.m_MaxSpeed = 0.1f;
            CamForTraders.m_LookAt = OBJ.transform;

        }

        if (OBJ.gameObject.CompareTag("House"))
        {
            FreeLook.enabled = false;
            InHouseCam.enabled = true;

        }
        if (OBJ.gameObject.CompareTag("What Is this?"))
        {
            Plattering = "Ah shit. what happened here?";
        }

        if (OBJ.gameObject.CompareTag("Arena"))
        {
            if (BossContinueButton != null)
            {
                ShowCursor();
                isAtTrader = true;
                Boss.Charge = false;
                BossPanel.SetActive(true);
                FreeLook.m_Orbits[1].m_Radius = 15;
                FreeLook.m_XAxis.m_MaxSpeed = 0;
                FreeLook.m_YAxis.m_MaxSpeed = 0;
                FreeLook.m_LookAt = Boss.transform;
            }

            if (BossContinueButton == null)
            {
                isAtTrader = false;
                HideCursor();
                BossPanel.SetActive(false);
                Boss.Charge = true;
                BossBar.SetActive(true);
                FreeLook.m_Orbits[1].m_Radius = 6;
                FreeLook.m_XAxis.m_MaxSpeed = 300;
                FreeLook.m_YAxis.m_MaxSpeed = 2;
                FreeLook.m_LookAt = Root;
            }
        }
    }
    public void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Life"))
        {
            TouchShroom = false;
        }
        if (OBJ.gameObject.CompareTag("Trader"))
        {
            isAtTrader = false;
            CamForTraders.enabled = false;
            CamForTraders.m_LookAt = null;
            FreeLook.enabled = true;
            FreeLook.m_LookAt.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Root.position), 0.5f);
        }
        if (OBJ.gameObject.CompareTag("Boss"))
        {
            BossBar.SetActive(false);
        }
        if (OBJ.gameObject.CompareTag("House"))
        {
            InHouseCam.enabled = false;
            FreeLook.enabled = true;

        }
    }

    public void Attack()
    {
        //if (isParried == false)
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
                BeatGrounded = GroundBeat * Otter.speed;
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

    }
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
                if (Input.GetKey(KeyCode.LeftShift) && Input.anyKey && Rolling==false)
                {
                    Otter.SetBool("run", true);
                    speed = Run;
                    steer = 0.12f;
                }
                else
                {
                    Otter.SetBool("run", false);
                    speed = Walk;
                    steer = 0.1f;
                }
                Player.velocity = (Direction.normalized * speed) + new Vector3(0, Player.velocity.y, 0);

            }
            if (grounded == false && Input.GetKey(KeyCode.LeftShift))
                Player.AddForce(Direction.normalized * 5);
        }
        if (OnPlatform == true)
        {
            {
                if (Input.GetKey(KeyCode.LeftShift) && Input.anyKey)
                {
                    Otter.SetBool("run", true);
                    speed = Run;
                    steer = 0.12f;
                }
                else
                {
                    Otter.SetBool("run", false);
                    speed = Walk;
                    steer = 0.1f;
                }
                Player.velocity = (Direction.normalized * speed) + PlatformVelocity;

            }
        }

    }
    public void PlayerRoll(Vector3 Direction)
    {
        rotGoal = Quaternion.LookRotation(new Vector3(Direction.x, 0, Direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.11f);
        //Evading Roll action
        {
            if (Input.GetKey(KeyCode.LeftControl) && CurrentStamina > 0 && grounded == true)
            {
                GroundAttack = 300;
                Rolling = true;
                Otter.SetBool("roll", true);
                CurrentStamina -= 0.1f;
                speed = 7;
                HealthBar.SetStamina(CurrentStamina);
                Player.velocity = (Direction.normalized * 6) + new Vector3(0, Player.velocity.y, 0);
            }
            if(!Input.GetKey(KeyCode.LeftControl)|| CurrentStamina <= 0 || grounded==false)
            {
                Rolling = false;
                Otter.SetBool("roll", false);
            }
        }
    }
    public void ShowCursor()
    {
        //show mouse icon
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void HideCursor()
    {
        //Lock and hide mouse icon
        FreeLook.m_LookAt = Root;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void ActivateLooseMenu()
    {
        //MusicOP.StopMusic();
        Time.timeScale = 0;
        ShowCursor();
        LooseScreen.SetActive(true);
    }
    public void HideLooseMenu()
    {
        //MusicOP.ResumeMusic();
        Time.timeScale = 1;
        HideCursor();
        LooseScreen.SetActive(false);
    }
    public void RestartCheckpoint()
    {
        HealthBar.SetHealth(MaxHealth);
        CurrentHealth = MaxHealth;
        transform.position = GM.lastCheckPointPos;
        Instantiate(PopUpEffect, transform.position, Quaternion.identity);
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
        BeatGrounded = BeatGrounded / 5;
        AirBeat = AirBeat / 5;
        ElectricEffect.SetActive(true);
    }
    public void GobletOFF()
    {
        GobletPicked = false;
        AnimSpeed = 1;
        Walk = InsertWalk;
        Run = InsertRun;
        JumpLimit = 3;
        BeatGrounded = 5 * BeatGrounded;
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

    public void Start()
    {

        CamForTraders.enabled = false;
        InHouseCam.enabled = false;
        Instantiate(PopUpEffect, Root.position, Quaternion.identity);
        HideCursor();
        AimIcon.SetActive(false);
        Player = GetComponent<Rigidbody>();
        GM = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        CurrentHealth = MaxHealth;
        HealthBar.SetMaxHealth(CurrentHealth);
        CurrentStamina = MaxStamina;
        HealthBar.SetMaxStamina(CurrentStamina);
        HealShape = HealEffect.shape;
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
        LooseScreen.SetActive(false);
        BossBar.SetActive(false);
    }
    [System.Obsolete]
    public void Update()
    {
        //if (!Input.anyKey)
        //    Player.velocity = new Vector3(0, Player.velocity.y, 0);

        //Update UI text
        if (Plattering != "")
        {
            ChangeSpeech -= Time.deltaTime;
            if (ChangeSpeech <= 0)
            {
                Plattering = "";
            }
        }

        //Reload Stamina Bar
        if (isParried == false)
        {
            if (CurrentStamina > MaxStamina)
            {
                CurrentStamina = MaxStamina;
            }
            if (CurrentStamina < MaxStamina && !Input.GetKey(KeyCode.LeftControl))
            {
                if (!Input.GetKey(KeyCode.Mouse1) && isParried == false && !Input.GetKey(KeyCode.Mouse0) && !Input.GetKey(KeyCode.F))
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
        else
        {
            CurrentStamina -= 0.05f;
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
        {
            if (Input.GetKeyDown(KeyCode.Space) && JumpNum > 0)
            {
                Rolling = false;
                Otter.SetBool("roll", false);
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
            //Decrease Jump num by 1 on jump action 
            if (Input.GetKey(KeyCode.Space) || Input.GetKeyUp(KeyCode.Space))
                JumpNum = JumpNumPreserve;
        }
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

        //Limit Stamina decrease down to 0 only
        if (CurrentStamina <= 0)
        {
            ParryOFF();
            CurrentStamina = 0;
        }

        //Crouch action - Place Logs
        {
            if (Input.GetKey(KeyCode.LeftControl) && grounded == true && Rolling == false && !Input.GetKey(KeyCode.Space))
            {
                if (!Input.GetKey(KeyCode.W)|| !Input.GetKey(KeyCode.Mouse0))
                {
                    speed = 0;
                        Otter.SetBool("crouch", true);
                }
                if (Honeypicked == true)
                {
                    HoneyOFF();
                    Instantiate(PlacedJar, AttackPoint.position - new Vector3(0, 0.5f, 0.3f), Quaternion.identity);
                }
                if (GoldPicked == true)
                {
                    GoldOFF();
                    Instantiate(PlacedGold, AttackPoint.position - new Vector3(0, 0.5f, 0.3f), Quaternion.identity);
                }
            }
            else
            {
                Otter.SetBool("crouch", false);
                ParryOFF();
                HealingText = "";
            }
        }

        //Stoning action
        {
            if (Input.GetKey(KeyCode.Mouse1) && CurrentStamina > 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                Otter.SetBool("fight", false);
                //Otter.SetBool("crouch", true);
                //Player.velocity = new Vector3(0, Player.velocity.y, 0);
                Stone.SetActive(true);
                AimIcon.SetActive(true);
                if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
                {
                    var CamForward = Camera.main.transform.TransformDirection(Vector3.forward);
                    var DirectionGoal = new Vector3(CamForward.x, 0, CamForward.z);
                    rotGoal = Quaternion.LookRotation(DirectionGoal);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
                }
            }
            if (Input.GetKeyUp(KeyCode.Mouse1) && Stone.active && CurrentStamina > 0)
            {
                Otter.Play("Throw");
                Instantiate(Ball, AttackPoint.position, Quaternion.identity);
                CurrentStamina -= 20;
                HealthBar.SetStamina(CurrentStamina);
            }
            if (!Input.GetKey(KeyCode.Mouse1))
            {
                Stone.SetActive(false);
                AimIcon.SetActive(false);
            }
        }

        //Plant Seed action
        {
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

        //Eat Apple
        if (Input.GetKeyUp(KeyCode.T) && Apple > 0)
        {
            Otter.SetBool("Consume", true);
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
        {
            if (Input.GetKeyUp(KeyCode.Y) && GobletPickup > 0)
            {
                //Otter.SetBool("Consume", true);
                GobletON();
            }
            if (GobletPicked == true)
            {
                CurrentStamina = MaxStamina;
                GobletClock -= Time.deltaTime;
                HealingText = "Boost time: " + Math.Round(GobletClock);
                if (GobletClock <= 0)
                {
                    GobletOFF();
                }
            }
        }

        if (isAtTrader == false && ParryShield.active==false)
        {
            //Melee action
            {
                if (Input.GetKey(KeyCode.Mouse0) && CurrentStamina > 0)
                {
                    CurrentStamina -= 0.1f;
                    HealthBar.SetStamina(CurrentStamina);
                    Otter.SetBool("fight", true); //Airkick leveitation
                    if (grounded == false)
                    {
                        if (Otter.GetCurrentAnimatorStateInfo(0).IsName("Air Kick"))
                        {
                            var WindTrail = Instantiate(KickWind, KickEffectPos.position, Quaternion.Euler(-90, UnityEngine.Random.Range(0f, 360f), 0));
                            WindTrail.transform.parent = KickEffectPos;
                        }
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
                        if (Otter.GetCurrentAnimatorStateInfo(1).IsName("AttackA"))
                        {
                            if (Root.childCount == 0)
                            {
                                var RightWind = Instantiate(BoxWind, Root.position - new Vector3(0, 0.3f, 0), rotGoal * Quaternion.Euler(-90, 90, 0));
                                RightWind.transform.parent = Root;
                            }
                        }
                        if (Otter.GetCurrentAnimatorStateInfo(1).IsName("AttackB"))
                        {
                            if (Root.childCount == 0)
                            {
                                var LeftWind = Instantiate(BoxWind, Root.position - new Vector3(0, 0.3f, 0), rotGoal * Quaternion.Euler(90, 90, 0));
                                LeftWind.transform.parent = Root;
                            }
                        }
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
                    GroundAttack = 50;
                    Beat = 0;
                    Otter.speed = AnimSpeed;
                }
            }
            //Parry Action
            {
                if (Input.GetKey(KeyCode.F) && CurrentStamina>0)
                    ParryON();
                else
                    ParryOFF();
            }
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
        Otter.SetBool("roll", false);
        Rolling = false;
        //Switch off hurt and heal effects automaticly
        {
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
        }

        //Basic movement setup
        {
            //No Crouch = Regular Movement
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                HealQue = 3;
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
            //Movement + Crouch = Roll & Evade
            if (Input.GetKey(KeyCode.LeftControl) && CurrentStamina>0)
            {
                HealQue = 3;
                if (Input.GetKey(KeyCode.W))
                {
                    PlayerRoll(XZForward);
                }
                if (Input.GetKey(KeyCode.S))
                {
                    PlayerRoll(XZBack);
                }
                if (Input.GetKey(KeyCode.D))
                {
                    PlayerRoll(XZRight);
                }
                if (Input.GetKey(KeyCode.A))
                {
                    PlayerRoll(XZLeft);
                }
                if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
                {
                    PlayerRoll(XZForward + XZRight);
                }
                if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))
                {
                    PlayerRoll(XZForward + XZLeft);
                }
                if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D))
                {
                    PlayerRoll(XZBack + XZRight);
                }
                if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A))
                {
                    PlayerRoll(XZBack + XZLeft);
                }
            }
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


    }

}

