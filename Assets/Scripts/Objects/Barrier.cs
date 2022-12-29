using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrier : MonoBehaviour
{
    public GameObject Wall;
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Honey"))
        {
           Destroy(Wall);
        }
    }
}
