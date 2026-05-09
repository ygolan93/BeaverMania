using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealthController : MonoBehaviour
{
    Behaviour owner;
    float staminaClock;
    float stopHurt;
    const float StaminaClockInitial = 0.5f;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
        staminaClock = StaminaClockInitial;
        if (owner == null)
        {
            return;
        }

        owner.CurrentHealth = owner.MaxHealth;
        if (owner.HealthBar != null)
        {
            owner.HealthBar.SetMaxHealth(owner.CurrentHealth);
        }

        owner.CurrentStamina = owner.MaxStamina;
        if (owner.HealthBar != null)
        {
            owner.HealthBar.SetMaxStamina(owner.CurrentStamina);
        }

        owner.Lives = 3;
    }

    public void TakeDamage(float damage)
    {
        if (owner == null)
        {
            return;
        }

        if (!owner.isParried)
        {
            owner.CurrentHealth -= damage;
            if (owner.HealthBar != null)
            {
                owner.HealthBar.SetHealth(owner.CurrentHealth);
            }
            owner.hurt = damage > 0;
            owner.heal = damage < 0;
            return;
        }

        owner.ToggleParryCounterAttack();
        owner.DefendAnim = 0.3f;
        owner.Defend = true;
        owner.CurrentStamina -= damage;
        if (owner.HealthBar != null)
        {
            owner.HealthBar.SetStamina(owner.CurrentStamina);
        }
    }

    public void TickStamina()
    {
        if (owner == null)
        {
            return;
        }

        if (!owner.isParried)
        {
            if (owner.CurrentStamina > owner.MaxStamina)
            {
                owner.CurrentStamina = owner.MaxStamina;
            }

            if (owner.CurrentStamina < owner.MaxStamina && !Input.GetKey(KeyCode.LeftControl))
            {
                if (!Input.GetKey(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Mouse0) && !Input.GetKey(KeyCode.F))
                {
                    staminaClock -= Time.deltaTime;
                    if (staminaClock <= 0)
                    {
                        owner.CurrentStamina += 1;
                        if (owner.HealthBar != null)
                        {
                            owner.HealthBar.SetStamina(owner.CurrentStamina);
                        }
                    }
                }
                else
                {
                    staminaClock = StaminaClockInitial;
                }
            }
            else
            {
                staminaClock = StaminaClockInitial;
            }
        }
        else
        {
            owner.CurrentStamina -= owner.ArmorEquipped ? 0.001f : 0.05f;
        }
    }

    public void ClampStaminaAndParry()
    {
        if (owner == null)
        {
            return;
        }

        if (owner.CurrentStamina > 0)
        {
            return;
        }

        owner.ParryOFF();
        owner.CurrentStamina = 0;
    }

    public void HandleDeathState()
    {
        if (owner == null)
        {
            return;
        }

        if (owner.CurrentHealth > 0)
        {
            return;
        }

        owner.HandleFailure(PlayerFailureReason.HealthDepleted);
    }

    public void TickHurtHealEffects()
    {
        if (owner == null)
        {
            return;
        }

        if (owner.hurt || owner.heal)
        {
            stopHurt += Time.deltaTime;
            if (stopHurt >= 0.2f)
            {
                owner.hurt = false;
                owner.heal = false;
            }
        }
        else
        {
            stopHurt = 0;
        }

        if (owner.CurrentHealth >= owner.MaxHealth)
        {
            owner.heal = false;
        }
    }
}
