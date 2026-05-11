using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] PlayerCameraReference cameraReference = new PlayerCameraReference();
    public CinemachineFreeLook CamForTraders;
    public float ChangeSpeech = 1F;
    GameInputReader inputReader;
    PlayerHealthController healthController;
    PlayerInventory inventory;
    PlayerInteractionController interactionController;
    PlayerTraderTriggerController traderTriggerController;
    PlayerCombatController combatController;
    PlayerMovementController movementController;
    PlayerFailureController failureController;
    PlayerPickupController pickupController;
    bool wasGameplayBlocked;

    PlayerCameraReference GameplayCamera
    {
        get
        {
            if (cameraReference == null)
            {
                cameraReference = new PlayerCameraReference();
            }

            cameraReference.Configure(this, FreeLook);
            return cameraReference;
        }
    }

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

    PlayerTraderTriggerController TraderTriggers
    {
        get
        {
            if (traderTriggerController == null)
            {
                traderTriggerController = GetComponent<PlayerTraderTriggerController>();
                if (traderTriggerController == null)
                {
                    traderTriggerController = gameObject.AddComponent<PlayerTraderTriggerController>();
                }
                traderTriggerController.Initialize(this);
            }

            return traderTriggerController;
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

    PlayerPickupController Pickup
    {
        get
        {
            if (pickupController == null)
            {
                pickupController = GetComponent<PlayerPickupController>();
                if (pickupController == null)
                {
                    pickupController = gameObject.AddComponent<PlayerPickupController>();
                }
                pickupController.Initialize(this);
            }

            return pickupController;
        }
    }

    public void ToggleParryCounterAttack() => WhichAttack = !WhichAttack;
    public void SetAppleModelActive(bool active) => appleOBJ.SetActive(active);
    public bool CanCollectArrowPickup => Arrows != null && arrowMunition < Arrows.Length && bowEquipped;
    public int ArrowCapacity => Arrows != null ? Arrows.Length : 0;
    public void PlayPickupEffect(Vector3 position)
    {
        if (PickUpEffect != null)
        {
            Instantiate(PickUpEffect, position + new Vector3(0, 0.3f, 0), Quaternion.identity);
        }
    }
    public void AddSeedPickup() => Inventory.AddSeed();
    public void AddApplePickup() => Inventory.AddApple();
    public void AddGobletPickup() => Inventory.AddGoblet();

    public void OnCollisionEnter(Collision OBJ)
    {
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
        if (OBJ.gameObject.CompareTag("NPC") && isParried == true 
            || OBJ.gameObject.CompareTag("Scorpion") && isParried == true 
            || OBJ.gameObject.CompareTag("Scorpion") && Input.GetKey(KeyCode.Mouse0) 
            || OBJ.gameObject.CompareTag("Scorpion") && Input.GetKey(KeyCode.Mouse1))
        {
            Plattering = "Get off me ya nasty bastards!";
            ChangeSpeech = 3;
            Vector3 ParryDirection = OBJ.transform.position - transform.position;
            if (SafeRotation.TryPlanarLookRotation(ParryDirection, out Quaternion parryRotation))
            {
                transform.rotation = parryRotation;
            }
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
                BuildSafeLogger.WarnOnce("Behaviour.SwitchMusic.InvalidName." + OBJ.gameObject.name, "The game object's name is not a valid integer: " + OBJ.gameObject.name, this);
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
        TraderTriggers.HandleTriggerStay(OBJ);
        if (OBJ.gameObject.CompareTag("House"))
        {
            TrySetFreeLookOrbits(FreeLook, 1f, 2f, 1f);
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
        TraderTriggers.HandleTriggerExit(OBJ);
        if (OBJ.gameObject.CompareTag("House"))
        {
            TrySetFreeLookOrbits(FreeLook, 4f, 6f, 5f);
            //FreeLook.m_Lens.FieldOfView = 25;
        }
        if (OBJ.gameObject.CompareTag("Scorpion"))
        {
            scorpAttack = false;
        }
    }
    public void TakeDamage(float Damage) => TakeDamage(new DamageEvent { Amount = Damage, Source = null, Point = transform.position, Type = DamageType.Generic, CanStun = false });
    public void TakeDamage(DamageEvent damageEvent) => Health.TakeDamage(damageEvent.Amount);
    public void HandleFailure(PlayerFailureReason reason)
    {
        ResetMovementRuntimeState();
        Failure.HandleFailure(reason);
    }
    public void PlayerMove(Vector3 Direction)
    {
        if (HandleGameplayBlocked())
        {
            return;
        }

        if (!CanProcessPlayerGameplay())
        {
            ResetMovementRuntimeState();
            return;
        }

        if (!SafeRotation.IsFinite(Direction))
        {
            return;
        }

        movementInvoked = true;
        //Regular walk
        if (keepLooking==false)
        {
            if (SafeRotation.TryLookRotation(Direction, out rotGoal))
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            }
            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Walk, true);
        }
        //Strafe
        if (keepLooking == true)
        {
            if (!GameplayCamera.TryGetPlanarBasis(out Vector3 forwardFace, out _))
            {
                return;
            }

            if (SafeRotation.TryLookRotation(forwardFace, out rotGoal))
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            }
            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Walk, false);
            if (Input.GetKey(KeyCode.W))
            {
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeForward, true);
            }
            if (Input.GetKey(KeyCode.S))
            {
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeBack, true);
            }
            if (Input.GetKey(KeyCode.A))
            {
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeLeft, true);
            }
            if (Input.GetKey(KeyCode.D))
            {
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeRight, true);
            }
        }
        //Stairs
        if (step==true)
        {
            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Climb, true);
        }
        else
        {
            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Climb, false);
        }
        //Sprint
        if (grounded == true)
        {
            if (Input.GetKey(KeyCode.LeftShift) && HasMovementInput() && Rolling == false && keepLooking==false)
            {
                if (ArmorEquipped==true)
                {
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Armor, true);
                }
                if (ArmorEquipped == false)
                {
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Armor, false);
                }
                if (step==false)
                {
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Run, true);
                    speed = Run;
                    steer = 0.12f;
                }
            }
            else
            {
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Run, false);
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
        if (HandleGameplayBlocked())
        {
            return;
        }

        if (!CanProcessPlayerGameplay())
        {
            ResetMovementRuntimeState();
            return;
        }

        if (!SafeRotation.IsFinite(Direction))
        {
            return;
        }

        if (SafeRotation.TryPlanarLookRotation(Direction, out rotGoal))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.11f);
        }
        //Evading Roll action
        {
            if (Input.GetKey(KeyCode.LeftControl) && CurrentStamina > 0 && grounded == true)
            {
                Rolling = true;
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Roll, true);
                CurrentStamina -= 0.1f;
                speed = 7;
                if (HealthBar != null)
                {
                    HealthBar.SetStamina(CurrentStamina);
                }
                Player.velocity = (Direction.normalized * 6) + new Vector3(0, Player.velocity.y, 0);
            }
            if (!Input.GetKey(KeyCode.LeftControl) || CurrentStamina <= 0 || grounded == false)
            {
                Rolling = false;
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Roll, false);
            }

        }
    }

    [Obsolete]
    public void RotateForward()
    {
        if (Spine == null)
        {
            return;
        }

        if (HandleGameplayBlocked())
        {
            return;
        }

        if (!CanProcessPlayerGameplay())
        {
            ResetMovementRuntimeState();
            return;
        }

        if (!GameplayCamera.TryGetPlanarBasis(out Vector3 camForward, out _))
        {
            return;
        }

        if (!SafeRotation.TryLookRotation(camForward, out Quaternion direction))
        {
            return;
        }

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
    public void EnterTraderInteraction(Collider trader) => Interaction.EnterTrader(trader);
    public void ExitTraderInteraction() => Interaction.ExitTrader();
    public void RestoreGameplayCursorAfterUiClose() => Interaction.RestoreGameplayCursorAfterUiClose();
    public void ActivateLooseMenu()
    {
        //MusicOP.StopMusic();
        ResetMovementRuntimeState();
        var gameFlow = GameFlowController.GetOrCreate();
        if (gameFlow.State != GameFlowState.GameOver && !gameFlow.SetGameOver())
        {
            return;
        }

        ShowCursor();
        ShowLosePanel();
    }
    public void HideLooseMenu()
    {
        //MusicOP.ResumeMusic();
        GameFlowController.GetOrCreate().SetPlaying();
        HideCursor();
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
        if (HealthBar != null)
        {
            HealthBar.SetHealth(CurrentHealth);
        }
    }

    public void MoveToCheckpoint()
    {
        var checkpoint = CheckpointService.GetOrCreate();
        if (checkpoint != null)
        {
            transform.position = checkpoint.LastCheckpointPosition;
        }

        if (PopUpEffect != null)
        {
            Instantiate(PopUpEffect, transform.position, Quaternion.identity);
        }
    }
    void SaveCheckpoint(Vector3 position)
    {
        State.checkpointPosition = position;
        var checkpoint = CheckpointService.GetOrCreate();
        if (checkpoint != null)
        {
            checkpoint.SaveCheckpoint(position);
        }
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

    bool IsGameplayActive()
    {
        var reader = GetInputReader();
        return reader != null && reader.IsGameplayInputEnabled;
    }

    bool HandleGameplayBlocked()
    {
        if (IsGameplayActive())
        {
            wasGameplayBlocked = false;
            return false;
        }

        if (!wasGameplayBlocked)
        {
            ResetMovementRuntimeState();
            wasGameplayBlocked = true;
        }

        return true;
    }

    bool CanProcessPlayerGameplay()
    {
        var flow = GameFlowController.GetOrCreate();
        return IsGameplayActive()
            && flow != null
            && flow.State == GameFlowState.Playing
            && Player != null
            && Otter != null;
    }

    bool HasMovementInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude > Mathf.Epsilon;
    }

    public void ResetMovementRuntimeState()
    {
        Rolling = false;
        movementInvoked = false;
        neutralAndMoving = false;
        keepLooking = false;
        speed = Walk;
        steer = 0.1f;

        if (Player != null)
        {
            Player.velocity = new Vector3(0f, Player.velocity.y, 0f);
        }

        SetMovementAnimatorIdle();
    }

    public void SetMovementAnimatorIdle()
    {
        ClearMovementAnimatorBools();
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Aim, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Draw, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Crouch, false);
    }

    void ClearMovementAnimatorBools()
    {
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Walk, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Run, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Midair, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Roll, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Moving, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeForward, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeBack, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeLeft, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.StrafeRight, false);
        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Climb, false);
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

        WarnMissingCameraReference(FreeLook, nameof(FreeLook));
        WarnMissingCameraReference(CamForTraders, nameof(CamForTraders));

        return valid;
    }

    bool TrySetFreeLookOrbits(CinemachineFreeLook cam, float top, float mid, float bottom)
    {
        if (!WarnMissingCameraReference(cam, nameof(FreeLook)))
        {
            return false;
        }

        if (cam.m_Orbits == null || cam.m_Orbits.Length < 3)
        {
            BuildSafeLogger.WarnOnce(
                nameof(Behaviour) + ".InvalidFreeLookOrbits",
                "FreeLook camera does not have the expected three orbit rigs.",
                this,
                nameof(FreeLook));
            return false;
        }

        cam.m_Orbits[0].m_Radius = top;
        cam.m_Orbits[1].m_Radius = mid;
        cam.m_Orbits[2].m_Radius = bottom;
        return true;
    }

    public bool TrySetFreeLookEnabled(bool enabled)
    {
        if (!WarnMissingCameraReference(FreeLook, nameof(FreeLook)))
        {
            return false;
        }

        FreeLook.enabled = enabled;
        return true;
    }

    public bool TrySetFreeLookLookAt(Transform lookAt)
    {
        if (!WarnMissingCameraReference(FreeLook, nameof(FreeLook)))
        {
            return false;
        }

        FreeLook.m_LookAt = lookAt;
        return true;
    }

    public bool TryConfigureFreeLookForBoss(float middleOrbitRadius, float xAxisMaxSpeed, float yAxisMaxSpeed, Transform lookAt)
    {
        if (!WarnMissingCameraReference(FreeLook, nameof(FreeLook)))
        {
            return false;
        }

        if (FreeLook.m_Orbits == null || FreeLook.m_Orbits.Length < 2)
        {
            BuildSafeLogger.WarnOnce(
                nameof(Behaviour) + ".InvalidFreeLookBossOrbit",
                "FreeLook camera does not have the expected middle orbit rig; boss camera changes will be skipped.",
                this,
                nameof(FreeLook));
            return false;
        }

        FreeLook.m_Orbits[1].m_Radius = middleOrbitRadius;
        FreeLook.m_XAxis.m_MaxSpeed = xAxisMaxSpeed;
        FreeLook.m_YAxis.m_MaxSpeed = yAxisMaxSpeed;
        FreeLook.m_LookAt = lookAt;
        return true;
    }

    public bool TryRotateFreeLookLookAtTowardRoot()
    {
        if (!WarnMissingCameraReference(FreeLook, nameof(FreeLook)))
        {
            return false;
        }

        if (FreeLook.m_LookAt == null)
        {
            BuildSafeLogger.WarnOnce(
                nameof(Behaviour) + ".MissingFreeLookLookAt",
                "FreeLook camera does not have a LookAt target; camera rotation reset will be skipped.",
                this,
                nameof(FreeLook));
            return false;
        }

        FreeLook.m_LookAt.rotation = Quaternion.Slerp(transform.rotation, SafeRotation.LookRotationOrCurrent(Root.position, transform.rotation), 0.5f);
        return true;
    }

    public bool TrySetTraderCameraEnabled(bool enabled)
    {
        if (!WarnMissingCameraReference(CamForTraders, nameof(CamForTraders)))
        {
            return false;
        }

        CamForTraders.enabled = enabled;
        return true;
    }

    public bool TrySetTraderCameraLookAt(Transform lookAt)
    {
        if (!WarnMissingCameraReference(CamForTraders, nameof(CamForTraders)))
        {
            return false;
        }

        CamForTraders.m_LookAt = lookAt;
        return true;
    }

    bool WarnMissingCameraReference(CinemachineFreeLook cam, string fieldName)
    {
        if (cam != null)
        {
            return true;
        }

        BuildSafeLogger.WarnOnce(
            nameof(Behaviour) + ".MissingCameraReference." + fieldName,
            fieldName + " is not assigned; camera-specific behaviour will be skipped.",
            this,
            fieldName);
        return false;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameplayCamera.Invalidate();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameplayCamera.Invalidate();
    }

    public void Start()
    {
        if (!ValidateStartReferences())
        {
            return;
        }

        inputReader = GameInputReader.GetOrCreate();
        GameFlowController.GetOrCreate().TrySetPlayingFromSceneStartup(nameof(Behaviour));
        Inventory.Initialize(this);
        Interaction.Initialize(this, inputReader);
        TraderTriggers.Initialize(this);
        Combat.Initialize(this);
        Movement.Initialize(this);
        Failure.Initialize(this);
        Pickup.Initialize(this);
        arrowModel.SetActive(false);
        bowAim = new Vector3(-0.33f, 20f, -0.3f);
        TrySetTraderCameraEnabled(false);
        if (PopUpEffect != null && Root != null)
        {
            Instantiate(PopUpEffect, Root.position, Quaternion.identity);
        }
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
        TrySetFreeLookOrbits(FreeLook, 4f, 6f, 5f);
    }
    [System.Obsolete]
    public void Update()
    {
        HandleDeathState();
        if (HandleGameplayBlocked())
        {
            return;
        }

        if (!CanProcessPlayerGameplay())
        {
            ResetMovementRuntimeState();
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
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Roll, false);
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
            if (FreeLook != null && Input.GetKey(KeyCode.LeftControl) && FreeLook.m_Lens.FieldOfView < 20)
            {
                TrySetFreeLookLookAt(Face);
            }

            if (Input.GetKey(KeyCode.LeftControl) && grounded == true && Rolling == false && !Input.GetKey(KeyCode.Space))
            {
                if (Load.i == 9)
                    HologramedBridge.SetActive(true);
                if (!Input.GetKey(KeyCode.W) || !Input.GetKey(KeyCode.Mouse0))
                {
                    speed = 0;
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Crouch, true);
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
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Crouch, false);
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
                                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Armor, false);
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
                                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Armor, false);
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
                                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Armor, false);
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
                                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Armor, false);
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
                                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Armor, true);
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
                    if (HealthBar != null)
                    {
                        HealthBar.SetStamina(CurrentStamina);
                    }
                    if (ArmorEquipped==false)
                    {
                        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Fight, true); //Airkick leveitation without sword
                        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Slash, false);
                    }
                    if (ArmorEquipped == true)
                    {
                        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Fight, false);
                        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Slash, true);//Airkick leveitation with sword
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
                            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Fight, false);
                        }      
                    }
                    if (grounded == true)
                    {
                        if (ArmorEquipped == false)
                        {
                            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Fight, true);
                            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Slash, false);
                        }
                        if (ArmorEquipped == true)
                        {
                            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Fight, false);
                            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Slash, true);
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
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Slash, false);
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Fight, false);
                    
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
        if (HandleGameplayBlocked())
        {
            return;
        }

        if (!CanProcessPlayerGameplay())
        {
            ResetMovementRuntimeState();
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
        if (HandleGameplayBlocked())
        {
            return;
        }

        if (!CanProcessPlayerGameplay())
        {
            ResetMovementRuntimeState();
            return;
        }

        //Camera vectors setup
        bool hasGameplayCamera = GameplayCamera.TryGetPlanarBasis(out Vector3 XZForward, out Vector3 XZRight);

        //Switch off additional animations if not invoked
        {
            ClearMovementAnimatorBools();
            PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Aim, false);
            Rolling = false;
            movementInvoked = false;
            //keepLooking = false;
        }
        //Stoning action
        if (hasGameplayCamera && bowEquipped == false)
        {
            if (Input.GetKey(KeyCode.Mouse1)
                && CurrentStamina > 0
                && !Input.GetKey(KeyCode.LeftControl)
                && bowEquipped == false && grounded == true)
            {
                Stone.SetActive(true);
                AimIcon.SetActive(true);
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Aim, true);
                Otter.Play("Aim");
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    if (SafeRotation.TryPlanarLookRotation(XZForward, out rotGoal))
                    {
                        transform.rotation = rotGoal;
                    }
                    Otter.Play("Crouch");
                }
                keepLooking = true;
            }
            if (Input.GetKeyUp(KeyCode.Mouse1) && Stone.active && CurrentStamina > 0)
            {
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Slash, false);
                Otter.Play("Throw");
                Instantiate(Ball, AttackPoint.position + new Vector3(0, 0.6f, 0), Spine.rotation);
                CurrentStamina -= 20;
                if (HealthBar != null)
                {
                    HealthBar.SetStamina(CurrentStamina);
                }
            }
            if (!Input.GetKey(KeyCode.Mouse1))
            {
                Stone.SetActive(false);
                AimIcon.SetActive(false);
            }
        }
        //Bow action       
        if (hasGameplayCamera && bowEquipped == true)
        {
            if (Input.GetKeyDown(KeyCode.Mouse1) && !Input.GetKeyUp(KeyCode.Mouse1))
            {
                if (SafeRotation.TryPlanarLookRotation(XZForward, out rotGoal))
                {
                    transform.rotation = rotGoal;
                }
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
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Draw, true);
                    stringLine.enabled = true;
                    stringLine.useWorldSpace = false;
                }
                else
                {
                    arrowReady = false;
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Draw, false);
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
                if (!SafeRotation.TryPlanarLookRotation(XZForward, out Quaternion arrowRotation))
                {
                    return;
                }

                Sound.ArrowShoot();
                Instantiate(Arrow, Spine.position + XZForward * 1.4f, arrowRotation * Quaternion.Euler(90, 0, 0));
                CurrentStamina -= 30;
                if (HealthBar != null)
                {
                    HealthBar.SetStamina(CurrentStamina);
                }
                arrowModel.SetActive(false);
                stringLine.enabled = false;
                bowString.SetActive(true);
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Draw, false);
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
        if (hasGameplayCamera)
        {
            Vector2 input = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            Vector3 move = (XZForward * input.y + XZRight * input.x).normalized;

            if (move.sqrMagnitude >= Mathf.Epsilon)
            {
                //No Crouch = Regular Movement
                if (!Input.GetKey(KeyCode.LeftControl))
                {
                    HealQue = 3;
                    PlayerMove(move);
                }
                //Movement + Crouch = Roll & Evade
                else if (CurrentStamina > 0)
                {
                    HealQue = 3;
                    PlayerRoll(move);
                }
            }
            else
            {
                speed = Walk;
                steer = 0.1f;
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Run, false);
            }
        }
       
        
        //Aurborne and landing sequence animations conditioning
        {
            //+ Player's slide and full-break conditioning on ground
            if (grounded == false)
            {
                if (JumpNum < JumpLimit)
                {
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Midair, true);
                }
                if (JumpNum == JumpLimit)
                {
                    PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Midair, false);
                    FallClock -= Time.deltaTime;
                    if (FallClock <= 0)
                    {
                        PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Midair, true);
                    }
                }
            }
            if (grounded == true)
            {
                JumpNum = JumpLimit;
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Midair, false);
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
                    if (SafeRotation.TryPlanarLookRotation(Player.velocity, out rotGoal))
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.5f);
                    }
                }
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Moving, true);
                SlideEffect.enableEmission = true;
            }
            if (neutralAndMoving == false)
            {
                PlayerAnimatorParameters.TrySetBool(Otter, PlayerAnimatorParameters.Moving, false);
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

