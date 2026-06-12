using UnityEngine;

namespace Beavermania.UI
{
    public enum NpcDialogueSessionCloseReason
    {
        None = 0,
        DialogueCompleted = 1,
        PlayerLeftRange = 2,
        SourceDisabled = 3,
        ExternalCancel = 4,
    }

    public interface INpcDialogueInteractionSource
    {
        Transform InteractionTransform { get; }

        Transform InteractionLookTarget { get; }

        string InteractionPromptText { get; }

        float InteractionDistance { get; }

        bool IsInteractionAvailable { get; }

        NpcDialogueSessionContext CreateDialogueSessionContext();

        void OnDialogueSessionOpened(NpcDialogueSessionContext context);

        void OnDialogueShopOpened();

        void OnDialogueShopClosed();

        void OnDialogueSessionClosed(NpcDialogueSessionCloseReason reason);
    }
}
