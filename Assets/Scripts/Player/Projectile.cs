using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
   public Rigidbody Ball;
    Behavior Player;
    NPC_Basic Enemy;
    float clock = 2f;
    // Start is called before the first frame update
    void Start()
    {
        Enemy = GameObject.FindGameObjectWithTag("NPC").GetComponent<NPC_Basic>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        Ball.velocity = Camera.main.transform.TransformDirection(Vector3.forward)*60+Vector3.up*12;
    }
    private void Update()
    {
        clock -= Time.deltaTime;
        if (clock<=0)
        {
            Destroy(gameObject);
        }
        
    }

}
