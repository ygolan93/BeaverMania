using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaspCourse : MonoBehaviour
{
    
    [Range(0, 10)]
    public float distance;
    public bool drawRayCast;
    bool contact;
    public bool blocked;
    public float timer = 2f;
    float timerReset;
    public LineRenderer LineTrace;
    public NPC_Basic Wasp;
    public LayerMask collisionLayer;
    Vector3 originRay;
    Vector3 directionRay;
    RaycastHit hit;
    // Update is called once per frame
    private void Start()
    {
        LineTrace.useWorldSpace=true;
        blocked = false;
        contact = false;
        timerReset = timer;
    }

    [System.Obsolete]
    void Update()
    {
        if (Wasp.NPC.velocity.magnitude > 0)
        {
            directionRay = Wasp.NPC.velocity.normalized;
        }
        directionRay.z = -1;
        originRay = transform.position;
        Ray ray = new Ray(originRay, directionRay);
        Debug.DrawRay(originRay, directionRay * distance, Color.yellow);
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

        if (Physics.Raycast(ray, out hit, distance, collisionLayer))
        {
            if (hit.transform.gameObject.tag == "Isle" && Wasp.combo>=3)
            {
                contact = true;
            }
        }
        if (contact==true && timer>0)
        {
            Wasp.NPC.velocity += new Vector3(0, 0, 1);
            timer -= Time.deltaTime * 0.1f;
        }
        else
        {
            contact = false;
            timer = timerReset;
        }


    }
}

