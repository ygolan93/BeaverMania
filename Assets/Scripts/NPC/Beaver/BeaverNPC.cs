using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeaverNPC : MonoBehaviour
{
    // Start is called before the first frame update
    Rigidbody Beaver;
    Animator BeaverAnimator;
    Behaviour Player;
    bool isHIT = false;
    public float steer = 0.1f;
    public float speed = 3;
    public float heading;
    public float time2turn = 10;
    public float time2REST = 4;
    public int MaxHealth = 2000;
    bool grounded=false;
    public Vector3 Wander;
    public Quaternion rotGoal;

    void Start()
    {
        Beaver = GetComponent<Rigidbody>();
         BeaverAnimator = GetComponent<Animator>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        Movement();
    }
    private void OnCollisionEnter(Collision OBJ)
    {

        if (OBJ.gameObject.CompareTag("NPC"))
        {
            Beaver.constraints = RigidbodyConstraints.None;
            isHIT = true;
        }
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            isHIT = true;
            Player.Currency--;
        }

    }
    [System.Obsolete]
    private void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.tag != "NPC" && OBJ.gameObject.tag != "Damage" && OBJ.gameObject.tag != "Isle")
        {
            Movement();
        }
        if (OBJ.gameObject.tag=="Isle")
        {
            grounded = true;
        }
        if (OBJ.gameObject.tag == "Player")
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                transform.rotation = Quaternion.EulerAngles(0, 0, 0);
                isHIT = false;
                Player.Currency++;
            }
        }



    }
    private void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Isle")
        {
            grounded = false;
        }

    }
    void Movement()
    {
        Wander.x = Mathf.Cos(heading);
        Wander.z = Mathf.Sin(heading);
        Wander.y = Beaver.velocity.y;

        rotGoal = Quaternion.LookRotation(new Vector3(Wander.x, 0, Wander.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, steer);
        Beaver.velocity = Wander.normalized * speed;
    }

    // Update is called once per frame


    void Update()
    {

        if (isHIT == false)
        {
            Beaver.constraints = RigidbodyConstraints.FreezeRotation;

            if (grounded == false)
            {
                BeaverAnimator.SetBool("Midair", true);
                BeaverAnimator.SetBool("Walk", false);
            }
            if (grounded == true)
            {
                BeaverAnimator.SetBool("Midair", false);


                if (time2turn > 0)
                {
                    time2turn -= Time.deltaTime;
                    BeaverAnimator.SetBool("Walk", true);
                    Movement();
                }

                if (time2turn <= 0)
                {
                    time2REST -= Time.deltaTime;
                    Beaver.velocity = new Vector3(0, Beaver.velocity.y, 0);
                    BeaverAnimator.SetBool("Walk", false);
                    if (time2REST <= 0)
                    {
                        time2turn = 10;
                        heading = Random.Range(0, 360);
                        time2REST = Random.value * 10;
                    }
                }
            }
        }
        else
        {
            Beaver.constraints = RigidbodyConstraints.None;
            BeaverAnimator.SetBool("Midair", false);
            BeaverAnimator.SetBool("Walk", false);

        }
    }
}
