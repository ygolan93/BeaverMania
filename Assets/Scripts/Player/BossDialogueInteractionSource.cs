using Beavermania.UI;
using UnityEngine;

namespace Beavermania.Player.Combat
{
    public sealed class BossDialogueInteractionSource : MonoBehaviour, INpcDialogueInteractionSource
    {
        const string DefaultPromptMessage = "Press E to interact";

        [SerializeField] BossHandler bossHandler;
        [SerializeField] GameObject legacyBossPanel;
        [SerializeField] string promptMessage = DefaultPromptMessage;

        bool isInteractionAvailable;
        bool isSessionOpen;

        public Transform InteractionTransform => transform;

        public Transform InteractionLookTarget => bossHandler != null && bossHandler.Boss != null
            ? bossHandler.Boss.transform
            : transform;

        public string InteractionPromptText => string.IsNullOrEmpty(promptMessage)
            ? DefaultPromptMessage
            : promptMessage;

        public float InteractionDistance => isInteractionAvailable ? 0f : float.MaxValue;

        public bool IsInteractionAvailable => isInteractionAvailable
            && !isSessionOpen
            && bossHandler != null
            && bossHandler.enabled
            && bossHandler.Boss != null
            && !bossHandler.Boss.isAttacking;

        public void Configure(BossHandler handler)
        {
            bossHandler = handler;
            if (legacyBossPanel == null && bossHandler != null)
                legacyBossPanel = bossHandler.BossPanel;
        }

        public void SetInteractionAvailable(bool available)
        {
            bool nextAvailable = available
                && bossHandler != null
                && bossHandler.enabled
                && bossHandler.Boss != null
                && !bossHandler.Boss.isAttacking;

            if (isInteractionAvailable == nextAvailable)
                return;

            isInteractionAvailable = nextAvailable;
            var presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter == null)
                return;

            if (isInteractionAvailable)
                presenter.RegisterSource(this);
            else
                presenter.UnregisterSource(this, NpcDialogueSessionCloseReason.PlayerLeftRange);
        }

        public NpcDialogueSessionContext CreateDialogueSessionContext()
        {
            var dialogue = ResolveLegacyDialogue();
            if (dialogue == null)
                return null;

            return new NpcDialogueSessionContext(
                dialogue.ConfiguredDialogueData,
                dialogue.ConfiguredLines,
                dialogue.ConfiguredTextSpeed,
                dialogue.ConfiguredIsBoss,
                dialogue.ConfiguredAdvanceObjectiveOnEnd,
                shopContentRoot: null);
        }

        public void OnDialogueSessionOpened(NpcDialogueSessionContext context)
        {
            isSessionOpen = true;
            if (bossHandler != null)
                bossHandler.BeginBossDialoguePresentation();
        }

        public void OnDialogueShopOpened()
        {
        }

        public void OnDialogueShopClosed()
        {
        }

        public void OnDialogueSessionClosed(NpcDialogueSessionCloseReason reason)
        {
            bool shouldStartBossFight = reason == NpcDialogueSessionCloseReason.DialogueCompleted
                || reason == NpcDialogueSessionCloseReason.ExternalCancel;

            isSessionOpen = false;
            if (shouldStartBossFight)
            {
                if (bossHandler != null)
                    bossHandler.SkipBossChat();

                isInteractionAvailable = false;
                return;
            }

            if (bossHandler != null)
                bossHandler.EndBossDialoguePresentation();
        }

        void Update()
        {
            if (isSessionOpen && bossHandler != null)
                bossHandler.BeginBossDialoguePresentation();
        }

        void OnDisable()
        {
            var presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter != null)
                presenter.UnregisterSource(this, NpcDialogueSessionCloseReason.SourceDisabled);
        }

        Dialogue ResolveLegacyDialogue()
        {
            if (legacyBossPanel == null && bossHandler != null)
                legacyBossPanel = bossHandler.BossPanel;

            if (legacyBossPanel == null)
                return null;

            var dialogue = legacyBossPanel.GetComponent<Dialogue>();
            if (dialogue != null)
                return dialogue;

            return legacyBossPanel.GetComponentInChildren<Dialogue>(true);
        }
    }
}
