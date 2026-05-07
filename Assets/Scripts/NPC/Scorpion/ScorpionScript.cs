using UnityEngine;

public class ScorpionScript : MonoBehaviour, IDamageable
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



    private void Start()
    {
        ApplyConfig();
        RBScorpion = gameObject.GetComponent<Rigidbody>();
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        Player = playerObject != null ? playerObject.GetComponent<Behaviour>() : null;

        if (!ValidateReferences())
        {
            return;
        }

        //AnotherScorpion = GameObject.FindGameObjectWithTag("Scorpion");
        Physics.IgnoreLayerCollision(10, 10);

        CurrentHealth = MaxHealth;
        Explosion.SetActive(false);
        HitEffect.SetActive(false);
        paceUp = chargeSpeed + 2;
        resetCharge = chargeClock;
        initialStun = StunnedClock;
        combo = 0;
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
        HitEffect.SetActive(false);
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
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        rotGoal = transform.rotation;
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }
    private void LookAtPlayer()
    {
        isAttacking = false;
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        if (transform.rotation != rotGoal)
        {
            Scorpion.SetBool("Walk", true);
            Scorpion.SetBool("Backwards", false);
            Scorpion.SetBool("Attack", false);
        }
        else
        {
            Scorpion.SetBool("Walk", false);
            Scorpion.SetBool("Backwards", false);
            Scorpion.SetBool("Attack", false);
        }
    }
    public void Charge(float speed)
    {
        isAttacking = true;
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        RBScorpion.velocity = new Vector3(Distance.x, 0, Distance.z).normalized * speed + new Vector3(0, RBScorpion.velocity.y, 0);

        HitEffect.SetActive(false);
        Scorpion.SetBool("Walk", true);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", false);
    }
    private void StopAndAttack()
    {
        isAttacking = true;
        RBScorpion.velocity = new Vector3(0, RBScorpion.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Attack", true);
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
    }
    private void Reverse()
    {
        isAttacking = true;
        rotGoal = Quaternion.LookRotation(new Vector3(Distance.x, 0, Distance.z));
        RBScorpion.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.05f);
        RBScorpion.velocity = new Vector3(-Distance.x, 0, -Distance.z).normalized * 5 + new Vector3(0, RBScorpion.velocity.y, 0);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Backwards", true);
        Scorpion.SetBool("Attack", false);
    }

    public void TakeDamage(int Damage) => TakeDamage(new DamageEvent { Amount = Damage, Source = null, Point = transform.position, Type = DamageType.Generic, CanStun = false });

    public void TakeDamage(DamageEvent damageEvent)
    {
        transform.rotation = rotGoal;
        HitEffect.SetActive(true);
        CurrentHealth -= Mathf.RoundToInt(damageEvent.Amount);
        combo++;
        if (damageEvent.CanStun && (damageEvent.Type == DamageType.Projectile || damageEvent.Type == DamageType.Fire))
        {
            combo += 3;
        }
        Sound.Beat();
        BossHealth.SetNPCHealth(CurrentHealth);
    }
    private void Stunned()
    {
        isAttacking = false;
        Scorpion.SetBool("Backwards", false);
        Scorpion.SetBool("Walk", false);
        Scorpion.SetBool("Attack", false);
        Scorpion.SetBool("Stunned", true);
        StunEffect.SetActive(true);
    }
    private void Recovered()
    {
        isAttacking = false;
        Scorpion.SetBool("Stunned", false);
        combo = 0;
        StunnedClock = initialStun;
        StunEffect.SetActive(false);
    }
    private void Death()
    {
        isAttacking = false;
        Explosion.SetActive(true);
        Explosion.transform.parent = null;
        foreach (var item in drops)
        {
            Instantiate(item, gameObject.transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);
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
            var Tree = OBJ.gameObject.GetComponent<LogSpawner>();
            Tree.DestroyTree(OBJ.transform);
            TakeDamage(10);
            combo = 10;
        }
    }



}

