using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using UnityEngine;

public class Behaviour : MonoBehaviour, IDamageable
{
    [Header("Config")]
    [SerializeField] HazardDamageConfig hazardDamageConfig;

    [Header("Movement and animation")]
    public Rigidbody Player;
    public bool grounded;
    public Carry Load;
    public bool movementInvoked = false;
    public Quaternion rotGoal;
    public bool keepLooking = false;
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
    public Transform Spine;
    public Transform Hips;
    public Animator Otter;
    [SerializeField] AnimatedAttack otterAction;
    public bool OnPlatform = false;

    [Header("Health")]
    float StopHurt = 0;
    [SerializeField] Rigidbody rb;
    [SerializeField] PlayerState state = new PlayerState();
    public PlayerState State
    {
        get
        {
            if (state == null)
            {
                state = new PlayerState();
            }

            return state;
        }
    }
    public float MaxHealth { get => State.maxHealth; set => State.maxHealth = value; }
    public float CurrentHealth { get => State.currentHealth; set => State.currentHealth = value; }
    public float MaxStamina { get => State.maxStamina; set => State.maxStamina = value; }
    public float CurrentStamina { get => State.currentStamina; set => State.currentStamina = value; }
    readonly float StaminaClockInitial = 0.5f;
    float StaminaClock;
    public Health_Bar_Script HealthBar;
    public bool heal;
    public bool TouchShroom;
    public bool hurt;
    public bool Rolling;
    public int Lives { get => State.lives; set => State.lives = value; }
    public GameObject ICON_1;
    public GameObject ICON_2;
    public GameObject ICON_3;
    [SerializeField] GameObject appleOBJ;
    [SerializeField] GameObject gobletOBJ;

    [Header("Combat")]
    [SerializeField] Vector3 bowAim;
    public List<String> Arsenal;
    public int arsenalBrowser = 0;
    public int ArsenalCounter = 0;
    public GameObject Arrow;
    bool arrowReady = false;
    public int arrowMunition = 0;
    [SerializeField] GameObject[] Arrows;
    [SerializeField] GameObject arrowModel;
    [SerializeField] LineRenderer stringLine;
    public GameObject[] Bow;
    public GameObject bowString;
    public GameObject Stone;
    public float Beat = 0;
    public Projectile Ball;
    public Transform AttackPoint;
    //public Transform Sphere;
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
    public GameObject ArrowPickup;
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
    public string ArrowText;


    [Header("Chat")]
    public string Plattering;

    [Header("Audio & Effects")]
    public bool seekMusic;
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
    [SerializeField] GameObject MunitionDisplay;
    public float moveSpeed = 5.0f;
    public GameObject LooseScreen;
    [SerializeField] LoseMenuController loseMenuController;
    public bool isAtTrader = false;
    public string LogCount;
    public string DebugText;
    public string StaminaText;
    public string HealingText;
    public string AppleText;
    public int GobletPickup { get => State.gobletPickup; set => State.gobletPickup = value; }
    public int Apple { get => State.apple; set => State.apple = value; }
    public int Currency { get => State.currency; set => State.currency = value; }
    public int NutCount { get => State.nutCount; set => State.nutCount = value; }
    public string SeedText;
    public string Wallet;
    public GameObject AimIcon;
    public CinemachineFreeLook FreeLook;
    public CinemachineFreeLook CamForTraders;
    public float ChangeSpeech = 1F;
    GameInputReader inputReader;
    PlayerHealthController healthController;
    PlayerInventory inventory;
    PlayerInteractionController interactionController;
    PlayerCombatController combatController;
    PlayerMovementController movementController;
    PlayerFailureController failureController;


    PlayerHealthController Health
    {
        get
        {
            if (healthController == null)
            {
                healthController = GetComponent<PlayerHealthController>();
                if (healthController == null)
                {
                    healthController = gameObject.AddComponent<PlayerHealthController>();
                }
            }

            return healthController;
        }
    }

    PlayerInventory Inventory
    {
        get
        {
            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
                if (inventory == null)
                {
                    inventory = gameObject.AddComponent<PlayerInventory>();
                }
                inventory.Initialize(this);
            }

            return inventory;
        }
    }

    PlayerInteractionController Interaction
    {
        get
        {
            if (interactionController == null)
            {
                interactionController = GetComponent<PlayerInteractionController>();
                if (interactionController == null)
                {
                    interactionController = gameObject.AddComponent<PlayerInteractionController>();
                }
                interactionController.Initialize(this, inputReader);
            }

            return interactionController;
        }
    }

    PlayerCombatController Combat
    {
        get
        {
            if (combatController == null)
            {
                combatController = GetComponent<PlayerCombatController>();
                if (combatController == null)
                {
                    combatController = gameObject.AddComponent<PlayerCombatController>();
                }
                combatController.Initialize(this);
            }

            return combatController;
        }
    }

    PlayerMovementController Movement
    {
        get
        {
            if (movementController == null)
            {
                movementController = GetComponent<PlayerMovementController>();
                if (movementController == null)
                {
                    movementController = gameObject.AddComponent<PlayerMovementController>();
                }
                movementController.Initialize(this);
            }

            return movementController;
        }
    }

    PlayerFailureController Failure
    {
        get
        {
            if (failureController == null)
            {
                failureController = GetComponent<PlayerFailureController>();
                if (failureController == null)
                {
                    failureController = gameObject.AddComponent<PlayerFailureController>();
                }
                failureController.Initialize(this);
            }

            return failureController;
        }
    }

    public void ToggleParryCounterAttack() => WhichAttack = !WhichAttack;
    public void SetAppleModelActive(bool active) => appleOBJ.SetActive(active);

    public void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Part")&& Load.CanCarry == true && grounded==true && !Input.GetKey(KeyCode.Mouse0)&& !Input.GetKey(KeyCode.Mouse1))
        {
            Otter.Play("Crouch");
            Sound.PickItem();
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
            Destroy(OBJ.gameObject);
        }
        if ( OBJ.gameObject.CompareTag("Seed") || OBJ.gameObject.CompareTag("Apple") || OBJ.gameObject.CompareTag("GobletKey"))
        {
            Otter.Play("Crouch");
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
            Destroy(OBJ.gameObject);

            if (OBJ.gameObject.CompareTag("Seed"))
            {
                Inventory.AddSeed();
                Sound.PickItem();
            }
            if (OBJ.gameObject.CompareTag("Apple"))
            {
                Sound.PickUp2();
                Inventory.AddApple();
            }
            if (OBJ.gameObject.CompareTag("GobletKey"))
            {
                Sound.PickUp2();
                Inventory.AddGoblet();
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
        if (arrowMunition<Arrows.Length && bowEquipped==true)
        {
            if (OBJ.gameObject.CompareTag("Arrow"))
            {
                Otter.Play("Crouch");
                Sound.PickUp2();
                arrowMunition++;
                Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
                if (bowEquipped == true)
                {
                    CountArrows();
                }
                Destroy(OBJ.gameObject);
            }
            if (OBJ.gameObject.CompareTag("ArrowBundle"))
            {
                Otter.Play("Crouch");
                Sound.PickUp2();
                Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
                if (arrowMunition + 10 < Arrows.Length)
                {
                    arrowMunition += 10;
                }
                else
                {
                    for (int i = 0; i <(arrowMunition-10); i++)
                    {
                        Instantiate(ArrowPickup, OBJ.transform.position+new Vector3(0,i* 0.3f, 0), Quaternion.Euler(0,0,90));
                    }
                    arrowMunition = Arrows.Length;
                }
                if (bowEquipped == true)
                {
                    CountArrows();
                }
                Destroy(OBJ.gameObject);
            }

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
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
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
            arrowMunition = 5;
            CountArrows();
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
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
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
            Destroy(OBJ.gameObject);
        }
        if (OBJ.gameObject.CompareTag("Honey") && Honeypicked == false)
        {
            Otter.Play("Crouch");
            HoneyON();
            Sound.PickItem();
            Instantiate(PickUpEffect, OBJ.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
            Destroy(OBJ.gameObject);
        }
        if (OBJ.gameObject.CompareTag("Gold") && GoldPicked == false)
        {
            Otter.Play("Crouch");
            GoldON();
            Destroy(OBJ.gameObject);
            
        }
        if (OBJ.gameObject.CompareTag("NPC") && isParried == true 
            || OBJ.gameObject.CompareTag("Scorpion") && isParried == true 
            || OBJ.gameObject.CompareTag("Scorpion") && Input.GetKey(KeyCode.Mouse0) 
            || OBJ.gameObject.CompareTag("Scorpion") && Input.GetKey(KeyCode.Mouse1))
        {
            Plattering = "Get off me ya nasty bastards!";
            ChangeSpeech = 3;
            var ParryDirection = new Vector3((OBJ.transform.position - transform.position).x, 0, (OBJ.transform.position - transform.position).z);
            transform.rotation = Quaternion.LookRotation(ParryDirection);
        }
        if (OBJ.gameObject.CompareTag("Strike"))
        {
            HandleFailure(PlayerFailureReason.Strike);
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
    float ScorpionJawDamage => hazardDamageConfig != null ? hazardDamageConfig.scorpionJawClampDamage : 15f;
    float ScorpionStingDamage => hazardDamageConfig != null ? hazardDamageConfig.scorpionStingDamage : 30f;

    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("SwitchMusic"))
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
            State.checkpointMessageUntil = Time.time + 3f;
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
                    if (CombatDebugGate.AllowsDamage(gameObject))
                    {
                        TakeDamage(ScorpionJawDamage);
                        if (CurrentHealth <= 0)
                        {
                            HandleFailure(PlayerFailureReason.BossDamage);
                        }
                    }
                }
            }
            else
            {

            }
        }
        if (OBJ.gameObject.CompareTag("ScorpionSting"))
        {
            if (scorpAttack == true && isParried==false)
            {
                if (CombatDebugGate.AllowsDamage(gameObject))
                {
                    TakeDamage(ScorpionStingDamage);
                    if (CurrentHealth <= 0)
                    {
                        HandleFailure(PlayerFailureReason.BossDamage);
                    }
                }
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
            SaveCheckpoint(OBJ.transform.position);
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
                Interaction.EnterTrader(OBJ);
            }
            if (skip ==true)
            {
                Interaction.ExitTrader();
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
            Interaction.ExitTrader();
        }
        if (OBJ.gameObject.CompareTag("House"))
        {
            FreeLook.m_Orbits[0].m_Radius = 4;
            FreeLook.m_Orbits[1].m_Radius = 6;
            FreeLook.m_Orbits[2].m_Radius = 5;
            //FreeLook.m_Lens.FieldOfView = 25;
        }
        if (OBJ.gameObject.CompareTag("Scorpion"))
        {
            scorpAttack = false;
        }
    }
    public void TakeDamage(float Damage) => TakeDamage(new DamageEvent { Amount = Damage, Source = null, Point = transform.position, Type = DamageType.Generic, CanStun = false });
    public void TakeDamage(DamageEvent damageEvent) => Health.TakeDamage(damageEvent.Amount);
    public void HandleFailure(PlayerFailureReason reason) => Failure.HandleFailure(reason);
    public void PlayerMove(Vector3 Direction)
    {
        movementInvoked = true;
        //Regular walk
        if (keepLooking==false)
        {
            rotGoal = Quaternion.LookRotation(Direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            Otter.SetBool("walk", true);
        }
        //Strafe
        if (keepLooking == true)
        {
            Vector3 forwardFace = Camera.main.transform.TransformDirection(Vector3.forward);
            rotGoal = Quaternion.LookRotation(new Vector3(forwardFace.x, 0, forwardFace.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            Otter.SetBool("walk", false);
            if (Input.GetKey(KeyCode.W))
            {
                Otter.SetBool("strafeForward", true);
            }
            if (Input.GetKey(KeyCode.S))
            {
                Otter.SetBool("strafeBack", true);
            }
            if (Input.GetKey(KeyCode.A))
            {
                Otter.SetBool("strafeLeft", true);
            }
            if (Input.GetKey(KeyCode.D))
            {
                Otter.SetBool("strafeRight", true);
            }
        }
        //Stairs
        if (step==true)
        {
            Otter.SetBool("climb", true);
        }
        else
        {
            Otter.SetBool("climb", false);
        }
        //Sprint
        if (grounded == true)
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.anyKey && Rolling == false && keepLooking==false)
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

    [Obsolete]
    public void RotateForward()
    {
        Vector3 camForward = Camera.main.transform.TransformDirection(Vector3.forward);
        Quaternion direction = Quaternion.LookRotation(camForward);
        if (bowEquipped==false)
        {
            Spine.rotation = direction;
        }
        if (bowEquipped == true && Input.GetKey(KeyCode.Mouse1) && arrowMunition > 0)
        {
            Spine.rotation = direction*Quaternion.EulerRotation(bowAim);
        }
  
    }
    public void CountArrows()
    {
        var i = 0;
        foreach (var arrow in Arrows)
        {
            if (i < arrowMunition )
            {
                arrow.SetActive(true);
            }
            else
            {
                arrow.SetActive(false);
            }
            i++;
        }
    }
    public void ShowCursor() => Interaction.ShowCursor();
    public void HideCursor() => Interaction.HideCursor();
    public void ActivateLooseMenu()
    {
        //MusicOP.StopMusic();
        GameFlowController.GetOrCreate().SetGameOver();
        ShowCursor();
        ShowLosePanel();
        GetInputReader().DisableGameplayInput();
    }
    public void HideLooseMenu()
    {
        //MusicOP.ResumeMusic();
        GameFlowController.GetOrCreate().SetPlaying();
        HideCursor();
        GetInputReader().EnableGameplayInput();
        HideLosePanel();
    }

    void ShowLosePanel()
    {
        if (loseMenuController == null && LooseScreen != null)
        {
            loseMenuController = LooseScreen.GetComponentInParent<LoseMenuController>();
        }

        if (loseMenuController != null)
        {
            loseMenuController.ShowLosePanel();
            return;
        }

        if (LooseScreen != null)
        {
            LooseScreen.SetActive(true);
        }
    }

    void HideLosePanel()
    {
        if (loseMenuController == null && LooseScreen != null)
        {
            loseMenuController = LooseScreen.GetComponentInParent<LoseMenuController>();
        }

        if (loseMenuController != null)
        {
            loseMenuController.HideLosePanel();
            return;
        }

        if (LooseScreen != null)
        {
            LooseScreen.SetActive(false);
        }
    }

    public void RestartCheckpoint() => HandleFailure(PlayerFailureReason.CompatibilityRestart);

    public void RestoreHealth()
    {
        CurrentHealth = MaxHealth;
        HealthBar.SetHealth(CurrentHealth);
    }

    public void MoveToCheckpoint()
    {
        transform.position = CheckpointService.GetOrCreate().LastCheckpointPosition;
        Instantiate(PopUpEffect, transform.position, Quaternion.identity);
    }
    void SaveCheckpoint(Vector3 position)
    {
        State.checkpointPosition = position;
        CheckpointService.GetOrCreate().SaveCheckpoint(position);
    }

    public void RestartGame()
    {
        SceneTransitionService.ReloadActiveScene();
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
    GameInputReader GetInputReader()
    {
        if (inputReader == null)
        {
            inputReader = GameInputReader.GetOrCreate();
        }

        return inputReader;
    }

    bool IsGameplayInputBlocked()
    {
        var reader = GetInputReader();
        return reader != null && !reader.IsGameplayInputEnabled;
    }

    void HandleDeathState() => Health.HandleDeathState();

    bool ValidateStartReferences()
    {
        Player = GetComponent<Rigidbody>();

        bool valid = RuntimeReferenceValidator.Require(Player, this, nameof(Player)) &
            RuntimeReferenceValidator.Require(Load, this, nameof(Load)) &
            RuntimeReferenceValidator.Require(Root, this, nameof(Root)) &
            RuntimeReferenceValidator.Require(Otter, this, nameof(Otter)) &
            RuntimeReferenceValidator.Require(otterAction, this, nameof(otterAction)) &
            RuntimeReferenceValidator.Require(arrowModel, this, nameof(arrowModel)) &
            RuntimeReferenceValidator.Require(HealthBar, this, nameof(HealthBar)) &
            RuntimeReferenceValidator.Require(HoneyJar, this, nameof(HoneyJar)) &
            RuntimeReferenceValidator.Require(GoldBrick, this, nameof(GoldBrick)) &
            RuntimeReferenceValidator.Require(PopUpEffect, this, nameof(PopUpEffect)) &
            RuntimeReferenceValidator.Require(HealEffect, this, nameof(HealEffect)) &
            RuntimeReferenceValidator.Require(ElectricEffect, this, nameof(ElectricEffect)) &
            RuntimeReferenceValidator.Require(MunitionDisplay, this, nameof(MunitionDisplay)) &
            RuntimeReferenceValidator.Require(LooseScreen, this, nameof(LooseScreen)) &
            RuntimeReferenceValidator.Require(AimIcon, this, nameof(AimIcon)) &
            RuntimeReferenceValidator.Require(FreeLook, this, nameof(FreeLook)) &
            RuntimeReferenceValidator.Require(CamForTraders, this, nameof(CamForTraders)) &
            RuntimeReferenceValidator.Require(HologramedBridge, this, nameof(HologramedBridge)) &
            RuntimeReferenceValidator.Require(appleOBJ, this, nameof(appleOBJ)) &
            RuntimeReferenceValidator.Require(gobletOBJ, this, nameof(gobletOBJ));

        if (ArmorSet != null)
        {
            for (int i = 0; i < ArmorSet.Length; i++)
            {
                valid &= RuntimeReferenceValidator.Require(ArmorSet[i], this, $"{nameof(ArmorSet)}[{i}]");
            }
        }

        if (Bow != null)
        {
            for (int i = 0; i < Bow.Length; i++)
            {
                valid &= RuntimeReferenceValidator.Require(Bow[i], this, $"{nameof(Bow)}[{i}]");
            }
        }

        return valid;
    }

    public void Start()
    {
        if (!ValidateStartReferences())
        {
            return;
        }

        inputReader = GameInputReader.GetOrCreate();
        inputReader.EnableGameplayInput();
        Inventory.Initialize(this);
        Interaction.Initialize(this, inputReader);
        Combat.Initialize(this);
        Movement.Initialize(this);
        Failure.Initialize(this);
        arrowModel.SetActive(false);
        bowAim = new Vector3(-0.33f, 20f, -0.3f);
        CamForTraders.enabled = false;
        Instantiate(PopUpEffect, Root.position, Quaternion.identity);
        HideCursor();
        SaveCheckpoint(transform.position);
        AimIcon.SetActive(false);
        MunitionDisplay.SetActive(false);
        //Enable/Disable Background music
        if (seekMusic == true)
        {
            var musicObject = GameObject.Find("GameMusic");
            if (!RuntimeReferenceValidator.Require(musicObject, this, "GameMusic"))
            {
                return;
            }

            Music = musicObject.GetComponent<MusicPlaylist>();
            if (!RuntimeReferenceValidator.Require(Music, this, nameof(Music)))
            {
                return;
            }

            Music.transform.parent = Player.transform;
            Music.transform.position = new Vector3(0, 0, 0);
            var MusicSwitches = GameObject.FindGameObjectsWithTag("SwitchMusic");
            foreach (var item in MusicSwitches)
            {
                item.SetActive(true);
            }

        }
        else
        {
            Music = null;
            var MusicSwitches = GameObject.FindGameObjectsWithTag("SwitchMusic");
            foreach (var item in MusicSwitches)
            {
                item.SetActive(false);
            }
        }
        Health.Initialize(this);
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
        HideLosePanel();
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
        HandleDeathState();
        if (IsGameplayInputBlocked())
        {
            return;
        }

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

        //Turn off glow attack effect
        if (!Input.GetKey(KeyCode.Mouse0))
        {
            otterAction.TurnOffGlow();
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
        Health.TickStamina();
        //Update UI
        {
            State.arrowMunition = arrowMunition;
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
        HandleDeathState();
        //Limit Stamina decrease down to 0 only
        Health.ClampStaminaAndParry();
        //Crouch action - Place Logs
        {
            if (Input.GetKey(KeyCode.LeftControl)&& FreeLook.m_Lens.FieldOfView<20)
            {
                FreeLook.m_LookAt = Face;
            }

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
                                GroundAttack = 50;
                                Otter.SetBool("armor", false);
                                RightHandWeapon.SetActive(false);
                                LeftHandWeapon.SetActive(false);

                                //Turn off Bow Gear
                                bowEquipped = false;
                                MunitionDisplay.SetActive(false);
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
                                GroundAttack = 450;
                                Otter.SetBool("armor", false);
                                RightHandWeapon.SetActive(true);
                                LeftHandWeapon.SetActive(true);

                                //Turn off Bow
                                bowEquipped = false;
                                MunitionDisplay.SetActive(false);
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
                                CountArrows();
                                //Turn off Hammers
                                HammerHeld = false;
                                GroundAttack = 50;
                                Otter.SetBool("armor", false);
                                RightHandWeapon.SetActive(false);
                                LeftHandWeapon.SetActive(false);

                                //Turn on Bow
                                bowEquipped = true;
                                MunitionDisplay.SetActive(true);
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
                                MunitionDisplay.SetActive(false);
                                for (int i = 0; i < Bow.Length; i++)
                                {
                                    Bow[i].SetActive(false);
                                }

                                //Turn on Armor Set
                                ArmorEquipped = true;
                                GroundAttack = 150;
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
        //Plant Seed action
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Inventory.TryPlantSeed();
            }
        }
        //Eat Apple
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Inventory.TryEatApple();
            }
            if (Input.GetKeyUp(KeyCode.T))
            {
                appleOBJ.SetActive(false);
            }
        }
        //Use Goblet
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                Inventory.TryUseGoblet();
            }
            if (GobletPicked == true)
            {
                CurrentStamina = MaxStamina;
                GobletClock -= Time.deltaTime;
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

    [Obsolete]
    public void LateUpdate()
    {
        if (IsGameplayInputBlocked())
        {
            return;
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            RotateForward();
        }
        if (Input.anyKeyDown && !Input.GetKey(KeyCode.Mouse1))
        {
            keepLooking = false;
        }
        if (bowEquipped==true && Input.GetKeyDown(KeyCode.Mouse1)&&arrowMunition>0 && CurrentStamina>0)
        {
            arrowMunition--;
        }
    }

    [System.Obsolete]
    public void FixedUpdate()
    {
        if (IsGameplayInputBlocked())
        {
            return;
        }

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

        //Switch off additional animations if not invoked
        {
            Otter.SetBool("walk", false);
            Otter.SetBool("strafeForward", false);
            Otter.SetBool("strafeBack", false);
            Otter.SetBool("strafeLeft", false);
            Otter.SetBool("strafeRight", false);
            Otter.SetBool("climb", false);
            Otter.SetBool("run", false);
            Otter.SetBool("midair", false);
            Otter.SetBool("roll", false);
            Otter.SetBool("aim", false);
            //Otter.SetBool("draw", false);
            Rolling = false;
            movementInvoked = false;
            //keepLooking = false;
        }
        //Stoning action
        if (bowEquipped == false)
        {
            if (Input.GetKey(KeyCode.Mouse1)
                && CurrentStamina > 0
                && !Input.GetKey(KeyCode.LeftControl)
                && bowEquipped == false && grounded == true)
            {
                Stone.SetActive(true);
                AimIcon.SetActive(true);
                Otter.SetBool("aim", true);
                Otter.Play("Aim");
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    rotGoal = Quaternion.LookRotation(cameraRelativeForward);
                    transform.rotation = rotGoal;
                    Otter.Play("Crouch");
                }
                keepLooking = true;
            }
            if (Input.GetKeyUp(KeyCode.Mouse1) && Stone.active && CurrentStamina > 0)
            {
                Otter.SetBool("slash", false);
                Otter.Play("Throw");
                Instantiate(Ball, AttackPoint.position + new Vector3(0, 0.6f, 0), Spine.rotation);
                CurrentStamina -= 20;
                HealthBar.SetStamina(CurrentStamina);
            }
            if (!Input.GetKey(KeyCode.Mouse1))
            {
                Stone.SetActive(false);
                AimIcon.SetActive(false);
            }
        }
        //Bow action       
        if (bowEquipped == true)
        {
            if (Input.GetKeyDown(KeyCode.Mouse1) && !Input.GetKeyUp(KeyCode.Mouse1))
            {
                rotGoal = Quaternion.LookRotation(cameraRelativeForward);
                transform.rotation = rotGoal;
                if (arrowMunition > 0 &&
                CurrentStamina > 0)
                {
                    CountArrows();
                    Sound.ArrowDraw();
                    arrowReady = true;
                    AimIcon.SetActive(true);
                    keepLooking = true;
                    arrowModel.SetActive(true);
                    bowString.SetActive(false);
                    Otter.SetBool("draw", true);
                    stringLine.enabled = true;
                    stringLine.useWorldSpace = false;
                }
                else
                {
                    arrowReady = false;
                    Otter.SetBool("draw", false);
                    Sound.Error();
                    if (CurrentStamina <= 0)
                    {
                        Plattering = "Jee, let me catch a breath!";
                    }
                    if (arrowMunition == 0)
                    {
                        Plattering = "Not enough arrows!";
                    }
                    ChangeSpeech = 1f;
                }
            }
            if (arrowReady == true && Input.GetKeyUp(KeyCode.Mouse1) && CurrentStamina > 0)
            {
                Sound.ArrowShoot();
                Instantiate(Arrow, Spine.position + new Vector3(0,1.4f * cameraRelativeForward.normalized.y, 1.4f * cameraRelativeForward.normalized.z), Quaternion.LookRotation(cameraRelativeForward)* Quaternion.Euler(90, 0, 0));
                CurrentStamina -= 30;
                HealthBar.SetStamina(CurrentStamina);
                arrowModel.SetActive(false);
                stringLine.enabled = false;
                bowString.SetActive(true);
                Otter.SetBool("draw", false);
                arrowReady = false;
            }
        }

        //Switch off hurt and heal effects automaticly
        {
            Health.TickHurtHealEffects();
        }
        //Control player rigidbody's friction
        {
            if (grounded == false || speed == Run)
            {
                Player.drag = 0;
            }
            else
            {
                Player.drag = 0.8f;
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
        {
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
                if (movementInvoked == false && Player.velocity.magnitude >= 6)
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
                if (isParried == false)
                {
                    rotGoal = Quaternion.LookRotation(new Vector3(Player.velocity.x, 0, Player.velocity.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.5f);
                }
                Otter.SetBool("moving", true);
                SlideEffect.enableEmission = true;
            }
            if (neutralAndMoving == false)
            {
                Otter.SetBool("moving", false);
                SlideEffect.enableEmission = false;
            }
        }
        //Heal and hurt effects conditioning
        {
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
}

