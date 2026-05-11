using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedAttack : MonoBehaviour
{
    [SerializeField] Behaviour Player;
    [SerializeField] Transform AttackPoint;
    [SerializeField] Transform Sphere;
    [SerializeField] GameObject GlowEffect;
    [SerializeField] LayerMask enemyLayers;

    private void Start()
    {
        Player = GetComponentInParent<Behaviour>();
        SetActiveIfChanged(GlowEffect, false);
    }

    static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    public void CauseDamage(Vector3 origin, float range, int Damage)
    {
        if (range <= 0f)
        {
            return;
        }

        Collider[] hitEnemies = Physics.OverlapSphere(origin, range, enemyLayers);
        var damaged = new HashSet<IDamageable>();
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.TryGetComponent(out IDamageable damageable))
            {
                if (!damaged.Add(damageable))
                {
                    continue;
                }

                damageable.TakeDamage(new DamageEvent
                {
                    Amount = Damage,
                    Source = Player != null ? Player.gameObject : gameObject,
                    Point = enemy.ClosestPoint(origin),
                    Type = DamageType.Melee,
                    CanStun = true
                });
                continue;
            }

            switch (enemy.tag)
            {
                case PlayerTags.Npc:
                    {
                        var Wasp = enemy.gameObject.GetComponent<NPC_Basic>();
                        if (Wasp != null)
                        {
                            Wasp.TakeDamage(Damage);
                        }
                        break;
                    }
                case "Hive":
                    {
                        var Hive = enemy.gameObject.GetComponent<Static_Hive>();
                        if (Hive != null)
                        {
                            Hive.TakeDamage(Damage);
                        }
                        break;
                    }
                case PlayerTags.Scorpion:
                    {
                        var Scorpion = enemy.gameObject.GetComponent<ScorpionScript>();
                        if (Scorpion != null)
                        {
                            Scorpion.TakeDamage(Damage);
                        }
                        break;
                    }
            }
        }
    }


    public void RollAttack()
    {
        if (AttackPoint != null)
        {
            CauseDamage(AttackPoint.position, 1.5f, 200);
        }
    }

    public void GroundAttack()
    {
        if (Player == null || AttackPoint == null || Sphere == null || Player.Arsenal == null || Player.Arsenal.Count == 0)
        {
            return;
        }

        var arsenal = Player.Arsenal;
        var weapon = Mathf.Clamp(Player.arsenalBrowser, 0, arsenal.Count - 1);
        switch (arsenal[weapon])
        {
            case "Bare Hands":
                {
                    CauseDamage(AttackPoint.position, 0.7f, 50);
                    break;
                }
            case PlayerTags.Bow:
                {
                    CauseDamage(AttackPoint.position, 1f, 50);
                    break;
                }
            case "Hammers":
                {
                    CauseDamage(AttackPoint.position, 2f, 700);
                    break;
                }
            case "ArmorSet":
                {
                    var feetPos = Sphere.position + new Vector3(0, 0.5f, 0);
                    CauseDamage(feetPos, 4f, 200);
                    break;
                }
        }

    }
    public void AirAttack()
    {
        if (Player == null || Sphere == null || Player.Arsenal == null || Player.Arsenal.Count == 0)
        {
            return;
        }

        var arsenal = Player.Arsenal;
        var weapon = Mathf.Clamp(Player.arsenalBrowser, 0, arsenal.Count - 1);
        switch (arsenal[weapon])
        {
            case "Bare Hands":
                {
                    CauseDamage(Sphere.position, 2.5f, 20);
                    break;
                }
            case "Hammers":
                {
                    CauseDamage(Sphere.position, 2.5f, 20);
                    break;
                }
            case "ArmorSet":
                {
                    CauseDamage(Sphere.position+new Vector3(0,0.5f,0), 4f, 200);
                    break;
                }
        }
    }

    public void ShieldParryON()
    {
        if (Player != null)
        {
            Player.ParryON();
        }
    }
    public void ShieldParryOFF()
    {
        if (Player != null)
        {
            Player.ParryOFF();
        }
    }

    public void TurnOnGlow()
    {
        SetActiveIfChanged(GlowEffect, true);
    }

    public void TurnOffGlow()
    {
        SetActiveIfChanged(GlowEffect, false);
    }
}
