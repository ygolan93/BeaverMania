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
    public bool drawRayCast;

    public Rigidbody player;
    public LayerMask collisionLayer;

    public LineRenderer LineTrace;
    public LineRenderer LineTrace2;

    Vector3 riseVector;
    Vector3 originRay;
    Vector3 directionRay;
    //RaycastHit hit;
    //RaycastHit hit2;
    Quaternion directionRot;
    // Update is called once per frame
    private void Start()
    {
        LineTrace.useWorldSpace=true;
        LineTrace.positionCount = 2;
        LineTrace2.positionCount = 2;
    }

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
        LineTrace.SetWidth(stepLeap * 0.001f, stepLeap * 0.001f);
        LineTrace.SetPosition(0, originRay);
        LineTrace.SetPosition(1, originRay + directionRay* distance);

        Ray ray2 = new(originRay+new Vector3(0,0.4f,0), directionRay);
        Debug.DrawRay(originRay + new Vector3(0, 0.4f, 0), 1.5f * distance * directionRay, Color.yellow);
        LineTrace2.SetWidth(stepLeap * 0.001f, stepLeap * 0.001f);
        LineTrace2.SetPosition(0, originRay/* + new Vector3(0, 1, 0)*/);
        LineTrace2.SetPosition(1, originRay /*+ new Vector3(0, 1, 0) */+ 1.5f * distance * directionRay);

        if (drawRayCast == true)
        {
            LineTrace.gameObject.SetActive(true);
            LineTrace2.gameObject.SetActive(true);
        }
        if (drawRayCast == false)
        {
            LineTrace.gameObject.SetActive(false);
            LineTrace2.gameObject.SetActive(false);
        }

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
        //if (!Input.anyKey)
        //{
        //    step = false;
        //}

    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.layer==9)
        {
            step = false;
            player.GetComponent<Behaviour>().Otter.SetBool("climb", false);
        }
    }
    //private void OnCollisionExit(Collision OBJ)
    //{
    //    if (OBJ.gameObject.layer == 9)
    //    {
    //        step = false;
    //        player.GetComponent<Behaviour>().Otter.SetBool("climb", false);
    //    }
    //}

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

