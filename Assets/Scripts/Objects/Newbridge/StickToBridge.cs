using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickToBridge : MonoBehaviour
{
    [SerializeField] Transform Ramp;
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
                var Child = OBJ.GetComponent<Transform>();
            Child.transform.parent = Ramp.transform;
      
        }
    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            var Child = OBJ.GetComponent<Transform>();
            Child.transform.parent = null;
        }
    }
}
