using UnityEngine;
using TMPro;
public class DebugReference : MonoBehaviour
{
    [SerializeField] public Behaviour Player;
    [SerializeField] public PlayerHudState PlayerHudState;

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

    void Start()
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
        {
            PlayerHudState = Player.gameObject.AddComponent<PlayerHudState>();
            PlayerHudState.CopyFrom(Player, Player.GetComponent<ObjectiveUI>());
        }
    }

    void Update()
    {
        if (PlayerHudState == null)
            BindPlayerHudState();

        if (PlayerHudState == null)
            return;

        SetText(ObjectiveText, PlayerHudState.ObjectiveText);
        SetText(DisplayText, PlayerHudState.DebugText);
        SetText(StaminaText, PlayerHudState.StaminaText);
        SetText(LogCountText, PlayerHudState.LogCount);
        SetText(CurrencyCount, PlayerHudState.Wallet);
        SetText(HealingDisplay, PlayerHudState.HealingText);
        SetText(SeedCount, PlayerHudState.SeedText);
        SetText(GobletCount, PlayerHudState.GobletText);
        SetText(AppleCount, PlayerHudState.AppleText);
        SetText(ArrowMunition, PlayerHudState.ArrowText);
    }

    static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
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
