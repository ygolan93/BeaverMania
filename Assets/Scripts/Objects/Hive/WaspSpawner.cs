using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaspSpawner : MonoBehaviour
{
    public GameObject Wasp;
    public GameObject Hive;
    public Behavior Player;
    public Vector3 Distance;
    public int WaspCounter=3;
    int Counter;
    public float SpawnClock=15f;
   public float RealClock;
    
    private void Start()
    {
        Counter=WaspCounter;
        RealClock = SpawnClock;
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
    }
    public void Update()
    {
         Distance = Player.transform.position - transform.position;

        if (Mathf.Abs(Distance.magnitude) < 100 )
        {
            if (Counter > 0)
            {
               Quaternion RotWasp = Quaternion.LookRotation(Distance);
               var NewWasp =Instantiate(Wasp, Hive.transform.position, RotWasp);
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
