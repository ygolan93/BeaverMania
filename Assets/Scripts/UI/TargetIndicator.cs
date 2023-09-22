using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TargetIndicator : MonoBehaviour
{
    public WayPoint target;
    public float rotationSpeed;

    
    //private void Update()
    //{
    //    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.position-transform.position), rotationSpeed*Time.deltaTime);
    //}
}
