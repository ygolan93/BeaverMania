using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTraceGround : MonoBehaviour
{
    
    [Range(0, 1)]
    public float distance;
    [Range(0, 1)]
    public float offset;
    [Range(0, 300)]
    public float stepLeap;
    [Range(0, 0.1f)]
    public float stepTime;
    public bool step;

    public Rigidbody player;
    public LayerMask collisionLayer;
    Vector3 riseVector;
    Vector3 originRay;
    Vector3 directionRay;
    Quaternion directionRot;
    [System.Obsolete]
    void Update()
    {
        directionRot = player.transform.rotation;

        directionRay = directionRot * Vector3.forward;
        directionRay = directionRay.normalized;

        
        directionRay.y = 0;

        riseVector = transform.position;
        originRay = transform.position + new Vector3(0, offset, 0);

        Ray ray = new(originRay, directionRay);
        Debug.DrawRay(originRay, directionRay * distance, Color.red);
        Ray ray2 = new(originRay+new Vector3(0,0.4f,0), directionRay);
        Debug.DrawRay(originRay + new Vector3(0, 0.4f, 0), 1.5f * distance * directionRay, Color.yellow);

        if (Physics.Raycast(ray, out _, distance, collisionLayer) || Physics.Raycast(ray2, out _, distance, collisionLayer))

        {
            if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)))
            {
                step = true;
            }
            else
            {
                step = false;
            }
        }
        else
        {
            step = false;
        }

        if (step == true)
        {
            riseVector.y += stepLeap;
            transform.position = Vector3.Lerp(transform.position, riseVector + directionRay+Vector3.forward, Time.deltaTime * stepTime);
        }
    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.layer==9)
        {
            step = false;
            player.GetComponent<Behaviour>().Otter.SetBool("climb", false);
        }
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (gameObject.CompareTag("Bridge"))
        {
            stepLeap = 50;
        }
        if (gameObject.CompareTag("stairs"))
        {
            stepLeap = 150;
        }

    }
}

