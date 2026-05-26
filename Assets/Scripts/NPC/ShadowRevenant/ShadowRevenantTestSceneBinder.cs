using System.Collections.Generic;
using Beavermania.Player;
using Beavermania.UI;
using Beavermania.UI.Hud;
using Beavermania.UI.Menus;
using Beavermania.UI.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.NPC
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ShadowRevenantTestSceneBinder : MonoBehaviour
    {
        void Awake()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            GameObject canvasObject = GameObject.Find("PlayerCanvas");
            if (playerObject == null || canvasObject == null)
            {
                Debug.LogWarning($"{nameof(ShadowRevenantTestSceneBinder)} could not find Player and PlayerCanvas scene roots.", this);
                return;
            }

            Wire(playerObject, canvasObject);
        }

        static void Wire(GameObject playerObject, GameObject canvasObject)
        {
            var player = playerObject.GetComponent<BeaverPlayerBehaviour>();
            if (player == null)
                return;

            var canvasRoot = canvasObject.transform;
            var hudState = playerObject.GetComponent<PlayerHudState>();
            var objective = playerObject.GetComponent<ObjectiveUI>();
            var healthBar = playerObject.GetComponent<Health_Bar_Script>();

            if (playerObject.GetComponent<ShadowRevenantPlayerTargetAdapter>() == null)
                playerObject.AddComponent<ShadowRevenantPlayerTargetAdapter>();

            if (healthBar != null)
            {
                healthBar.HealthSlider = FindComponentByName<Slider>(canvasRoot, "Health Bar");
                healthBar.StaminaSlider = FindComponentByName<Slider>(canvasRoot, "StaminaBar");
            }

            player.HealthBar = healthBar;
            player.ICON_1 = FindChildGameObject(canvasRoot, "Icon1");
            player.ICON_2 = FindChildGameObject(canvasRoot, "Icon2");
            player.ICON_3 = FindChildGameObject(canvasRoot, "Icon3");
            player.LooseScreen = FindChildGameObject(canvasRoot, "Loose Menu Panel");
            player.AimIcon = FindChildGameObject(canvasRoot, "Aim_Icon");
            player.PlayerHudState = hudState;
            player.PlayerObjective = objective;

            var debug = canvasObject.GetComponent<DebugReference>();
            if (debug != null)
            {
                debug.Player = player;
                debug.PlayerHudState = hudState;
                debug.ObjectiveText = FindComponentByName<TextMeshProUGUI>(canvasRoot, "ObjectiveText");
                debug.DisplayText = FindComponentByName<TextMeshProUGUI>(canvasRoot, "HealthNum");
                debug.StaminaText = FindComponentByName<TextMeshProUGUI>(canvasRoot, "StamiNum");
                debug.LogCountText = FindComponentByName<TextMeshProUGUI>(canvasRoot, "LogCount");
                debug.HealingDisplay = FindComponentByName<TextMeshProUGUI>(canvasRoot, "HealingDisplay");
                debug.CurrencyCount = FindComponentByName<TextMeshProUGUI>(canvasRoot, "CurrencyCount");
                debug.SeedCount = FindComponentByName<TextMeshProUGUI>(canvasRoot, "SeedCount");
                debug.GobletCount = FindComponentByName<TextMeshProUGUI>(canvasRoot, "GobletCount");
                debug.AppleCount = FindComponentByName<TextMeshProUGUI>(canvasRoot, "Apple Count");
                debug.ArrowMunition = FindComponentByName<TextMeshProUGUI>(canvasRoot, "ArrowMunition");
            }

            var uiMenu = canvasObject.GetComponent<UIMenu>();
            if (uiMenu != null)
            {
                uiMenu.Player = player;
                uiMenu.PauseMenu = FindChildGameObject(canvasRoot, "Pause Menu Panel");
                uiMenu.Question = FindChildGameObject(canvasRoot, "Quiry");
            }

            foreach (var dialogue in canvasObject.GetComponentsInChildren<Dialogue>(true))
            {
                dialogue.Player = player;
                dialogue.PlayerObjective = objective;
                if (dialogue.panel == null && dialogue.transform.parent != null)
                    dialogue.panel = dialogue.transform.parent;
            }

            foreach (var shop in canvasObject.GetComponentsInChildren<Shop>(true))
                shop.Player = player;
        }

        static GameObject FindChildGameObject(Transform root, string name)
        {
            Transform child = FindChild(root, name);
            return child != null ? child.gameObject : null;
        }

        static T FindComponentByName<T>(Transform root, string name) where T : Component
        {
            Transform child = FindChild(root, name);
            return child != null ? child.GetComponent<T>() ?? child.GetComponentInChildren<T>(true) : null;
        }

        static Transform FindChild(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                if (current.name == name)
                    return current;

                for (int i = current.childCount - 1; i >= 0; i--)
                    stack.Push(current.GetChild(i));
            }

            return null;
        }
    }
}
