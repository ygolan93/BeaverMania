using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedAttack : MonoBehaviour
{
    [SerializeField] Behaviour Player;
    [SerializeField] Transform AttackPoint;
    [SerializeField] Transform Sphere;
     float attackRange;
    int Damage;
    [SerializeField] LayerMask enemyLayers;

    private void Start()
    {
        Player = transform.parent.GetComponent<Behaviour>();
    }

    public void CauseDamage(Vector3 origin, float range, int Damage)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(origin, range, enemyLayers);
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.name != null)
            {
                Debug.Log("Hit " + enemy.name);

            }
            switch (enemy.tag)
            {
                case "NPC":
                    {
                        enemy.GetComponent<NPC_Basic>().TakeDamage(Damage);
                        break;
                    }
                case "Hive":
                    {
                        enemy.GetComponent<Static_Hive>().TakeDamage(Damage);
                        break;
                    }
                case "Boss":
                    {
                        enemy.GetComponent<BossScript>().TakeDamage(Damage);
                        break;
                    }
            }
        }
    }


    public void RollAttack()
    {
        CauseDamage(AttackPoint.position, 1.5f, 200);
    }

    public void GroundAttack()
    {
        var arsenal = Player.GetComponent<Behaviour>().Arsenal;
        var weapon = Player.GetComponent<Behaviour>().arsenalBrowser;
        switch (arsenal[weapon])
        {
            case "Bare Hands":
                {
                    CauseDamage(AttackPoint.position, 0.7f, 50);
                    break;
                }
            case "Bow":
                {
                    CauseDamage(AttackPoint.position, 1f, 55);
                    break;
                }
            case "Hammers":
                {
                    CauseDamage(AttackPoint.position, 2f, 150);
                    break;
                }
            case "ArmorSet":
                {
                    var feetPos = Sphere.position + new Vector3(0, 0.5f, 0);
                    CauseDamage(feetPos, 4f, 100);
                    break;
                }
        }

    }
    public void AirAttack()
    {
        var arsenal = Player.GetComponent<Behaviour>().Arsenal;
        var weapon = Player.GetComponent<Behaviour>().arsenalBrowser;
        switch (arsenal[weapon])
        {
            case "Bare Hands":
                {
                    CauseDamage(Sphere.position, 1f, 20);
                    break;
                }
            case "Hammers":
                {
                    CauseDamage(Sphere.position, 1f, 20);
                    break;
                }
            case "ArmorSet":
                {
                    CauseDamage(Sphere.position, 4f, 60);
                    break;
                }
        }
    }
}
