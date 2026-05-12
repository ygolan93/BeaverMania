using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.UI.Objectives
{

    public class ObjectiveUI: MonoBehaviour
    {
        public BeaverPlayer Player;
        public string[] Objective;
        public int i;
        public WayPoint currentPoint;
       public string Instruction;
        public void Update()
        {
            Player = transform.GetComponent<BeaverPlayer>();
            i = currentPoint.GetComponent<WayPoint>().i;
            Instruction = Objective[i];

        }

        //public void OnTriggerStay(Collider GameObjective) 
        //{
        //    if (GameObjective.CompareTag("Objective"))
        //    {
        //        i = GameObjective.GetComponent<ChangeOBJ>().ObjectiveNum;
        //    }
        //}
        //public void /*OnTriggerExit*/(Collider GameObjective)
        //{
        //    if (GameObjective.CompareTag("Objective"))
        //    {
        //        i = 0;
        //    }
        //}
        public void UpdateObjective()
        {
            i++;
        }

    }
}
