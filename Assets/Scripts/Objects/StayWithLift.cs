using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StayWithLift : MonoBehaviour
{
    [SerializeField] Transform Ramp;


    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.tag=="Bridge")
        {
            var Child = OBJ.GetComponent<Transform>();
            Child.transform.parent = Ramp.transform;
        }
    }

    //private void OnTriggerExit(Collider OBJ)
    //{
    //    if (OBJ.gameObject.tag == "Player")
    //    {
    //        var Child = OBJ.GetComponent<Rigidbody>();
    //        Child.transform.parent = null;
    //    }
    //}
}
