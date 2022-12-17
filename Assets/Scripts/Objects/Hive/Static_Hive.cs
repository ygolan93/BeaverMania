using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Static_Hive : MonoBehaviour
{
    public int MaxHealth = 10000;
    public int CurrentHealth;
    public NPC_Health HiveBar;
    public GameObject Hive;
    public Transform Log1;
    public Transform Log2;
    public Transform Log3;
    public Transform Log4;
    public Transform Log5;
    public Transform Log6;
    public ParticleSystem NpcDeath;
    public AudioSource Sound;
    public GameObject HitEffect;

    //Start is called before the first frame update
    public void Start()
    {
        CurrentHealth = MaxHealth;
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
        Instantiate(NpcDeath, transform.position, Quaternion.identity);
        Instantiate(Log1, gameObject.transform.position, Quaternion.identity);
        Instantiate(Log2, gameObject.transform.position, Quaternion.identity);
        Instantiate(Log3, gameObject.transform.position, Quaternion.identity);
        Instantiate(Log4, gameObject.transform.position, Quaternion.identity);
        Instantiate(Log5, gameObject.transform.position, Quaternion.identity);
        Instantiate(Log6, gameObject.transform.position, Quaternion.identity);
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
