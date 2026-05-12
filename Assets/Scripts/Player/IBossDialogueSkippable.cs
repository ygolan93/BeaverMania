namespace Beavermania.Player
{
    /// <summary>
    /// Lets UI (e.g. dialogue) skip boss intro chat without referencing combat types.
    /// </summary>
    public interface IBossDialogueSkippable
    {
        void SkipBossChat();
    }
}
