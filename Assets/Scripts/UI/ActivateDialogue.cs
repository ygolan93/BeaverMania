using UnityEngine;

namespace Beavermania.UI
{
    public class ActivateDialogue : MonoBehaviour, INpcDialogueInteractionSource
    {
        const string DefaultInteractionPromptMessage = "Press E to interact";

        public GameObject Dialogue;
        public GameObject Target;
        public GameObject Player;
        public float Distance;
        [SerializeField] string interactionPromptMessage = DefaultInteractionPromptMessage;
        [SerializeField] float interactionRange = 5f;

        public Transform InteractionTransform => Target != null ? Target.transform : transform;

        public Transform InteractionLookTarget => InteractionTransform;

        public string InteractionPromptText => string.IsNullOrEmpty(interactionPromptMessage)
            ? DefaultInteractionPromptMessage
            : interactionPromptMessage;

        public float InteractionDistance => isPlayerInRange ? Distance : float.MaxValue;

        public bool IsInteractionAvailable => isPlayerInRange && !isSessionOpen;

        bool isPlayerInRange;
        bool isSessionOpen;
        bool isRegisteredWithPresenter;
        bool loggedMissingDialogue;

        void Start()
        {
            if (Player == null)
                ResolvePlayer();

            UpdatePresenterRegistration();
        }

        void Update()
        {
            if (Player == null)
                ResolvePlayer();

            Transform targetTransform = InteractionTransform;
            if (Player == null || targetTransform == null)
            {
                isPlayerInRange = false;
                UpdatePresenterRegistration();
                return;
            }

            Distance = (Player.transform.position - targetTransform.position).magnitude;
            isPlayerInRange = Distance <= interactionRange;
            UpdatePresenterRegistration();
        }

        public NpcDialogueSessionContext CreateDialogueSessionContext()
        {
            var legacyDialogue = ResolveLegacyDialogue();
            if (legacyDialogue == null)
            {
                if (!loggedMissingDialogue)
                {
                    loggedMissingDialogue = true;
                    Debug.LogWarning($"{nameof(ActivateDialogue)} on '{name}' could not resolve a legacy {nameof(global::Beavermania.UI.Dialogue)} source.", this);
                }

                return null;
            }

            return new NpcDialogueSessionContext(
                legacyDialogue.ConfiguredDialogueData,
                legacyDialogue.ConfiguredLines,
                legacyDialogue.ConfiguredTextSpeed,
                legacyDialogue.ConfiguredIsBoss,
                legacyDialogue.ConfiguredAdvanceObjectiveOnEnd,
                shopContentRoot: null);
        }

        public void OnDialogueSessionOpened(NpcDialogueSessionContext context)
        {
            isSessionOpen = true;
        }

        public void OnDialogueShopOpened()
        {
        }

        public void OnDialogueShopClosed()
        {
        }

        public void OnDialogueSessionClosed(NpcDialogueSessionCloseReason reason)
        {
            isSessionOpen = false;
        }

        void OnDisable()
        {
            if (!isRegisteredWithPresenter)
                return;

            var presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter != null)
                presenter.UnregisterSource(this, NpcDialogueSessionCloseReason.SourceDisabled);

            isRegisteredWithPresenter = false;
            isSessionOpen = false;
            isPlayerInRange = false;
        }

        void ResolvePlayer()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                Player = playerObject;
        }

        void UpdatePresenterRegistration()
        {
            var presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter == null)
            {
                isRegisteredWithPresenter = false;
                return;
            }

            bool shouldRegister = isPlayerInRange;
            if (shouldRegister)
            {
                presenter.RegisterSource(this);
                isRegisteredWithPresenter = true;
            }
            else if (isRegisteredWithPresenter)
            {
                presenter.UnregisterSource(this, NpcDialogueSessionCloseReason.PlayerLeftRange);
                isRegisteredWithPresenter = false;
            }
        }

        global::Beavermania.UI.Dialogue ResolveLegacyDialogue()
        {
            if (Dialogue == null)
                return null;

            var legacyDialogue = Dialogue.GetComponent<global::Beavermania.UI.Dialogue>();
            if (legacyDialogue != null)
                return legacyDialogue;

            return Dialogue.GetComponentInChildren<global::Beavermania.UI.Dialogue>(true);
        }
    }
}
