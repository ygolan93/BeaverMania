using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NPC_Basic : MonoBehaviour, IDamageable, IRuntimeResettable
{
    enum WaspState
    {
        Patrol,
        Chase,
        ContactRecover,
        Stunned,
        Dead
    }

    [Header("Config")]
    [SerializeField] EnemyAIConfig aiConfig;

    [Header("Body and animation")]
    public Rigidbody NPC;
    [SerializeField] Animator Wasp;
    [Header("Movement")]
    GameObject PlayerTarget;
    Transform playerTransform;
    Behaviour PlayerHealth;
    public Vector3 Distance;
    public Quaternion rotGoal;
    readonly float steer = 0.5f;
    public float AttackSpeed = 7f;
    public float PlayerDistance;
    float ChangeNav = 5f;
    public float Recovery = 10f;
    float ChargeClock = 0.7f;
    bool Contact = false;
    WaspState state = WaspState.Patrol;
    Coroutine floatCoroutine;
    bool refreshPatrolMovement;
    float a;
    float b;
    float c;
    //public WaspCourse course;
    Vector3 SpawnPos;
    public Vector3 currentPos;
    public bool floating;
    public float floatSpeed = 1.0f;
    public float floatDistance = 1.0f;
    public float maxTiltAngle = 10.0f;
    [Header("Health and damage")]
    public int hit2stun;
    public int combo = 0;
    public int Damage2Player = 1;
    public int MaxHealth = 2000;
    public int CurrentHealth;
    public NPC_Health NPCHealthBar;
    public GameObject HitEffect;
    public GameObject SlashEffect;
    public GameObject Explosion;
    [SerializeField] CombatFeedbackEmitter feedback;

    [Header("Sound")]
    public NPC_Audio Sound;
    [SerializeField] GameObject BuzzSource;
    [Header("On Death")]
    public GameObject Body;
    public GameObject Head;
    public GameObject Wing;
    public GameObject Leg;
    public GameObject Reward;

    Vector3 initialPosition;
    Quaternion initialRotation;
    int initialHealth;
    float initialChangeNav;
    float initialRecovery;
    float initialChargeClock;
    bool initialContact;
    bool initialFloating;
    WaspState initialState;
    Vector3 initialVelocity;
    Vector3 initialAngularVelocity;
    RigidbodyConstraints initialConstraints;
    bool initialUseGravity;
    readonly Dictionary<string, bool> initialAnimatorBools = new Dictionary<string, bool>();
    bool isDead;
    readonly List<GameObject> spawnedOnDeath = new List<GameObject>();
    // Start is called before the first frame update    
    public void Start()
    {
        ApplyConfig();
        SpawnPos = transform.position;
        Wasp = GetComponent<Animator>();
        CurrentHealth = MaxHealth;
        if (PlayerReference.TryGetPlayer(out PlayerHealth))
        {
            PlayerTarget = PlayerHealth.gameObject;
            playerTransform = PlayerHealth.transform;
        }
        if (feedback == null)
        {
            feedback = GetComponent<CombatFeedbackEmitter>();
        }

        if (!ValidateReferences())
        {
            return;
        }

        feedback?.ResetFeedback();
        SetState(WaspState.Patrol);
        RandoMovement();
        NPC.velocity = Vector3.forward;
        SetBuzzActive(true);
        CaptureRuntimeState();
    }

    void CaptureRuntimeState()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialHealth = CurrentHealth;
        initialChangeNav = ChangeNav;
        initialRecovery = Recovery;
        initialChargeClock = ChargeClock;
        initialContact = Contact;
        initialFloating = floating;
        initialState = state;

        if (NPC != null)
        {
            initialVelocity = NPC.velocity;
            initialAngularVelocity = NPC.angularVelocity;
            initialConstraints = NPC.constraints;
            initialUseGravity = NPC.useGravity;
        }

        CaptureAnimatorBools();
    }

    void CaptureAnimatorBools()
    {
        initialAnimatorBools.Clear();
        if (Wasp == null)
        {
            return;
        }

        foreach (var parameter in Wasp.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                initialAnimatorBools[parameter.name] = Wasp.GetBool(parameter.name);
            }
        }
    }

    public void RuntimeReset()
    {
        StopFloat();
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        SpawnPos = initialPosition;
        CurrentHealth = initialHealth;
        ChangeNav = initialChangeNav;
        Recovery = initialRecovery;
        ChargeClock = initialChargeClock;
        Contact = initialContact;
        floating = initialFloating;
        state = initialState;
        isDead = false;
        combo = 0;
        refreshPatrolMovement = false;

        if (NPC != null)
        {
            NPC.velocity = initialVelocity;
            NPC.angularVelocity = initialAngularVelocity;
            NPC.constraints = initialConstraints;
            NPC.useGravity = initialUseGravity;
        }

        if (Wasp != null)
        {
            foreach (var animatorBool in initialAnimatorBools)
            {
                // Restore cached bools through the guarded setter with the Wasp animator.
                SetAnimatorBoolIfChanged(Wasp, animatorBool.Key, animatorBool.Value);
            }
        }

        if (NPCHealthBar != null)
        {
            NPCHealthBar.SetNPCHealth(CurrentHealth);
        }

        feedback?.ResetFeedback();

        SetBuzzActive(true);

        for (int i = spawnedOnDeath.Count - 1; i >= 0; i--)
        {
            if (spawnedOnDeath[i] != null)
            {
                Destroy(spawnedOnDeath[i]);
            }
        }

        spawnedOnDeath.Clear();
    }

    void SetBuzzActive(bool active)
    {
        SetActiveIfChanged(BuzzSource, active);
    }

    void SetWaspBool(string param, bool value)
    {
        SetAnimatorBoolIfChanged(Wasp, param, value);
    }

    static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    static void SetAnimatorBoolIfChanged(Animator animator, string param, bool value)
    {
        if (animator != null && animator.GetBool(param) != value)
        {
            animator.SetBool(param, value);
        }
    }

    bool ValidateReferences()
    {
        bool valid = RuntimeReferenceValidator.Require(NPC, this, nameof(NPC)) &
            RuntimeReferenceValidator.Require(Wasp, this, nameof(Wasp)) &
            RuntimeReferenceValidator.Require(PlayerTarget, this, "Player tag") &
            RuntimeReferenceValidator.Require(PlayerHealth, this, nameof(PlayerHealth)) &
            RuntimeReferenceValidator.Require(NPCHealthBar, this, nameof(NPCHealthBar)) &
            RuntimeReferenceValidator.Require(feedback, this, nameof(feedback)) &
            RuntimeReferenceValidator.Require(Explosion, this, nameof(Explosion)) &
            RuntimeReferenceValidator.Require(Sound, this, nameof(Sound)) &
            RuntimeReferenceValidator.Require(BuzzSource, this, nameof(BuzzSource)) &
            RuntimeReferenceValidator.Require(Body, this, nameof(Body)) &
            RuntimeReferenceValidator.Require(Head, this, nameof(Head)) &
            RuntimeReferenceValidator.Require(Wing, this, nameof(Wing)) &
            RuntimeReferenceValidator.Require(Leg, this, nameof(Leg)) &
            RuntimeReferenceValidator.Require(Reward, this, nameof(Reward));

        RuntimeReferenceValidator.Optional(HitEffect, this, nameof(HitEffect));
        RuntimeReferenceValidator.Optional(SlashEffect, this, nameof(SlashEffect));

        return valid;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            if (!PlayerReference.TryGetPlayer(out PlayerHealth) || PlayerHealth == null)
            {
                return;
            }

            PlayerTarget = PlayerHealth.gameObject;
            playerTransform = PlayerHealth.transform;
        }

        Distance = playerTransform.position - transform.position;
        PlayerDistance = Distance.magnitude;
        ChangeNav -= Time.deltaTime;

        if (CurrentHealth <= 0)
        {
            SetState(WaspState.Dead);
            return;
        }

        if (combo >= hit2stun)
        {
            SetState(WaspState.Stunned);
            Recovery -= Time.deltaTime;
            if (Recovery <= 0)
            {
                combo = 0;
                SetState(WaspState.Patrol);
            }
        }
        else if (Contact)
        {
            SetState(WaspState.ContactRecover);
            ChargeClock -= Time.deltaTime;
            if (ChargeClock <= 0)
            {
                Contact = false;
            }
        }
        else if (PlayerDistance < AggroDistance)
        {
            SetState(WaspState.Chase);
        }
        else
        {
            if (ChangeNav <= 0)
            {
                refreshPatrolMovement = true;
                ChangeNav = 5f;
            }

            SetState(WaspState.Patrol);
        }

        if (floating)
        {
            StartFloat(transform.position);
        }
        else
        {
            StopFloat();
        }

        if (PlayerDistance > 6)
        {
            SetWaspBool("Beat", false);
        }
    }

    public void FixedUpdate()
    {
        if (floating || state == WaspState.Dead)
        {
            return;
        }

        switch (state)
        {
            case WaspState.Patrol:
                RotateTowardVelocity();
                if (refreshPatrolMovement)
                {
                    SetBuzzActive(true);
                    SetWaspBool("Sting", false);

                    RandoMovement();
                    refreshPatrolMovement = false;
                }
                else
                {
                    TurnBack();
                }
                break;
            case WaspState.Chase:
                RotateTowardVelocity();
                SetBuzzActive(true);
                SetWaspBool("Sting", true);

                if (NPC != null)
                {
                    NPC.velocity = Distance.normalized * AttackSpeed;
                }
                break;
            case WaspState.ContactRecover:
                SetWaspBool("Sting", false);

                if (NPC != null)
                {
                    NPC.AddForce(new Vector3(-Distance.x, 0.01f, -Distance.z).normalized * 0.1f);
                }

                transform.rotation = SafeRotation.LookRotationOrCurrent(Distance, transform.rotation);
                break;
            case WaspState.Stunned:
                Stunned();
                break;
        }
    }

    void SetState(WaspState next)
    {
        if (state == next || state == WaspState.Dead)
        {
            return;
        }

        if (state == WaspState.Stunned && next != WaspState.Patrol && next != WaspState.Dead)
        {
            return;
        }

        state = next;

        if (next == WaspState.Chase)
        {
            ChargeClock = 0.7f;
            ChangeNav = 0;
        }
        else if (next == WaspState.Patrol)
        {
            Recovered();
        }
        else if (next == WaspState.Dead)
        {
            Death();
        }
    }

    void RotateTowardVelocity()
    {
        if (NPC == null || NPC.velocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        rotGoal = SafeRotation.LookRotationOrCurrent(NPC.velocity, transform.rotation);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
    }

    private void Death()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        StopFloat();
        feedback?.EmitDeath();
        if (PlayerHealth != null)
        {
            PlayerHealth.Plattering = ("HA! gotcha");
            PlayerHealth.ChangeSpeech = 1;
        }
        spawnedOnDeath.Add(Instantiate(Explosion, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Body, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Head, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Wing, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Wing, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Leg, transform.position + new Vector3(0, 1, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Reward, transform.position + new Vector3(0, 7, 0), transform.rotation));
        spawnedOnDeath.Add(Instantiate(Reward, transform.position + new Vector3(0, 7, 0), transform.rotation));
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject == PlayerTarget)
        {
            if (combo < hit2stun)
            {
                Sting();
            }
            Contact = true;
            if (PlayerHealth != null && PlayerHealth.isParried == true)
            {
                TakeDamage(20);
                combo += 10;
            }
        }
        if (OBJ.gameObject.CompareTag("Strike"))
        {
            SetState(WaspState.Dead);
        }
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            if (Wasp != null)
            {
                SetWaspBool("Beat", true);
            }

            TakeDamage(15);
            if (Sound != null)
            {
                Sound.Beat();
            }
            combo = hit2stun;
        }
        if (OBJ.gameObject.CompareTag("Isle"))
        {
            Contact = true;
            if (NPC != null)
            {
                NPC.velocity += new Vector3(Random.Range(-1, 1), 1, Random.Range(-1,1));
            }
        }
    }
    public void TurnBack()
    {
        SetBuzzActive(true);
        SetWaspBool("Sting", false);

        if ((transform.position - SpawnPos).magnitude > LeashDistance)
        {
            if (NPC != null)
            {
                NPC.velocity = (SpawnPos - transform.position).normalized * 10;
            }
        }
    }


    void StartFloat(Vector3 initialPosition)
    {
        if (floatCoroutine != null)
        {
            return;
        }

        floatCoroutine = StartCoroutine(FloatObject(initialPosition));
    }

    void StopFloat()
    {
        if (floatCoroutine == null)
        {
            return;
        }

        StopCoroutine(floatCoroutine);
        floatCoroutine = null;
    }

    private IEnumerator FloatObject(Vector3 initialPosition)
    {
        float direction = -1.0f;
        while (true)
        {
            float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatDistance+1;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            float tiltAngle = direction * maxTiltAngle * Mathf.Sin(Time.time * floatSpeed);
            transform.rotation = Quaternion.Euler(tiltAngle, transform.rotation.eulerAngles.y, 0);
            if (newY >= initialPosition.y + floatDistance || newY <= initialPosition.y - floatDistance)
            {
                direction *= 1.0f;
            }

            yield return null;
        }
    }
   public void Stunned()
    {
        StopFloat();
        SetBuzzActive(false);

        floating = false;
        if (NPC != null)
        {
            NPC.constraints = RigidbodyConstraints.None;
            NPC.useGravity = true;
        }

        SetWaspBool("Stunned", true);
        SetWaspBool("Sting", false);
    }

    void Recovered()
    {
        SetBuzzActive(true);
        floating = false;
        SetWaspBool("Stunned", false);

        Recovery = RecoverySeconds;
        if (NPC != null)
        {
            NPC.constraints = RigidbodyConstraints.FreezeRotation;
            NPC.useGravity = false;
        }
    }
    public void Sting()
    {
        floating = false;
        Recovered();
        if (PlayerHealth == null)
        {
            return;
        }

        if (PlayerHealth.isParried == false && PlayerHealth.Rolling == false)
        {
            if (Sound != null)
            {
                Sound.Sting();
            }

            if (CombatDebugGate.AllowsDamage(PlayerHealth.gameObject))
            {
                PlayerHealth.TakeDamage(Damage2Player);
            }
        }

        SetWaspBool("Sting", false);

        if (PlayerHealth.isParried == true)
        {
            PlayerHealth.TakeDamage(0.01f);
            TakeDamage(1);
            combo = 3;
        }
    }

    void OnDisable()
    {
        StopFloat();
    }

    float AggroDistance => aiConfig != null ? aiConfig.aggroDistance : 50f;
    float LeashDistance => aiConfig != null ? aiConfig.leashDistance : 30f;
    float RecoverySeconds => aiConfig != null ? aiConfig.recoverySeconds : 10f;

    void ApplyConfig()
    {
        if (aiConfig == null)
        {
            BuildSafeLogger.WarnOnce(nameof(NPC_Basic) + ".MissingConfig", "Missing enemy AI config; using prefab values.", this, nameof(aiConfig));
            return;
        }

        MaxHealth = aiConfig.maxHealth;
        hit2stun = aiConfig.hitToStun;
        Damage2Player = aiConfig.damageToPlayer;
        AttackSpeed = aiConfig.attackSpeed;
        Recovery = aiConfig.recoverySeconds;
    }

    public void RandoMovement()
    {
        SetBuzzActive(true);
        if ((SpawnPos - transform.position).magnitude < LeashDistance)
        {
            Recovered();
            a = Random.Range(-1, 1);
            b = Random.Range(-0.1f, 0.1f);
            c = Random.Range(-1, 1);
            if (NPC != null)
            {
                NPC.velocity = new Vector3(a, b, c);
            }
        }
        else if (NPC != null)
        {
            NPC.velocity = (SpawnPos - transform.position).normalized * 5;
        }
    }

    public void TakeDamage(int Damage) => TakeDamage(new DamageEvent { Amount = Damage, Source = null, Point = transform.position, Type = DamageType.Generic, CanStun = false });

    public void TakeDamage(DamageEvent damageEvent)
    {
        if (isDead)
        {
            return;
        }

        SetBuzzActive(false);

        rotGoal = SafeRotation.LookRotationOrCurrent(Distance, transform.rotation);
        transform.rotation = rotGoal;
        if (NPC != null)
        {
            NPC.useGravity = true;
        }

        SetWaspBool("Beat", true);
        SetWaspBool("Sting", false);

        CurrentHealth -= Mathf.RoundToInt(damageEvent.Amount);
        if (damageEvent.Type == DamageType.Hazard)
        {
            feedback?.EmitHazard();
            if (Sound != null)
            {
                Sound.Beat();
            }
        }
        else
        {
            var playerArsenal = PlayerHealth != null ? PlayerHealth.Arsenal : null;
            var currentWeapon = PlayerHealth != null ? PlayerHealth.arsenalBrowser : 0;
            if (playerArsenal == null || playerArsenal.Count == 0)
            {
                feedback?.EmitHit();
                if (Sound != null)
                {
                    Sound.Beat();
                }
            }
            else
            {
                switch (playerArsenal[Mathf.Clamp(currentWeapon, 0, playerArsenal.Count - 1)])
                {
                    case "ArmorSet":
                        feedback?.EmitSlashHit();
                        if (Sound != null)
                        {
                            Sound.LiteSwordDamage();
                        }
                        break;
                    default:
                        feedback?.EmitHit();
                        if (Sound != null)
                        {
                            Sound.Beat();
                        }
                        break;
                }
            }
        }

        combo++;
        if (damageEvent.CanStun && (damageEvent.Type == DamageType.Projectile || damageEvent.Type == DamageType.Fire))
        {
            combo += hit2stun;
        }

        if (NPCHealthBar != null)
        {
            NPCHealthBar.SetNPCHealth(CurrentHealth);
        }

        if (CurrentHealth <= 0)
        {
            SetState(WaspState.Dead);
        }
    }

}
