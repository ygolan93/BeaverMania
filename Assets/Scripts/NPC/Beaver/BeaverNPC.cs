using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeaverNPC : MonoBehaviour
{
    // Start is called before the first frame update
    Rigidbody Beaver;
    Animator BeaverAnimator;
    Behaviour Player;
    public Transform[] wayPoints;
    public GameObject[] beavers;

    public int currentPoint  = 0;
    public bool isHIT = false;
    public float steer = 0.1f;
    public float speed = 3;
    public float heading;
    public float wait = 4;
    public int MaxHealth = 2000;
    bool grounded=false;
    bool moving;
    bool metPlayer = false;
    bool onward;
    [SerializeField] float moveClock;
    float initialClock;
    public Vector3 Wander;
    Vector3 Break;
    public Quaternion rotGoal;
    Vector3 awakePos;


    [System.Obsolete]
    void Start()
    {
        Beaver = GetComponent<Rigidbody>();
        BeaverAnimator = GetComponent<Animator>();
        beavers = GameObject.FindGameObjectsWithTag("FriendlyNPC");
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        awakePos = transform.position;
        moving = true;
        onward = true;
        Physics.IgnoreLayerCollision(12, 11, true);
        Physics.IgnoreLayerCollision(12, 12, true);

        moveClock = 10.0f;
        initialClock = moveClock;
    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("NPC"))
        {
            isHIT = true;
        }
        if (OBJ.gameObject.CompareTag("Player")|| OBJ.gameObject.CompareTag("Damage"))
        {
            moving = false;
            metPlayer = true;
        }
        if (OBJ.gameObject.CompareTag("Strike"))
        {
            transform.position = awakePos;
        }
    }

    [System.Obsolete]
    private void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Isle"))
        {
            grounded = true;
        }
    }
    private void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Isle"))
        {
            grounded = false;
        }
    }


    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("NPCWP"))
        {
            if (currentPoint < wayPoints.Length && onward == true)
            {
                currentPoint++;
            }
            if (currentPoint == wayPoints.Length)
            {
                currentPoint--;
                onward = false;
            }
            if (currentPoint > 0 && onward == false)
            {
                currentPoint--;
            }
            if (currentPoint == 0 && onward == false)
            {
                onward = true;
            }

        }
    }
    void Complain(Vector3 Direction)
    {
        if (wait>0&&moving==false)
        {
            transform.gameObject.layer = LayerMask.NameToLayer("IgnoreOtherBeavers".ToString());
            rotGoal = Quaternion.LookRotation(Direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            BeaverAnimator.SetBool("Walk", false);
            wait -= Time.deltaTime;
        }
       else
        {
            metPlayer = false;
            BeaverAnimator.SetBool("Yell", false);
            BeaverAnimator.SetBool("Walk", true);
            transform.gameObject.layer = LayerMask.NameToLayer("BeaverNPC".ToString());
            moving = true;
            wait = 4;
        }
    }
    void Movement()
    { 
        if (moveClock>0)
        {
            moveClock -= Time.deltaTime;
            BeaverAnimator.SetBool("Walk", true);
            Wander = wayPoints[currentPoint].position - transform.position;
            Wander.y = 0;
            rotGoal = Quaternion.LookRotation(Wander);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
            Beaver.velocity = Wander.normalized * speed;
        }
        if (moveClock <= 0)
        {
            moveClock = initialClock;
        }
    }

    // Update is called once per frame
    [System.Obsolete]
    void Update()
    {
        //Turn on and off collision between beavers
        foreach (var beaver in beavers)
        {
            var beaverPoint = beaver.GetComponent<BeaverNPC>().currentPoint;
            if (beaverPoint == currentPoint)
            {
                transform.gameObject.layer = LayerMask.NameToLayer("BeaverNPC".ToString());
            }
        }
        for (int i = 0; i < beavers.Length; i++)
        {
            if (beavers[i].gameObject.GetComponent<BeaverNPC>().currentPoint != currentPoint)
            {
                transform.gameObject.layer = LayerMask.NameToLayer("IgnoreOtherBeavers".ToString());
            }
        }
        Vector3 playerPos = Player.transform.position;
        if (grounded == false)
        {
            BeaverAnimator.SetBool("Midair", true);
            BeaverAnimator.SetBool("Walk", false);
        }
        if (grounded == true)
        {
            BeaverAnimator.SetBool("Midair", false);

            if (moving == true)
            {
             Movement();
             
            }
            if (moving == false)
            {
                if (metPlayer == true)
                {
                    BeaverAnimator.SetBool("Yell", true);
                    Break = playerPos - transform.position;
                }
                else
                {
                    Break = awakePos;
                }
                Complain(Break);
            }
        }
        
    }
}
    







