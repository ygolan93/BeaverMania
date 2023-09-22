using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveUI: MonoBehaviour
{
    public Behaviour Player;
    public string[] Objective;
    public int i;
    public WayPoint currentPoint;
   public string Instruction;
    public void Update()
    {
        Player = transform.GetComponent<Behaviour>();
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
