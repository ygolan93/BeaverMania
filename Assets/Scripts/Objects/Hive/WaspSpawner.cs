using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaspSpawner : MonoBehaviour
{
    public GameObject Wasp;
    public GameObject Hive;
    public Behaviour Player;
    public Vector3 Distance;
    [SerializeField] float SpawnDistance;
    public int WaspCounter=3;
    int Counter;
    public float SpawnClock=15f;
   public float RealClock;
    
    private void Start()
    { 
        Counter=WaspCounter;
        RealClock = SpawnClock;
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
    }


    public void Update()
    {
         Distance = Player.transform.position - gameObject.transform.position;

        if (Mathf.Abs(Distance.magnitude) < SpawnDistance )
        {
            if (Counter > 0)
            {
               Quaternion RotWasp = Quaternion.LookRotation(Distance);
               Instantiate(Wasp, Hive.transform.position, RotWasp);
                Counter--;
            }
            if (Counter <=0)
            {

                RealClock -= Time.deltaTime;
                if (RealClock <= 0)
                {
                    Counter = WaspCounter;
                    RealClock = SpawnClock;
                }
            }
        }

       else
        {
            Counter = 0;
            RealClock = 0;
        }
    }
}
