using TMPro;
using UnityEngine;

public sealed class DebugReference : MonoBehaviour
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

    HudPresenter presenter;
    bool presenterOwnsUpdate = true;

    private void Start()
    {
        presenter = GetComponent<HudPresenter>();
        if (presenter == null)
        {
            presenter = gameObject.AddComponent<HudPresenter>();
        }

        presenter.Bind(Player, PlayerObjective, CheckpointService.GetOrCreate());
        presenterOwnsUpdate = presenter.isActiveAndEnabled;
        presenter.ObjectiveText = ObjectiveText;
        presenter.DisplayText = DisplayText;
        presenter.StaminaText = StaminaText;
        presenter.LogCountText = LogCountText;
        presenter.HealingDisplay = HealingDisplay;
        presenter.CurrencyCount = CurrencyCount;
        presenter.SeedCount = SeedCount;
        presenter.GobletCount = GobletCount;
        presenter.AppleCount = AppleCount;
        presenter.ArrowMunition = ArrowMunition;
    }

    private void Update()
    {
        if (presenter == null)
        {
            return;
        }

        presenterOwnsUpdate = presenter.isActiveAndEnabled;
        if (!presenterOwnsUpdate)
        {
            presenter.RenderNow();
        }
    }
}
