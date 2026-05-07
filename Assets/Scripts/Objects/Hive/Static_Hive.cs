using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Static_Hive : MonoBehaviour
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

    //Start is called before the first frame update
    public void Start()
    {
        if (!ValidateReferences())
        {
            return;
        }

        CurrentHealth = MaxHealth;
        Explosion.SetActive(false);
    }

    bool ValidateReferences()
    {
        bool valid = RuntimeReferenceValidator.Require(HiveBar, this, nameof(HiveBar)) &
            RuntimeReferenceValidator.Require(Hive, this, nameof(Hive)) &
            RuntimeReferenceValidator.Require(Wasp, this, nameof(Wasp)) &
            RuntimeReferenceValidator.Require(Explosion, this, nameof(Explosion)) &
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
        HitEffect.SetActive(false);
        if (CurrentHealth <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        Explosion.SetActive(true);
        Explosion.transform.parent = null;
        foreach (var OBJ in SpawnedObjects)
        {
            Instantiate(OBJ, gameObject.transform.position, Quaternion.identity);

        }
        for (int i = 0; i < 30; i++)
        {
            Instantiate(Wasp, gameObject.transform.position, Quaternion.identity);
        }
        Destroy(Hive);
    }

    public void TakeDamage(int Damage)
    {
        HitEffect.SetActive(true);
        CurrentHealth -= Damage;
        Sound.Play();
        HiveBar.SetNPCHealth(CurrentHealth);
    }
}
