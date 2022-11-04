using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_Behavior : MonoBehaviour
{
    //Character
    public Rigidbody Boss;
    [SerializeField] Animator Scorpion;
    //Player Relations
    public GameObject PlayerTarget;
    public float PlayerDistance;
    public Behavior PlayerHealth;

    //Movement
    float steer = 0.3f;
    public Vector3 Distance;
    public Quaternion rotGoal;
    Vector3 SpawnPos;

    // Start is called before the first frame update
    public void Start()
    {
        SpawnPos = transform.position;
    }

    // Update is called once per frame
    public void Update()
    {
       Vector3 Distance = PlayerTarget.transform.position - transform.position;
        PlayerDistance = Distance.magnitude;
        rotGoal = Quaternion.LookRotation(new Vector3(-Distance.x, Boss.velocity.y, -Distance.z));
        //transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
        if (PlayerDistance<=50 && PlayerDistance>15)
        {
            Boss.velocity = new Vector3(Distance.normalized.x*15, Boss.velocity.y, Distance.normalized.z*15);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal,steer);
            Scorpion.SetBool("Walk",true);
        }
        else
        {
            Boss.velocity = new Vector3(0, Boss.velocity.y, 0);
            Scorpion.SetBool("Walk", false);
        }
    }
    //private void OnCollisionEnter(Collision OBJ)
    //{
    //    if (OBJ.gameObject.tag != "Isle")
    //        Boss.velocity = new Vector3(Boss.velocity.x, 10, Boss.velocity.z);
    //}
}
