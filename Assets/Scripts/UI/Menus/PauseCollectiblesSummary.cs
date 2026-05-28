using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Menus
{
    [DisallowMultipleComponent]
    public sealed class PauseCollectiblesSummary : MonoBehaviour
    {
        const string PanelName = "Collectibles Summary";

        Text titleText;
        Text bodyText;

        public static PauseCollectiblesSummary Ensure(GameObject pauseMenu)
        {
            if (pauseMenu == null)
                return null;

            PauseCollectiblesSummary existing = pauseMenu.GetComponentInChildren<PauseCollectiblesSummary>(true);
            if (existing != null)
                return existing;

            return Create(pauseMenu.transform);
        }

        public void Refresh(BeaverPlayer player)
        {
            EnsureView();

            if (titleText != null)
                titleText.text = "Collectibles";

            if (bodyText == null)
                return;

            if (player == null)
            {
                bodyText.text = "Player data unavailable";
                return;
            }

            int logCount = player.Load != null ? player.Load.i : 0;
            bodyText.text =
                $"Coins: {player.Currency}\n" +
                $"Nuts: {player.NutCount}\n" +
                $"Apples: {player.Apple}\n" +
                $"Goblets: {player.GobletPickup}\n" +
                $"Arrows: {player.arrowMunition}\n" +
                $"Logs: {logCount}/9";
        }

        static PauseCollectiblesSummary Create(Transform parent)
        {
            var panelObject = new GameObject(PanelName, typeof(RectTransform), typeof(Image), typeof(PauseCollectiblesSummary));
            panelObject.transform.SetParent(parent, false);

            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-48f, 0f);
            rect.sizeDelta = new Vector2(260f, 220f);

            var background = panelObject.GetComponent<Image>();
            background.color = new Color(0.05f, 0.035f, 0.025f, 0.68f);
            background.raycastTarget = false;

            var summary = panelObject.GetComponent<PauseCollectiblesSummary>();
            summary.EnsureView();
            return summary;
        }

        void EnsureView()
        {
            if (titleText != null && bodyText != null)
                return;

            titleText = ResolveOrCreateText("Title", new Vector2(14f, 176f), new Vector2(-14f, -12f), 24, FontStyle.Bold);
            bodyText = ResolveOrCreateText("Body", new Vector2(14f, 14f), new Vector2(-14f, -52f), 20, FontStyle.Normal);
        }

        Text ResolveOrCreateText(string childName, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle fontStyle)
        {
            Transform child = transform.Find(childName);
            Text text = child != null ? child.GetComponent<Text>() : null;
            if (text == null)
            {
                var textObject = new GameObject(childName, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(transform, false);
                text = textObject.GetComponent<Text>();
            }

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
