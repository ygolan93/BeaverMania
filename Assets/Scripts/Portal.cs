using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] Transform destination;
    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            OBJ.gameObject.transform.position = destination.position;
        }
    }
}
