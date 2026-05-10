using System.Collections;
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
    bool isDisposed;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ExpireAfterLifetime());
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

    IEnumerator ExpireAfterLifetime()
    {
        yield return new WaitForSeconds(clock);
        Dispose(spawnArrowPickup: isArrow, explode: false);
    }

    public void Explode()
    {
        Dispose(spawnArrowPickup: false, explode: true);
    }

    void Dispose(bool spawnArrowPickup, bool explode)
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        if (explode && Explosion != null)
        {
            var explodeInstance = Instantiate(Explosion, transform.position, transform.rotation);
            explodeInstance.transform.localScale += new Vector3(1, 1, 1);
        }

        if (spawnArrowPickup && arrowPickup != null)
        {
            Instantiate(arrowPickup, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    public void RockHit()
    {
        if (Sound == null || isDisposed)
        {
            return;
        }

        Sound.PlayOneShot(Sound.clip);
        Sound.volume = 0.2f;
        Sound.pitch = 0.8f;
    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (isDisposed || OBJ == null || OBJ.gameObject == null)
        {
            return;
        }

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
        if (target == null || !target.TryGetComponent(out IDamageable damageable))
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
