using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fan : MonoBehaviour
{
    [Range(0, 100)]
    [SerializeField] float elevateSpeed;
    [SerializeField] Animator rotor;
    public bool turnON;
    [SerializeField] float counter;
    float initialCounter;
    [SerializeField] AudioSource sound;
    [SerializeField] AudioClip clip;
    private void Start()
    {
        initialCounter = counter;
    }
    private void FixedUpdate()
    {
        if (turnON==true)
        {
            rotor.SetBool("turnOn", true);
            sound.clip = clip;
            sound.PlayOneShot(clip);
            counter -= Time.deltaTime;
            if (counter<=0)
            {
                turnON = false;
            }
        }
        if (turnON==false)
        {
            sound.Stop();
            rotor.SetBool("turnOn", false);
            counter = initialCounter;
        }
    }
    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player")&&turnON==true)
        {
            bool isGrounded = OBJ.gameObject.GetComponent<Behaviour>().grounded;
            Rigidbody Player = OBJ.gameObject.GetComponent<Rigidbody>();
            if (isGrounded == false && Input.GetKeyDown(KeyCode.Mouse0))
            {
                Player.velocity += new Vector3(0, elevateSpeed, 0);
            }
        }
    }
}
