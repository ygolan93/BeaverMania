using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DebugReference : MonoBehaviour
{
    public Behaviour Player;
    public PlayerHudState PlayerHudState;
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
    public TextMeshProUGUI ArrowMunition;
    private void Start()
    {
        BindPlayerHudState();
    }

    void BindPlayerHudState()
    {
        if (Player == null)
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();

        if (PlayerHudState == null)
            PlayerHudState = Player.GetComponent<PlayerHudState>();

        if (PlayerHudState == null)
            PlayerHudState = Player.gameObject.AddComponent<PlayerHudState>();

        if (PlayerObjective == null)
            PlayerObjective = Player.GetComponent<ObjectiveUI>();
    }

    void Update()
    {
        if (PlayerHudState == null)
            BindPlayerHudState();

        PlayerHudState.CopyFrom(Player, PlayerObjective);

        ObjectiveText.text = PlayerHudState.ObjectiveText;
        DisplayText.text = PlayerHudState.DebugText;
        StaminaText.text = PlayerHudState.StaminaText;
        LogCountText.text = PlayerHudState.LogCount;
        CurrencyCount.text = PlayerHudState.Wallet;
        HealingDisplay.text = PlayerHudState.HealingText;
        SeedCount.text = PlayerHudState.SeedText;
        GobletCount.text = PlayerHudState.GobletText;
        AppleCount.text = PlayerHudState.AppleText;
        ArrowMunition.text = PlayerHudState.ArrowText;
    }
}
