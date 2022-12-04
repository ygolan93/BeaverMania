using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickToBridge : MonoBehaviour
{
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            if (transform.parent != null)
            {
                var Parent = transform.parent;
                var Child = OBJ.GetComponent<Rigidbody>();
                Child.transform.parent = Parent;
            }
            //else
            //{
            //    var Child = OBJ.GetComponent<Rigidbody>();
            //    Child.transform.parent = transform;
            //}
        }
    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            var Child = OBJ.GetComponent<Rigidbody>();
            Child.transform.parent = null;
        }
    }
}
