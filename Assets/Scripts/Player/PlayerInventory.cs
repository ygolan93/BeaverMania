using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    public void AddSeed() => owner.NutCount++;
    public void AddApple() => owner.Apple++;
    public void AddGoblet() => owner.GobletPickup++;

    public bool TryPlantSeed()
    {
        if (owner.NutCount <= 0)
        {
            return false;
        }

        owner.Otter.Play("Crouch");
        Instantiate(owner.Seed, owner.AttackPoint.position, Quaternion.identity);
        owner.NutCount--;
        return true;
    }

    public bool TryEatApple()
    {
        if (owner.Apple <= 0)
        {
            return false;
        }

        owner.SetAppleModelActive(true);
        owner.Otter.Play("Consume");
        owner.Sound.Eat();
        if (owner.MaxHealth - owner.CurrentHealth > 500)
        {
            owner.TakeDamage(-500);
            owner.HealthBar.SetHealth(owner.CurrentHealth);
        }
        else
        {
            owner.CurrentHealth = owner.MaxHealth;
            owner.HealthBar.SetMaxHealth(owner.MaxHealth);
        }
        owner.Apple--;
        return true;
    }

    public bool TryUseGoblet()
    {
        if (owner.GobletPickup <= 0)
        {
            return false;
        }

        owner.Otter.Play("Consume");
        owner.Sound.Drink();
        owner.GobletON();
        return true;
    }
}
