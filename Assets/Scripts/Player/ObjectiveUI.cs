using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveUI: MonoBehaviour
{
    public Behavior Player;
    public string[] Objective; 
    public int i = 0;
   public string Instruction;
    public void Update()
    {
        Instruction = Objective[i];
    }

    public void OnTriggerStay(Collider GameObjective)
    {
        if (GameObjective.CompareTag("Objective"))
        {
            i = GameObjective.GetComponent<ChangeOBJ>().ObjectiveNum;
        }
    }
    public void OnTriggerExit(Collider GameObjective)
    {
        if (GameObjective.CompareTag("Objective"))
        {
            i = 0;
        }
    }
    public void UpdateObjective()
    {
        i++;
    }

}
