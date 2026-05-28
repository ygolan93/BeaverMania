using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Tips
{
    [DisallowMultipleComponent]
    public sealed class TipCardPresenter : MonoBehaviour
    {
        [SerializeField] RectTransform cardRoot;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float fadeSeconds = 0.2f;
        [SerializeField] Vector2 hiddenOffset = new(24f, 0f);

        RectTransform rectTransform;
        Vector2 visiblePosition;
        Vector2 hiddenPosition;
        float hideAtTime;
        float alphaTarget;

        public bool IsShowing => canvasGroup != null && canvasGroup.alpha > 0.01f && Time.unscaledTime < hideAtTime;
        public int CurrentPriority { get; private set; }

        void Awake()
        {
            EnsureRuntimeView();
        }

        void OnEnable()
        {
            TipsSettings.Changed += OnTipsSettingChanged;
        }

        void OnDisable()
        {
            TipsSettings.Changed -= OnTipsSettingChanged;
        }

        void Update()
        {
            if (canvasGroup == null || rectTransform == null)
                return;

            alphaTarget = Time.unscaledTime < hideAtTime && TipsSettings.Enabled ? 1f : 0f;
            float fadeSpeed = fadeSeconds <= 0f ? 1f : Time.unscaledDeltaTime / fadeSeconds;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, alphaTarget, fadeSpeed);
            rectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, canvasGroup.alpha);

            if (canvasGroup.alpha <= 0.01f && alphaTarget <= 0f)
                CurrentPriority = int.MinValue;
        }

        public void Show(string message, int priority, float displaySeconds)
        {
            EnsureRuntimeView();
            if (messageText == null || canvasGroup == null)
                return;

            CurrentPriority = priority;
            messageText.text = message;
            hideAtTime = Time.unscaledTime + Mathf.Max(1f, displaySeconds);
            alphaTarget = 1f;
        }

        void EnsureRuntimeView()
        {
            if (cardRoot == null)
                CreateRuntimeCard();

            rectTransform = cardRoot;
            if (canvasGroup == null && cardRoot != null)
                canvasGroup = cardRoot.GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (rectTransform != null)
            {
                visiblePosition = rectTransform.anchoredPosition;
                hiddenPosition = visiblePosition + hiddenOffset;
                rectTransform.anchoredPosition = hiddenPosition;
            }

            CurrentPriority = int.MinValue;
        }

        void CreateRuntimeCard()
        {
            var card = new GameObject("TipsCard", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            card.transform.SetParent(transform, false);
            cardRoot = card.GetComponent<RectTransform>();
            cardRoot.anchorMin = new Vector2(1f, 1f);
            cardRoot.anchorMax = new Vector2(1f, 1f);
            cardRoot.pivot = new Vector2(1f, 1f);
            cardRoot.anchoredPosition = new Vector2(-28f, -112f);
            cardRoot.sizeDelta = new Vector2(360f, 86f);

            canvasGroup = card.GetComponent<CanvasGroup>();
            var background = card.GetComponent<Image>();
            background.color = new Color(0.05f, 0.035f, 0.025f, 0.78f);
            background.raycastTarget = false;

            var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(card.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);

            messageText = textObject.GetComponent<TextMeshProUGUI>();
            messageText.text = string.Empty;
            messageText.fontSize = 22f;
            messageText.enableWordWrapping = true;
            messageText.alignment = TextAlignmentOptions.MidlineLeft;
            messageText.raycastTarget = false;
        }

        void OnTipsSettingChanged(bool enabled)
        {
            if (!enabled)
                hideAtTime = 0f;
        }
    }
}
