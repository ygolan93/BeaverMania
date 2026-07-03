using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.UI.Hud
{
    public class UpdatePlattering : MonoBehaviour
    {
        public BeaverPlayer Player;
        public TextMeshProUGUI PlatetringText;

        string lastPlattering;

        void Update()
        {
            if (PlatetringText == null || Player == null)
                return;

            string current = Player.Plattering ?? string.Empty;
            if (current != lastPlattering)
            {
                lastPlattering = current;
                PlatetringText.text = current;
            }
        }
    }
}
