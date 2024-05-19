using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Behaviour : MonoBehaviour
{
    [Header("Movement and animation")]
    public Rigidbody Player;
    public bool grounded;
    public Carry Load;
    public Quaternion rotGoal;
    //public RayTraceGround obsticle;
    bool step;
    public float speed = 5;
    public float steer = 0.12f;
    public float JumpForce = 3;
    public float Walk = 4;
    public float Run = 12;
    public float rayCastHeightOffSet = 0.26f;
    float InsertWalk;
    float InsertRun;
    float levitation = 10f;
    float InitiateAir = 0.5f;
    float AnimSpeed = 1;
    public bool neutralAndMoving;
    [SerializeField] int JumpNum;
    int JumpLimit;
    int GobletJumpLimit;
    int JumpNumPreserve;
    public Transform Root;
    public Transform Face;
    public Animator Otter;
    public bool OnPlatform = false;
    [Header("Health")]
    float StopHurt = 0;
    public float MaxHealth = 1000;
    public float CurrentHealth;
    public float MaxStamina = 100;
    public float CurrentStamina;
    readonly float StaminaClockInitial = 0.5f;
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
    [SerializeField] GameObject appleOBJ;
    [SerializeField] GameObject gobletOBJ;
    [Header("Combat")]
    public List<String> Arsenal;
    public int arsenalBrowser = 0;
    public int ArsenalCounter = 0;
    public GameObject Arrow;
    [SerializeField] GameObject arrowModel;
    [SerializeField] LineRenderer stringLine;
    public GameObject[] Bow;
    public GameObject bowString;
    public GameObject Stone;
    public float Beat = 0;
    public Projectile Ball;
    public Transform AttackPoint;
    public Transform Sphere;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public float GroundBeat = 0.3f;
    public float AirBeat = 0.2f;
    bool scorpAttack;
    float FallClock;
    [SerializeField] float InitialFall = 0.2f;
    public int GroundAttack = 50;
    public bool bowEquipped;
    public bool HammerHeld;
    public bool ArmorEquipped;
    public bool Defend = false;
    public float DefendAnim = 0.3f;
    bool WhichAttack = false;
    public bool isParried;
    [SerializeField] GameObject RightHandWeapon;
    [SerializeField] GameObject LeftHandWeapon;
    [SerializeField] GameObject[] ArmorSet;

    [Header("Other Objects")]
    public GameObject HologramedBridge;
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
    [SerializeField] bool seekMusic;
    public MusicPlaylist Music;
    public AudioScript Sound;
    public float HealQue = 3;
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
    [SerializeField] GameObject SwordCopter;
    [SerializeField] GameObject SwordPlainTrail;
    [SerializeField] GameObject BoxWind;
    [SerializeField] Transform KickEffectPos;

    [Header("UI")]
    public float moveSpeed = 5.0f;
    public GameObject LooseScreen;
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
        if (OBJ.gameObject.CompareTag("Isle") || OBJ.gameObject.CompareTag("Bridge") || OBJ.gameObject.CompareTag("stairs") || OBJ.gameObject.CompareTag("Tile"))
        {
            FallClock = InitialFall;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
            {
                if (OBJ.gameObject.CompareTag("stairs"))
                {
                    Player.velocity += new Vector3(0, 1, 0);
                    Player.drag = 0;
                }
            }
        }
        if (OBJ.gameObject.CompareTag("Coin"))
        {
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
            Sound.Coin();
        }
        if (OBJ.gameObject.CompareTag("NPC")&& isParried==true)
        {
            var ParryDirection = new Vector3((OBJ.transform.position - transform.position).x, 0, (OBJ.transform.position - transform.position).z);
            transform.rotation = Quaternion.LookRotation(ParryDirection);
        }

    }
    public void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Isle") || 
            OBJ.gameObject.CompareTag("Bridge") || 
            OBJ.gameObject.CompareTag("Tile") || 
            OBJ.gameObject.CompareTag("House") || 
            OBJ.gameObject.CompareTag("stairs") || 
            OBJ.gameObject.CompareTag("Tile"))
        {
            grounded = true;
        }

        if (OBJ.gameObject.CompareTag("Weapon"))
        {
            Plattering = ("Hammers!");
            ChangeSpeech = 1;
            Otter.Play("Crouch");
            Arsenal.Add("Hammers");
            ArsenalCounter++;
            Sound.PickItem();
            Destroy(OBJ.gameObject);
        }
        if (OBJ.gameObject.CompareTag("Bow"))
        {
            Plattering = ("Booya!");
            ChangeSpeech = 1;
            Otter.Play("Crouch");
            Arsenal.Add("Bow");
            ArsenalCounter++;
            Sound.PickItem();
            Destroy(OBJ.gameObject);
        }
        if (OBJ.gameObject.CompareTag("Armor"))
        {
            Plattering = ("Oh my!");
            ChangeSpeech = 1;
            Otter.Play("Crouch");
            Arsenal.Add("ArmorSet");
            ArsenalCounter++;
            Sound.PickItem();
            Destroy(OBJ.gameObject);
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
        if (OBJ.gameObject.CompareTag("Strike"))
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
        if (OBJ.gameObject.CompareTag("Isle") || 
            OBJ.gameObject.CompareTag("Bridge") || 
            OBJ.gameObject.CompareTag("House") || 
            OBJ.gameObject.CompareTag("stairs") || 
            OBJ.gameObject.CompareTag("Tile"))
        {
            grounded = false;
        }
        if (OBJ.gameObject.CompareTag("Life"))
        {
            TouchShroom = false;
        }
    }
    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Music"))
        {
            OBJ.gameObject.SetActive(false);
            if (int.TryParse(OBJ.gameObject.name, out int songIndex))
            {
                Music.ChangeSong(songIndex);
            }
            else
            {
                Debug.LogWarning("The game object's name is not a valid integer: " + OBJ.gameObject.name);
            }

        }
        if (OBJ.gameObject.CompareTag("Life"))
        {
            HealingText = "Checkpoint saved";
        }
        if (OBJ.gameObject.CompareTag("Bridge"))
        {
            Player.velocity += new Vector3(0, 1, 0);
        }

        if (OBJ.gameObject.CompareTag("Tile"))
        {   
            Player.transform.SetParent(OBJ.gameObject.transform, true);
            OnPlatform = true;
        }
        if (OBJ.gameObject.CompareTag("ScorpionDamage"))
        {
            if (isParried == false)
            {
                if (scorpAttack == true)
                {
                    TakeDamage(15);
                }
            }
        }
        if (OBJ.gameObject.CompareTag("ScorpionSting"))
        {
            if (scorpAttack == true && isParried==false)
            {
                TakeDamage(30);
            }
        }
    }
    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Scorpion"))
        {
            scorpAttack = OBJ.gameObject.GetComponent<ScorpionScript>().isAttacking;
        }


        if (OBJ.gameObject.CompareTag("Tile"))
        {
            OnPlatform = true;
            Player.transform.parent = OBJ.gameObject.transform;
        }
        if (OBJ.gameObject.CompareTag("stairs"))
        {
            step = true;
        }
        if (OBJ.gameObject.CompareTag("Life"))
        {
            GM.lastCheckPointPos = OBJ.transform.position;
            Plattering = ("Shroom!");
            ChangeSpeech = 1;
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
            var skip = OBJ.gameObject.GetComponent<Trader>().skipPressed;
            if (skip ==false)
            {
                ShowCursor();
                isAtTrader = true;
                FreeLook.enabled = false;
                CamForTraders.enabled = true;
                CamForTraders.m_LookAt = OBJ.transform;
            }
            if (skip ==true)
            {
                isAtTrader = false;
                CamForTraders.enabled = false;
                CamForTraders.m_LookAt = null;
                FreeLook.enabled = true;
                FreeLook.m_LookAt.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Root.position), 0.5f);
            }
        }

        if (OBJ.gameObject.CompareTag("House"))
        {
            FreeLook.m_Orbits[0].m_Radius = 1f;
            FreeLook.m_Orbits[1].m_Radius = 2f;
            FreeLook.m_Orbits[2].m_Radius =1f;
        }
        if (OBJ.gameObject.CompareTag("What Is this?"))
        {
            Plattering = "Ah shit. what happened here?";
        }
    }
    public void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("stairs"))
        {
            step = false;
        }
        if (OBJ.gameObject.CompareTag("Tile"))
        {
            grounded = false;
            OnPlatform = false;
            Player.transform.parent = null;
            Player.transform.localScale = new(1, 1, 1);
        }

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
        if (OBJ.gameObject.CompareTag("House"))
        {
            FreeLook.m_Orbits[0].m_Radius = 4;
            FreeLook.m_Orbits[1].m_Radius = 6;
            FreeLook.m_Orbits[2].m_Radius = 5;
            //FreeLook.m_Lens.FieldOfView = 25;
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
            WhichAttack = !WhichAttack;
            DefendAnim = 0.3f;
            Defend = true;
            CurrentStamina -= Damage;
            HealthBar.SetStamina(CurrentStamina);
        }
    }
    public void PlayerMove(Vector3 Direction)
    {
        rotGoal = Quaternion.LookRotation(new Vector3(Direction.x, 0, Direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);

        Otter.SetBool("walk", true);

        if (step==true)
        {
            Otter.SetBool("climb", true);
        }
        else
        {
            Otter.SetBool("climb", false);
        }

        if (grounded == true)
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.anyKey && Rolling == false)
            {
                if (ArmorEquipped==true)
                {
                        Otter.SetBool("armor", true);
                }
                if (ArmorEquipped == false)
                {
                    Otter.SetBool("armor", false);
                }
                if (step==false)
                {
                    Otter.SetBool("run", true);
                    speed = Run;
                    steer = 0.12f;
                }

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
    public void PlayerRoll(Vector3 Direction)
    {
        rotGoal = Quaternion.LookRotation(new Vector3(Direction.x, 0, Direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.11f);
        //Evading Roll action
        {
            if (Input.GetKey(KeyCode.LeftControl) && CurrentStamina > 0 && grounded == true)
            {
                Rolling = true;
                Otter.SetBool("roll", true);
                CurrentStamina -= 0.1f;
                speed = 7;
                HealthBar.SetStamina(CurrentStamina);
                Player.velocity = (Direction.normalized * 6) + new Vector3(0, Player.velocity.y, 0);
            }
            if (!Input.GetKey(KeyCode.LeftControl) || CurrentStamina <= 0 || grounded == false)
            {
                Rolling = false;
                Otter.SetBool("roll", false);
            }

        }
    }
    public void RotateForward()
    {
        var CamForward = Camera.main.transform.TransformDirection(Vector3.forward);
        rotGoal = Quaternion.LookRotation(CamForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
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
        gobletOBJ.SetActive(true);
        GobletPicked = true;
        AnimSpeed = 3;
        Walk = 7;
        Run = 18;
        JumpLimit = GobletJumpLimit;
        AirBeat /= 5;
        ElectricEffect.SetActive(true);
        GobletClock = 10f;
        GobletPickup--;
    }
    public void GobletOFF()
    {
        GobletPicked = false;
        AnimSpeed = 1;
        Walk = InsertWalk;
        Run = InsertRun;
        JumpLimit = GobletJumpLimit-2;
        //BeatGrounded = 5 * BeatGrounded;
        AirBeat = 5 * AirBeat;
        ElectricEffect.SetActive(false);
        GobletClock = 10F;

    }
    public void ParryON()
    {
        if (Defend == false)
        {
            if (HammerHeld == false)
            {
                Otter.SetBool("Parry", true);
                Otter.SetBool("HammerParry", false);
            }
            if (HammerHeld == true)
            {
                Otter.SetBool("HammerParry", true);
                Otter.SetBool("Parry", false);
                Otter.SetBool("shieldParry", false);
            }
            if (ArmorEquipped == true)
            {
                Otter.SetBool("shieldParry", true);
                Otter.SetBool("Parry", false);
                Otter.SetBool("HammerParry", false);
            }
        }
        isParried = true;
    }
    public void ParryOFF()
    {
        Otter.SetBool("Parry", false);
        Otter.SetBool("HammerParry", false);
        Otter.SetBool("shieldParry", false);
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
        Physics.IgnoreLayerCollision(gameObject.layer, 7, true);
    }
    public void GoldOFF()
    {
        GoldPicked = false;
        GoldBrick.SetActive(false);
        Physics.IgnoreLayerCollision(gameObject.layer, 7, false);
    }

    public void Start()
    {
        arrowModel.SetActive(false);
        //playerCollider = GetComponent<CapsuleCollider>();
        CamForTraders.enabled = false;
        //InHouseCam.enabled = false;
        Instantiate(PopUpEffect, Root.position, Quaternion.identity);
        HideCursor();
        AimIcon.SetActive(false);
        Player = GetComponent<Rigidbody>();
        GM = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        if(seekMusic==true)
        {
            Music = GameObject.Find("GameMusic").GetComponent<MusicPlaylist>();
            Music.transform.parent = Player.transform;
            Music.transform.position = new Vector3(0, 0, 0);
        }
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
        StaminaClock = StaminaClockInitial;
        JumpLimit = JumpNum;
        GobletJumpLimit = JumpNum + 2;
        FallClock = InitialFall;
        InsertWalk = Walk;
        InsertRun = Run;
        LooseScreen.SetActive(false);
        HologramedBridge.SetActive(false);
        appleOBJ.SetActive(false);
        gobletOBJ.SetActive(false);
        for (int i = 0; i < ArmorSet.Length; i++)
        {
            ArmorSet[i].SetActive(false);
        }
        for (int i = 0; i < Bow.Length; i++)
        {
            Bow[i].SetActive(false);
        }
        FreeLook.m_Orbits[0].m_Radius = 4;
        FreeLook.m_Orbits[1].m_Radius = 6;
        FreeLook.m_Orbits[2].m_Radius = 5;
    }
    [System.Obsolete]
    public void Update()
    {
        ////Climb on stairs/obsticle
        //step = obsticle.step;
        //if (step == true)
        //{
        //    Otter.SetBool("climb", true);
        //}
        //if (step == false)
        //{
        //    Otter.SetBool("climb", false);
        //}

        //Parry animations
        if (Defend == true)
        {
            DefendAnim -= Time.deltaTime;
            if (DefendAnim > 0)
            {
                if (WhichAttack == true)
                    Otter.Play("AttackA");
                if (WhichAttack == false)
                    Otter.Play("AttackB");
            }
            else
            {
                Defend = false;
                Otter.StopPlayback();
            }

        }

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
            if (ArmorEquipped==false)
            {
                CurrentStamina -= 0.05f;
            }
            if (ArmorEquipped==true)
            {
                CurrentStamina -= 0.001f;
            }
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
        else
        {
            GroundAttack = 50;
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
            if (Input.GetKey(KeyCode.LeftControl)&& FreeLook.m_Lens.FieldOfView<20)
            {
                FreeLook.m_LookAt = Face;
            }
            //else
            //{
            //    FreeLook.m_LookAt = Root;
            //}

            if (Input.GetKey(KeyCode.LeftControl) && grounded == true && Rolling == false && !Input.GetKey(KeyCode.Space))
            {
                if (Load.i == 9)
                    HologramedBridge.SetActive(true);
                if (!Input.GetKey(KeyCode.W) || !Input.GetKey(KeyCode.Mouse0))
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
                    Physics.IgnoreLayerCollision(gameObject.layer, 7, true);
                }
            }
            else
            {
                Physics.IgnoreLayerCollision(gameObject.layer, 7, false);
                HologramedBridge.SetActive(false);
                Otter.SetBool("crouch", false);
                ParryOFF();
                HealingText = "";
            }

        }

        //Browse Arsenal
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (ArsenalCounter==0)
                {
                    Sound.Error();
                    Plattering = ("Damn, I don't have any arsenal yet.");
                    ChangeSpeech = 1;
                }
                else
                {
                    Sound.SwitchItem();
                    if (arsenalBrowser < ArsenalCounter)
                    {
                        arsenalBrowser++;
                    }
                    else
                    {
                        arsenalBrowser = 0;
                    }

                    switch (Arsenal[arsenalBrowser])
                    {
                        case "Bare Hands":
                            {
                                Otter.Play("Disarm");
                                //Turn off Hammers
                                HammerHeld = false;
                                Otter.SetBool("armor", false);
                                RightHandWeapon.SetActive(false);
                                LeftHandWeapon.SetActive(false);

                                //Turn off Bow Gear
                                bowEquipped = false;
                                for (int i = 0; i < Bow.Length; i++)
                                {
                                    Bow[i].SetActive(false);
                                }

                                //Turn off Armor Set
                                ArmorEquipped = false;
                                for (int item = 0; item < ArmorSet.Length; item++)
                                {
                                    ArmorSet[item].SetActive(false);
                                }

                                break;
                            }

                        case "Hammers":
                            {
                                Otter.Play("Equip");
                                //Turn on Hammers
                                HammerHeld = true;
                                Otter.SetBool("armor", false);
                                RightHandWeapon.SetActive(true);
                                LeftHandWeapon.SetActive(true);

                                //Turn off Bow
                                bowEquipped = false;
                                for (int i = 0; i < Bow.Length; i++)
                                {
                                    Bow[i].SetActive(false);
                                }

                                //Turn off Armor Set
                                ArmorEquipped = false;
                                for (int item = 0; item < ArmorSet.Length; item++)
                                {
                                    ArmorSet[item].SetActive(false);
                                }

                                break;
                            }

                        case "Bow":
                            {
                                Otter.Play("Equip");
                                //Turn off Hammers
                                HammerHeld = false;
                                Otter.SetBool("armor", false);
                                RightHandWeapon.SetActive(false);
                                LeftHandWeapon.SetActive(false);

                                //Turn on Bow
                                bowEquipped = true;
                                for (int i = 0; i < Bow.Length; i++)
                                {
                                    Bow[i].SetActive(true);
                                }

                                //Turn off Armor Set
                                ArmorEquipped = false;
                                for (int item = 0; item < ArmorSet.Length; item++)
                                {
                                    ArmorSet[item].SetActive(false);
                                }

                                break;
                            }
                        case "ArmorSet":
                            {
                                Otter.Play("Equip");
                                //Turn off Hammers
                                HammerHeld = false;
                                Otter.SetBool("armor", false);
                                RightHandWeapon.SetActive(false);
                                LeftHandWeapon.SetActive(false);

                                //Turn off Bow
                                bowEquipped = false;
                                for (int i = 0; i < Bow.Length; i++)
                                {
                                    Bow[i].SetActive(false);
                                }

                                //Turn on Armor Set
                                ArmorEquipped = true;
                                Otter.SetBool("armor", true);
                                for (int item = 0; item < ArmorSet.Length; item++)
                                {
                                    ArmorSet[item].SetActive(true);
                                }
                                break;
                            }

                    }

                }
            }
        }
        //Bow action
        if (bowEquipped == true)
        {
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Sound.ArrowDraw();
            }

            if (Input.GetKey(KeyCode.Mouse1) && CurrentStamina > 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
                {
                    AimIcon.SetActive(true);
                    RotateForward();
                }
                arrowModel.SetActive(true);
                bowString.SetActive(false);
                Otter.SetBool("draw", true);
                stringLine.enabled = true;
                stringLine.useWorldSpace = false;
            }
            if (Input.GetKeyUp(KeyCode.Mouse1) && CurrentStamina > 0)
            {
                Sound.ArrowShoot();
                var arrow = Instantiate(Arrow, AttackPoint.position + new Vector3(0, 0.6f, 0), rotGoal * Quaternion.Euler(90, 0, 0));
                CurrentStamina -= 30;
                HealthBar.SetStamina(CurrentStamina);
                arrowModel.SetActive(false);
                stringLine.enabled = false;
                bowString.SetActive(true);
                Otter.SetBool("draw", false);
            }
        }
        //Stoning action
        {
            if (Input.GetKey(KeyCode.Mouse1) && CurrentStamina > 0 && !Input.GetKey(KeyCode.LeftControl) && bowEquipped == false)
            {
                Stone.SetActive(true);
                AimIcon.SetActive(true);
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    Otter.Play("Crouch");
                }
                if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
                {
                    RotateForward();
                }
            }
            if (Input.GetKeyUp(KeyCode.Mouse1) && Stone.active && CurrentStamina > 0)
            {
                Otter.SetBool("fight", false);
                Otter.SetBool("slash", false);
                Otter.Play("Throw");
                Instantiate(Ball, AttackPoint.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
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
        }
        //Eat Apple
        {
            if (Input.GetKeyDown(KeyCode.T) && Apple > 0)
            {
                appleOBJ.SetActive(true);
                Otter.Play("Consume");
                Sound.Eat();
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
            if (Input.GetKeyUp(KeyCode.T))
            {
                appleOBJ.SetActive(false);
            }
        }
        //Use Goblet
        {
            if (Input.GetKeyDown(KeyCode.Y) && GobletPickup > 0)
            {
                Otter.Play("Consume");
                Sound.Drink();
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

            if (Input.GetKeyUp(KeyCode.Y))
            {
                gobletOBJ.SetActive(false);
            }
        }

        if (isAtTrader == false /*&& ParryShield.active == false*/)
        {
            //Melee action
            {
                if (Input.GetKey(KeyCode.Mouse0) && CurrentStamina > 0)
                {
                    if (ArmorEquipped==false && HammerHeld==false)
                    {
                        CurrentStamina -= 0.03f;
                    }
                    if (ArmorEquipped==true)
                    {
                        if (grounded==true)
                        {
                            CurrentStamina -= 0.2f;
                        }
                        if (grounded==false)
                        {
                            CurrentStamina -= 0.5f;
                        }
                    }
                    if (HammerHeld==true)
                    {
                        CurrentStamina -= 0.1f;
                    }
                    HealthBar.SetStamina(CurrentStamina);
                    if (ArmorEquipped==false)
                    {
                        Otter.SetBool("fight", true); //Airkick leveitation without sword
                        Otter.SetBool("slash", false);
                    }
                    if (ArmorEquipped == true)
                    {
                        Otter.SetBool("fight", false); 
                        Otter.SetBool("slash", true);//Airkick leveitation with sword
                    }

                    if (grounded == false)
                    {
                        if (Otter.GetCurrentAnimatorStateInfo(0).IsName("Air Kick"))
                        {
                            var WindTrail = Instantiate(KickWind, KickEffectPos.position, Quaternion.Euler(-90, UnityEngine.Random.Range(0f, 360f), 0));
                            WindTrail.transform.parent = KickEffectPos;
                        }
                        if (Otter.GetCurrentAnimatorStateInfo(0).IsName("HuricaneSword"))
                        {
                            var SwordTrail = Instantiate(SwordCopter, KickEffectPos.position +new Vector3(0,0.5f,0), Quaternion.Euler(-90, UnityEngine.Random.Range(0f, 360f), 0));
                            SwordTrail.transform.parent = KickEffectPos;
                        }
                        //BeatAir -= Time.deltaTime;
                        //if (BeatAir <= 0)
                        //{
                        //    //Attack();
                        //}
                        InitiateAir -= Time.deltaTime;
                        if (InitiateAir <= 0)
                        {
                            if (Otter.speed > 0.4)
                            {
                                if (ArmorEquipped==false)
                                {
                                    levitation -= 0.05f;
                                    Otter.speed -= 0.005f;
                                    Player.AddForce(0, levitation, 0);
                                }
                                if (ArmorEquipped==true)
                                {
                                    Player.useGravity = false;
                                }

                            }
                            else
                            {
                                Player.useGravity = true;
                                Otter.SetBool("fight", false);
                            }
                        }
                    }
                    if (grounded == true)
                    {
                        if (ArmorEquipped == false)
                        {
                            Otter.SetBool("fight", true);
                            Otter.SetBool("slash", false);
                        }
                        if (ArmorEquipped == true)
                        {
                            Otter.SetBool("fight", false);
                            Otter.SetBool("slash", true);
                        }
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
                        //BeatGrounded -= Time.deltaTime;
                        //if (BeatGrounded <= 0)
                        //{
                        //    //Attack();
                        //}
                    }

                }
                else
                {
                    Player.useGravity = true;
                    Otter.SetBool("slash", false);
                    Otter.SetBool("fight", false);
                    InitiateAir = 0.5f;
                    GroundAttack = 50;
                    Beat = 0;
                    Otter.speed = AnimSpeed;
                }
            }
            //Parry Action
            {
                if (Input.GetKey(KeyCode.F) && CurrentStamina > 0 && !Input.GetKey(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1))
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
        Vector3 XZForward = new(cameraRelativeForward.x, 0, cameraRelativeForward.z);
        Vector3 XZBack = new(cameraRelativeBack.x, 0, cameraRelativeBack.z);
        Vector3 XZRight = new(cameraRelativeRight.x, 0, cameraRelativeRight.z);
        Vector3 XZLeft = new(cameraRelativeLeft.x, 0, cameraRelativeLeft.z);
        //Vector3 movement = new Vector3(horizontalInput, 0, verticalInput) * moveSpeed * Time.deltaTime;
        //transform.Translate(movement);

        Otter.SetBool("walk", false);
        Otter.SetBool("turn right", false);
        Otter.SetBool("turn left", false);
        Otter.SetBool("climb", false);
        Otter.SetBool("run", false);
        Otter.SetBool("midair", false);
        Otter.SetBool("roll", false);
        Rolling = false;
        
        //Switch off hurt and heal effects automaticly
        {
            if (hurt == true || heal == true)
            {
                StopHurt += Time.deltaTime;
                if (StopHurt >= 0.2f)
                {
                    hurt = false;
                    heal = false;
                }
            }
            else
            {
                StopHurt = 0;
            }
            if (CurrentHealth >= MaxHealth)
            {
                heal = false;
            }
        }

        if (grounded == false || speed == Run)
        {
            Player.drag = 0;
        }
        else
        {
            Player.drag = 0.8f;
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
                    //Otter.Play("Walk Right");
                    PlayerMove(XZRight);
                }
                if (Input.GetKey(KeyCode.A))
                {
                    //Otter.Play("Walk Left");
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
            if (Input.GetKey(KeyCode.LeftControl) && CurrentStamina > 0)
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

        //if (Input.GetKey(KeyCode.J))
        //{
        //    Sound.SwordSwing1();
        //}
        //if (Input.GetKey(KeyCode.K))
        //{
        //    Sound.SwordSwing2();
        //}
        //if (Input.GetKey(KeyCode.L))
        //{
        //    Sound.SwordSwing3();
        //}

    }

}

