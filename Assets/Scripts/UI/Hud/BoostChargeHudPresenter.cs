using Beavermania.Player;
using Beavermania.Player.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class BoostChargeHudPresenter : MonoBehaviour
    {
        [SerializeField] BoostChargeController boostCharge;
        [SerializeField] Image fillImage;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Color chargingColor = new(0.45f, 0.4f, 0.35f, 0.85f);
        [SerializeField] Color readyColor = new(0.95f, 0.78f, 0.2f, 1f);
        [SerializeField] Color activeColor = new(0.35f, 0.85f, 1f, 1f);

        void Awake()
        {
            if (boostCharge == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    boostCharge = player.GetComponent<BoostChargeController>();
            }
        }

        void Update()
        {
            if (boostCharge == null)
                return;

            float normalized = boostCharge.NormalizedCharge;
            if (fillImage != null)
            {
                fillImage.fillAmount = normalized;
                if (boostCharge.IsBoostActive)
                    fillImage.color = activeColor;
                else if (boostCharge.IsReady)
                    fillImage.color = readyColor;
                else
                    fillImage.color = chargingColor;
            }

            if (statusText != null)
                statusText.text = boostCharge.GetHudLabel();
        }
    }
}
