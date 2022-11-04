using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Static_Hive : MonoBehaviour
{
    public int MaxHealth = 10000;
    public int CurrentHealth;
    public NPC_Health HiveBar;
    public Rigidbody Hive;
    public Transform Log1;
    public Transform Log2;
    public Transform Log3;
    public Transform Log4;
    public Transform Log5;
    public Transform Log6;
    public ParticleSystem NpcDeath;
    //public ParticleSystem Hit;

    // Start is called before the first frame update    
    public void Start()
    {
       CurrentHealth = MaxHealth;
        Hive = GetComponent<Rigidbody>();
    }

    public void TakeDamage(int Damage)
    {
        CurrentHealth -= Damage;
        HiveBar.SetNPCHealth(CurrentHealth);

        //var InstaHit = Instantiate(Hit, transform.position, Quaternion.identity);
        //InstaHit.transform.parent = Hive.transform;

        if (CurrentHealth <= 0)
        {

            Instantiate(NpcDeath, transform.position, Quaternion.identity);
            Instantiate(Log1, gameObject.transform.position, Quaternion.identity);
            Instantiate(Log2, gameObject.transform.position, Quaternion.identity);
            Instantiate(Log3, gameObject.transform.position, Quaternion.identity);
            Instantiate(Log4, gameObject.transform.position, Quaternion.identity);
            Instantiate(Log5, gameObject.transform.position, Quaternion.identity);
            Instantiate(Log6, gameObject.transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }


}
