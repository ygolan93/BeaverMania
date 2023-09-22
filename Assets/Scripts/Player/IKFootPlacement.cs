using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootPlacement : MonoBehaviour
{
   [SerializeField] Animator anim;
    [Range(0, 1f)]
    public float DistanceToGround;
    [Range (0,1f)]
    public float PositionWeight;
    public LayerMask layerMask;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }


    private void OnAnimatorIK(int layerIndex)
    {
        if (anim)
        {
            //Left Foot
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, PositionWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
            Ray rayL = new(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
            if (Physics.Raycast(rayL, out RaycastHit hitL, DistanceToGround + 1f, layerMask))
            {
                if (hitL.transform.CompareTag("Isle"))
                {
                    Vector3 footPosition = hitL.point;
                    footPosition.y += DistanceToGround;
                    anim.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                    anim.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hitL.normal));
                }
            }
            //Right Foot
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, PositionWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
            Ray rayR = new(anim.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
            if (Physics.Raycast(rayR, out RaycastHit hitR, DistanceToGround + 1f, layerMask))
            {
                if (hitR.transform.CompareTag("Isle"))
                {
                    Vector3 footPosition = hitR.point;
                    footPosition.y += DistanceToGround;
                    anim.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                    anim.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hitR.normal));
                }
            }
        }
    }
}
