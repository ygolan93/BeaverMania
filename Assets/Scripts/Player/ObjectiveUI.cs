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

    public void OnTriggerEnter(Collider GameObjective)
    {
        if (GameObjective.CompareTag("Objective"))
        {
            i++;
            Destroy(GameObjective);
        }
    }
    public void UpdateObjective()
    {
        i++;
    }

}
