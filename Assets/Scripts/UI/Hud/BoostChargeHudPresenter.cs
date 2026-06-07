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
        [SerializeField] Slider chargeSlider;
        [SerializeField] RectTransform staminaBarTemplate;
        [SerializeField] Image fillImage;
        [SerializeField] Image backgroundImage;
        [SerializeField] TextMeshProUGUI barLabel;
        [SerializeField] float gapAboveStamina = 6f;
        [SerializeField] Color chargingTrackColor = new(0.72f, 0.58f, 0.08f, 1f);
        [SerializeField] Color chargingFillColor = new(0.98f, 0.86f, 0.2f, 1f);
        [SerializeField] Color readyTrackColor = new(0.12f, 0.52f, 0.2f, 1f);
        [SerializeField] Color readyFillColor = new(0.22f, 0.82f, 0.34f, 1f);
        [SerializeField] Color activeTrackColor = new(0.75f, 0.38f, 0.05f, 1f);
        [SerializeField] Color activeFillColor = new(0.98f, 0.55f, 0.1f, 1f);
        [SerializeField] Color emptyTrackColor = new(0.18f, 0.18f, 0.18f, 0.9f);
        [SerializeField] Color emptyFillColor = new(0.55f, 0.45f, 0.1f, 0.35f);
        [SerializeField] Color barLabelColor = new(0.12f, 0.1f, 0.06f, 1f);
        [SerializeField] float barLabelFontSize = 16f;
        [SerializeField] Vector2 barLabelPadding = new(10f, 4f);

        bool boostBarCreatedAtRuntime;
        bool barLabelCreatedAtRuntime;
        const float RefreshIntervalSeconds = 0.066f;
        float refreshTimer;
        int lastSliderPercent = -1;
        string lastLabelText;

        void Awake()
        {
            EnsureBoostBarFromStaminaTemplate();
            DisableLegacyTextOnBoostBar();
            ResolveBarLabelReference();
            EnsureBarLabel();
            CacheBackgroundImageFromSlider();
        }

        void OnEnable()
        {
            ResolveBoostCharge();
            ResolveBarLabelReference();
            if (chargeSlider != null && barLabel != null)
            {
                DisableLegacyTextOnBoostBar();
                EnsureBarLabelInsideBar();
                ConfigureBarLabel(barLabel);
            }
        }

        void Update()
        {
            if (boostCharge == null || chargeSlider == null)
                return;

            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f)
                return;

            refreshTimer = RefreshIntervalSeconds;

            if (boostCharge.IsBoostActive)
            {
                int percent = boostCharge.GetActiveDisplayPercent();
                ApplyVisualStateIfChanged(activeTrackColor, activeFillColor, percent, boostCharge.GetHudLabelText());
                return;
            }

            if (boostCharge.IsReady)
            {
                ApplyVisualStateIfChanged(readyTrackColor, readyFillColor, 100, boostCharge.GetHudLabelText());
                return;
            }

            int chargePercent = boostCharge.GetChargeDisplayPercent();
            if (chargePercent > 0)
            {
                ApplyVisualStateIfChanged(chargingTrackColor, chargingFillColor, chargePercent, boostCharge.GetHudLabelText());
                return;
            }

            ApplyVisualStateIfChanged(emptyTrackColor, emptyFillColor, 0, "Boost 0%");
        }

        void ApplyVisualStateIfChanged(Color trackColor, Color fillColor, int sliderPercent, string labelText)
        {
            int clampedPercent = Mathf.Clamp(sliderPercent, 0, 100);
            if (clampedPercent == lastSliderPercent && labelText == lastLabelText)
                return;

            lastSliderPercent = clampedPercent;
            lastLabelText = labelText;
            ApplyVisualState(trackColor, fillColor, clampedPercent, labelText);
        }

        void ApplyVisualState(Color trackColor, Color fillColor, int sliderPercent, string labelText)
        {
            int clampedPercent = Mathf.Clamp(sliderPercent, 0, 100);
            chargeSlider.value = clampedPercent;

            if (backgroundImage != null)
                backgroundImage.color = trackColor;

            if (fillImage != null)
                fillImage.color = fillColor;

            if (barLabel == null)
                return;

            barLabel.text = labelText;
            barLabel.color = barLabelColor;
        }

        void ResolveBoostCharge()
        {
            if (boostCharge != null)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                boostCharge = player.GetComponent<BoostChargeController>();

            if (boostCharge == null)
                Debug.LogWarning($"{nameof(BoostChargeHudPresenter)}: BoostChargeController not assigned or found on Player.", this);
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

        void ResolveBarLabelReference()
        {
            if (chargeSlider == null)
                return;

            if (barLabel != null && barLabel.transform.IsChildOf(chargeSlider.transform))
                return;

            Transform labelTransform = chargeSlider.transform.Find("BoostBarLabel");
            if (labelTransform != null)
                barLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }

        void DisableLegacyTextOnBoostBar()
        {
            if (chargeSlider == null)
                return;

            var legacyText = chargeSlider.GetComponent<Text>();
            if (legacyText != null)
                legacyText.enabled = false;
        }

        void EnsureBarLabel()
        {
            if (chargeSlider == null)
                return;

            ResolveBarLabelReference();

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

            var barRect = chargeSlider.GetComponent<RectTransform>();
            var rect = barLabel.rectTransform;
            rect.SetParent(barRect, false);
            rect.SetAsLastSibling();
            rect.localRotation = Quaternion.identity;

            Vector3 parentScale = barRect.localScale;
            rect.localScale = new Vector3(
                SafeInverseScale(parentScale.x),
                SafeInverseScale(parentScale.y),
                1f);

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = new Vector2(barLabelPadding.x, barLabelPadding.y);
            rect.offsetMax = new Vector2(-barLabelPadding.x, -barLabelPadding.y);
        }

        static float SafeInverseScale(float axisScale)
        {
            return axisScale > 0.0001f ? 1f / axisScale : 1f;
        }

        void ConfigureBarLabel(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.Center;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.fontSize = barLabelFontSize;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            label.text = "Boost 0%";
            label.color = barLabelColor;
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

        void OnDestroy()
        {
            if (boostBarCreatedAtRuntime && chargeSlider != null)
                Destroy(chargeSlider.gameObject);
            else if (barLabelCreatedAtRuntime && barLabel != null)
                Destroy(barLabel.gameObject);
        }
    }
}
