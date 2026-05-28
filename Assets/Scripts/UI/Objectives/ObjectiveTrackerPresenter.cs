using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Objectives
{
    [DisallowMultipleComponent]
    public sealed class ObjectiveTrackerPresenter : MonoBehaviour
    {
        const string TrackerPanelName = "ObjectiveTrackerPanel";
        const string SeparatorName = "ObjectiveTrackerSeparator";

        [SerializeField] TextMeshProUGUI objectiveText;
        [SerializeField] RectTransform panelRoot;
        [SerializeField] Vector2 anchoredPosition = new(32f, 80f);
        [SerializeField] Vector2 size = new(420f, 128f);
        [SerializeField] float fontSize = 24f;

        readonly StringBuilder builder = new(256);

        public void Bind(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            objectiveText = text;
            EnsureRuntimePresentation();
        }

        public void SetObjective(string value, bool isOverride)
        {
            EnsureRuntimePresentation();

            if (objectiveText == null)
                return;

            string formatted = FormatObjective(value, isOverride);
            objectiveText.text = formatted;

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(!string.IsNullOrWhiteSpace(formatted));
        }

        void EnsureRuntimePresentation()
        {
            if (objectiveText == null)
                return;

            if (panelRoot == null)
                panelRoot = ResolveOrCreatePanel();

            StylePanel();
            StyleText();
        }

        RectTransform ResolveOrCreatePanel()
        {
            Transform existing = objectiveText.transform.parent != null
                ? objectiveText.transform.parent.Find(TrackerPanelName)
                : null;

            RectTransform panel = existing != null ? existing.GetComponent<RectTransform>() : null;
            if (panel == null)
            {
                var panelObject = new GameObject(TrackerPanelName, typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(objectiveText.transform.parent, false);
                panel = panelObject.GetComponent<RectTransform>();

                var background = panelObject.GetComponent<Image>();
                background.color = new Color(0.04f, 0.035f, 0.03f, 0.72f);
                background.raycastTarget = false;

                CreateSeparator(panel);
            }

            objectiveText.transform.SetParent(panel, false);
            return panel;
        }

        static void CreateSeparator(RectTransform panel)
        {
            var separatorObject = new GameObject(SeparatorName, typeof(RectTransform), typeof(Image));
            separatorObject.transform.SetParent(panel, false);
            var separatorRect = separatorObject.GetComponent<RectTransform>();
            separatorRect.anchorMin = new Vector2(0f, 0f);
            separatorRect.anchorMax = new Vector2(0f, 1f);
            separatorRect.pivot = new Vector2(0f, 0.5f);
            separatorRect.anchoredPosition = new Vector2(10f, 0f);
            separatorRect.sizeDelta = new Vector2(3f, 0f);

            var separator = separatorObject.GetComponent<Image>();
            separator.color = new Color(1f, 0.75f, 0.25f, 0.85f);
            separator.raycastTarget = false;
        }

        void StylePanel()
        {
            panelRoot.anchorMin = new Vector2(0f, 0.5f);
            panelRoot.anchorMax = new Vector2(0f, 0.5f);
            panelRoot.pivot = new Vector2(0f, 0.5f);
            panelRoot.anchoredPosition = anchoredPosition;
            panelRoot.sizeDelta = size;
        }

        void StyleText()
        {
            RectTransform textRect = objectiveText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 12f);
            textRect.offsetMax = new Vector2(-14f, -12f);
            textRect.pivot = new Vector2(0f, 0.5f);

            objectiveText.alignment = TextAlignmentOptions.TopLeft;
            objectiveText.fontSize = fontSize;
            objectiveText.enableWordWrapping = true;
            objectiveText.raycastTarget = false;
        }

        string FormatObjective(string value, bool isOverride)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            builder.Clear();
            builder.Append("<b>");
            builder.Append(isOverride ? "Interaction" : "Objectives");
            builder.AppendLine("</b>");

            string[] entries = value.Split(new[] { '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i].Trim();
                if (entry.Length == 0)
                    continue;

                if (entry.StartsWith("- "))
                    builder.AppendLine(entry);
                else
                    builder.Append("- ").AppendLine(entry);
            }

            return builder.ToString().TrimEnd();
        }
    }
}
