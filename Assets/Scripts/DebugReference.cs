using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DebugReference : MonoBehaviour
{
    public Behavior Player;


    public TextMeshProUGUI PlatetringText;
    public TextMeshProUGUI DisplayText;
    public TextMeshProUGUI LogCountText;
    public TextMeshProUGUI HealingDisplay;
    public TextMeshProUGUI CurrencyCount;
    public TextMeshProUGUI SeedCount;

    //// Text LegacyText;
    //private void Start()
    //{
    //}
    // Update is called once per frame
    void Update()
    {
        PlatetringText.text = Player.Plattering;
        DisplayText.text = Player.DebugText;
        LogCountText.text = Player.LogCount;
        CurrencyCount.text = Player.Wallet;
        HealingDisplay.text = Player.HealingText;
        SeedCount.text = Player.SeedText;
    }
}
