using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossHitbox : MonoBehaviour
{
    public ScorpionScript owner;
    public float damage = 15f;
    public DamageType damageType = DamageType.Hazard;
    public bool parryable = true;
    public float hitCooldownSeconds = 0.5f;

    readonly Dictionary<Behaviour, float> nextHitTimeByTarget = new Dictionary<Behaviour, float>();
    const int ParryCounterDamage = 10;

    void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<ScorpionScript>();
        }
    }

    void OnDisable() => nextHitTimeByTarget.Clear();

    void OnTriggerEnter(Collider other)
    {
        if (!CanDamage())
        {
            return;
        }

        Behaviour target = ResolveTarget(other);
        if (target == null || !CanHit(target))
        {
            return;
        }

        nextHitTimeByTarget[target] = Time.time + Mathf.Max(0f, hitCooldownSeconds);

        if (parryable && target.isParried)
        {
            target.TakeDamage(damage);
            owner.TakeDamage(new DamageEvent
            {
                Amount = ParryCounterDamage,
                Source = target.gameObject,
                Point = other.ClosestPoint(transform.position),
                Type = DamageType.Melee,
                CanStun = false
            });
            return;
        }

        if (!CombatDebugGate.AllowsDamage(target.gameObject))
        {
            return;
        }

        target.TakeDamage(new DamageEvent
        {
            Amount = damage,
            Source = owner.gameObject,
            Point = other.ClosestPoint(transform.position),
            Type = damageType,
            CanStun = false
        });

        if (target.CurrentHealth <= 0)
        {
            target.HandleFailure(PlayerFailureReason.BossDamage);
        }
    }

    bool CanDamage() => owner != null && owner.isActiveAndEnabled && owner.CurrentHealth > 0 && owner.isAttacking && owner.combo < owner.comboLimit;

    static Behaviour ResolveTarget(Collider other) => other.GetComponentInParent<Behaviour>();

    bool CanHit(Behaviour target)
    {
        float nextHitTime;
        return !nextHitTimeByTarget.TryGetValue(target, out nextHitTime) || Time.time >= nextHitTime;
    }
}
