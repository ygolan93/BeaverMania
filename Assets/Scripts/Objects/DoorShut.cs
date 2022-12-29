using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorShut : MonoBehaviour
{
    public GameObject Wall;
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Gold"))
        {
            Destroy(Wall);
        }
    }
}
