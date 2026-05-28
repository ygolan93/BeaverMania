using UnityEngine;

namespace Beavermania.Data.Tips
{
    public enum TipGameStage
    {
        EarlyGame,
        MidGame,
        EndGame
    }

    [CreateAssetMenu(fileName = "TipDefinition", menuName = "Beavermania/UI/Tip Definition")]
    public sealed class TipDefinition : ScriptableObject
    {
        [SerializeField] string uniqueId;
        [SerializeField] [TextArea(2, 4)] string displayText;
        [SerializeField] int priority;
        [SerializeField] TipGameStage category;
        [SerializeField] float cooldownSeconds = 20f;
        [SerializeField] int maxDisplayCount = 1;
        [SerializeField] bool showOnlyOnce = true;
        [SerializeField] int minimumProgress;
        [SerializeField] bool blockDuringCombat = true;
        [SerializeField] bool idleOnly;
        [SerializeField] bool activeOnly;
        [SerializeField] int triggerInterval = 1;
        [SerializeField] float displaySeconds = 4f;

        public string UniqueId => string.IsNullOrWhiteSpace(uniqueId) ? name : uniqueId;
        public string DisplayText => displayText;
        public int Priority => priority;
        public TipGameStage Category => category;
        public float CooldownSeconds => cooldownSeconds;
        public int MaxDisplayCount => maxDisplayCount;
        public bool ShowOnlyOnce => showOnlyOnce;
        public int MinimumProgress => minimumProgress;
        public bool BlockDuringCombat => blockDuringCombat;
        public bool IdleOnly => idleOnly;
        public bool ActiveOnly => activeOnly;
        public int TriggerInterval => triggerInterval;
        public float DisplaySeconds => displaySeconds;

        void OnValidate()
        {
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            maxDisplayCount = Mathf.Max(1, maxDisplayCount);
            triggerInterval = Mathf.Max(1, triggerInterval);
            displaySeconds = Mathf.Max(1f, displaySeconds);
            minimumProgress = Mathf.Max(0, minimumProgress);
        }
    }
}
