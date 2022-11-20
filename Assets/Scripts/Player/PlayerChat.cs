using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerChat : MonoBehaviour
{
    public Trader Merchant;
    public TextMeshProUGUI PlayerUI;

    // Update is called once per frame
    void Update()
    {
        PlayerUI.text = Merchant.PlayerResponse;
    }
}
