using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DebugReference : MonoBehaviour
{
    public Behavior Player;


    public TextMeshProUGUI DisplayText;
    public TextMeshProUGUI LogCountText;
    public TextMeshProUGUI HealingDisplay;
    public TextMeshProUGUI CurrencyCount;
    public TextMeshProUGUI SeedCount;
    public TextMeshProUGUI GobletCount;
    public TextMeshProUGUI AppleCount;
    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
    }

    void Update()
    {
        DisplayText.text = Player.DebugText;
        LogCountText.text = Player.LogCount;
        CurrencyCount.text = Player.Wallet;
        HealingDisplay.text = Player.HealingText;
        SeedCount.text = Player.SeedText;
        GobletCount.text = Player.GobletText;
        AppleCount.text = Player.AppleText;
    }
}
