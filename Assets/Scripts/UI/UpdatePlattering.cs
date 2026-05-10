using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UpdatePlattering : MonoBehaviour
{
    public Behaviour Player;
    public TextMeshProUGUI PlatetringText;

    // Update is called once per frame
    void Update()
    {
        if (Player == null || PlatetringText == null)
        {
            return;
        }

        PlatetringText.text = Player.Plattering;
    }
}
