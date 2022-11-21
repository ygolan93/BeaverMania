using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerChat : MonoBehaviour
{
    public Trader Merchant;
    public Behavior Player;
    public TextMeshProUGUI PlayerUI;
    public TextMeshProUGUI TradeUI;
    // Update is called once per frame
    void Update()
    {
        TradeUI.text = Merchant.PlayerResponse;
        PlayerUI.text = Player.Plattering;
    }
}
