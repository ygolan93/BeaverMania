using System;
using System.Collections;
using System.Collections.Generic;
using Beavermania.Audio;
using Beavermania.Core.GameFlow;
using Beavermania.Display;
using Beavermania.Core.Input;
using Beavermania.NPC;
using Beavermania.Objects;
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
        bool _restoreJumpLimitAfterGobletWhenGrounded;
        public Transform Root;
        public Transform Face;
        public Transform Spine;
        public Transform Hips;
        public Animator Otter;
        [SerializeField] AnimatedAttack otterAction;
        [SerializeField] Combat.SwordEffects swordEffects;
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
        [SerializeField] Combat.BoostChargeController boostChargeController;
        [SerializeField] Combat.Weapon weaponLoadout;

        public Combat.BoostChargeController BoostCharge => boostChargeController;

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
        bool _swordGlareAvailabilityCached;
        bool _swordGlareAvailabilityCacheValid;
        const float SwordShieldGroundStaminaPerFixedStep = 0.2f;
        const float SwordShieldAirStaminaPerFixedStep = 0.5f;

        [System.Flags]
        enum AimMarkSource
        {
            None = 0,
            Bow = 1 << 0,
            Stoning = 1 << 1,
            SwordShieldSlash = 1 << 2,
        }

        AimMarkSource _aimMarkSources = AimMarkSource.None;
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
        public const float BoostDurationSeconds = 10f;
        public float GobletClock = BoostDurationSeconds;
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
        public float ChangeSpeech = 0.55f;
        Vector3 stableCameraForwardXZ = Vector3.forward;
        Camera cachedMainCamera;
        Transform cachedMainCameraTransform;
        const float LookRotationEpsilon = 0.0001f;

        public PlayerCombatBalanceData CombatBalance => combatBalance;
        public WeaponData EquippedWeaponData => weaponLoadout != null ? weaponLoadout.EquippedWeapon : null;

        float EffectiveScorpionLightDamage => combatBalance != null ? combatBalance.scorpionLightDamage : 15f;
        float EffectiveScorpionHeavyDamage => combatBalance != null ? combatBalance.scorpionHeavyDamage : 30f;
        float EffectiveShroomHealPerTick => combatBalance != null ? combatBalance.shroomHealPerTick : 2f;
        float EffectiveAppleHealAmount => combatBalance != null ? combatBalance.appleHealAmount : 500f;
        float EffectiveStoneThrowStaminaCost => combatBalance != null ? combatBalance.stoneThrowStaminaCost : 20f;
        float EffectiveBowShotStaminaCost
        {
            get
            {
                WeaponData bowData = weaponLoadout != null ? weaponLoadout.ResolveBow() : null;
                return bowData != null ? bowData.bowShotStaminaCost : 30f;
            }
        }

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
                ResolveWeaponLoadout()?.TryAddOwned(ResolveWeaponLoadout()?.ResolveHammer());
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
                ResolveWeaponLoadout()?.TryAddOwned(ResolveWeaponLoadout()?.ResolveBow());
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
                ResolveWeaponLoadout()?.TryAddOwned(ResolveWeaponLoadout()?.ResolveArmorSet());
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
                Sound.PickItem();
                CollectiblePickUpVfxEmitter.TryPlay(OBJ.gameObject);
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
                if (Music == null)
                    return;

                if (int.TryParse(OBJ.gameObject.name, out int songIndex))
                    Music.ChangeSong(songIndex);
                else
                    Debug.LogWarning("The game object's name is not a valid integer: " + OBJ.gameObject.name);
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
                    if (trader.skipPressed)
                        RestoreGameplayAfterTrader();
                    else if (trader.IsTraderSessionActive())
                        ApplyTraderOfferPresentation(OBJ.transform);
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
                var trader = OBJ.GetComponentInParent<Trader>();
                if (trader == null)
                    trader = OBJ.GetComponent<Trader>();
                if (trader == null || !trader.IsTraderSessionActive())
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
        void ApplyHealthLoss(float amount)
        {
            if (amount <= 0f)
                return;

            CurrentHealth -= amount;
            if (HealthBar != null)
                HealthBar.SetHealth(CurrentHealth);

            NotifyPlayerHealthChanged();
        }

        static void SetParticleEmissionEnabled(ParticleSystem particleSystem, bool enabled)
        {
            if (particleSystem == null)
                return;

            ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
            emissionModule.enabled = enabled;
        }

        public void TriggerHurtFeedbackOnly()
        {
            hurt = true;
            heal = false;
            StopHurt = 0f;

            SetParticleEmissionEnabled(HurtEffect, true);
            SetParticleEmissionEnabled(HealEffect, false);

            if (HurtLight != null)
            {
                HurtLight.enabled = true;
                if (HealLight != null)
                    HealLight.enabled = false;
            }
        }

        public void TakeDamage(float Damage)
        {
            if (isParried == false)
            {
                if (Damage > 0)
                {
                    ApplyHealthLoss(Damage);
                    TriggerHurtFeedbackOnly();
                    if (boostChargeController != null)
                        boostChargeController.RegisterPlayerDamaged();
                }
                if (Damage < 0)
                {
                    CurrentHealth -= Damage;
                    if (HealthBar != null)
                        HealthBar.SetHealth(CurrentHealth);
                    hurt = false;
                    heal = true;
                    NotifyPlayerHealthChanged();
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

        public bool IsSwordAndShieldEquipped() => ArmorEquipped;

        public bool HasEnoughHpForFireBreath()
        {
            if (MaxHealth <= 0f)
                return false;

            float cost = GetFireBreathHealthCost();
            if (cost <= 0f)
                return true;

            return CurrentHealth > cost;
        }

        public bool CanActivateFireBreath()
        {
            if (!IsSwordAndShieldEquipped())
                return true;

            return HasEnoughHpForFireBreath();
        }

        public void UpdateSwordGlareAvailability()
        {
            bool shouldEnableSwordGlare = IsSwordAndShieldEquipped() && HasEnoughHpForFireBreath();
            _swordGlareAvailabilityCacheValid = true;
            _swordGlareAvailabilityCached = shouldEnableSwordGlare;
            ResolveSwordEffects()?.SetSwordGlareActive(shouldEnableSwordGlare);
        }

        void SyncSwordGlareAvailabilityPassive()
        {
            bool shouldEnableSwordGlare = ArmorEquipped && HasEnoughHpForFireBreath();
            if (_swordGlareAvailabilityCacheValid && shouldEnableSwordGlare == _swordGlareAvailabilityCached)
                return;

            _swordGlareAvailabilityCacheValid = true;
            _swordGlareAvailabilityCached = shouldEnableSwordGlare;
            ResolveSwordEffects()?.SetSwordGlareActive(shouldEnableSwordGlare);
        }

        void NotifyPlayerHealthChanged()
        {
            UpdateSwordGlareAvailability();
        }

        public void StopFireBreathAttackPresentation()
        {
            if (otterAction != null)
                otterAction.StopFireBreathPresentation();

            ResolveSwordEffects()?.StopFireBreathTrailPresentation();
        }

        public void StopFireBreathEffects()
        {
            StopFireBreathAttackPresentation();
            UpdateSwordGlareAvailability();
        }

        public bool TryActivateFireBreath(Projectile fireBreathPrefab, Transform firePoint)
        {
            if (!IsSwordAndShieldEquipped())
                return false;

            if (!HasEnoughHpForFireBreath())
            {
                StopFireBreathAttackPresentation();
                UpdateSwordGlareAvailability();
                return false;
            }

            if (fireBreathPrefab == null || firePoint == null)
            {
                StopFireBreathAttackPresentation();
                UpdateSwordGlareAvailability();
                return false;
            }

            if (!TryLaunchFireBreathFromAttackPoint(fireBreathPrefab, firePoint, Vector3.zero))
            {
                StopFireBreathAttackPresentation();
                UpdateSwordGlareAvailability();
                return false;
            }

            ResolveSwordEffects()?.OnFireBreathActivated();
            UpdateSwordGlareAvailability();
            return true;
        }

        SwordEffects ResolveSwordEffects()
        {
            if (swordEffects != null)
                return swordEffects;

            swordEffects = GetComponentInChildren<SwordEffects>(true);
            return swordEffects;
        }

        public float GetFireBreathHealthCost()
        {
            if (MaxHealth <= 0f)
                return 0f;

            float costPercent = 0f;
            if (EquippedWeaponData != null && EquippedWeaponData.supportsFireBreath)
                costPercent = EquippedWeaponData.fireBreathHealthCostPercent;
            else if (weaponLoadout != null)
            {
                WeaponData armorData = weaponLoadout.ResolveArmorSet();
                if (armorData != null)
                    costPercent = armorData.fireBreathHealthCostPercent;
            }

            if (costPercent <= 0f)
                return 0f;

            return MaxHealth * (costPercent / 100f);
        }

        public bool CanPayFireBreathCost()
        {
            if (!IsSwordAndShieldEquipped())
                return true;

            return HasEnoughHpForFireBreath();
        }

        public bool TryPayFireBreathHealthCost()
        {
            if (!IsSwordAndShieldEquipped())
                return true;

            if (MaxHealth <= 0f)
            {
                if (debugFireBreathLaunch)
                    Debug.Log("FireBreath blocked: invalid max health.", this);
                return false;
            }

            if (isParried)
            {
                if (debugFireBreathLaunch)
                    Debug.Log("FireBreath blocked: cannot pay HP cost while parried.", this);
                return false;
            }

            if (!CanPayFireBreathCost())
            {
                if (debugFireBreathLaunch)
                    Debug.Log("FireBreath blocked: insufficient HP.", this);
                return false;
            }

            float cost = GetFireBreathHealthCost();
            if (cost <= 0f)
                return true;

            ApplyHealthLoss(cost);
            TriggerHurtFeedbackOnly();

            if (debugFireBreathLaunch)
                Debug.Log("FireBreath activated: HP cost paid.", this);
            return true;
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

            if (IsSwordAndShieldEquipped() && !TryPayFireBreathHealthCost())
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

        void SetAimMarkVisible(AimMarkSource source, bool visible)
        {
            if (source == AimMarkSource.None)
                return;

            if (visible)
                _aimMarkSources |= source;
            else
                _aimMarkSources &= ~source;

            RefreshAimMarkVisibility();
        }

        void RefreshAimMarkVisibility()
        {
            if (AimIcon != null)
                AimIcon.SetActive(_aimMarkSources != AimMarkSource.None);
        }

        void ClearAllAimMarkSources()
        {
            _aimMarkSources = AimMarkSource.None;
            RefreshAimMarkVisibility();
        }

        public void ShowAimMarkForBow() => SetAimMarkVisible(AimMarkSource.Bow, true);

        public void HideAimMarkForBow() => SetAimMarkVisible(AimMarkSource.Bow, false);

        public void ShowAimMarkForStoning() => SetAimMarkVisible(AimMarkSource.Stoning, true);

        public void HideAimMarkForStoning() => SetAimMarkVisible(AimMarkSource.Stoning, false);

        public void ShowAimMarkForSwordShieldSlash() => SetAimMarkVisible(AimMarkSource.SwordShieldSlash, true);

        public void HideAimMarkForSwordShieldSlash() => SetAimMarkVisible(AimMarkSource.SwordShieldSlash, false);

        bool IsSwordShieldAimMarkRequested()
        {
            if (!ArmorEquipped)
                return false;

            if (_swordShieldCycleActive || _swordShieldChainRoutine != null)
                return true;

            if (otterAction != null && otterAction.IsFinisherGlowActive())
                return true;

            if (Otter != null)
            {
                AnimatorStateInfo baseState = Otter.GetCurrentAnimatorStateInfo(0);
                if (baseState.IsName("HuricaneSword") || baseState.IsName("NewSwordJump"))
                    return true;

                if (Otter.GetBool("slash"))
                    return true;
            }

            return _swordShieldAttackHeld && PlayerInputReader.IsPrimaryHeld();
        }

        void SyncSwordShieldAimMark()
        {
            SetAimMarkVisible(AimMarkSource.SwordShieldSlash, IsSwordShieldAimMarkRequested());
        }

        public void ResetSwordAttackPresentation()
        {
            HideAimMarkForSwordShieldSlash();
            StopFireBreathAttackPresentation();
            UpdateSwordGlareAvailability();

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
                && HasStaminaForSwordShieldFixedStep();
        }

        float GetSwordShieldStaminaCostPerFixedStep()
        {
            return grounded ? SwordShieldGroundStaminaPerFixedStep : SwordShieldAirStaminaPerFixedStep;
        }

        bool HasStaminaForSwordShieldFixedStep()
        {
            return CurrentStamina >= GetSwordShieldStaminaCostPerFixedStep();
        }

        void EndSwordShieldAttackDueToStamina()
        {
            _swordShieldCycleActive = false;
            StopSwordShieldChainRoutine();

            if (CurrentStamina < GetSwordShieldStaminaCostPerFixedStep())
                CurrentStamina = 0f;

            if (HealthBar != null)
                HealthBar.SetStamina(CurrentStamina);

            if (Player != null)
                Player.useGravity = true;

            ResetSwordAttackPresentation();
        }

        void BeginSwordShieldAttackCycle()
        {
            _swordShieldCycleActive = true;
            SyncSwordShieldAimMark();

            if (Otter == null)
                return;

            Otter.SetBool("fight", false);
            SetSlashAnimator(true);
        }

        void EndSwordShieldCycleGlowOnly()
        {
            StopFireBreathAttackPresentation();
            UpdateSwordGlareAvailability();
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
                SyncSwordShieldAimMark();
            }
        }

        public void InterruptSwordAttackPresentation()
        {
            _swordShieldAttackHeld = false;
            _swordShieldCycleActive = false;
            StopSwordShieldChainRoutine();
            if (Player != null)
                Player.useGravity = true;
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
            SyncSwordShieldAimMark();
        }

        public void OnSwordFinisherAnimationEnded()
        {
            CompleteSwordShieldCycle(tryChainNext: true);
        }

        void OnEnable()
        {
            UpdateSwordGlareAvailability();
        }

        void OnDisable()
        {
            _swordShieldAttackHeld = false;
            _swordShieldCycleActive = false;
            _restoreJumpLimitAfterGobletWhenGrounded = false;
            StopSwordShieldChainRoutine();
            if (Player != null)
                Player.useGravity = true;
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
                EndSwordShieldAttackDueToStamina();
                return;
            }

            if (!_swordShieldCycleActive && _swordShieldChainRoutine == null)
                BeginSwordShieldAttackCycle();

            SyncSwordShieldAimMark();

            if (!_swordShieldCycleActive)
                return;

            CurrentStamina -= GetSwordShieldStaminaCostPerFixedStep();
            if (CurrentStamina < 0f)
                CurrentStamina = 0f;
            if (HealthBar != null)
                HealthBar.SetStamina(CurrentStamina);

            if (!HasStaminaForSwordShieldFixedStep())
            {
                EndSwordShieldAttackDueToStamina();
                return;
            }

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

        public void PlayCoinPickupFeedback(Vector3 worldPosition)
        {
            SpawnPickUpEffect(worldPosition + new Vector3(0f, 0.3f, 0f));
            if (Sound != null)
                Sound.Coin();
        }

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
            if (activePauseController != null
                && (activePauseController.ActivePause || activePauseController.IsPauseMenuVisible))
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
            if (Music != null)
                Music.StopMusic();
            ShowCursor();
            if (LooseScreen != null)
                LooseScreen.SetActive(true);
        }

        public void HideLooseMenu()
        {
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.LooseScreen, false);
            if (Music != null)
                Music.ResumeMusic();
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

        public void RecoverOutOfBounds(Vector3 safePosition)
        {
            transform.position = safePosition;
            if (Player != null)
            {
                Player.velocity = Vector3.zero;
                Player.angularVelocity = Vector3.zero;
            }
        }

        public void RestartGame()
        {
            GameTimeScaleGate.ClearAll();
            Projectile.ClearAllPools();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public void GobletON()
        {
            gobletOBJ.SetActive(true);
            GobletPicked = true;
            _restoreJumpLimitAfterGobletWhenGrounded = false;
            AnimSpeed = 3;
            Walk = 7;
            Run = 18;
            JumpLimit = GobletJumpLimit;
            AirBeat /= 5;
            ElectricEffect.SetActive(true);
            PlayElectricEffectSoundOnce();
            GobletClock = BoostDurationSeconds;
            if (boostChargeController != null)
            {
                boostChargeController.SetBoostActive(true);
                boostChargeController.SetActiveBoostTime(GobletClock, BoostDurationSeconds);
            }
        }
        public void GobletOFF()
        {
            GobletPicked = false;
            if (boostChargeController != null)
                boostChargeController.SetBoostActive(false);
            AnimSpeed = 1;
            Walk = InsertWalk;
            Run = InsertRun;
            if (grounded)
            {
                JumpLimit = GobletJumpLimit - 2;
                _restoreJumpLimitAfterGobletWhenGrounded = false;
            }
            else
                _restoreJumpLimitAfterGobletWhenGrounded = true;
            //BeatGrounded = 5 * BeatGrounded;
            AirBeat = 5 * AirBeat;
            ElectricEffect.SetActive(false);
            StopElectricEffectSound();
            GobletClock = BoostDurationSeconds;

            if (!grounded && Otter != null)
                Otter.SetBool("midair", true);
        }

        void ApplyDeferredGobletJumpLimitIfGrounded()
        {
            if (!_restoreJumpLimitAfterGobletWhenGrounded || !grounded)
                return;

            JumpLimit = GobletJumpLimit - 2;
            _restoreJumpLimitAfterGobletWhenGrounded = false;
        }
        void PlayElectricEffectSoundOnce()
        {
            AudioSource electricSource = ResolveElectricEffectSource();
            if (electricSource == null || electricSource.clip == null || electricSource.isPlaying)
                return;

            electricSource.Play();
        }

        void StopElectricEffectSound()
        {
            AudioSource electricSource = ResolveElectricEffectSource();
            if (electricSource == null)
                return;

            electricSource.Stop();
        }

        AudioSource ResolveElectricEffectSource()
        {
            if (ElectricEffect == null)
                return null;

            AudioSource electricSource = ElectricEffect.GetComponent<AudioSource>();
            return AudioSourceRouting.EnsureRoute(electricSource, AudioSourceRoute.Sfx) ? electricSource : null;
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
            Player = GetComponent<Rigidbody>();
            CacheMainCamera();
            if (!ValidateRequiredStartupReferences())
                return;

            BindHudState();
            if (boostChargeController == null)
                boostChargeController = GetComponent<Combat.BoostChargeController>();
            if (arrowModel != null)
                arrowModel.SetActive(false);
            bowAim = new Vector3(-0.33f, 20f, -0.3f);
            if (CamForTraders != null)
                CamForTraders.enabled = false;
            if (Root != null)
                SpawnPopUpEffect(Root.position);
            HideCursor();
            ClearAllAimMarkSources();
            if (MunitionDisplay != null)
                MunitionDisplay.SetActive(false);
            var gmObject = GameObject.FindGameObjectWithTag("GM");
            if (gmObject != null)
            {
                var taggedGameMaster = gmObject.GetComponent<GameMaster>();
                if (taggedGameMaster != null)
                    GM = taggedGameMaster;
            }

            if (GM == null)
                GM = FindObjectOfType<GameMaster>();
            //Enable/Disable Background music
            if (seekMusic == true)
            {
                var gameMusicObject = GameObject.Find("GameMusic");
                if (gameMusicObject != null)
                    Music = gameMusicObject.GetComponent<MusicPlaylist>();

                if (Music != null && Music.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    Music.transform.parent = Player.transform;
                    Music.transform.position = new Vector3(0, 0, 0);
                }

                var MusicSwitches = GameObject.FindGameObjectsWithTag("SwitchMusic");
                foreach (var item in MusicSwitches)
                    item.SetActive(true);
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
            BootstrapWeaponLoadout();
            SyncHudState();
            UpdateSwordGlareAvailability();
        }

        bool ValidateRequiredStartupReferences()
        {
            bool valid = true;

            if (Player == null)
            {
                LogMissingRequiredStartupReference("Rigidbody component");
                valid = false;
            }

            if (Otter == null)
            {
                LogMissingRequiredStartupReference(nameof(Otter));
                valid = false;
            }

            if (cachedMainCamera == null)
            {
                LogMissingRequiredStartupReference("Camera.main");
                valid = false;
            }

            if (valid)
                return true;

            enabled = false;
            return false;
        }

        void LogMissingRequiredStartupReference(string referenceName)
        {
            Debug.LogError($"{nameof(BeaverPlayerBehaviour)} on '{name}' is missing required reference '{referenceName}'. Disabling this component to prevent repeated input-loop NullReferenceExceptions.", this);
        }

        [System.Obsolete]
        public void Update()
        {
            ApplyPauseLikeUiCameraState();
            bool inputLocked = IsGameplayInputLocked();
            BufferSecondaryMouseInputEdges(inputLocked);
            CleanupInterruptedSwordFinisherPresentation();
            SyncSwordGlareAvailabilityPassive();

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
                Wallet = Currency.ToString();
                SeedText = NutCount.ToString();
                AppleText = Apple.ToString();
                GobletText = string.Empty;
                ArrowText = arrowMunition.ToString();
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
                        Combat.Weapon loadout = ResolveWeaponLoadout();
                        if (loadout != null && loadout.TryCycleNext(ArsenalCounter, out WeaponData nextWeapon, out int newBrowserIndex))
                        {
                            arsenalBrowser = newBrowserIndex;
                            InterruptSwordAttackPresentation();
                            ApplyEquippedWeaponVisuals(nextWeapon);
                        }

                        UpdateSwordGlareAvailability();
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
                        NotifyPlayerHealthChanged();
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
                    if (PlayerInputReader.WasGobletUsePressed())
                    {
                        if (boostChargeController != null && boostChargeController.TryBeginBoost())
                        {
                            Otter.Play("Consume");
                            Sound.Drink();
                            GobletON();
                        }
                        else if (boostChargeController == null && GobletPickup > 0)
                        {
                            Otter.Play("Consume");
                            Sound.Drink();
                            GobletON();
                        }
                        else if (boostChargeController != null && Sound != null)
                        {
                            Sound.Error();
                        }
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
                    if (boostChargeController != null)
                        boostChargeController.SetActiveBoostTime(GobletClock, BoostDurationSeconds);
                    if (GobletClock <= 0)
                    {
                        GobletOFF();
                    }
                }
            }

            if (!inputLocked /*&& ParryShield.active == false*/)
            {
                bool primaryMeleeHeld = ArmorEquipped
                    ? CanHoldSwordShieldAttack()
                    : PlayerInputReader.IsPrimaryHeld() && CurrentStamina > 0f;
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
            if (IsPauseLikeFullscreenUi())
                PlayerCursorRules.ApplyUnlockedVisible();

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
                ClearAllAimMarkSources();
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
                        ShowAimMarkForStoning();
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
                            HideAimMarkForStoning();
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
                        HideAimMarkForStoning();
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
                            ShowAimMarkForBow();
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
                    if (_restoreJumpLimitAfterGobletWhenGrounded || JumpNum < JumpLimit)
                    {
                        Otter.SetBool("midair", true);
                    }
                    else if (JumpNum == JumpLimit)
                    {
                        Otter.SetBool("midair", false);
                        FallClock -= Time.deltaTime;
                        if (FallClock <= 0)
                            Otter.SetBool("midair", true);
                    }
                }
                if (grounded == true)
                {
                    ApplyDeferredGobletJumpLimitIfGrounded();
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
                    SetParticleEmissionEnabled(SlideEffect, true);
                }
                if (neutralAndMoving == false)
                {
                    Otter.SetBool("moving", false);
                    SetParticleEmissionEnabled(SlideEffect, false);
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
                    SetParticleEmissionEnabled(HurtEffect, false);
                    SetParticleEmissionEnabled(HealEffect, true);
                    HurtLight.enabled = false;
                    HealLight.enabled = true;
                }
                if (hurt == true)
                {
                    SetParticleEmissionEnabled(HurtEffect, true);
                    SetParticleEmissionEnabled(HealEffect, false);
                    HurtLight.enabled = true;
                    HealLight.enabled = false;
                }
                if (heal == false && hurt == false && TouchShroom == false)
                {
                    SetParticleEmissionEnabled(HurtEffect, false);
                    SetParticleEmissionEnabled(HealEffect, false);
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
                    SetParticleEmissionEnabled(HurtEffect, false);
                    SetParticleEmissionEnabled(HealEffect, true);
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

            UpdateSwordGlareAvailability();
        }

        Combat.Weapon ResolveWeaponLoadout()
        {
            if (weaponLoadout != null)
                return weaponLoadout;

            weaponLoadout = GetComponent<Combat.Weapon>();
            return weaponLoadout;
        }

        void BootstrapWeaponLoadout()
        {
            Combat.Weapon loadout = ResolveWeaponLoadout();
            if (loadout == null)
            {
                Debug.LogWarning($"{nameof(BeaverPlayerBehaviour)}: missing {nameof(Combat.Weapon)} component; weapon data fallback only.", this);
                return;
            }

            loadout.BootstrapFromLegacyArsenal(Arsenal, arsenalBrowser);
            ApplyEquippedWeaponVisuals(loadout.EquippedWeapon, playEquipAnim: false);
        }

        void ApplyEquippedWeaponVisuals(WeaponData weaponData, bool playEquipAnim = true)
        {
            if (weaponData == null || Otter == null)
                return;

            switch (weaponData.category)
            {
                case WeaponCategory.BareHands:
                    if (playEquipAnim)
                        Otter.Play("Disarm");
                    HammerHeld = false;
                    Otter.SetBool("armor", false);
                    if (RightHandWeapon != null)
                        RightHandWeapon.SetActive(false);
                    if (LeftHandWeapon != null)
                        LeftHandWeapon.SetActive(false);
                    bowEquipped = false;
                    if (MunitionDisplay != null)
                        MunitionDisplay.SetActive(false);
                    SetBowActive(false);
                    ArmorEquipped = false;
                    SetArmorSetActive(false);
                    break;
                case WeaponCategory.Hammer:
                    if (playEquipAnim)
                        Otter.Play("Equip");
                    HammerHeld = true;
                    Otter.SetBool("armor", false);
                    if (RightHandWeapon != null)
                        RightHandWeapon.SetActive(true);
                    if (LeftHandWeapon != null)
                        LeftHandWeapon.SetActive(true);
                    bowEquipped = false;
                    if (MunitionDisplay != null)
                        MunitionDisplay.SetActive(false);
                    SetBowActive(false);
                    ArmorEquipped = false;
                    SetArmorSetActive(false);
                    break;
                case WeaponCategory.Bow:
                    if (playEquipAnim)
                        Otter.Play("Equip");
                    CountArrows();
                    HammerHeld = false;
                    Otter.SetBool("armor", false);
                    if (RightHandWeapon != null)
                        RightHandWeapon.SetActive(false);
                    if (LeftHandWeapon != null)
                        LeftHandWeapon.SetActive(false);
                    bowEquipped = true;
                    if (MunitionDisplay != null)
                        MunitionDisplay.SetActive(true);
                    SetBowActive(true);
                    ArmorEquipped = false;
                    SetArmorSetActive(false);
                    break;
                case WeaponCategory.ArmorSet:
                    if (playEquipAnim)
                        Otter.Play("Equip");
                    HammerHeld = false;
                    Otter.SetBool("armor", true);
                    if (RightHandWeapon != null)
                        RightHandWeapon.SetActive(false);
                    if (LeftHandWeapon != null)
                        LeftHandWeapon.SetActive(false);
                    bowEquipped = false;
                    if (MunitionDisplay != null)
                        MunitionDisplay.SetActive(false);
                    SetBowActive(false);
                    ArmorEquipped = true;
                    SetArmorSetActive(true);
                    break;
            }

            UpdateSwordGlareAvailability();
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
            HideAimMarkForBow();
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
                PlayerHudState.CopyPlayerStatsFrom(this);
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
            ResolveWeaponLoadout()?.TryAddOwned(ResolveWeaponLoadout()?.ResolveHammer());
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
            ResolveWeaponLoadout()?.TryAddOwned(ResolveWeaponLoadout()?.ResolveBow());

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
            ResolveWeaponLoadout()?.TryAddOwned(ResolveWeaponLoadout()?.ResolveArmorSet());
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
