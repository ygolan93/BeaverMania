using System;
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

    CheckpointService checkpointService;

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (Player == null)
        {
            ResolvePlayerReferences();
        }

        if (Player == null)
        {
            return;
        }

        var state = Player.State;
        SetText(ObjectiveText, BuildObjectiveText());
        SetText(DisplayText, BuildHealthText(state));
        SetText(StaminaText, BuildStaminaText(state));
        SetText(LogCountText, BuildLogCountText(state));
        SetText(CurrencyCount, BuildCurrencyText(state));
        SetText(HealingDisplay, BuildStatusText(state));
        SetText(SeedCount, BuildSeedText(state));
        SetText(GobletCount, BuildGobletText(state));
        SetText(AppleCount, BuildAppleText(state));
        SetText(ArrowMunition, BuildArrowText(state));
    }

    public void Bind(Behaviour player, ObjectiveUI objective, CheckpointService checkpoints)
    {
        Player = player;
        PlayerObjective = objective;
        checkpointService = checkpoints;
    }

    public void RenderNow()
    {
        Update();
    }

    void ResolveReferences()
    {
        ResolvePlayerReferences();
        checkpointService = CheckpointService.GetOrCreate();

        RuntimeReferenceValidator.Require(ObjectiveText, this, nameof(ObjectiveText));
        RuntimeReferenceValidator.Require(DisplayText, this, nameof(DisplayText));
        RuntimeReferenceValidator.Require(StaminaText, this, nameof(StaminaText));
        RuntimeReferenceValidator.Require(LogCountText, this, nameof(LogCountText));
        RuntimeReferenceValidator.Require(HealingDisplay, this, nameof(HealingDisplay));
        RuntimeReferenceValidator.Require(CurrencyCount, this, nameof(CurrencyCount));
        RuntimeReferenceValidator.Require(SeedCount, this, nameof(SeedCount));
        RuntimeReferenceValidator.Require(GobletCount, this, nameof(GobletCount));
        RuntimeReferenceValidator.Require(AppleCount, this, nameof(AppleCount));
        RuntimeReferenceValidator.Require(ArrowMunition, this, nameof(ArrowMunition));
    }

    void ResolvePlayerReferences()
    {
        if (Player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (!RuntimeReferenceValidator.Require(playerObject, this, "Player tag"))
            {
                return;
            }

            Player = playerObject.GetComponent<Behaviour>();
        }

        if (PlayerObjective == null && Player != null)
        {
            PlayerObjective = Player.GetComponent<ObjectiveUI>();
        }

        RuntimeReferenceValidator.Require(Player, this, nameof(Player));
        RuntimeReferenceValidator.Require(PlayerObjective, this, nameof(PlayerObjective));
    }

    string BuildObjectiveText()
    {
        return PlayerObjective != null ? PlayerObjective.Instruction : string.Empty;
    }

    string BuildHealthText(PlayerState state)
    {
        return FormatPercent(state.currentHealth, state.maxHealth);
    }

    string BuildStaminaText(PlayerState state)
    {
        return FormatPercent(state.currentStamina, state.maxStamina);
    }

    string BuildLogCountText(PlayerState state)
    {
        if (state.carriedLogs >= state.maxCarriedLogs)
        {
            return "Log count: 9/9. Press LCtrl+Rmouse for bridge construction";
        }

        return "Log count: " + state.carriedLogs + "/" + state.maxCarriedLogs;
    }

    string BuildCurrencyText(PlayerState state)
    {
        return "COINS: " + state.currency;
    }

    string BuildStatusText(PlayerState state)
    {
        if (Player.GobletPicked)
        {
            return "Boost time: " + Math.Round(Player.GobletClock);
        }

        if (checkpointService != null &&
            checkpointService.LastCheckpointPosition == state.checkpointPosition &&
            state.checkpointMessageUntil > Time.time)
        {
            return "Checkpoint saved";
        }

        return string.Empty;
    }

    string BuildSeedText(PlayerState state)
    {
        return "NUTS (R): " + state.nutCount;
    }

    string BuildGobletText(PlayerState state)
    {
        return "GOBLETS (Y): " + state.gobletPickup;
    }

    string BuildAppleText(PlayerState state)
    {
        return "APPLES (T): " + state.apple;
    }

    string BuildArrowText(PlayerState state)
    {
        return "ARROWS (RM): " + state.arrowMunition;
    }

    static string FormatPercent(float current, float max)
    {
        if (max <= 0f)
        {
            return "0%";
        }

        return Math.Round((current / max) * 100f, 1) + "%";
    }

    static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
