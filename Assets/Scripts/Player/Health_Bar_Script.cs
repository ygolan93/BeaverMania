using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health_Bar_Script : MonoBehaviour
{
    public Slider HealthSlider;
    public Slider StaminaSlider;
    public void SetMaxHealth (float health)
    {
        HealthSlider.maxValue = health;
        HealthSlider.value = health;
    }
    public void SetHealth (float health)
    {
        HealthSlider.value = health;
    }

    public void SetMaxStamina(float stamina)
    {
        StaminaSlider.maxValue = stamina;
        StaminaSlider.value = stamina;
    }
    public void SetStamina(float stamina)
    {
        StaminaSlider.value = stamina;
    }
}
