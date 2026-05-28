using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Tips
{
    public static class TipsPauseMenuToggleInstaller
    {
        const string ToggleName = "Tips Toggle";

        public static void Ensure(GameObject pauseMenu)
        {
            if (pauseMenu == null)
                return;

            Toggle toggle = FindExistingToggle(pauseMenu.transform);
            Text label = null;

            if (toggle == null)
                toggle = CreateToggle(pauseMenu.transform, out label);
            else
                label = toggle.GetComponentInChildren<Text>(true);

            if (toggle == null)
                return;

            WireToggle(toggle, label);
        }

        static Toggle FindExistingToggle(Transform root)
        {
            Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < toggles.Length; i++)
            {
                if (toggles[i] != null && toggles[i].gameObject.name == ToggleName)
                    return toggles[i];
            }

            return null;
        }

        static Toggle CreateToggle(Transform pauseMenuRoot, out Text label)
        {
            label = null;
            Transform parent = ResolveControlsParent(pauseMenuRoot);
            var root = new GameObject(ToggleName, typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 32f);
            rect.anchoredPosition = ResolveTogglePosition(parent);

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(root.transform, false);
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0f, 0.5f);
            backgroundRect.pivot = new Vector2(0f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(24f, 24f);
            backgroundRect.anchoredPosition = Vector2.zero;
            var background = backgroundObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.8f);

            var checkmarkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkObject.transform.SetParent(backgroundObject.transform, false);
            var checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(14f, 14f);
            checkmarkRect.anchoredPosition = Vector2.zero;
            var checkmark = checkmarkObject.GetComponent<Image>();
            checkmark.color = new Color(0.2f, 0.8f, 0.35f, 1f);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(34f, 0f);
            labelRect.offsetMax = Vector2.zero;
            label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;

            var toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            return toggle;
        }

        static Transform ResolveControlsParent(Transform pauseMenuRoot)
        {
            Slider[] sliders = pauseMenuRoot.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                if (sliders[i] != null && sliders[i].transform.parent != null)
                    return sliders[i].transform.parent;
            }

            return pauseMenuRoot;
        }

        static Vector2 ResolveTogglePosition(Transform parent)
        {
            Slider[] sliders = parent.GetComponentsInChildren<Slider>(true);
            if (sliders.Length == 0)
                return new Vector2(0f, -150f);

            RectTransform lowest = null;
            for (int i = 0; i < sliders.Length; i++)
            {
                var rect = sliders[i].GetComponent<RectTransform>();
                if (rect == null)
                    continue;

                if (lowest == null || rect.anchoredPosition.y < lowest.anchoredPosition.y)
                    lowest = rect;
            }

            return lowest != null ? lowest.anchoredPosition + new Vector2(0f, -45f) : new Vector2(0f, -150f);
        }

        static void WireToggle(Toggle toggle, Text label)
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.SetIsOnWithoutNotify(TipsSettings.Enabled);
            UpdateLabel(label, TipsSettings.Enabled);
            toggle.onValueChanged.AddListener(enabled =>
            {
                TipsSettings.Enabled = enabled;
                UpdateLabel(label, enabled);
            });
        }

        static void UpdateLabel(Text label, bool enabled)
        {
            if (label != null)
                label.text = enabled ? "Tips: On" : "Tips: Off";
        }
    }
}
