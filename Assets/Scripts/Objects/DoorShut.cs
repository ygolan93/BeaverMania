using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorShut : MonoBehaviour
{
    public GameObject Shutters;
    public Behavior Player;

    private void Update()
    {
        if (Player.GobletPickup == 2)
        {
            Destroy(Shutters);
        }
    }

}
