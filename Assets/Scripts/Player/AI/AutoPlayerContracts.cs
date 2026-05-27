using UnityEngine;

namespace Beavermania.Player.AI
{
    public interface IAutoInteractable
    {
        bool CanAutoInteract(GameObject interactor);
        void AutoInteract(GameObject interactor);
        bool IsAutoInteractionBusy();
    }

    public interface IAutoChoppable
    {
        bool CanAutoChop(GameObject interactor);
        Transform ChopTargetPoint { get; }
    }

    public interface IAutoBuildTarget
    {
        bool NeedsLogs { get; }
        int LogsRequired { get; }
        int LogsApplied { get; }
        bool IsComplete { get; }
        Transform BuildPoint { get; }
    }
}
