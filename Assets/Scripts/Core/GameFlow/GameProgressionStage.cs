namespace Beavermania.Core.GameFlow
{
    // Legacy semantic phases kept only as an adapter for older gameplay callers.
    public enum GameProgressionStage
    {
        TalkToTrader = 0,
        CollectLogs = 1,
        DeliverLogsToBridge = 2,
        BuildBridge = 3,
        ContinueJourney = 4
    }
}
