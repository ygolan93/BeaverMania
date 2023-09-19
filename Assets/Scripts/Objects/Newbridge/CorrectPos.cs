using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrectPos : MonoBehaviour
{
    [SerializeField] Transform spawnHere;
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            int counter = OBJ.GetComponent<Carry>().i;
            if (Input.GetKey(KeyCode.LeftControl)&& counter>0)
            {
                OBJ.transform.position = spawnHere.position;
            }
        }
    }
}
