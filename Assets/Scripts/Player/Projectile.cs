using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
   public Rigidbody Ball;
    Behavior Player;
    float clock = 2f;
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        Ball.velocity = Camera.main.transform.TransformDirection(Vector3.forward)*30+Vector3.up*12;
    }
    private void Update()
    {
        clock -= Time.deltaTime;
        if (clock<=0)
        {
            Destroy(gameObject);
        }
        
    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("NPC"))
        {
            OBJ.gameObject.GetComponent<NPC_Basic>().TakeDamage((int)Ball.velocity.magnitude);
            if (Ball.velocity.magnitude>70)
            {
                OBJ.gameObject.GetComponent<NPC_Basic>().combo = 3;

            }
        }
        if (OBJ.gameObject.CompareTag("Hive"))
        {
            OBJ.gameObject.GetComponent<Static_Hive>().TakeDamage((int)Ball.velocity.magnitude);
        }
    }
}
