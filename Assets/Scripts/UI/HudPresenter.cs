using TMPro;
using UnityEngine;

public sealed class HudPresenter : MonoBehaviour
{
    public Behaviour Player;
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
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (!RuntimeReferenceValidator.Require(playerObject, this, "Player tag"))
        {
            return;
        }

        Player = playerObject.GetComponent<Behaviour>();
        PlayerObjective = playerObject.GetComponent<ObjectiveUI>();
        if (!RuntimeReferenceValidator.Require(Player, this, nameof(Player)) ||
            !RuntimeReferenceValidator.Require(PlayerObjective, this, nameof(PlayerObjective)) ||
            !RuntimeReferenceValidator.Require(ObjectiveText, this, nameof(ObjectiveText)) ||
            !RuntimeReferenceValidator.Require(DisplayText, this, nameof(DisplayText)) ||
            !RuntimeReferenceValidator.Require(StaminaText, this, nameof(StaminaText)) ||
            !RuntimeReferenceValidator.Require(LogCountText, this, nameof(LogCountText)) ||
            !RuntimeReferenceValidator.Require(HealingDisplay, this, nameof(HealingDisplay)) ||
            !RuntimeReferenceValidator.Require(CurrencyCount, this, nameof(CurrencyCount)) ||
            !RuntimeReferenceValidator.Require(SeedCount, this, nameof(SeedCount)) ||
            !RuntimeReferenceValidator.Require(GobletCount, this, nameof(GobletCount)) ||
            !RuntimeReferenceValidator.Require(AppleCount, this, nameof(AppleCount)) ||
            !RuntimeReferenceValidator.Require(ArrowMunition, this, nameof(ArrowMunition)))
        {
            return;
        }
    }

    private void Update()
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
        ArrowMunition.text = Player.ArrowText;
    }
}
