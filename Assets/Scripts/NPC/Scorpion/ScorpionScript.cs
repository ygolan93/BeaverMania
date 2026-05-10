using System.Collections.Generic;
using UnityEngine;

public class ScorpionScript : MonoBehaviour, IDamageable, IRuntimeResettable
{
    [Header("Config")]
    [SerializeField] BossConfig bossConfig;

    [Header("General Stats")]
    Rigidbody RBScorpion;
    [SerializeField] Animator Scorpion;
    //GameObject AnotherScorpion;
    public NPC_Health BossHealth;
    public int CurrentHealth;
    public int MaxHealth = 2000;
    public Behaviour Player;
    public GameObject[] drops;

    [Header("Combat Factors")]
    public int combo = 0;
    public int comboLimit;
    public float StunnedClock = 10f;
    float initialStun;
    public bool isAttacking = false;
    public float chargeSpeed;
    float paceUp;
    [SerializeField] float chargeClock;
    float resetCharge;
    public Collider Jaw1A;
    public Collider Jaw1B;
    public Collider Jaw2A;
    public Collider Jaw2B;
    public Collider Sting;
    [SerializeField] private EnemyState state = EnemyState.Idle;

    [Header("Mobility")]
    public float lookDistance;
    public float chargeDistance;
    public float attackDistance;
    public float currentDistance;
    Vector3 Distance;
    public Quaternion rotGoal;

    [Header("Effects & Sound")]
    public GameObject HitEffect;
    public GameObject Explosion;
    public GameObject StunEffect;
    public NPC_Audio Sound;
    [SerializeField] CombatFeedbackEmitter feedback;

    Vector3 initialPosition;
    Quaternion initialRotation;
    int initialHealth;
    int initialCombo;
    float initialStunnedClock;
    float initialChargeClock;
    bool initialIsAttacking;
    EnemyState initialState;
    Vector3 initialVelocity;
    Vector3 initialAngularVelocity;
    RigidbodyConstraints initialConstraints;
    bool initialUseGravity;
    Transform initialExplosionParent;
    Vector3 initialExplosionLocalPosition;
    Quaternion initialExplosionLocalRotation;
    bool initialExplosionActive;
    readonly Dictionary<string, bool> initialAnimatorBools = new Dictionary<string, bool>();
    bool isDead;
    readonly List<GameObject> spawnedOnDeath = new List<GameObject>();

    private void Start()
    {
        ApplyConfig();
        RBScorpion = gameObject.GetComponent<Rigidbody>();
        PlayerReference.TryGetPlayer(out Player);
        if (feedback == null)
        {
            feedback = GetComponent<CombatFeedbackEmitter>();
        }

        if (!ValidateReferences())
        {
            return;
        }

        //AnotherScorpion = GameObject.FindGameObjectWithTag("Scorpion");
        Physics.IgnoreLayerCollision(10, 10);

        CurrentHealth = MaxHealth;
        SetActiveIfChanged(Explosion, false);
        feedback?.ResetFeedback();
        paceUp = chargeSpeed + 2;
        resetCharge = chargeClock;
        initialStun = StunnedClock;
        combo = 0;
        CaptureRuntimeState();
    }

    void CaptureRuntimeState()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialHealth = CurrentHealth;
        initialCombo = combo;
        initialStunnedClock = StunnedClock;
        initialChargeClock = chargeClock;
        initialIsAttacking = isAttacking;
        initialState = state;

        if (RBScorpion != null)
        {
            initialVelocity = RBScorpion.velocity;
            initialAngularVelocity = RBScorpion.angularVelocity;
            initialConstraints = RBScorpion.constraints;
            initialUseGravity = RBScorpion.useGravity;
        }

        if (Explosion != null)
        {
            initialExplosionParent = Explosion.transform.parent;
            initialExplosionLocalPosition = Explosion.transform.localPosition;
            initialExplosionLocalRotation = Explosion.transform.localRotation;
            initialExplosionActive = Explosion.activeSelf;
        }

        CaptureAnimatorBools();
    }

    void CaptureAnimatorBools()
    {
        initialAnimatorBools.Clear();
        if (Scorpion == null)
        {
            return;
        }

        foreach (var parameter in Scorpion.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                initialAnimatorBools[parameter.name] = Scorpion.GetBool(parameter.name);
            }
        }
    }

    public void RuntimeReset()
    {
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        CurrentHealth = initialHealth;
        combo = initialCombo;
        StunnedClock = initialStunnedClock;
        chargeClock = initialChargeClock;
        isAttacking = initialIsAttacking;
        state = initialState;
        isDead = false;

        if (RBScorpion != null)
        {
            RBScorpion.velocity = initialVelocity;
            RBScorpion.angularVelocity = initialAngularVelocity;
            RBScorpion.constraints = initialConstraints;
            RBScorpion.useGravity = initialUseGravity;
        }

        if (Scorpion != null)
        {
            foreach (var animatorBool in initialAnimatorBools)
            {
                SetAnimatorBoolIfChanged(Scorpion, animatorBool.Key, animatorBool.Value);
            }
        }

        if (Explosion != null)
        {
            Explosion.transform.SetParent(initialExplosionParent);
            Explosion.transform.localPosition = initialExplosionLocalPosition;
            Explosion.transform.localRotation = initialExplosionLocalRotation;
            SetActiveIfChanged(Explosion, initialExplosionActive);
        }

        feedback?.ResetFeedback();

        if (StunEffect != null)
        {
            SetActiveIfChanged(StunEffect, false);
        }

        if (BossHealth != null)
        {
            BossHealth.SetNPCHealth(CurrentHealth);
        }

        for (int i = spawnedOnDeath.Count - 1; i >= 0; i--)
        {
            if (spawnedOnDeath[i] != null)
            {
                Destroy(spawnedOnDeath[i]);
            }
        }

        spawnedOnDeath.Clear();
    }

    void SetScorpionBool(string param, bool value)
    {
        SetAnimatorBoolIfChanged(Scorpion, param, value);
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
        bool valid = RuntimeReferenceValidator.Require(RBScorpion, this, nameof(RBScorpion)) &
            RuntimeReferenceValidator.Require(Scorpion, this, nameof(Scorpion)) &
            RuntimeReferenceValidator.Require(BossHealth, this, nameof(BossHealth)) &
            RuntimeReferenceValidator.Require(Player, this, nameof(Player)) &
            RuntimeReferenceValidator.Require(Jaw1A, this, nameof(Jaw1A)) &
            RuntimeReferenceValidator.Require(Jaw1B, this, nameof(Jaw1B)) &
            RuntimeReferenceValidator.Require(Jaw2A, this, nameof(Jaw2A)) &
            RuntimeReferenceValidator.Require(Jaw2B, this, nameof(Jaw2B)) &
            RuntimeReferenceValidator.Require(Sting, this, nameof(Sting)) &
            RuntimeReferenceValidator.Require(feedback, this, nameof(feedback)) &
            RuntimeReferenceValidator.Require(HitEffect, this, nameof(HitEffect)) &
            RuntimeReferenceValidator.Require(Explosion, this, nameof(Explosion)) &
            RuntimeReferenceValidator.Require(StunEffect, this, nameof(StunEffect)) &
            RuntimeReferenceValidator.Require(Sound, this, nameof(Sound));

        if (drops != null)
        {
            for (int i = 0; i < drops.Length; i++)
            {
                valid &= RuntimeReferenceValidator.Require(drops[i], this, $"{nameof(drops)}[{i}]");
            }
        }

        return valid;
    }

    void ApplyConfig()
    {
        if (bossConfig == null)
        {
            BuildSafeLogger.WarnOnce(nameof(ScorpionScript) + ".MissingConfig", "Missing boss config; using prefab values.", this, nameof(bossConfig));
            return;
        }

        MaxHealth = bossConfig.maxHealth;
        comboLimit = bossConfig.comboLimit;
        StunnedClock = bossConfig.stunSeconds;
        chargeSpeed = bossConfig.chargeSpeed;
        chargeClock = bossConfig.chargeClock;
        lookDistance = bossConfig.lookDistance;
        chargeDistance = bossConfig.chargeDistance;
        attackDistance = bossConfig.attackDistance;
    }

    public void SetState(EnemyState next)
    {
        if (state == next)
        {
            return;
        }

        if (state == EnemyState.Dead || (state == EnemyState.Stunned && next != EnemyState.Recover && next != EnemyState.Dead))
        {
            return;
        }

        state = next;

        if (next == EnemyState.Recover)
        {
            Recovered();
            state = EnemyState.Idle;
        }
        else if (next == EnemyState.Dead)
        {
            Death();
        }
    }

    public void FixedUpdate()
    {
        Distance = Player.transform.position - RBScorpion.position;
        currentDistance = Mathf.Abs(Distance.magnitude);
        switch (state)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Detect:
                LookAtPlayer();
                break;
            case EnemyState.Chase:
                Scorpion.speed = 1;
                Charge(chargeSpeed);
                break;
            case EnemyState.Patrol:
                Reverse();
                break;
            case EnemyState.Attack:
                StopAndAttack();
                break;
            case EnemyState.Stunned:
                Stunned();
                break;
            case EnemyState.Recover:
                Recovered();
                break;
        }


    }

    private void Update()
    {
        if (Player == null || RBScorpion == null)
        {
            return;
        }

        Distance = Player.transform.position - RBScorpion.position;
        currentDistance = Mathf.Abs(Distance.magnitude);

        if (CurrentHealth <= 0)
        {
            SetState(EnemyState.Dead);
            return;
        }

        if (combo >= comboLimit)
        {
            SetState(EnemyState.Stunned);
            StunnedClock -= Time.deltaTime;
            if (StunnedClock <= 0)
            {
                SetState(EnemyState.Recover);
            }

            return;
        }

        if (currentDistance > lookDistance)
        {
            SetState(EnemyState.Idle);
            return;
        }

        if (combo == 1)
        {
            SetState(EnemyState.Detect);
        }

        if (combo == 3 || combo >= comboLimit - 5)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (currentDistance > chargeDistance)
        {
            SetState(EnemyState.Detect);
            return;
        }

        if (chargeClock > 0)
        {
            SetState(EnemyState.Chase);
            chargeClock -= Time.deltaTime;
            return;
        }

        if (currentDistance > attackDistance)
        {
            SetState(EnemyState.Chase);
            return;
        }

        SetState(EnemyState.Attack);
        chargeClock = resetCharge;
    }

    private void Idle()
    {
        isAttacking = false;
        SetScorpionBool("Walk", false);
        SetScorpionBool("Backwards", false);
        SetScorpionBool("Attack", false);
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        rotGoal = transform.rotation;
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }
    private void LookAtPlayer()
    {
        isAttacking = false;
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        rotGoal = SafeRotation.PlanarLookRotationOrCurrent(Distance, transform.rotation);
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        if (transform.rotation != rotGoal)
        {
            SetScorpionBool("Walk", true);
            SetScorpionBool("Backwards", false);
            SetScorpionBool("Attack", false);
        }
        else
        {
            SetScorpionBool("Walk", false);
            SetScorpionBool("Backwards", false);
            SetScorpionBool("Attack", false);
        }
    }
    public void Charge(float speed)
    {
        isAttacking = true;
        rotGoal = SafeRotation.PlanarLookRotationOrCurrent(Distance, transform.rotation);
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        RBScorpion.velocity = new Vector3(Distance.x, 0, Distance.z).normalized * speed + new Vector3(0, RBScorpion.velocity.y, 0);

        SetScorpionBool("Walk", true);
        SetScorpionBool("Backwards", false);
        SetScorpionBool("Attack", false);
    }
    private void StopAndAttack()
    {
        isAttacking = true;
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        SetScorpionBool("Walk", false);
        SetScorpionBool("Backwards", false);
        SetScorpionBool("Attack", true);
        rotGoal = SafeRotation.PlanarLookRotationOrCurrent(Distance, transform.rotation);
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }
    private void Reverse()
    {
        isAttacking = true;
        rotGoal = SafeRotation.PlanarLookRotationOrCurrent(Distance, transform.rotation);
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        RBScorpion.velocity = new Vector3(-Distance.x, 0, -Distance.z).normalized * 5 + new Vector3(0, RBScorpion.velocity.y, 0);
        SetScorpionBool("Walk", false);
        SetScorpionBool("Backwards", true);
        SetScorpionBool("Attack", false);
    }

    public void TakeDamage(int Damage) => TakeDamage(new DamageEvent { Amount = Damage, Source = null, Point = transform.position, Type = DamageType.Generic, CanStun = false });

    public void TakeDamage(DamageEvent damageEvent)
    {
        transform.rotation = rotGoal;
        if (damageEvent.Type == DamageType.Hazard)
        {
            feedback?.EmitHazard();
        }
        else
        {
            feedback?.EmitHit();
        }
        CurrentHealth -= Mathf.RoundToInt(damageEvent.Amount);
        combo++;
        if (damageEvent.CanStun && (damageEvent.Type == DamageType.Projectile || damageEvent.Type == DamageType.Fire))
        {
            combo += 3;
        }
        if (Sound != null)
        {
            Sound.Beat();
        }

        if (BossHealth != null)
        {
            BossHealth.SetNPCHealth(CurrentHealth);
        }
    }
    private void Stunned()
    {
        isAttacking = false;
        SetScorpionBool("Backwards", false);
        SetScorpionBool("Walk", false);
        SetScorpionBool("Attack", false);
        SetScorpionBool("Stunned", true);
        SetActiveIfChanged(StunEffect, true);
    }
    private void Recovered()
    {
        isAttacking = false;
        SetScorpionBool("Stunned", false);
        combo = 0;
        StunnedClock = initialStun;
        SetActiveIfChanged(StunEffect, false);
    }
    private void Death()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isAttacking = false;
        feedback?.EmitDeath();
        if (Explosion != null)
        {
            SetActiveIfChanged(Explosion, true);
            Explosion.transform.parent = null;
        }

        if (drops != null)
        {
            foreach (var item in drops)
            {
                if (item != null)
                {
                    spawnedOnDeath.Add(Instantiate(item, gameObject.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity));
                }
            }
        }
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Strike"))
        {
            SetState(EnemyState.Dead);
        }
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            TakeDamage(5);
        }
        if (OBJ.gameObject.CompareTag("Bridge"))
        {
            if (OBJ.gameObject.TryGetComponent(out LogSpawner tree))
            {
                tree.DestroyTree(OBJ.transform);
            }
            feedback?.EmitHazard();
            TakeDamage(10);
            combo = 10;
        }
    }



}

