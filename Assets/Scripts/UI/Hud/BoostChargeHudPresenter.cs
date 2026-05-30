using Beavermania.Player.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class BoostChargeHudPresenter : MonoBehaviour
    {
        const string ReadyPromptText = "Ready - Press Y to activate";

        [SerializeField] BoostChargeController boostCharge;
        [SerializeField] Slider chargeSlider;
        [SerializeField] RectTransform staminaBarTemplate;
        [SerializeField] Image fillImage;
        [SerializeField] Image backgroundImage;
        [SerializeField] TextMeshProUGUI barLabel;
        [SerializeField] float gapAboveStamina = 6f;
        [SerializeField] Color chargingBackgroundColor = new(0.92f, 0.78f, 0.12f, 1f);
        [SerializeField] Color readyBackgroundColor = new(0.18f, 0.72f, 0.28f, 1f);
        [SerializeField] Color activeBackgroundColor = new(0.95f, 0.52f, 0.12f, 1f);
        [SerializeField] Color chargingFillColor = new(1f, 1f, 1f, 1f);
        [SerializeField] Color readyFillColor = new(0.92f, 1f, 0.92f, 1f);
        [SerializeField] Color activeFillColor = new(1f, 0.92f, 0.75f, 1f);

        bool boostBarCreatedAtRuntime;
        bool barLabelCreatedAtRuntime;

        void Awake()
        {
            ResolveBoostCharge();
            EnsureBoostBarFromStaminaTemplate();
            EnsureBarLabel();
            CacheBackgroundImageFromSlider();
        }

        void Update()
        {
            if (boostCharge == null)
            {
                ResolveBoostCharge();
                if (boostCharge == null)
                    return;
            }

            if (chargeSlider == null)
                return;

            if (boostCharge.IsBoostActive)
            {
                float remaining = boostCharge.NormalizedBoostTimeRemaining;
                int remainingPercent = Mathf.RoundToInt(remaining * 100f);
                chargeSlider.value = remainingPercent;

                ApplyBackgroundColor(activeBackgroundColor);

                if (fillImage != null)
                    fillImage.color = activeFillColor;

                if (barLabel != null)
                    barLabel.text = $"{remainingPercent}%";

                return;
            }

            float normalized = boostCharge.NormalizedCharge;
            int chargePercent = Mathf.RoundToInt(normalized * 100f);
            chargeSlider.value = chargePercent;

            if (boostCharge.IsReady)
            {
                chargeSlider.value = chargeSlider.maxValue;
                ApplyBackgroundColor(readyBackgroundColor);

                if (fillImage != null)
                    fillImage.color = readyFillColor;

                if (barLabel != null)
                    barLabel.text = ReadyPromptText;

                return;
            }

            ApplyBackgroundColor(chargingBackgroundColor);

            if (fillImage != null)
                fillImage.color = chargingFillColor;

            if (barLabel != null)
                barLabel.text = $"{chargePercent}%";
        }

        void ResolveBoostCharge()
        {
            if (boostCharge != null)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                boostCharge = player.GetComponent<BoostChargeController>();
        }

        void EnsureBoostBarFromStaminaTemplate()
        {
            if (chargeSlider == null)
            {
                var existing = transform.Find("BoostBar");
                if (existing != null)
                    chargeSlider = existing.GetComponent<Slider>();
            }

            if (chargeSlider != null)
            {
                ConfigureBoostSlider(chargeSlider);
                CacheFillImageFromSlider();
                CacheBackgroundImageFromSlider();
                return;
            }

            RectTransform staminaBar = staminaBarTemplate != null
                ? staminaBarTemplate
                : FindStaminaBarRectTransform();
            if (staminaBar == null)
            {
                Debug.LogWarning($"{nameof(BoostChargeHudPresenter)}: StaminaBar template not found; boost meter unavailable.", this);
                return;
            }

            var boostBarObject = Instantiate(staminaBar.gameObject, staminaBar.parent);
            boostBarObject.name = "BoostBar";
            boostBarObject.transform.SetSiblingIndex(staminaBar.GetSiblingIndex());

            var boostRect = boostBarObject.GetComponent<RectTransform>();
            float staminaHeight = staminaBar.rect.height * staminaBar.localScale.y;
            Vector2 staminaPosition = staminaBar.anchoredPosition;
            boostRect.anchorMin = staminaBar.anchorMin;
            boostRect.anchorMax = staminaBar.anchorMax;
            boostRect.pivot = staminaBar.pivot;
            boostRect.localScale = staminaBar.localScale;
            boostRect.sizeDelta = staminaBar.sizeDelta;
            boostRect.anchoredPosition = new Vector2(staminaPosition.x, staminaPosition.y + staminaHeight + gapAboveStamina);

            chargeSlider = boostBarObject.GetComponent<Slider>();
            ConfigureBoostSlider(chargeSlider);
            CacheFillImageFromSlider();
            CacheBackgroundImageFromSlider();
            boostBarCreatedAtRuntime = true;
        }

        void EnsureBarLabel()
        {
            if (chargeSlider == null)
                return;

            if (barLabel == null)
            {
                Transform existing = chargeSlider.transform.Find("BoostBarLabel");
                if (existing != null)
                    barLabel = existing.GetComponent<TextMeshProUGUI>();
            }

            if (barLabel == null)
            {
                var labelObject = new GameObject("BoostBarLabel", typeof(RectTransform));
                labelObject.transform.SetParent(chargeSlider.transform, false);

                barLabel = labelObject.AddComponent<TextMeshProUGUI>();
                barLabelCreatedAtRuntime = true;
            }

            ConfigureBarLabel(barLabel);
            EnsureBarLabelInsideBar();
        }

        void EnsureBarLabelInsideBar()
        {
            if (barLabel == null || chargeSlider == null)
                return;

            var rect = barLabel.rectTransform;
            rect.SetParent(chargeSlider.transform, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = new Vector2(12f, 6f);
            rect.offsetMax = new Vector2(-12f, -6f);
            rect.SetAsLastSibling();
        }

        static void ConfigureBarLabel(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 14f;
            label.overflowMode = TextOverflowModes.Truncate;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            label.text = "0%";
        }

        RectTransform FindStaminaBarRectTransform()
        {
            foreach (var slider in GetComponentsInChildren<Slider>(true))
            {
                if (slider != null && string.Equals(slider.gameObject.name, "StaminaBar", System.StringComparison.OrdinalIgnoreCase))
                    return slider.GetComponent<RectTransform>();
            }

            return null;
        }

        void ConfigureBoostSlider(Slider slider)
        {
            if (slider == null)
                return;

            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.value = 0f;

            if (slider.handleRect != null)
                slider.handleRect.gameObject.SetActive(false);
        }

        void CacheFillImageFromSlider()
        {
            if (chargeSlider == null || chargeSlider.fillRect == null)
                return;

            fillImage = chargeSlider.fillRect.GetComponent<Image>();
        }

        void CacheBackgroundImageFromSlider()
        {
            if (backgroundImage != null || chargeSlider == null)
                return;

            Transform barBackground = chargeSlider.transform.Find("Bar");
            if (barBackground != null)
                backgroundImage = barBackground.GetComponent<Image>();
        }

        void ApplyBackgroundColor(Color color)
        {
            if (backgroundImage != null)
                backgroundImage.color = color;
        }

        void OnDestroy()
        {
            if (boostBarCreatedAtRuntime && chargeSlider != null)
                Destroy(chargeSlider.gameObject);
            else if (barLabelCreatedAtRuntime && barLabel != null)
                Destroy(barLabel.gameObject);
        }
    }
}
