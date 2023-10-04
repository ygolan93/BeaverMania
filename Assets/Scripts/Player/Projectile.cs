using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
   public Rigidbody Ball;
    Behaviour Player;
    public bool isArrow;
    [SerializeField] AudioSource RockSound;
    [SerializeField] float clock = 2f;
    [SerializeField] int forwardVel=65;
    [SerializeField] int upwardVel=12;
    int ArrowDMG;
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        Ball.velocity = Camera.main.transform.TransformDirection(Vector3.forward)* forwardVel + Vector3.up* upwardVel;
        if (isArrow == true)
        {
            ArrowDMG = 500;
        }
        else
        {
            ArrowDMG = 0;
        }
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
        if (OBJ.gameObject != Player)
        {
            RockSound.PlayOneShot(RockSound.clip);
            RockSound.volume = 0.2f;
            RockSound.pitch = 0.8f;
        }
        if (OBJ.gameObject.CompareTag("NPC"))
        {
            OBJ.gameObject.GetComponent<NPC_Basic>().TakeDamage((int)Ball.velocity.magnitude+ArrowDMG);
            OBJ.gameObject.GetComponent<NPC_Basic>().combo = 3;
            Player.Plattering = "Bam! Take that";
            Player.ChangeSpeech = 1f;
        }
        if (OBJ.gameObject.CompareTag("Boss"))
        {
            OBJ.gameObject.GetComponent<BossScript>().TakeDamage(1+ArrowDMG/10);
            OBJ.gameObject.GetComponent<BossScript>().combo+=1;
        }
        if (OBJ.gameObject.CompareTag("Hive"))
        {
            OBJ.gameObject.GetComponent<Static_Hive>().TakeDamage((int)Ball.velocity.magnitude +ArrowDMG/5);
        }
    }
}
