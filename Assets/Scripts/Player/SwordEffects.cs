using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordEffects : MonoBehaviour
{
    Rigidbody Player;
    [SerializeField] GameObject FireSwordTrail;
    [SerializeField] GameObject FireBreath;
    [SerializeField] Transform Sword;
    [SerializeField] Transform AttackPoint;
    [SerializeField] GameObject SwordCopter;
    [SerializeField] GameObject SwordPlainTrail;
    private void Start()
    {
        Player = transform.parent.GetComponent<Rigidbody>();
    }
    public void FireSword()
    {
        var BigSwordTrail = Instantiate(FireSwordTrail, Sword.position, Sword.rotation);
        BigSwordTrail.transform.parent = Sword;
        var FireBall = Instantiate(FireBreath, AttackPoint.position + new Vector3(0, 1, 0), Sword.rotation);
        Player.velocity = Player.transform.TransformDirection(Vector3.forward) * 2 + Vector3.up * 0.5f;
    }

    public void GreatSwing()
    {
        var greatTrail = Instantiate(SwordCopter, Sword.position, Sword.rotation);
        greatTrail.transform.parent = Sword;
    }
    public void SmallSwing()
    {
        var smallTrail = Instantiate(SwordPlainTrail, Sword.position, Sword.rotation);
        smallTrail.transform.parent = Sword;
    }
}
