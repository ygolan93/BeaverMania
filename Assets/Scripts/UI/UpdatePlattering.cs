using TMPro;
using UnityEngine;

public class UpdatePlattering : MonoBehaviour
{
    public Behaviour Player;
    public TextMeshProUGUI PlatetringText;

    void Update()
    {
        if (Player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Player = playerObject.GetComponent<Behaviour>();
            }
        }

        if (Player == null || PlatetringText == null)
        {
            return;
        }

        PlatetringText.text = Player.Plattering ?? string.Empty;
    }
}
