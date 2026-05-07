using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
   public Rigidbody Ball;
    Behaviour Player;
    public bool isArrow;
    public bool isFireBall;
    [SerializeField] AudioSource Sound;
    [SerializeField] float clock;
    [SerializeField] int forwardVel;
    [SerializeField] int upwardVel;
    [SerializeField] GameObject Explosion;
    [SerializeField] int Damage;
    [SerializeField] GameObject arrowPickup;
    // Start is called before the first frame update
    void Start()
    {
        var mainCamera = Camera.main;
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        Player = playerObject != null ? playerObject.GetComponent<Behaviour>() : null;

        if (!ValidateReferences(mainCamera))
        {
            return;
        }

        Vector3 launchDirection = mainCamera.transform.TransformDirection(Vector3.forward);
        Physics.IgnoreLayerCollision(1, 3);
        Ball.velocity = launchDirection * forwardVel + Vector3.up * upwardVel;
    }

    bool ValidateReferences(Camera mainCamera)
    {
        bool valid = RuntimeReferenceValidator.Require(mainCamera, this, nameof(mainCamera)) &
            RuntimeReferenceValidator.Require(Player, this, nameof(Player)) &
            RuntimeReferenceValidator.Require(Ball, this, nameof(Ball));

        if (isFireBall)
        {
            valid &= RuntimeReferenceValidator.Require(Explosion, this, nameof(Explosion));
        }

        if (isArrow)
        {
            valid &= RuntimeReferenceValidator.Require(arrowPickup, this, nameof(arrowPickup));
        }

        if (!isFireBall)
        {
            valid &= RuntimeReferenceValidator.Require(Sound, this, nameof(Sound));
        }

        return valid;
    }
    private void Update()
    {
        clock -= Time.deltaTime;
        if (clock<=0)
        {
            Destroy(gameObject);
            if (isArrow==true)
            {
                Instantiate(arrowPickup, transform.position, Quaternion.identity);
            }
        }
        
    }


    public void Explode()
    {
        var explode = Instantiate(Explosion, transform.position, transform.rotation);
        explode.transform.localScale += new Vector3(1, 1, 1);
        Destroy(transform.gameObject);
    }

    public void RockHit()
    {
        Sound.PlayOneShot(Sound.clip);
        Sound.volume = 0.2f;
        Sound.pitch = 0.8f;
    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("NPC"))
        {
            OBJ.gameObject.GetComponent<NPC_Basic>().TakeDamage(Damage);
            OBJ.gameObject.GetComponent<NPC_Basic>().combo += OBJ.gameObject.GetComponent<NPC_Basic>().hit2stun;
            if (isFireBall == true)
            {
                Explode();
            }
            if (isFireBall == false)
            {
                RockHit();
            }
            Player.Plattering = "Bam! Take that";
            Player.ChangeSpeech = 1f;
        }
        if (OBJ.gameObject.CompareTag("Scorpion"))
        {
            OBJ.gameObject.GetComponent<ScorpionScript>().TakeDamage(Damage);
            OBJ.gameObject.GetComponent<ScorpionScript>().combo += 3;
            if (isFireBall == true)
            {
                Explode();
            }
            if (isFireBall == false)
            {
                RockHit();
            }
        }
        if (OBJ.gameObject.CompareTag("Hive"))
        {
            OBJ.gameObject.GetComponent<Static_Hive>().TakeDamage(Damage);
            if (isFireBall == true)
            {
                Explode();
            }
            if (isFireBall == false)
            {
                RockHit();
            }
        }
        if (OBJ.gameObject.CompareTag("Isle"))
        {
            if (isFireBall == true)
            {
                Explode();
            }
            if (isFireBall == false)
            {
                RockHit();
            }
        }
    }
}
