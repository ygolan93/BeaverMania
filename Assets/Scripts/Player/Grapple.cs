using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grapple : MonoBehaviour
{
    [Header ("References")]
    [SerializeField] Behaviour Player;
    Rigidbody rb_player;
    [SerializeField] Transform PlayerCam;
    [SerializeField] Transform Hand;
    public LayerMask GrapplingLayer;

    [Header("Grappling")]
    [SerializeField] float maxGrapplingDistance;
    Vector3 grapplePoint;

    [Header("Input")]
    public KeyCode grapplingKey=KeyCode.Tab;
    // Start is called before the first frame update
    void Start()
    {
        Player = gameObject.GetComponent<Behaviour>();
        rb_player = gameObject.GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void Update()
    {   
        
    }

    void shootGrapple()
    {
        RaycastHit Hit;
        if (Physics.Raycast(PlayerCam.position, PlayerCam.forward, out Hit, maxGrapplingDistance, GrapplingLayer))
        {
            grapplePoint = Hit.point;
            Invoke(nameof(ExecuteGrapple), 0.3f);
        }
        else
        {
            grapplePoint = PlayerCam.position + PlayerCam.forward * maxGrapplingDistance;
            Invoke(nameof(StopGrapple), 0.3f);
        }
    }

    void ExecuteGrapple()
    {
        Player.transform.rotation = Quaternion.LookRotation(grapplePoint);
    }
    void StopGrapple()
    {

    }
}
