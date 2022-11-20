using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TraderUI : MonoBehaviour
{
    public Trader Merchant;
    public TextMeshProUGUI TradeDisplay;

    // Update is called once per frame
    void Update()
    {
        TradeDisplay.text = Merchant.Words;
    }
}
