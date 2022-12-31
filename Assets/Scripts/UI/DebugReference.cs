using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DebugReference : MonoBehaviour
{
    public Behavior Player;
    public ObjectiveUI PlayerObjective;

    public TextMeshProUGUI ObjectiveText;
    public TextMeshProUGUI DisplayText;
    public TextMeshProUGUI StaminaText;
    public TextMeshProUGUI LogCountText;
    public TextMeshProUGUI HealingDisplay;
    public TextMeshProUGUI CurrencyCount;
    public TextMeshProUGUI SeedCount;
    public TextMeshProUGUI GobletCount;
    public TextMeshProUGUI AppleCount;
    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        PlayerObjective = GameObject.FindGameObjectWithTag("Player").GetComponent<ObjectiveUI>();
    }

    void Update()
    {
        ObjectiveText.text = PlayerObjective.Instruction;
        DisplayText.text = Player.DebugText;
        StaminaText.text = Player.StaminaText;
        LogCountText.text = Player.LogCount;
        CurrencyCount.text = Player.Wallet;
        HealingDisplay.text = Player.HealingText;
        SeedCount.text = Player.SeedText;
        GobletCount.text = Player.GobletText;
        AppleCount.text = Player.AppleText;
    }
}
