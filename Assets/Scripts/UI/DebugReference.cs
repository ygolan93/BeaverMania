using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DebugReference : MonoBehaviour
{
    [SerializeField] public Behaviour Player;
    [SerializeField] public PlayerHudState PlayerHudState;
    [SerializeField] public ObjectiveUI PlayerObjective;

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

    bool loggedMissingPlayer;
    bool loggedMissingPlayerObjective;

    private void Start()
    {
        BindPlayerHudState();
    }

    void BindPlayerHudState()
    {
        if (Player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                Player = playerObject.GetComponent<Behaviour>();

            if (Player == null)
            {
                LogMissingReference(nameof(Player), ref loggedMissingPlayer);
                return;
            }
        }

        if (PlayerHudState == null)
            PlayerHudState = Player.GetComponent<PlayerHudState>();

        if (PlayerHudState == null)
            PlayerHudState = Player.gameObject.AddComponent<PlayerHudState>();

        if (PlayerObjective == null)
            PlayerObjective = Player.GetComponent<ObjectiveUI>();

        if (PlayerObjective == null)
            LogMissingReference(nameof(PlayerObjective), ref loggedMissingPlayerObjective);
    }

    void Update()
    {
        if (PlayerHudState == null)
            BindPlayerHudState();

        if (PlayerHudState == null)
            return;

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

    void LogMissingReference(string referenceName, ref bool logged)
    {
        if (logged)
            return;

        logged = true;
#if DEVELOPMENT_BUILD
        Debug.LogWarning($"{nameof(DebugReference)} could not resolve {referenceName} fallback.", this);
#endif
    }
}
