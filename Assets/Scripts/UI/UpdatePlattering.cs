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

        // Update is called once per frame
        void Update()
        {
            if (PlatetringText == null || Player == null)
                return;

            PlatetringText.text = Player.Plattering ?? string.Empty;
        }
    }
}
