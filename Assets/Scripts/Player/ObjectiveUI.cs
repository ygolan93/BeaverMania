using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveUI: MonoBehaviour
{
    public Behavior Player;
    string[] Objective = { "Talk to trader", 
     "Construct the bridge in order to get into the town" , 
     "Ride the elevator upward",
    "Speak with the Elder Beaver", 
     "Go to the sacred garden", 
     "Look for the Shady Trader",
     "Deliver the package to the Warden at the Watchtower. Construct a bridge",
    "Construct a Bridge in order to get to the Remote Farm",
    "Take one of the gold bricks",
     "Deliver the Gold to the Guard at the garden",
    "Get into the elevator",
    "Equip yourself as much as you can with apples and goblets, then get into the other side",
    "Speak with the wounded soldier",
    "Take the hammers",
    "Smash your way into the arena",
    "Beat the Scorpion king",
    "Free your friend"};
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
