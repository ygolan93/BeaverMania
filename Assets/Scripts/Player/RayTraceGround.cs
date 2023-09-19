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
    public LineRenderer LineTrace;
    public Rigidbody player;
    public LayerMask collisionLayer;
    Vector3 riseVector;
    Vector3 originRay;
    Vector3 directionRay;
    RaycastHit hit;
    // Update is called once per frame
    private void Start()
    {
        LineTrace.useWorldSpace=true;
    }

    [System.Obsolete]
    void Update()
    {
        if (player.velocity.magnitude > 0)
        {
            directionRay = player.velocity.normalized;
        }
        directionRay.y = 0;
        riseVector = transform.position;
        originRay = transform.position + new Vector3(0, offset, 0);
        Ray ray = new Ray(originRay, directionRay);
        Debug.DrawRay(originRay, directionRay * distance, Color.red);
        LineTrace.SetWidth(stepLeap * 0.001f, stepLeap * 0.001f);
        LineTrace.SetPosition(0, originRay);
        LineTrace.SetPosition(1, originRay + directionRay * distance);
        if (drawRayCast == true)
        {
            LineTrace.gameObject.SetActive(true);
        }
        if (drawRayCast == false)
        {
            LineTrace.gameObject.SetActive(false);
        }

        if (Physics.Raycast(ray, out hit, distance, collisionLayer) && directionRay.y == 0 && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)))
        {
            riseVector.y += stepLeap;
            step = true;
        }
        if (step == true)
        {
            transform.position = Vector3.Lerp(transform.position, riseVector, Time.deltaTime * stepTime);
        }
        if (!Input.anyKey)
        {
            step = false;
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
    private void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.layer == 9)
        {
            step = false;
            player.GetComponent<Behaviour>().Otter.SetBool("climb", false);
        }
    }
}

