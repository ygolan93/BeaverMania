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

    private void Awake()
    {
        ApplyReferences();
    }

    private void Start()
    {
        ApplyReferences();
        enabled = presenter == null;
    }

    private void Update()
    {
        if (presenter == null)
        {
            ApplyReferences();
        }

        enabled = false;
    }

    private void OnValidate()
    {
        ApplyReferences();
    }

    void ApplyReferences()
    {
        presenter = GetComponent<HudPresenter>();
        if (presenter == null)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            presenter = gameObject.AddComponent<HudPresenter>();
        }

        presenter.Bind(Player, PlayerObjective, Application.isPlaying ? CheckpointService.GetOrCreate() : null);
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
}
