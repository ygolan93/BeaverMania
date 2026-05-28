namespace Beavermania.Data.Tips
{
    public readonly struct TipRequest
    {
        public TipRequest(
            string uniqueId,
            string displayText,
            int priority = 0,
            TipGameStage category = TipGameStage.EarlyGame,
            float cooldownSeconds = 20f,
            int maxDisplayCount = 1,
            bool showOnlyOnce = true,
            int minimumProgress = 0,
            bool blockDuringCombat = true,
            bool idleOnly = false,
            bool activeOnly = false,
            int triggerInterval = 1,
            float displaySeconds = 4f)
        {
            UniqueId = uniqueId;
            DisplayText = displayText;
            Priority = priority;
            Category = category;
            CooldownSeconds = cooldownSeconds;
            MaxDisplayCount = maxDisplayCount;
            ShowOnlyOnce = showOnlyOnce;
            MinimumProgress = minimumProgress;
            BlockDuringCombat = blockDuringCombat;
            IdleOnly = idleOnly;
            ActiveOnly = activeOnly;
            TriggerInterval = triggerInterval;
            DisplaySeconds = displaySeconds;
        }

        public string UniqueId { get; }
        public string DisplayText { get; }
        public int Priority { get; }
        public TipGameStage Category { get; }
        public float CooldownSeconds { get; }
        public int MaxDisplayCount { get; }
        public bool ShowOnlyOnce { get; }
        public int MinimumProgress { get; }
        public bool BlockDuringCombat { get; }
        public bool IdleOnly { get; }
        public bool ActiveOnly { get; }
        public int TriggerInterval { get; }
        public float DisplaySeconds { get; }
    }
}
