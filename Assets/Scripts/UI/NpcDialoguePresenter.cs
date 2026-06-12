using System.Collections.Generic;
using Beavermania.Core.GameFlow;
using Beavermania.Core.Input;
using Beavermania.Player;
using TMPro;
using UnityEngine;

namespace Beavermania.UI
{
    /// <summary>
    /// Single owner for NPC prompts and the shared PlayerCanvas dialogue session.
    /// It can be attached to an inactive dialogue root because a runtime host
    /// ticks it while sources register and unregister themselves.
    /// </summary>
    public class NpcDialoguePresenter : MonoBehaviour
    {
        static NpcDialoguePresenter instance;
        static NpcDialoguePresenterRuntimeHost runtimeHost;

        [SerializeField] Dialogue dialogue;
        [SerializeField] GameObject promptRoot;
        [SerializeField] TextMeshProUGUI promptText;
        [SerializeField] GameObject dialogueRoot;

        readonly List<INpcDialogueInteractionSource> registeredSources = new();

        INpcDialogueInteractionSource selectedSource;
        INpcDialogueInteractionSource activeSource;
        NpcDialogueSessionContext activeSession;
        PlayerHudState playerHudState;
        TextMeshProUGUI fallbackObjectiveText;
        bool isPromptVisible;

        public static NpcDialoguePresenter ResolveInstance()
        {
            if (instance == null)
            {
                var presenters = FindObjectsOfType<NpcDialoguePresenter>(true);
                if (presenters != null && presenters.Length > 0)
                    instance = presenters[0];
            }

            EnsureRuntimeHost();
            return instance;
        }

        public void RegisterSource(INpcDialogueInteractionSource source)
        {
            if (!IsSourceReferenceAlive(source))
                return;

            if (!registeredSources.Contains(source))
                registeredSources.Add(source);
        }

        public void UnregisterSource(INpcDialogueInteractionSource source, NpcDialogueSessionCloseReason closeReason)
        {
            if (source == null)
                return;

            registeredSources.Remove(source);

            if (ReferenceEquals(activeSource, source))
            {
                CloseCurrentSession(closeReason);
                return;
            }

            if (ReferenceEquals(selectedSource, source))
                selectedSource = null;
        }

        public bool TryOpenShop(INpcDialogueInteractionSource source)
        {
            if (!ReferenceEquals(activeSource, source) || activeSession == null || !activeSession.HasShop)
                return false;

            ShowShopView(activeSession.ShopContentRoot);
            activeSource.OnDialogueShopOpened();
            return true;
        }

        public bool TryCloseShop(INpcDialogueInteractionSource source)
        {
            if (!ReferenceEquals(activeSource, source) || activeSession == null)
                return false;

            ShowDialogueView(activeSession.ShopContentRoot);
            activeSource.OnDialogueShopClosed();
            return true;
        }

        public bool TryCloseSession(INpcDialogueInteractionSource source, NpcDialogueSessionCloseReason closeReason)
        {
            if (!ReferenceEquals(activeSource, source))
                return false;

            CloseCurrentSession(closeReason);
            return true;
        }

        public void CloseCurrentSession(NpcDialogueSessionCloseReason closeReason)
        {
            if (activeSource == null)
                return;

            var source = activeSource;
            var session = activeSession;

            activeSource = null;
            activeSession = null;
            selectedSource = null;

            HidePrompt();
            HideSessionUi(session != null ? session.ShopContentRoot : null);

            if (dialogue != null)
                dialogue.ClearRuntimeSession();

            source.OnDialogueSessionClosed(closeReason);
        }

        internal void RuntimeTick()
        {
            ResolveBindings();
            CleanupDeadSourceReferences();

            if (activeSource != null)
            {
                HidePrompt();
                return;
            }

            selectedSource = SelectBestAvailableSource();
            UpdatePromptState(selectedSource);

            if (selectedSource == null)
                return;

            if (!PlayerInputReader.WasWorldInteractPressed())
                return;

            OpenSource(selectedSource);
        }

        void Awake()
        {
            instance = this;
            ResolveBindings();
            EnsureRuntimeHost();
        }

        void OnDestroy()
        {
            if (ReferenceEquals(instance, this))
                instance = null;
        }

        void OpenSource(INpcDialogueInteractionSource source)
        {
            if (source == null)
                return;

            var session = source.CreateDialogueSessionContext();
            if (session == null || !session.HasAnyLines())
                return;

            ResolveBindings();
            if (dialogue == null)
                return;

            activeSource = source;
            activeSession = session;
            selectedSource = null;

            HidePrompt();
            dialogue.BindRuntimeSession(session, source, this);
            ShowDialogueView(session.ShopContentRoot);
            source.OnDialogueSessionOpened(session);
            dialogue.RestartDialogue();
        }

        void ResolveBindings()
        {
            if (dialogue == null)
                dialogue = GetComponentInChildren<Dialogue>(true);

            if (dialogueRoot == null)
                dialogueRoot = gameObject;

            if (playerHudState == null || fallbackObjectiveText == null)
                ResolveHudFallbackBindings();
        }

        void ResolveHudFallbackBindings()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null && playerHudState == null)
                playerHudState = playerObject.GetComponent<PlayerHudState>();

            if (fallbackObjectiveText != null)
                return;

            GameObject canvasObject = GameObject.Find("PlayerCanvas");
            if (canvasObject == null)
                return;

            var texts = canvasObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == "ObjectiveText")
                {
                    fallbackObjectiveText = texts[i];
                    break;
                }
            }
        }

        void CleanupDeadSourceReferences()
        {
            for (int index = registeredSources.Count - 1; index >= 0; index--)
            {
                if (!IsSourceReferenceAlive(registeredSources[index]))
                    registeredSources.RemoveAt(index);
            }
        }

        INpcDialogueInteractionSource SelectBestAvailableSource()
        {
            INpcDialogueInteractionSource bestSource = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < registeredSources.Count; i++)
            {
                var source = registeredSources[i];
                if (source == null || !source.IsInteractionAvailable)
                    continue;

                if (source.InteractionDistance < bestDistance)
                {
                    bestDistance = source.InteractionDistance;
                    bestSource = source;
                }
            }

            return bestSource;
        }

        void UpdatePromptState(INpcDialogueInteractionSource source)
        {
            if (source == null)
            {
                HidePrompt();
                return;
            }

            bool canUseDedicatedPrompt = promptRoot != null && promptText != null;
            string promptMessage = string.IsNullOrEmpty(source.InteractionPromptText)
                ? "Press E to interact"
                : source.InteractionPromptText;

            if (canUseDedicatedPrompt)
            {
                promptText.text = promptMessage;
                promptRoot.SetActive(true);
                isPromptVisible = true;
                ClearObjectivePromptFallback();
                return;
            }

            ApplyObjectivePromptFallback(promptMessage);
            isPromptVisible = true;
        }

        void HidePrompt()
        {
            if (!isPromptVisible)
                return;

            if (promptRoot != null)
                promptRoot.SetActive(false);

            ClearObjectivePromptFallback();
            isPromptVisible = false;
        }

        void ApplyObjectivePromptFallback(string promptMessage)
        {
            ResolveHudFallbackBindings();

            if (playerHudState != null)
            {
                playerHudState.ObjectiveTextOverride = promptMessage;
                playerHudState.ObjectiveTextOverrideActive = true;
            }

            if (fallbackObjectiveText != null)
                fallbackObjectiveText.text = promptMessage;
        }

        void ClearObjectivePromptFallback()
        {
            if (playerHudState != null)
            {
                playerHudState.ObjectiveTextOverrideActive = false;
                playerHudState.ObjectiveTextOverride = null;
            }

            var objectiveService = ObjectiveSyncService.Instance;
            if (objectiveService != null)
            {
                objectiveService.RefreshBindingsAndReapply();
                return;
            }

            if (fallbackObjectiveText != null && playerHudState != null)
                fallbackObjectiveText.text = playerHudState.ObjectiveText ?? string.Empty;
        }

        void ShowDialogueView(GameObject shopRoot)
        {
            GameObject dialogueViewRoot = ResolveDialogueViewRoot();
            if (dialogueViewRoot == null)
                return;

            SetActiveWithParents(dialogueViewRoot.transform, true);

            if (shopRoot == null)
            {
                SafeSetActive(dialogueRoot, true);
                return;
            }

            if (shopRoot.transform.IsChildOf(dialogueViewRoot.transform))
            {
                for (int i = 0; i < dialogueViewRoot.transform.childCount; i++)
                {
                    Transform child = dialogueViewRoot.transform.GetChild(i);
                    bool isShopBranch = child.gameObject == shopRoot || shopRoot.transform.IsChildOf(child);
                    child.gameObject.SetActive(!isShopBranch);
                }

                return;
            }

            SafeSetActive(shopRoot, false);
            SafeSetActive(dialogueViewRoot, true);
        }

        void ShowShopView(GameObject shopRoot)
        {
            if (shopRoot == null)
                return;

            GameObject dialogueViewRoot = ResolveDialogueViewRoot();
            if (dialogueViewRoot == null)
            {
                SetActiveWithParents(shopRoot.transform, true);
                return;
            }

            if (shopRoot.transform.IsChildOf(dialogueViewRoot.transform))
            {
                SetActiveWithParents(dialogueViewRoot.transform, true);

                for (int i = 0; i < dialogueViewRoot.transform.childCount; i++)
                {
                    Transform child = dialogueViewRoot.transform.GetChild(i);
                    bool shouldShow = child.gameObject == shopRoot || shopRoot.transform.IsChildOf(child);
                    child.gameObject.SetActive(shouldShow);
                }

                SetActiveWithParents(shopRoot.transform, true);
                return;
            }

            SafeSetActive(dialogueViewRoot, false);
            SetActiveWithParents(shopRoot.transform, true);
        }

        void HideSessionUi(GameObject shopRoot)
        {
            if (shopRoot != null)
                SafeSetActive(shopRoot, false);

            GameObject dialogueViewRoot = ResolveDialogueViewRoot();
            SafeSetActive(dialogueViewRoot, false);

            if (dialogueRoot != null && dialogueRoot != dialogueViewRoot)
                SafeSetActive(dialogueRoot, false);
        }

        GameObject ResolveDialogueViewRoot()
        {
            ResolveBindings();

            if (dialogue != null)
                return dialogue.PanelRoot;

            return dialogueRoot != null ? dialogueRoot : gameObject;
        }

        static void SafeSetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        static void SetActiveWithParents(Transform target, bool active)
        {
            if (target == null)
                return;

            if (active)
            {
                Transform current = target;
                while (current != null)
                {
                    if (!current.gameObject.activeSelf)
                        current.gameObject.SetActive(true);

                    current = current.parent;
                }
            }

            target.gameObject.SetActive(active);
        }

        static bool IsSourceReferenceAlive(INpcDialogueInteractionSource source)
        {
            if (source == null)
                return false;

            return source is not Object unityObject || unityObject != null;
        }

        static void EnsureRuntimeHost()
        {
            if (!Application.isPlaying || runtimeHost != null)
                return;

            var hostObject = new GameObject(nameof(NpcDialoguePresenterRuntimeHost));
            Object.DontDestroyOnLoad(hostObject);
            runtimeHost = hostObject.AddComponent<NpcDialoguePresenterRuntimeHost>();
        }
    }

    sealed class NpcDialoguePresenterRuntimeHost : MonoBehaviour
    {
        void Update()
        {
            var presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter != null)
                presenter.RuntimeTick();
        }
    }
}
