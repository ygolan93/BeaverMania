using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Objects
{

    public class WaspSpawner : MonoBehaviour
    {
        const float LookRotationEpsilon = 0.0001f;
        public GameObject Wasp;
        public GameObject Hive;
        public BeaverPlayer Player;
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
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<BeaverPlayer>();
        }


        public void Update()
        {
             Distance = Player.transform.position - gameObject.transform.position;

            if (Mathf.Abs(Distance.magnitude) < SpawnDistance )
            {
                if (Counter > 0)
                {
                   Quaternion RotWasp = Distance.sqrMagnitude > LookRotationEpsilon ? Quaternion.LookRotation(Distance) : Quaternion.identity;
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
}
