using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Static_Hive : MonoBehaviour, IDamageable, IRuntimeResettable
{
    public int MaxHealth = 10000;
    public int CurrentHealth;
    public NPC_Health HiveBar;
    public GameObject Hive;
    [SerializeField] GameObject[] SpawnedObjects;
    //public Transform Log1;
    //public Transform Log2;
    //public Transform Log3;
    //public Transform Log4;
    //public Transform Log5;
    //public Transform Log6;
    public Transform Wasp;
    public GameObject Explosion;
    public AudioSource Sound;
    public GameObject HitEffect;
    [SerializeField] CombatFeedbackEmitter feedback;

    Vector3 initialPosition;
    Quaternion initialRotation;
    int initialHealth;
    bool initialHiveActive;
    Transform initialExplosionParent;
    Vector3 initialExplosionLocalPosition;
    Quaternion initialExplosionLocalRotation;
    bool initialExplosionActive;
    bool deathResolved;
    bool isDead;
    readonly List<GameObject> spawnedOnDeath = new List<GameObject>();

    //Start is called before the first frame update
    public void Start()
    {
        if (feedback == null)
        {
            feedback = GetComponent<CombatFeedbackEmitter>();
        }

        if (!ValidateReferences())
        {
            return;
        }

        CurrentHealth = MaxHealth;
        Explosion.SetActive(false);
        feedback?.ResetFeedback();
        CaptureRuntimeState();
    }

    void CaptureRuntimeState()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialHealth = CurrentHealth;
        initialHiveActive = Hive.activeSelf;

        if (Explosion != null)
        {
            initialExplosionParent = Explosion.transform.parent;
            initialExplosionLocalPosition = Explosion.transform.localPosition;
            initialExplosionLocalRotation = Explosion.transform.localRotation;
            initialExplosionActive = Explosion.activeSelf;
        }
    }

    public void RuntimeReset()
    {
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        CurrentHealth = initialHealth;
        deathResolved = false;
        isDead = false;

        if (Hive != null)
        {
            Hive.SetActive(initialHiveActive);
        }

        if (Explosion != null)
        {
            Explosion.transform.SetParent(initialExplosionParent);
            Explosion.transform.localPosition = initialExplosionLocalPosition;
            Explosion.transform.localRotation = initialExplosionLocalRotation;
            Explosion.SetActive(initialExplosionActive);
        }

        feedback?.ResetFeedback();

        if (HiveBar != null)
        {
            HiveBar.SetNPCHealth(CurrentHealth);
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

    bool ValidateReferences()
    {
        bool valid = RuntimeReferenceValidator.Require(HiveBar, this, nameof(HiveBar)) &
            RuntimeReferenceValidator.Require(Hive, this, nameof(Hive)) &
            RuntimeReferenceValidator.Require(Wasp, this, nameof(Wasp)) &
            RuntimeReferenceValidator.Require(Explosion, this, nameof(Explosion)) &
            RuntimeReferenceValidator.Require(feedback, this, nameof(feedback)) &
            RuntimeReferenceValidator.Require(Sound, this, nameof(Sound)) &
            RuntimeReferenceValidator.Require(HitEffect, this, nameof(HitEffect));

        if (SpawnedObjects != null)
        {
            for (int i = 0; i < SpawnedObjects.Length; i++)
            {
                valid &= RuntimeReferenceValidator.Require(SpawnedObjects[i], this, $"{nameof(SpawnedObjects)}[{i}]");
            }
        }

        return valid;
    }
    private void LateUpdate()
    {
        if (CurrentHealth <= 0 && !deathResolved)
        {
            Death();
        }
    }

    public void Death()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        deathResolved = true;
        feedback?.EmitDeath();
        if (Explosion != null)
        {
            Explosion.SetActive(true);
            Explosion.transform.parent = null;
        }

        if (SpawnedObjects != null)
        {
            foreach (var OBJ in SpawnedObjects)
            {
                if (OBJ != null)
                {
                    spawnedOnDeath.Add(Instantiate(OBJ, gameObject.transform.position, Quaternion.identity));
                }
            }
        }

        if (Wasp != null)
        {
            for (int i = 0; i < 30; i++)
            {
                spawnedOnDeath.Add(Instantiate(Wasp, gameObject.transform.position, Quaternion.identity).gameObject);
            }
        }

        if (Hive != null)
        {
            Hive.SetActive(false);
        }
    }

    public void TakeDamage(int Damage) => TakeDamage(new DamageEvent { Amount = Damage, Source = null, Point = transform.position, Type = DamageType.Generic, CanStun = false });

    public void TakeDamage(DamageEvent damageEvent)
    {
        if (damageEvent.Type == DamageType.Hazard)
        {
            feedback?.EmitHazard();
        }
        else
        {
            feedback?.EmitHit();
        }
        CurrentHealth -= Mathf.RoundToInt(damageEvent.Amount);
        if (Sound != null)
        {
            Sound.Play();
        }

        if (HiveBar != null)
        {
            HiveBar.SetNPCHealth(CurrentHealth);
        }
    }
}
