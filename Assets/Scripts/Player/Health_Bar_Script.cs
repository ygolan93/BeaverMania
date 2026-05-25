using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Hud
{

    public class Health_Bar_Script : MonoBehaviour
    {
        public Slider HealthSlider;
        public Slider StaminaSlider;
        public void SetMaxHealth(float health)
        {
            if (HealthSlider == null)
                return;
            HealthSlider.maxValue = health;
            HealthSlider.value = health;
        }

        public void SetHealth(float health)
        {
            if (HealthSlider == null)
                return;
            HealthSlider.value = health;
        }

        public void SetMaxStamina(float stamina)
        {
            if (StaminaSlider == null)
                return;
            StaminaSlider.maxValue = stamina;
            StaminaSlider.value = stamina;
        }

        public void SetStamina(float stamina)
        {
            if (StaminaSlider == null)
                return;
            StaminaSlider.value = stamina;
        }
    }
}
