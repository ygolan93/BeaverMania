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
        PlayerReference.TryGetPlayer(out Player);

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
        if (TryDamage(OBJ.gameObject, OBJ.contactCount > 0 ? OBJ.GetContact(0).point : transform.position))
        {
            if (isFireBall == true)
            {
                Explode();
            }
            if (isFireBall == false)
            {
                RockHit();
            }
            return;
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

    bool TryDamage(GameObject target, Vector3 point)
    {
        if (!target.TryGetComponent(out IDamageable damageable))
        {
            return false;
        }

        damageable.TakeDamage(new DamageEvent
        {
            Amount = Damage,
            Source = Player != null ? Player.gameObject : gameObject,
            Point = point,
            Type = isFireBall ? DamageType.Fire : DamageType.Projectile,
            CanStun = true
        });

        if (target.CompareTag("NPC") && Player != null)
        {
            Player.Plattering = "Bam! Take that";
            Player.ChangeSpeech = 1f;
        }

        return true;
    }
}
