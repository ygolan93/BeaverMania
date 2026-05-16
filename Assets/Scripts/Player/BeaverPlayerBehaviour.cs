using System;
using System.Collections;
using System.Collections.Generic;
using Beavermania.Audio;
using Beavermania.Core.GameFlow;
using Beavermania.Display;
using Beavermania.Core.Input;
using Beavermania.NPC;
using Beavermania.Data.Combat;
using Beavermania.Player.Combat;
using Beavermania.Player.Movement;
using Beavermania.UI.Hud;
using Beavermania.UI.Objectives;
using Cinemachine;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.Player
{

    [RequireComponent(typeof(PlayerHudState))]
    public class BeaverPlayerBehaviour : MonoBehaviour
    {
        [Header("Movement and animation")]
        public Rigidbody Player;
        public bool grounded;
        public Carry Load;
        public bool movementInvoked = false;
        public Quaternion rotGoal;
        /// <summary>Legacy strafe-lock flag (not set during bow/stoning aim; normal locomotion stays active).</summary>
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
        [SerializeField] PlayerCombatBalanceData combatBalance;
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
        [Header("Bow aim")]
        [Tooltip("Optional override; when null, Camera.main / cached gameplay camera is used.")]
        [SerializeField] Camera bowAimCamera;
        [SerializeField] float bowAimRayMaxDistance = 400f;
        [Tooltip("Viewport Y offset above screen center for projectile aim (0.03 = slight high).")]
        [SerializeField] float bowAimViewportYOffset = 0.03f;
        [Tooltip("Layers for center-screen aim raycast. Leave at Nothing to raycast all layers.")]
        [SerializeField] LayerMask bowAimRaycastLayers;
        [Tooltip("Offset spawn along fire direction so the arrow clears body/bow colliders (meters).")]
        [SerializeField] float bowProjectileSpawnClearance = 0.5f;
        [Tooltip("Ignore screen-center hits closer than this to the camera when aiming FireBreath.")]
        [SerializeField] float minFireBreathCameraHitDistance = 1.25f;
        [Tooltip("Push FireBreath spawn along aim direction away from the fire point (meters).")]
        [SerializeField] float fireBreathSpawnForwardOffset = 0.5f;
        [Tooltip("Draw aim rays and log FireBreath launch vectors for one test session.")]
        [SerializeField] bool debugFireBreathLaunch;
        [Tooltip("Logical arrow ammo cap (independent of quiver mesh slot count).")]
        [SerializeField] int maxArrowMunition = 99;
        [Tooltip("Minimum time between completed bow shots before a new draw can start.")]
        [SerializeField] float bowShotCooldown = 0.25f;
        public float GroundBeat = 0.3f;
        public float AirBeat = 0.2f;
        bool scorpAttack;
        float FallClock;

        /// <summary>Mouse1 edges buffered in Update so FixedUpdate never misses sub-timestep clicks.</summary>
        bool _mouse1DownBuffered;
        bool _mouse1UpBuffered;
        /// <summary>RMB held state at end of previous FixedUpdate (for reliable press/release edges in FixedUpdate).</summary>
        bool _secondaryHeldLastFixed;
        /// <summary>Stoning: RMB aim session active (survives one frame without relying on Stone.active).</summary>
        bool _stoneAimSessionActive;
        /// <summary>Primary held last FixedUpdate — avoids Update/FixedUpdate input desync cancelling slash.</summary>
        bool _primaryMeleeHeldLastFixed;
        /// <summary>LMB held while Sword and Shield is equipped.</summary>
        bool _swordShieldAttackHeld;
        /// <summary>True while a sword/shield attack cycle should keep slash active and consume stamina.</summary>
        bool _swordShieldCycleActive;
        Coroutine _swordShieldChainRoutine;
        const float SwordShieldGroundStaminaPerFixedStep = 0.2f;
        const float SwordShieldAirStaminaPerFixedStep = 0.5f;
        bool _loggedMissingBowAimCamera;
        bool _loggedMissingSpineForAim;
        /// <summary>True while RMB draw session is open; cleared after release/cancel.</summary>
        bool _bowAimSessionOpen;
        /// <summary>Prevents a second fire/consume for the same draw before RMB is pressed again.</summary>
        bool _bowFiredThisAimSession;
        /// <summary>Blocks a new draw until RMB has been released after the previous shot/cancel.</summary>
        bool _bowWaitingForRmbRelease;
        float _bowNextDrawAllowedTime;
        [SerializeField] bool debugBowCombat;
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
        public bool isAtTrader = false;
        PauseController pauseController;
        float initialCamForTradersXAxisSpeed;
        float initialCamForTradersYAxisSpeed;
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
        public PlayerHudState PlayerHudState;
        public ObjectiveUI PlayerObjective;
        bool loggedRuntimeHudStateFallback;
        public CinemachineFreeLook FreeLook;
        public CinemachineFreeLook CamForTraders;
        [SerializeField] PlayerCheckpointRespawn checkpointRespawn;
        public float ChangeSpeech = 1F;
        Vector3 stableCameraForwardXZ = Vector3.forward;
        Camera cachedMainCamera;
        Transform cachedMainCameraTransform;
        const float LookRotationEpsilon = 0.0001f;

        public PlayerCombatBalanceData CombatBalance => combatBalance;

        float EffectiveScorpionLightDamage => combatBalance != null ? combatBalance.scorpionLightDamage : 15f;
        float EffectiveScorpionHeavyDamage => combatBalance != null ? combatBalance.scorpionHeavyDamage : 30f;
        float EffectiveShroomHealPerTick => combatBalance != null ? combatBalance.shroomHealPerTick : 2f;
        float EffectiveAppleHealAmount => combatBalance != null ? combatBalance.appleHealAmount : 500f;
        float EffectiveBowShotStaminaCost => combatBalance != null ? combatBalance.bowShotStaminaCost : 30f;
        float EffectiveStoneThrowStaminaCost => combatBalance != null ? combatBalance.stoneThrowStaminaCost : 20f;
        int EffectiveBareHandsDamage => combatBalance != null ? combatBalance.bareHandsMeleeDamage : 50;
        int EffectiveHammerDamage => combatBalance != null ? combatBalance.hammerMeleeDamage : 700;
        int EffectiveBowEquippedMeleeDamage => combatBalance != null ? combatBalance.bowEquippedMeleeDamage : 50;
        int EffectiveArmorSetDamage => combatBalance != null ? combatBalance.armorSetMeleeDamage : 200;

        public void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject.CompareTag("Part")&& Load.CanCarry == true && grounded==true && !PlayerInputReader.IsPrimaryHeld()&& !PlayerInputReader.IsSecondaryHeld())
            {
                Otter.Play("Crouch");
                Sound.PickItem();
                SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
                Destroy(OBJ.gameObject);
            }
            if ( OBJ.gameObject.CompareTag("Seed") || OBJ.gameObject.CompareTag("Apple") || OBJ.gameObject.CompareTag("GobletKey"))
            {
                Otter.Play("Crouch");
                SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
                Destroy(OBJ.gameObject);

                if (OBJ.gameObject.CompareTag("Seed"))
                {
                    NutCount++;
                    Sound.PickItem();
                }
                if (OBJ.gameObject.CompareTag("Apple"))
                {
                    Sound.PickUp2();
                    Apple++;
                }
                if (OBJ.gameObject.CompareTag("GobletKey"))
                {
                    Sound.PickUp2();
                    GobletPickup++;
                    SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
                    Destroy(OBJ.gameObject);
                }

            }
            if (OBJ.gameObject.CompareTag("Isle") || OBJ.gameObject.CompareTag("Bridge") || OBJ.gameObject.CompareTag("stairs") || OBJ.gameObject.CompareTag("Tile"))
            {
                FallClock = InitialFall;
                if (PlayerInputReader.WasAnyMoveAxisPressedDown())
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
                SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
                Sound.Coin();
            }
            if (arrowMunition<Arrows.Length && bowEquipped==true)
            {
                if (OBJ.gameObject.CompareTag("Arrow"))
                {
                    Otter.Play("Crouch");
                    Sound.PickUp2();
                    ApplyArrowMunitionDelta(1);
                    SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
                    Destroy(OBJ.gameObject);
                }
                if (OBJ.gameObject.CompareTag("ArrowBundle"))
                {
                    Otter.Play("Crouch");
                    Sound.PickUp2();
                    SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
                    if (arrowMunition + 10 < Arrows.Length)
                    {
                        ApplyArrowMunitionDelta(10);
                    }
                    else
                    {
                        for (int i = 0; i <(arrowMunition-10); i++)
                        {
                            Instantiate(ArrowPickup, OBJ.transform.position+new Vector3(0,i* 0.3f, 0), Quaternion.Euler(0,0,90));
                        }
                        SetArrowMunition(Arrows.Length);
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
                SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
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
                SetArrowMunition(5);
                CountArrows();
                SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
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
                SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
                Destroy(OBJ.gameObject);
            }
            if (OBJ.gameObject.CompareTag("Honey") && Honeypicked == false)
            {
                Otter.Play("Crouch");
                HoneyON();
                Sound.PickItem();
                SpawnPickUpEffect(OBJ.transform.position + new Vector3(0, 0.3f, 0));
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
                || OBJ.gameObject.CompareTag("Scorpion") && PlayerInputReader.IsPrimaryHeld() 
                || OBJ.gameObject.CompareTag("Scorpion") && PlayerInputReader.IsSecondaryHeld())
            {
                Plattering = "Get off me ya nasty bastards!";
                ChangeSpeech = 3;
                var ParryDirection = new Vector3((OBJ.transform.position - transform.position).x, 0, (OBJ.transform.position - transform.position).z);
                if (ParryDirection.sqrMagnitude > LookRotationEpsilon)
                    transform.rotation = Quaternion.LookRotation(ParryDirection);
            }
            if (OBJ.gameObject.CompareTag("Strike"))
            {
                if (Lives > 0 && GM != null)
                {
                    transform.position = GM.lastCheckPointPos + new Vector3(1, 1, 1);
                    SpawnPopUpEffect(transform.position);
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
                        TakeDamage(EffectiveScorpionLightDamage);
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
                    TakeDamage(EffectiveScorpionHeavyDamage);
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
                if (GM != null)
                    GM.lastCheckPointPos = OBJ.transform.position;
                Plattering = ("Shroom!");
                ChangeSpeech = 1;
                if (CurrentHealth < MaxHealth)
                {
                    TakeDamage(-EffectiveShroomHealPerTick);
                    TouchShroom = true;
                }
                if (CurrentHealth >= MaxHealth)
                    TouchShroom = false;
            }
            if (OBJ.gameObject.CompareTag("Trader"))
            {
                var trader = OBJ.GetComponentInParent<Trader>();
                if (trader == null)
                    trader = OBJ.GetComponent<Trader>();
                if (trader != null)
                {
                    var skip = trader.skipPressed;
                    bool inOfferRadius = trader.Merchant != null
                        && Vector3.Distance(transform.position, trader.Merchant.transform.position) < trader.GetOfferPanelDistance();

                    if (skip == false && inOfferRadius)
                        ApplyTraderOfferPresentation(OBJ.transform);
                    if (skip == true)
                        RestoreGameplayAfterTrader();
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
                RestoreGameplayAfterTrader();
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
            Direction = ResolveHorizontalMoveDirection(Direction);
            movementInvoked = true;
            //Regular walk
            if (keepLooking==false)
            {
                if (Direction.sqrMagnitude > LookRotationEpsilon)
                {
                    rotGoal = Quaternion.LookRotation(Direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
                }
                Otter.SetBool("walk", true);
            }
            //Strafe
            if (keepLooking == true)
            {
                Vector3 forwardFace = ResolveMainCameraTransform().TransformDirection(Vector3.forward);
                Vector3 flatFace = new Vector3(forwardFace.x, 0, forwardFace.z);
                if (flatFace.sqrMagnitude < 1e-8f)
                    flatFace = ResolveHorizontalMoveDirection(flatFace);
                if (flatFace.sqrMagnitude > LookRotationEpsilon)
                {
                    rotGoal = Quaternion.LookRotation(flatFace);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
                }
                Otter.SetBool("walk", false);
                if (PlayerInputReader.IsMoveForwardHeld())
                {
                    Otter.SetBool("strafeForward", true);
                }
                if (PlayerInputReader.IsMoveBackHeld())
                {
                    Otter.SetBool("strafeBack", true);
                }
                if (PlayerInputReader.IsMoveLeftHeld())
                {
                    Otter.SetBool("strafeLeft", true);
                }
                if (PlayerInputReader.IsMoveRightHeld())
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
                if (PlayerInputReader.IsSprintHeld() && PlayerInputReader.IsAnyKeyHeld() && Rolling == false && keepLooking==false)
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
            if (grounded == false && PlayerInputReader.IsSprintHeld())
                Player.AddForce(Direction.normalized * 5);
        }
        public void PlayerRoll(Vector3 Direction)
        {
            Vector3 flat = new Vector3(Direction.x, 0, Direction.z);
            if (flat.sqrMagnitude < 1e-8f)
                flat = ResolveHorizontalMoveDirection(flat);
            if (flat.sqrMagnitude > LookRotationEpsilon)
                rotGoal = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.11f);
            //Evading Roll action
            {
                if (PlayerInputReader.IsRollHeld() && CurrentStamina > 0 && grounded == true)
                {
                    if (ArmorEquipped)
                        InterruptSwordAttackPresentation();
                    Rolling = true;
                    Otter.SetBool("roll", true);
                    CurrentStamina -= 0.1f;
                    speed = 7;
                    HealthBar.SetStamina(CurrentStamina);
                    Player.velocity = (Direction.normalized * 6) + new Vector3(0, Player.velocity.y, 0);
                }
                if (!PlayerInputReader.IsRollHeld() || CurrentStamina <= 0 || grounded == false)
                {
                    Rolling = false;
                    Otter.SetBool("roll", false);
                }

            }
        }

        [Obsolete]
        public void RotateForward()
        {
            Transform camT = ResolveMainCameraTransform();
            Vector3 camForward = camT.TransformDirection(Vector3.forward);
            if (camForward.sqrMagnitude < 1e-8f)
                camForward = transform.forward;
            if (camForward.sqrMagnitude > LookRotationEpsilon)
            {
                if (Spine == null)
                {
                    if (!_loggedMissingSpineForAim)
                    {
                        _loggedMissingSpineForAim = true;
                        Debug.LogWarning($"{nameof(BeaverPlayerBehaviour)}: Spine transform is not assigned; aim body rotation skipped.", this);
                    }
                    return;
                }

                Quaternion direction = Quaternion.LookRotation(camForward);
                if (bowEquipped==false)
                {
                    Spine.rotation = direction;
                }
                if (bowEquipped == true && PlayerInputReader.IsSecondaryHeld() && arrowMunition > 0)
                {
                    Spine.rotation = direction*Quaternion.EulerRotation(bowAim);
                }
            }
  
        }
        public void CountArrows()
        {
            ClampArrowMunition();
            if (Arrows == null)
                return;

            var i = 0;
            foreach (var arrow in Arrows)
            {
                if (arrow == null)
                {
                    i++;
                    continue;
                }
                int visibleArrows = Arrows != null ? Mathf.Min(arrowMunition, Arrows.Length) : arrowMunition;
                if (i < visibleArrows)
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

        /// <summary>Whether the player has arrows to nock (does not check stamina).</summary>
        public bool HasArrowAmmo()
        {
            return bowEquipped && arrowMunition > 0;
        }

        /// <summary>Whether a bow shot can be released right now (ammo + stamina).</summary>
        public bool CanFireArrow()
        {
            return HasArrowAmmo() && CurrentStamina > 0;
        }

        int ResolveMaxArrowMunition()
        {
            int cap = maxArrowMunition > 0 ? maxArrowMunition : 99;
            if (Arrows != null && Arrows.Length > 0)
                cap = Mathf.Max(cap, Arrows.Length);
            return cap;
        }

        void ClampArrowMunition()
        {
            arrowMunition = Mathf.Clamp(arrowMunition, 0, ResolveMaxArrowMunition());
        }

        void RefreshArrowHudText()
        {
            ArrowText = "ARROWS (RM): " + arrowMunition;
            if (PlayerHudState != null)
                PlayerHudState.ArrowText = ArrowText;
        }

        void SetArrowMunition(int value)
        {
            arrowMunition = value;
            ClampArrowMunition();
            CountArrows();
            RefreshArrowHudText();
        }

        void ApplyArrowMunitionDelta(int delta)
        {
            arrowMunition += delta;
            ClampArrowMunition();
            CountArrows();
            RefreshArrowHudText();
        }

        bool CanBeginBowDraw()
        {
            if (!bowEquipped || arrowReady || _bowAimSessionOpen)
                return false;
            if (_bowWaitingForRmbRelease)
                return false;
            if (Time.time < _bowNextDrawAllowedTime)
                return false;
            return true;
        }

        void MarkBowShotResolved()
        {
            _bowWaitingForRmbRelease = true;
            _bowNextDrawAllowedTime = Time.time + bowShotCooldown;
        }

        /// <summary>Decrements stored arrow count after a valid shot (or other authoritative spend). Clamps and refreshes quiver visuals.</summary>
        public void ConsumeArrow(int amount = 1)
        {
            if (amount <= 0)
                return;
            arrowMunition -= amount;
            ClampArrowMunition();
            CountArrows();
            RefreshArrowHudText();
        }

        Camera ResolveBowAimCamera()
        {
            if (bowAimCamera != null)
                return bowAimCamera;
            if (cachedMainCamera == null)
                CacheMainCamera();
            if (cachedMainCamera != null)
                return cachedMainCamera;
            return Camera.main;
        }

        LayerMask ResolveBowAimRaycastLayers()
        {
            return bowAimRaycastLayers.value != 0 ? bowAimRaycastLayers : (LayerMask)~0;
        }

        bool IsBowAimHitSelf(Collider c)
        {
            return IsCameraAimIgnoredCollider(c);
        }

        bool IsCameraAimIgnoredCollider(Collider c)
        {
            if (c == null)
                return true;

            if (c.GetComponentInParent<BeaverPlayerBehaviour>() == this)
                return true;

            return c.transform.IsChildOf(transform);
        }

        Vector3 ResolveBowAimTargetPoint(Ray ray, float maxDist, LayerMask mask)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDist, mask, QueryTriggerInteraction.Ignore))
                return ray.origin + ray.direction * maxDist;

            if (!IsBowAimHitSelf(hit.collider))
                return hit.point;

            const float skin = 0.08f;
            var origin = hit.point + ray.direction * skin;
            float remain = maxDist - hit.distance - skin;
            if (remain > 0.01f && Physics.Raycast(origin, ray.direction, out RaycastHit hit2, remain, mask, QueryTriggerInteraction.Ignore)
                && !IsBowAimHitSelf(hit2.collider))
                return hit2.point;

            return ray.origin + ray.direction * maxDist;
        }

        bool TryGetBowViewportAimRay(out Ray aimRay, out Vector3 targetPoint)
        {
            aimRay = default;
            targetPoint = default;

            Camera cam = ResolveBowAimCamera();
            if (cam == null)
                return false;

            aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f + bowAimViewportYOffset, 0f));
            float maxDist = Mathf.Max(1f, bowAimRayMaxDistance);
            LayerMask mask = ResolveBowAimRaycastLayers();
            targetPoint = ResolveBowAimTargetPoint(aimRay, maxDist, mask);
            return true;
        }

        /// <summary>Authoritative projectile aim from viewport center. Does not depend on spine/bow transform jitter.</summary>
        bool TryGetAimDirectionFromCameraCenter(out Vector3 aimDirection)
        {
            aimDirection = Vector3.forward;

            if (!TryGetBowViewportAimRay(out Ray aimRay, out _))
            {
                aimDirection = Spine != null ? Spine.forward : transform.forward;
                if (aimDirection.sqrMagnitude < 1e-8f)
                    aimDirection = Vector3.forward;
                else
                    aimDirection.Normalize();
                return false;
            }

            aimDirection = aimRay.direction.normalized;
            return true;
        }

        bool TryGetProjectileLaunchFromCameraCenter(
            Transform spawnOrigin,
            Vector3 spawnLocalOffset,
            float clearanceAlongAim,
            bool directionFromTargetPoint,
            out Vector3 aimDirection,
            out Vector3 spawnPosition)
        {
            Transform originTransform = spawnOrigin != null ? spawnOrigin : transform;
            Vector3 origin = originTransform.position + spawnLocalOffset;
            aimDirection = Vector3.forward;

            if (TryGetBowViewportAimRay(out Ray aimRay, out Vector3 targetPoint))
            {
                if (directionFromTargetPoint)
                {
                    aimDirection = targetPoint - origin;
                    if (aimDirection.sqrMagnitude > 1e-8f)
                        aimDirection.Normalize();
                    else
                        aimDirection = aimRay.direction.normalized;
                }
                else
                {
                    aimDirection = aimRay.direction.normalized;
                }
            }
            else
            {
                if (!_loggedMissingBowAimCamera)
                {
                    _loggedMissingBowAimCamera = true;
                    Debug.LogWarning($"{nameof(BeaverPlayerBehaviour)}: no Camera available for screen-center aim; using spine forward as fallback.", this);
                }

                aimDirection = Spine != null ? Spine.forward : transform.forward;
                if (aimDirection.sqrMagnitude < 1e-8f)
                    aimDirection = Vector3.forward;
                else
                    aimDirection.Normalize();
            }

            float clearance = Mathf.Max(0f, clearanceAlongAim);
            spawnPosition = origin + aimDirection * clearance;
            return true;
        }

        bool TryGetScreenCenterViewportRay(out Ray aimRay, bool exactScreenCenter = false)
        {
            aimRay = default;
            Camera cam = ResolveBowAimCamera();
            if (cam == null)
                return false;

            float viewportY = exactScreenCenter ? 0.5f : 0.5f + bowAimViewportYOffset;
            aimRay = cam.ViewportPointToRay(new Vector3(0.5f, viewportY, 0f));
            return true;
        }

        bool TryGetValidScreenCenterHit(Ray ray, float maxDist, LayerMask mask, out RaycastHit validHit)
        {
            validHit = default;
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDist, mask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            float minCameraHit = Mathf.Max(0.1f, minFireBreathCameraHitDistance);

            for (var i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || IsBowAimHitSelf(col))
                    continue;

                if (hits[i].distance < minCameraHit)
                    continue;

                validHit = hits[i];
                return true;
            }

            return false;
        }

        Camera ResolveFireBreathAimCamera()
        {
            if (Camera.main != null)
                return Camera.main;

            return ResolveBowAimCamera();
        }

        public bool TryResolveFireBreathLaunchPose(
            Vector3 firePointWorld,
            out Vector3 projectileOrigin,
            out Vector3 aimDirection,
            out Vector3 targetPoint)
        {
            projectileOrigin = firePointWorld;
            aimDirection = Vector3.forward;
            targetPoint = firePointWorld + Vector3.forward * 10f;

            Camera cam = ResolveFireBreathAimCamera();
            float maxDist = Mathf.Max(1f, bowAimRayMaxDistance);
            Vector3 cameraForward = Vector3.forward;

            if (cam == null)
            {
                if (!_loggedMissingBowAimCamera)
                {
                    _loggedMissingBowAimCamera = true;
                    Debug.LogWarning($"{nameof(BeaverPlayerBehaviour)}: no Camera available for FireBreath screen-center aim; using forward fallback.", this);
                }

                cameraForward = Spine != null ? Spine.forward : transform.forward;
                if (cameraForward.sqrMagnitude < 1e-8f)
                    cameraForward = Vector3.forward;
                else
                    cameraForward.Normalize();

                targetPoint = firePointWorld + cameraForward * maxDist;
            }
            else
            {
                Ray aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                cameraForward = aimRay.direction.sqrMagnitude > 1e-8f ? aimRay.direction.normalized : cam.transform.forward;

                LayerMask mask = ResolveBowAimRaycastLayers();
                if (TryGetValidScreenCenterHit(aimRay, maxDist, mask, out RaycastHit hit))
                    targetPoint = hit.point;
                else
                    targetPoint = aimRay.origin + aimRay.direction * maxDist;

                if (debugFireBreathLaunch)
                    Debug.DrawRay(aimRay.origin, aimRay.direction * maxDist, Color.yellow, 3f);
            }

            Vector3 toTarget = targetPoint - firePointWorld;
            if (toTarget.sqrMagnitude < 1e-8f || Vector3.Dot(toTarget.normalized, cameraForward) < 0.01f)
                aimDirection = cameraForward;
            else
                aimDirection = toTarget.normalized;

            float spawnOffset = Mathf.Max(0f, fireBreathSpawnForwardOffset);
            projectileOrigin = firePointWorld + aimDirection * spawnOffset;

            if (debugFireBreathLaunch)
            {
                Debug.Log(
                    $"{nameof(BeaverPlayerBehaviour)} FireBreath aim camForward={cameraForward} firePoint={firePointWorld} origin={projectileOrigin} target={targetPoint} dir={aimDirection}",
                    this);
                Debug.DrawLine(projectileOrigin, targetPoint, Color.cyan, 3f);
                Debug.DrawRay(projectileOrigin, aimDirection * 24f, Color.red, 3f);
            }

            return true;
        }

        public void CollectOwnerProjectileIgnoreColliders(HashSet<Collider> colliders)
        {
            if (colliders == null || ArmorSet == null)
                return;

            for (var i = 0; i < ArmorSet.Length; i++)
            {
                GameObject armorPiece = ArmorSet[i];
                if (armorPiece == null || !armorPiece.activeInHierarchy)
                    continue;

                foreach (Collider c in armorPiece.GetComponentsInChildren<Collider>(true))
                {
                    if (c != null)
                        colliders.Add(c);
                }
            }
        }

        public bool TryLaunchFireBreathFromAttackPoint(Projectile fireBreathPrefab, Transform firePoint, Vector3 spawnLocalOffset)
        {
            if (fireBreathPrefab == null || firePoint == null)
                return false;

            Vector3 worldFirePoint = firePoint.position;
            if (spawnLocalOffset.sqrMagnitude > 1e-8f)
                worldFirePoint += firePoint.TransformDirection(spawnLocalOffset);

            if (!TryResolveFireBreathLaunchPose(worldFirePoint, out Vector3 spawnPosition, out Vector3 aimDirection, out _))
                return false;

            Quaternion spawnRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            Projectile.Spawn(fireBreathPrefab, spawnPosition, spawnRotation, aimDirection, this);
            return true;
        }

        public void ResetHeadAnimatorLayer()
        {
            if (Otter == null)
                return;

            int headLayer = Otter.GetLayerIndex("Head");
            if (headLayer < 0)
                return;

            Otter.SetLayerWeight(headLayer, 1f);
            Otter.CrossFadeInFixedTime("LookSideWays", 0.05f, headLayer, 0f);
        }

        public void SetShieldParryAnimator(bool active)
        {
            if (Otter == null)
                return;

            Otter.SetBool("shieldParry", active);
        }

        void SetSlashAnimator(bool active)
        {
            if (Otter == null)
                return;

            if (Otter.GetBool("slash") != active)
                Otter.SetBool("slash", active);
        }

        public void ResetSwordAttackPresentation()
        {
            if (otterAction != null)
                otterAction.SetGlowActive(false);

            if (Otter != null)
            {
                SetSlashAnimator(false);
                Otter.SetBool("fight", false);
                SetShieldParryAnimator(false);
            }

            ResetHeadAnimatorLayer();
        }

        bool CanHoldSwordShieldAttack()
        {
            return ArmorEquipped
                && PlayerInputReader.IsPrimaryHeld()
                && CurrentStamina > 0f;
        }

        float GetSwordShieldStaminaCostPerFixedStep()
        {
            return grounded ? SwordShieldGroundStaminaPerFixedStep : SwordShieldAirStaminaPerFixedStep;
        }

        bool HasStaminaForSwordShieldFixedStep()
        {
            return CurrentStamina >= GetSwordShieldStaminaCostPerFixedStep();
        }

        void BeginSwordShieldAttackCycle()
        {
            _swordShieldCycleActive = true;
            if (Otter == null)
                return;

            Otter.SetBool("fight", false);
            SetSlashAnimator(true);
        }

        void EndSwordShieldCycleGlowOnly()
        {
            if (otterAction != null)
                otterAction.SetGlowActive(false);
        }

        void StopSwordShieldChainRoutine()
        {
            if (_swordShieldChainRoutine == null)
                return;

            StopCoroutine(_swordShieldChainRoutine);
            _swordShieldChainRoutine = null;
        }

        IEnumerator PulseSlashForNextCycle()
        {
            _swordShieldChainRoutine = null;
            SetSlashAnimator(false);
            yield return null;

            if (!CanHoldSwordShieldAttack())
                yield break;

            BeginSwordShieldAttackCycle();
        }

        void RequestSwordShieldCycleRestart()
        {
            StopSwordShieldChainRoutine();
            _swordShieldChainRoutine = StartCoroutine(PulseSlashForNextCycle());
        }

        void CompleteSwordShieldCycle(bool tryChainNext)
        {
            EndSwordShieldCycleGlowOnly();
            _swordShieldCycleActive = false;

            if (tryChainNext && CanHoldSwordShieldAttack() && HasStaminaForSwordShieldFixedStep())
            {
                RequestSwordShieldCycleRestart();
                return;
            }

            if (!PlayerInputReader.IsPrimaryHeld())
                ResetSwordAttackPresentation();
            else
            {
                SetSlashAnimator(false);
                if (Otter != null)
                    Otter.SetBool("fight", false);
                ResetHeadAnimatorLayer();
            }
        }

        public void InterruptSwordAttackPresentation()
        {
            _swordShieldAttackHeld = false;
            _swordShieldCycleActive = false;
            StopSwordShieldChainRoutine();
            ResetSwordAttackPresentation();
        }

        void CleanupInterruptedSwordFinisherPresentation()
        {
            if (Otter == null || otterAction == null || !ArmorEquipped)
                return;

            if (PlayerInputReader.IsPrimaryHeld() || _primaryMeleeHeldLastFixed)
                return;

            if (!otterAction.IsFinisherGlowActive())
                return;

            for (var layer = 0; layer < Otter.layerCount; layer++)
            {
                if (Otter.GetCurrentAnimatorStateInfo(layer).IsName("NewSwordJump"))
                    return;
            }

            InterruptSwordAttackPresentation();
        }

        public void OnSwordFinisherAnimationStarted()
        {
            _swordShieldCycleActive = true;
        }

        public void OnSwordFinisherAnimationEnded()
        {
            CompleteSwordShieldCycle(tryChainNext: true);
        }

        void OnDisable()
        {
            _swordShieldAttackHeld = false;
            _swordShieldCycleActive = false;
            StopSwordShieldChainRoutine();
            ResetSwordAttackPresentation();
        }

        void UpdateSwordShieldHeldMelee(bool primaryMeleeHeld)
        {
            _swordShieldAttackHeld = primaryMeleeHeld;

            if (!primaryMeleeHeld)
            {
                _swordShieldCycleActive = false;
                StopSwordShieldChainRoutine();
                ResetSwordAttackPresentation();
                return;
            }

            if (!HasStaminaForSwordShieldFixedStep())
            {
                _swordShieldCycleActive = false;
                StopSwordShieldChainRoutine();
                SetSlashAnimator(false);
                if (Otter != null)
                    Otter.SetBool("fight", false);
                return;
            }

            if (!_swordShieldCycleActive && _swordShieldChainRoutine == null)
                BeginSwordShieldAttackCycle();

            if (!_swordShieldCycleActive)
                return;

            CurrentStamina -= GetSwordShieldStaminaCostPerFixedStep();
            HealthBar.SetStamina(CurrentStamina);

            if (Otter == null)
                return;

            Otter.SetBool("fight", false);
            SetSlashAnimator(true);

            if (grounded == false)
            {
                if (Otter.GetCurrentAnimatorStateInfo(0).IsName("Air Kick"))
                {
                    var WindTrail = Instantiate(KickWind, KickEffectPos.position, Quaternion.Euler(-90, UnityEngine.Random.Range(0f, 360f), 0));
                    WindTrail.transform.parent = KickEffectPos;
                }
                if (Otter.GetCurrentAnimatorStateInfo(0).IsName("HuricaneSword"))
                {
                    var SwordTrail = Instantiate(SwordCopter, KickEffectPos.position + new Vector3(0, 0.5f, 0), Quaternion.Euler(-90, UnityEngine.Random.Range(0f, 360f), 0));
                    SwordTrail.transform.parent = KickEffectPos;
                }
                if (Otter.speed > 0.4)
                    Player.useGravity = false;
                else
                    Player.useGravity = true;
            }
            else if (Otter.GetCurrentAnimatorStateInfo(1).IsName("AttackA"))
            {
                if (Root.childCount == 0)
                {
                    var RightWind = Instantiate(BoxWind, Root.position - new Vector3(0, 0.3f, 0), rotGoal * Quaternion.Euler(-90, 90, 0));
                    RightWind.transform.parent = Root;
                }
            }
            else if (Otter.GetCurrentAnimatorStateInfo(1).IsName("AttackB"))
            {
                if (Root.childCount == 0)
                {
                    var LeftWind = Instantiate(BoxWind, Root.position - new Vector3(0, 0.3f, 0), rotGoal * Quaternion.Euler(90, 90, 0));
                    LeftWind.transform.parent = Root;
                }
            }
        }

        bool TryGetBowScreenAim(out Vector3 fireDirection, out Vector3 spawnPosition, out Quaternion projectileRotation)
        {
            Transform spawnOrigin = Spine != null ? Spine : transform;
            TryGetProjectileLaunchFromCameraCenter(
                spawnOrigin,
                Vector3.zero,
                bowProjectileSpawnClearance,
                directionFromTargetPoint: false,
                out fireDirection,
                out spawnPosition);
            projectileRotation = Quaternion.LookRotation(fireDirection);
            return true;
        }

        Projectile ResolveArrowProjectilePrefab()
        {
            if (Arrow == null)
                return null;

            if (Arrow.TryGetComponent(out Projectile single) && single.isArrow)
                return single;

            var all = Arrow.GetComponents<Projectile>();
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].isArrow)
                    return all[i];
            }

            return all.Length > 0 ? all[0] : null;
        }

        bool TryFireBowFromScreenCenter()
        {
            Projectile projectilePrefab = ResolveArrowProjectilePrefab();
            if (projectilePrefab == null)
                return false;

            if (!TryGetBowScreenAim(out Vector3 fireDirection, out Vector3 spawnPos, out _))
                return false;

            int ammoBefore = arrowMunition;
            var spawned = Projectile.Spawn(projectilePrefab, spawnPos, Quaternion.identity, fireDirection, this);
            if (spawned == null)
            {
                if (debugBowCombat)
                    Debug.Log($"[Bow] Spawn failed; ammo stays {ammoBefore}", this);
                return false;
            }

            ConsumeArrow(1);
            MarkBowShotResolved();
            if (debugBowCombat)
                Debug.Log($"[Bow] Fired 1 arrow. Ammo {ammoBefore} -> {arrowMunition}", this);

            Sound.ArrowShoot();
            CurrentStamina -= EffectiveBowShotStaminaCost;
            if (HealthBar != null)
                HealthBar.SetStamina(CurrentStamina);
            return true;
        }
        public void SpawnCheckpointPopUpEffect(Vector3 position) => SpawnPopUpEffect(position);

        void SpawnPickUpEffect(Vector3 position) => PooledOneShotVfx.Spawn(PickUpEffect, position, Quaternion.identity);

        void SpawnPopUpEffect(Vector3 position) => PooledOneShotVfx.Spawn(PopUpEffect, position, Quaternion.identity);

        public void ShowCursor()
        {
            PlayerCursorRules.ApplyUnlockedVisible();
        }

        public void HideCursor()
        {
            PlayerCursorRules.ApplyLockedHidden(FreeLook, Root);
        }

        public void ApplyTraderOfferPresentation(Transform traderLookTarget)
        {
            if (traderLookTarget == null)
                return;

            ShowCursor();
            isAtTrader = true;
            if (FreeLook != null)
                FreeLook.enabled = false;
            if (CamForTraders != null)
            {
                CamForTraders.enabled = true;
                CamForTraders.m_LookAt = traderLookTarget;
                CamForTraders.m_XAxis.m_MaxSpeed = 0f;
                CamForTraders.m_YAxis.m_MaxSpeed = 0f;
            }
        }

        public void RestoreGameplayAfterTrader()
        {
            isAtTrader = false;
            if (CamForTraders != null)
            {
                CamForTraders.enabled = false;
                CamForTraders.m_LookAt = null;
                CamForTraders.m_XAxis.m_MaxSpeed = initialCamForTradersXAxisSpeed;
                CamForTraders.m_YAxis.m_MaxSpeed = initialCamForTradersYAxisSpeed;
            }
            if (FreeLook != null && FreeLook.m_LookAt != null)
            {
                FreeLook.enabled = true;
                Vector3 safeLookDir = Vector3.forward;
                if (Root != null)
                {
                    safeLookDir = Root.position - transform.position;
                    safeLookDir.y = 0f;
                }

                if (safeLookDir.sqrMagnitude < 1e-6f)
                {
                    safeLookDir = new Vector3(transform.forward.x, 0, transform.forward.z);
                }

                if (safeLookDir.sqrMagnitude < 1e-6f)
                    safeLookDir = Vector3.forward;
                safeLookDir.Normalize();

                if (safeLookDir.sqrMagnitude > LookRotationEpsilon)
                {
                    var targetLookRot = Quaternion.LookRotation(safeLookDir, Vector3.up);
                    FreeLook.m_LookAt.rotation = Quaternion.Slerp(transform.rotation, targetLookRot, 0.5f);
                }
            }
            HideCursorUnlessGamePaused();
        }

        void HideCursorUnlessGamePaused()
        {
            if (IsPauseLikeFullscreenUi())
                return;

            HideCursor();
        }

        PauseController ResolvePauseController()
        {
            if (pauseController == null)
                pauseController = FindObjectOfType<PauseController>();

            return pauseController;
        }

        public bool IsGameplayInputLocked()
        {
            if (isAtTrader)
                return true;

            PauseController activePauseController = ResolvePauseController();
            if (activePauseController != null && activePauseController.ActivePause)
                return true;

            if (LooseScreen != null && LooseScreen.activeSelf)
                return true;
            return false;
        }

        bool IsPauseLikeFullscreenUi()
        {
            PauseController activePauseController = ResolvePauseController();
            if (activePauseController != null && activePauseController.ActivePause)
                return true;

            if (LooseScreen != null && LooseScreen.activeSelf)
                return true;
            return false;
        }

        void ApplyPauseLikeUiCameraState()
        {
            if (IsPauseLikeFullscreenUi())
            {
                PlayerCursorRules.ApplyUnlockedVisible();
                if (FreeLook != null)
                    FreeLook.enabled = false;
                if (CamForTraders != null)
                    CamForTraders.enabled = false;
                return;
            }

            if (isAtTrader)
            {
                PlayerCursorRules.ApplyUnlockedVisible();
                if (CamForTraders != null && CamForTraders.m_LookAt != null)
                {
                    if (FreeLook != null)
                        FreeLook.enabled = false;
                    CamForTraders.enabled = true;
                    CamForTraders.m_XAxis.m_MaxSpeed = 0f;
                    CamForTraders.m_YAxis.m_MaxSpeed = 0f;
                }
                else
                {
                    if (CamForTraders != null)
                        CamForTraders.enabled = false;
                    if (FreeLook != null)
                        FreeLook.enabled = true;
                }

                return;
            }

            if (CamForTraders != null)
            {
                CamForTraders.enabled = false;
                CamForTraders.m_LookAt = null;
                CamForTraders.m_XAxis.m_MaxSpeed = initialCamForTradersXAxisSpeed;
                CamForTraders.m_YAxis.m_MaxSpeed = initialCamForTradersYAxisSpeed;
            }

            if (FreeLook != null)
                FreeLook.enabled = true;
        }

        public void ActivateLooseMenu()
        {
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.LooseScreen, true);
            ShowCursor();
            if (LooseScreen != null)
                LooseScreen.SetActive(true);
        }

        public void HideLooseMenu()
        {
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.LooseScreen, false);
            if (LooseScreen != null)
                LooseScreen.SetActive(false);
            if (!IsGameplayInputLocked())
                HideCursor();
        }
        PlayerCheckpointRespawn CheckpointRespawn
        {
            get
            {
                if (checkpointRespawn == null)
                {
                    checkpointRespawn = GetComponent<PlayerCheckpointRespawn>();
                }

                if (checkpointRespawn == null)
                {
                    checkpointRespawn = gameObject.AddComponent<PlayerCheckpointRespawn>();
                }

                checkpointRespawn.Bind(this);
                return checkpointRespawn;
            }
        }

        public void RestartCheckpoint()
        {
            CheckpointRespawn.RestartCheckpoint();
        }
        public void RestartGame()
        {
            GameTimeScaleGate.ClearAll();
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
                    SetShieldParryAnimator(true);
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
            SetShieldParryAnimator(false);
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
            CacheMainCamera();
            BindHudState();
            if (arrowModel != null)
                arrowModel.SetActive(false);
            bowAim = new Vector3(-0.33f, 20f, -0.3f);
            if (CamForTraders != null)
                CamForTraders.enabled = false;
            if (Root != null)
                SpawnPopUpEffect(Root.position);
            HideCursor();
            if (AimIcon != null)
                AimIcon.SetActive(false);
            if (MunitionDisplay != null)
                MunitionDisplay.SetActive(false);
            Player = GetComponent<Rigidbody>();
            var gmObject = GameObject.FindGameObjectWithTag("GM");
            GM = gmObject != null ? gmObject.GetComponent<GameMaster>() : null;
            //Enable/Disable Background music
            if (seekMusic == true)
            {
                Music = GameObject.Find("GameMusic").GetComponent<MusicPlaylist>();
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
            if (combatBalance != null)
            {
                MaxHealth = combatBalance.maxHealth;
                MaxStamina = combatBalance.maxStamina;
                attackRange = combatBalance.attackRange;
                GroundBeat = combatBalance.groundBeat;
                AirBeat = combatBalance.airBeat;
            }

            CurrentHealth = MaxHealth;
            if (HealthBar != null)
            {
                HealthBar.SetMaxHealth(CurrentHealth);
                CurrentStamina = MaxStamina;
                HealthBar.SetMaxStamina(CurrentStamina);
            }
            else
            {
                CurrentStamina = MaxStamina;
            }

            if (HealEffect != null)
                HealShape = HealEffect.shape;
            ParryOFF();
            HoneyOFF();
            GoldOFF();
            if (ElectricEffect != null)
                ElectricEffect.SetActive(false);
            Lives = 3;
            StaminaClock = StaminaClockInitial;
            JumpLimit = JumpNum;
            GobletJumpLimit = JumpNum + 2;
            FallClock = InitialFall;
            InsertWalk = Walk;
            InsertRun = Run;
            if (LooseScreen != null)
                LooseScreen.SetActive(false);
            if (HologramedBridge != null)
                HologramedBridge.SetActive(false);
            if (appleOBJ != null)
                appleOBJ.SetActive(false);
            if (gobletOBJ != null)
                gobletOBJ.SetActive(false);
            if (ArmorSet != null)
            {
                for (int i = 0; i < ArmorSet.Length; i++)
                {
                    if (ArmorSet[i] != null)
                        ArmorSet[i].SetActive(false);
                }
            }

            if (Bow != null)
            {
                for (int i = 0; i < Bow.Length; i++)
                {
                    if (Bow[i] != null)
                        Bow[i].SetActive(false);
                }
            }

            if (FreeLook != null)
            {
                FreeLook.m_Orbits[0].m_Radius = 4;
                FreeLook.m_Orbits[1].m_Radius = 6;
                FreeLook.m_Orbits[2].m_Radius = 5;
            }
            pauseController = FindObjectOfType<PauseController>();
            if (CamForTraders != null)
            {
                initialCamForTradersXAxisSpeed = CamForTraders.m_XAxis.m_MaxSpeed;
                initialCamForTradersYAxisSpeed = CamForTraders.m_YAxis.m_MaxSpeed;
            }
            var flatFwd = new Vector3(transform.forward.x, 0, transform.forward.z);
            stableCameraForwardXZ = flatFwd.sqrMagnitude > 1e-6f ? flatFwd.normalized : Vector3.forward;
            CountArrows();
            SyncHudState();
        }
        [System.Obsolete]
        public void Update()
        {
            ApplyPauseLikeUiCameraState();
            bool inputLocked = IsGameplayInputLocked();
            BufferSecondaryMouseInputEdges(inputLocked);
            CleanupInterruptedSwordFinisherPresentation();

            //Parry animations
            if (!inputLocked && Defend == true)
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
                if (CurrentStamina < MaxStamina)
                {
                    bool regenBlockedByCombatInput = !inputLocked
                        && (PlayerInputReader.IsRollHeld()
                            || PlayerInputReader.IsSecondaryHeld()
                            || PlayerInputReader.IsPrimaryHeld()
                            || PlayerInputReader.IsDefendKeyHeld());
                    if (!regenBlockedByCombatInput)
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
            //Update UI
            {
                HealthPercent = System.Math.Round((CurrentHealth / MaxHealth) * 100f, 1);
                DebugText = HealthPercent + "%";
                StaminaPercent = System.Math.Round((CurrentStamina / MaxStamina) * 100f, 1);
                StaminaText = StaminaPercent + "%";
                Wallet = "COINS: " + Currency;
                SeedText = "NUTS (R): " + NutCount;
                AppleText = "APPLES (T): " + Apple;
                GobletText = "GOBLETS (Y): " + GobletPickup;
                ArrowText = "ARROWS (RM): " + arrowMunition;
                if (ICON_1 != null || ICON_2 != null || ICON_3 != null)
                {
                    if (Lives >= 3)
                    {
                        if (ICON_1 != null) ICON_1.SetActive(true);
                        if (ICON_2 != null) ICON_2.SetActive(true);
                        if (ICON_3 != null) ICON_3.SetActive(true);
                    }
                    else if (Lives == 2)
                    {
                        if (ICON_1 != null) ICON_1.SetActive(false);
                        if (ICON_2 != null) ICON_2.SetActive(true);
                        if (ICON_3 != null) ICON_3.SetActive(true);
                    }
                    else if (Lives == 1)
                    {
                        if (ICON_1 != null) ICON_1.SetActive(false);
                        if (ICON_2 != null) ICON_2.SetActive(false);
                        if (ICON_3 != null) ICON_3.SetActive(true);
                    }
                }
            }
            if (!inputLocked)
            {
            //Jump action
            {
                if (PlayerInputReader.WasJumpPressed() && JumpNum > 0)
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
                if (PlayerInputReader.IsJumpHeld() || PlayerInputReader.WasJumpReleased())
                    JumpNum = JumpNumPreserve;
            }
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
            if (!inputLocked)
            {
            //Crouch action - Place Logs
            {
                if (PlayerInputReader.IsRollHeld() && FreeLook.m_Lens.FieldOfView<20)
                {
                    FreeLook.m_LookAt = Face;
                }

                if (PlayerInputReader.IsRollHeld() && grounded == true && Rolling == false && !PlayerInputReader.IsJumpHeld())
                {
                    if (Load.i == 9)
                        HologramedBridge.SetActive(true);
                    if (!PlayerInputReader.IsMoveForwardHeld() || !PlayerInputReader.IsPrimaryHeld())
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
                if (PlayerInputReader.WasArsenalBrowsePressed())
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

                        InterruptSwordAttackPresentation();

                        switch (Arsenal[arsenalBrowser])
                        {
                            case "Bare Hands":
                                {
                                    Otter.Play("Disarm");
                                    //Turn off Hammers
                                    HammerHeld = false;
                                    GroundAttack = EffectiveBareHandsDamage;
                                    Otter.SetBool("armor", false);
                                    RightHandWeapon.SetActive(false);
                                    LeftHandWeapon.SetActive(false);

                                    //Turn off Bow Gear
                                    bowEquipped = false;
                                    if (MunitionDisplay != null)
                                        MunitionDisplay.SetActive(false);
                                    SetBowActive(false);

                                    //Turn off Armor Set
                                    ArmorEquipped = false;
                                    SetArmorSetActive(false);

                                    break;
                                }

                            case "Hammers":
                                {
                                    Otter.Play("Equip");
                                    //Turn on Hammers
                                    HammerHeld = true;
                                    GroundAttack = EffectiveHammerDamage;
                                    Otter.SetBool("armor", false);
                                    RightHandWeapon.SetActive(true);
                                    LeftHandWeapon.SetActive(true);

                                    //Turn off Bow
                                    bowEquipped = false;
                                    if (MunitionDisplay != null)
                                        MunitionDisplay.SetActive(false);
                                    SetBowActive(false);

                                    //Turn off Armor Set
                                    ArmorEquipped = false;
                                    SetArmorSetActive(false);

                                    break;
                                }

                            case "Bow":
                                {
                                    Otter.Play("Equip");
                                    CountArrows();
                                    //Turn off Hammers
                                    HammerHeld = false;
                                    GroundAttack = EffectiveBowEquippedMeleeDamage;
                                    Otter.SetBool("armor", false);
                                    RightHandWeapon.SetActive(false);
                                    LeftHandWeapon.SetActive(false);

                                    //Turn on Bow
                                    bowEquipped = true;
                                    if (MunitionDisplay != null)
                                        MunitionDisplay.SetActive(true);
                                    SetBowActive(true);

                                    //Turn off Armor Set
                                    ArmorEquipped = false;
                                    SetArmorSetActive(false);

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
                                    if (MunitionDisplay != null)
                                        MunitionDisplay.SetActive(false);
                                    SetBowActive(false);

                                    //Turn on Armor Set
                                    ArmorEquipped = true;
                                    GroundAttack = EffectiveArmorSetDamage;
                                    Otter.SetBool("armor", true);
                                    SetArmorSetActive(true);
                                    break;
                                }

                        }

                    }
                }
            }
            //Plant Seed action
            {
                if (PlayerInputReader.WasNutThrowPressed() && NutCount > 0)
                {
                    Otter.Play("Crouch");
                    Instantiate(Seed, AttackPoint.position, Quaternion.identity);
                    NutCount--;
                }
            }
            //Eat Apple
            {
                if (PlayerInputReader.WasAppleUsePressed() && Apple > 0)
                {
                    appleOBJ.SetActive(true);
                    Otter.Play("Consume");
                    Sound.Eat();
                    if (MaxHealth - CurrentHealth > EffectiveAppleHealAmount)
                    {
                        TakeDamage(-EffectiveAppleHealAmount);
                        HealthBar.SetHealth(CurrentHealth);
                    }
                    else
                    {
                        CurrentHealth = MaxHealth;
                        HealthBar.SetMaxHealth(MaxHealth);
                    }
                    Apple--;
                }
                if (PlayerInputReader.WasAppleUseReleased())
                {
                    appleOBJ.SetActive(false);
                }
            }
            }
            //Use Goblet
            {
                if (!inputLocked)
                {
                    if (PlayerInputReader.WasGobletUsePressed() && GobletPickup > 0)
                    {
                        Otter.Play("Consume");
                        Sound.Drink();
                        GobletON();
                    }

                    if (PlayerInputReader.WasGobletUseReleased())
                    {
                        gobletOBJ.SetActive(false);
                    }
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

            if (!inputLocked /*&& ParryShield.active == false*/)
            {
                bool primaryMeleeHeld = PlayerInputReader.IsPrimaryHeld() && CurrentStamina > 0;
                _primaryMeleeHeldLastFixed = primaryMeleeHeld;

                //Melee action
                {
                    if (primaryMeleeHeld)
                    {
                        if (ArmorEquipped)
                        {
                            UpdateSwordShieldHeldMelee(true);
                        }
                        else
                        {
                            if (HammerHeld == false)
                                CurrentStamina -= 0.03f;
                            if (HammerHeld)
                                CurrentStamina -= 0.1f;

                            HealthBar.SetStamina(CurrentStamina);
                            Otter.SetBool("fight", true);
                            Otter.SetBool("slash", false);

                            if (grounded == false)
                            {
                                if (Otter.GetCurrentAnimatorStateInfo(0).IsName("Air Kick"))
                                {
                                    var WindTrail = Instantiate(KickWind, KickEffectPos.position, Quaternion.Euler(-90, UnityEngine.Random.Range(0f, 360f), 0));
                                    WindTrail.transform.parent = KickEffectPos;
                                }
                                if (Otter.speed > 0.4)
                                {
                                    levitation -= 0.05f;
                                    Otter.speed -= 0.005f;
                                    Player.AddForce(0, levitation, 0);
                                }
                            }
                            else
                            {
                                Otter.SetBool("fight", true);
                                Otter.SetBool("slash", false);
                            }
                        }
                    }
                    else
                    {
                        Player.useGravity = true;
                        if (ArmorEquipped)
                            UpdateSwordShieldHeldMelee(false);
                        else
                        {
                            Otter.SetBool("slash", false);
                            Otter.SetBool("fight", false);
                        }

                        GroundAttack = EffectiveBareHandsDamage;
                        Beat = 0;
                        Otter.speed = AnimSpeed;
                    }
                }
                //Parry Action
                {
                    if (PlayerInputReader.IsDefendKeyHeld() && CurrentStamina > 0 && !PlayerInputReader.IsPrimaryHeld() && !PlayerInputReader.IsSecondaryHeld())
                        ParryON();
                    else if (!primaryMeleeHeld || !ArmorEquipped)
                        ParryOFF();
                }
            }
        }

        [Obsolete]
        public void LateUpdate()
        {
            if (IsGameplayInputLocked())
                return;

            if (PlayerInputReader.IsSecondaryHeld())
            {
                RotateForward();
            }

            if (PlayerInputReader.WasAnyKeyPressedDown() && !PlayerInputReader.IsSecondaryHeld())
            {
                keepLooking = false;
            }
        }

        void CacheMainCamera()
        {
            cachedMainCamera = Camera.main;
            cachedMainCameraTransform = cachedMainCamera != null ? cachedMainCamera.transform : null;
        }

        Transform ResolveMainCameraTransform()
        {
            if (cachedMainCamera == null)
                CacheMainCamera();
            return cachedMainCameraTransform != null ? cachedMainCameraTransform : transform;
        }

        void ReconcileHorizontalCameraAxes(ref Vector3 xzForward, ref Vector3 xzBack, ref Vector3 xzRight, ref Vector3 xzLeft)
        {
            if (xzForward.sqrMagnitude > 1e-8f)
            {
                stableCameraForwardXZ = xzForward;
                return;
            }

            Vector3 fallback = new Vector3(transform.forward.x, 0, transform.forward.z);
            if (fallback.sqrMagnitude > 1e-8f)
                xzForward = fallback;
            else if (stableCameraForwardXZ.sqrMagnitude > 1e-8f)
                xzForward = stableCameraForwardXZ;
            else
                xzForward = Vector3.forward;

            xzBack = new Vector3(-xzForward.x, 0, -xzForward.z);
            xzRight = new Vector3(xzForward.z, 0, -xzForward.x);
            xzLeft = new Vector3(-xzForward.z, 0, xzForward.x);
            stableCameraForwardXZ = xzForward;
        }

        Vector3 ResolveHorizontalMoveDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 1e-8f)
                return direction;
            Vector3 f = new Vector3(transform.forward.x, 0, transform.forward.z);
            if (f.sqrMagnitude > 1e-8f)
                return f;
            if (stableCameraForwardXZ.sqrMagnitude > 1e-8f)
                return stableCameraForwardXZ;
            return Vector3.forward;
        }

        [System.Obsolete]
        public void FixedUpdate()
        {
            //Camera vectors setup
            Transform camTransform = ResolveMainCameraTransform();
            Vector3 cameraRelativeForward = camTransform.TransformDirection(Vector3.forward);
            Vector3 cameraRelativeBack = camTransform.TransformDirection(Vector3.back);
            Vector3 cameraRelativeRight = camTransform.TransformDirection(Vector3.right);
            Vector3 cameraRelativeLeft = camTransform.TransformDirection(Vector3.left);

            //Horizontal camera vectors
            Vector3 XZForward = new(cameraRelativeForward.x, 0, cameraRelativeForward.z);
            Vector3 XZBack = new(cameraRelativeBack.x, 0, cameraRelativeBack.z);
            Vector3 XZRight = new(cameraRelativeRight.x, 0, cameraRelativeRight.z);
            Vector3 XZLeft = new(cameraRelativeLeft.x, 0, cameraRelativeLeft.z);

            ReconcileHorizontalCameraAxes(ref XZForward, ref XZBack, ref XZRight, ref XZLeft);

            bool inputLocked = IsGameplayInputLocked();

            bool secondaryHeldRmb = PlayerInputReader.IsSecondaryHeld();

            if (inputLocked)
            {
                InterruptSwordAttackPresentation();
                if (Stone != null)
                    Stone.SetActive(false);
                if (AimIcon != null)
                    AimIcon.SetActive(false);
                _stoneAimSessionActive = false;
                _bowAimSessionOpen = false;
                _bowFiredThisAimSession = false;
                ClearBowDrawState();
            }

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
                if (!arrowReady && !_stoneAimSessionActive)
                    Otter.SetBool("aim", false);
                //Otter.SetBool("draw", false);
                Rolling = false;
                movementInvoked = false;
                //keepLooking = false;
            }
            if (!inputLocked)
            {
                bool mouse1Down = _mouse1DownBuffered;
                bool mouse1Up = _mouse1UpBuffered;
                _mouse1DownBuffered = false;
                _mouse1UpBuffered = false;

                bool secondaryPressEdge = secondaryHeldRmb && !_secondaryHeldLastFixed;
                bool secondaryReleaseEdge = !secondaryHeldRmb && _secondaryHeldLastFixed;

                // Stoning (RMB hold to aim, release to throw / cancel)
                if (bowEquipped == false)
                {
                    if (secondaryHeldRmb
                        && CurrentStamina > 0
                        && !PlayerInputReader.IsRollHeld()
                        && grounded == true)
                    {
                        _stoneAimSessionActive = true;
                        if (Stone != null)
                            Stone.SetActive(true);
                        if (AimIcon != null)
                            AimIcon.SetActive(true);
                        Otter.SetBool("aim", true);
                        Otter.Play("Aim");
                        if (PlayerInputReader.WasInteractPressed())
                        {
                            if (XZForward.sqrMagnitude > LookRotationEpsilon)
                            {
                                rotGoal = Quaternion.LookRotation(XZForward);
                                transform.rotation = rotGoal;
                            }
                            Otter.Play("Crouch");
                        }
                    }

                    if (_stoneAimSessionActive && secondaryHeldRmb)
                    {
                        bool canMaintainStoneAim = CurrentStamina > 0
                            && !PlayerInputReader.IsRollHeld()
                            && grounded;
                        if (!canMaintainStoneAim)
                        {
                            Otter.SetBool("aim", false);
                            if (Stone != null)
                                Stone.SetActive(false);
                            if (AimIcon != null)
                                AimIcon.SetActive(false);
                            _stoneAimSessionActive = false;
                            keepLooking = false;
                        }
                    }

                    if (_stoneAimSessionActive && !secondaryHeldRmb)
                    {
                        if (CurrentStamina > 0)
                        {
                            Otter.SetBool("slash", false);
                            Otter.Play("Throw");
                            Transform stoneSpawnOrigin = AttackPoint != null ? AttackPoint : (Spine != null ? Spine : transform);
                            TryGetProjectileLaunchFromCameraCenter(
                                stoneSpawnOrigin,
                                new Vector3(0f, 0.6f, 0f),
                                bowProjectileSpawnClearance,
                                directionFromTargetPoint: false,
                                out Vector3 stoneAimDirection,
                                out Vector3 stoneSpawnPosition);
                            Projectile.Spawn(
                                Ball,
                                stoneSpawnPosition,
                                Quaternion.LookRotation(stoneAimDirection),
                                stoneAimDirection,
                                this);
                            CurrentStamina -= EffectiveStoneThrowStaminaCost;
                            HealthBar.SetStamina(CurrentStamina);
                        }

                        Otter.SetBool("aim", false);
                        if (Stone != null)
                            Stone.SetActive(false);
                        if (AimIcon != null)
                            AimIcon.SetActive(false);
                        _stoneAimSessionActive = false;
                        keepLooking = false;
                    }
                }

                // Bow (press to nock, release edge to fire once per draw session)
                if (bowEquipped)
                {
                    if (!secondaryHeldRmb)
                        _bowWaitingForRmbRelease = false;

                    bool bowPressEdge = secondaryPressEdge || mouse1Down;
                    if (CanBeginBowDraw() && bowPressEdge)
                    {
                        if (XZForward.sqrMagnitude > LookRotationEpsilon)
                        {
                            rotGoal = Quaternion.LookRotation(XZForward);
                            transform.rotation = rotGoal;
                        }
                        if (HasArrowAmmo())
                        {
                            _bowAimSessionOpen = true;
                            _bowFiredThisAimSession = false;
                            CountArrows();
                            Sound.ArrowDraw();
                            arrowReady = true;
                            if (AimIcon != null)
                                AimIcon.SetActive(true);
                            if (arrowModel != null)
                                arrowModel.SetActive(true);
                            if (bowString != null)
                                bowString.SetActive(false);
                            Otter.SetBool("draw", true);
                            Otter.SetBool("aim", true);
                            if (stringLine != null)
                            {
                                stringLine.enabled = true;
                                stringLine.useWorldSpace = false;
                            }
                            if (debugBowCombat)
                                Debug.Log($"[Bow] Nock started. Ammo={arrowMunition}", this);
                        }
                        else if (arrowMunition <= 0)
                        {
                            Sound.Error();
                            Plattering = "Not enough arrows!";
                            ChangeSpeech = 1f;
                        }
                    }

                    bool bowReleaseEdge = secondaryReleaseEdge || mouse1Up;
                    if (_bowAimSessionOpen && arrowReady && !_bowFiredThisAimSession && bowReleaseEdge)
                    {
                        _bowFiredThisAimSession = true;
                        _bowAimSessionOpen = false;

                        if (!HasArrowAmmo())
                        {
                            Sound.Error();
                            Plattering = "Not enough arrows!";
                            ChangeSpeech = 1f;
                            MarkBowShotResolved();
                            ClearBowDrawState();
                        }
                        else if (CurrentStamina <= 0)
                        {
                            Plattering = "Jee, let me catch a breath!";
                            ChangeSpeech = 1f;
                            MarkBowShotResolved();
                            ClearBowDrawState();
                        }
                        else if (TryFireBowFromScreenCenter())
                        {
                            ClearBowDrawState();
                        }
                        else
                        {
                            MarkBowShotResolved();
                            ClearBowDrawState();
                        }
                    }
                    else if (arrowReady)
                    {
                        Otter.SetBool("draw", true);
                        Otter.SetBool("aim", true);
                    }
                }

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
                if (!PlayerInputReader.IsRollHeld())
                {
                    HealQue = 3;
                    if (PlayerInputReader.IsMoveForwardHeld())
                    {
                        PlayerMove(XZForward);
                    }
                    if (PlayerInputReader.IsMoveBackHeld())
                    {
                        PlayerMove(XZBack);
                    }
                    if (PlayerInputReader.IsMoveRightHeld())
                    {
                        //Otter.Play("Walk Right");
                        PlayerMove(XZRight);
                    }
                    if (PlayerInputReader.IsMoveLeftHeld())
                    {
                        //Otter.Play("Walk Left");
                        PlayerMove(XZLeft);
                    }
                    if (PlayerInputReader.IsMoveForwardHeld() && PlayerInputReader.IsMoveRightHeld())
                    {
                        PlayerMove(XZForward + XZRight);
                    }
                    if (PlayerInputReader.IsMoveForwardHeld() && PlayerInputReader.IsMoveLeftHeld())
                    {
                        PlayerMove(XZForward + XZLeft);
                    }
                    if (PlayerInputReader.IsMoveBackHeld() && PlayerInputReader.IsMoveRightHeld())
                    {
                        PlayerMove(XZBack + XZRight);
                    }
                    if (PlayerInputReader.IsMoveBackHeld() && PlayerInputReader.IsMoveLeftHeld())
                    {
                        PlayerMove(XZBack + XZLeft);
                    }
                }
                //Movement + Crouch = Roll & Evade
                if (PlayerInputReader.IsRollHeld() && CurrentStamina > 0)
                {
                    HealQue = 3;
                    if (PlayerInputReader.IsMoveForwardHeld())
                    {
                        PlayerRoll(XZForward);
                    }
                    if (PlayerInputReader.IsMoveBackHeld())
                    {
                        PlayerRoll(XZBack);
                    }
                    if (PlayerInputReader.IsMoveRightHeld())
                    {
                        PlayerRoll(XZRight);
                    }
                    if (PlayerInputReader.IsMoveLeftHeld())
                    {
                        PlayerRoll(XZLeft);
                    }
                    if (PlayerInputReader.IsMoveForwardHeld() && PlayerInputReader.IsMoveRightHeld())
                    {
                        PlayerRoll(XZForward + XZRight);
                    }
                    if (PlayerInputReader.IsMoveForwardHeld() && PlayerInputReader.IsMoveLeftHeld())
                    {
                        PlayerRoll(XZForward + XZLeft);
                    }
                    if (PlayerInputReader.IsMoveBackHeld() && PlayerInputReader.IsMoveRightHeld())
                    {
                        PlayerRoll(XZBack + XZRight);
                    }
                    if (PlayerInputReader.IsMoveBackHeld() && PlayerInputReader.IsMoveLeftHeld())
                    {
                        PlayerRoll(XZBack + XZLeft);
                    }
                }
            }

            }

            _secondaryHeldLastFixed = secondaryHeldRmb;

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
                        Vector3 velFlat = new Vector3(Player.velocity.x, 0, Player.velocity.z);
                        if (velFlat.sqrMagnitude < 1e-8f)
                            velFlat = ResolveHorizontalMoveDirection(velFlat);
                        else
                            velFlat.Normalize();
                        if (velFlat.sqrMagnitude > LookRotationEpsilon)
                        {
                            rotGoal = Quaternion.LookRotation(velFlat);
                            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.5f);
                        }
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

        void BufferSecondaryMouseInputEdges(bool inputLocked)
        {
            if (inputLocked)
            {
                _mouse1DownBuffered = false;
                _mouse1UpBuffered = false;
                return;
            }

            _mouse1DownBuffered |= UnityEngine.Input.GetKeyDown(KeyCode.Mouse1);
            _mouse1UpBuffered |= UnityEngine.Input.GetKeyUp(KeyCode.Mouse1);
        }

        void SetBowActive(bool active)
        {
            if (Bow == null)
                return;

            for (int i = 0; i < Bow.Length; i++)
            {
                if (Bow[i] != null)
                    Bow[i].SetActive(active);
            }
        }

        void SetArmorSetActive(bool active)
        {
            if (ArmorSet == null)
                return;

            for (int i = 0; i < ArmorSet.Length; i++)
            {
                if (ArmorSet[i] != null)
                    ArmorSet[i].SetActive(active);
            }
        }

        void ClearBowDrawState()
        {
            _bowAimSessionOpen = false;
            if (arrowModel != null)
                arrowModel.SetActive(false);
            if (stringLine != null)
                stringLine.enabled = false;
            if (bowString != null)
                bowString.SetActive(true);
            if (Otter != null)
            {
                Otter.SetBool("draw", false);
                Otter.SetBool("aim", false);
            }
            if (AimIcon != null)
                AimIcon.SetActive(false);
            arrowReady = false;
            keepLooking = false;
        }

        void BindHudState()
        {
            if (PlayerHudState == null)
                PlayerHudState = GetComponent<PlayerHudState>();

            if (PlayerHudState == null)
            {
                PlayerHudState = gameObject.AddComponent<PlayerHudState>();
                LogHudStateFallback();
            }

            if (PlayerObjective == null)
                PlayerObjective = GetComponent<ObjectiveUI>();
        }

        void LogHudStateFallback()
        {
            if (loggedRuntimeHudStateFallback)
                return;

            loggedRuntimeHudStateFallback = true;
            Debug.LogWarning($"{nameof(BeaverPlayerBehaviour)} added a missing {nameof(PlayerHudState)} component to {name} at runtime so HUD text can update.", this);
        }

        void SyncHudState()
        {
            if (PlayerHudState == null)
                BindHudState();

            if (PlayerHudState != null)
                PlayerHudState.CopyFrom(this, PlayerObjective != null, PlayerObjective != null ? PlayerObjective.Instruction : null);
        }

        public bool OwnsArsenalItem(string itemId)
        {
            return Arsenal != null && Arsenal.Contains(itemId);
        }

        public bool TryAcquireHammersFromShop()
        {
            if (OwnsArsenalItem("Hammers"))
                return false;

            if (Arsenal == null)
                Arsenal = new List<string>();

            Arsenal.Add("Hammers");
            ArsenalCounter++;
            return true;
        }

        public bool TryAcquireBowFromShop(int starterArrowCount = 5)
        {
            if (OwnsArsenalItem("Bow"))
                return false;

            if (Arsenal == null)
                Arsenal = new List<string>();

            Arsenal.Add("Bow");
            ArsenalCounter++;

            if (arrowMunition <= 0)
                SetArrowMunition(starterArrowCount);
            else
                ApplyArrowMunitionDelta(starterArrowCount);

            return true;
        }

        public bool TryAcquireArmorSetFromShop()
        {
            if (OwnsArsenalItem("ArmorSet"))
                return false;

            if (Arsenal == null)
                Arsenal = new List<string>();

            Arsenal.Add("ArmorSet");
            ArsenalCounter++;
            return true;
        }

        public void AddArrowMunition(int amount)
        {
            if (amount <= 0)
                return;

            ApplyArrowMunitionDelta(amount);
        }
    }
}
