using System.Collections.Generic;
using Beavermania.Core.Input;
using Beavermania.Data.Tips;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.UI.Tips
{
    [DisallowMultipleComponent]
    public sealed class TipsService : MonoBehaviour
    {
        sealed class TipRuntimeState
        {
            public int DisplayCount;
            public int TriggerCount;
            public float LastDisplayTime = float.NegativeInfinity;
        }

        static readonly Dictionary<string, TipRuntimeState> RuntimeState = new();

        [SerializeField] BeaverPlayer player;
        [SerializeField] TipCardPresenter presenter;
        [SerializeField] PlayerIdleStateTracker idleState;
        [SerializeField] TipGameStage currentStage = TipGameStage.EarlyGame;
        [SerializeField] int currentProgress;

        bool scriptedBlockActive;

        public static TipsService Instance { get; private set; }

        public static TipsService EnsureForCanvas(GameObject canvasRoot, BeaverPlayer player)
        {
            if (Instance != null)
            {
                Instance.BindPlayer(player);
                return Instance;
            }

            if (canvasRoot == null)
                return null;

            TipsService service = canvasRoot.GetComponent<TipsService>();
            if (service == null)
                service = canvasRoot.AddComponent<TipsService>();

            service.BindPlayer(player);
            return service;
        }

        public static bool TryShowTip(TipDefinition tip, string areaKey = null)
        {
            return Instance != null && Instance.TryShow(tip, areaKey);
        }

        public static void SetProgress(int progress)
        {
            if (Instance != null)
                Instance.currentProgress = Mathf.Max(0, progress);
        }

        public static void SetStage(TipGameStage stage)
        {
            if (Instance != null)
                Instance.currentStage = stage;
        }

        public static void SetScriptedBlock(bool blocked)
        {
            if (Instance != null)
                Instance.scriptedBlockActive = blocked;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsurePresenter();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BindPlayer(BeaverPlayer newPlayer)
        {
            if (newPlayer == null)
                return;

            player = newPlayer;
            idleState = player.GetComponent<PlayerIdleStateTracker>();
            if (idleState == null)
                idleState = player.gameObject.AddComponent<PlayerIdleStateTracker>();
        }

        public bool TryShow(TipDefinition tip, string areaKey = null)
        {
            EnsurePresenter();

            if (!CanShowTip(tip))
                return false;

            string stateKey = BuildStateKey(tip.UniqueId, areaKey);
            TipRuntimeState state = GetState(stateKey);
            state.TriggerCount++;

            if (state.TriggerCount % tip.TriggerInterval != 0)
                return false;

            if (HasExceededDisplayLimit(tip, state))
                return false;

            if (Time.unscaledTime - state.LastDisplayTime < tip.CooldownSeconds)
                return false;

            if (presenter != null && presenter.IsShowing && presenter.CurrentPriority >= tip.Priority)
                return false;

            presenter.Show(tip.DisplayText, tip.Priority, tip.DisplaySeconds);
            state.DisplayCount++;
            state.LastDisplayTime = Time.unscaledTime;
            return true;
        }

        bool CanShowTip(TipDefinition tip)
        {
            if (!TipsSettings.Enabled || tip == null || string.IsNullOrWhiteSpace(tip.DisplayText))
                return false;

            if (player != null && player.IsGameplayInputLocked())
                return false;

            if (tip.MinimumProgress > currentProgress)
                return false;

            if (tip.Category != currentStage)
                return false;

            if (tip.BlockDuringCombat && IsCombatOrPressureActive())
                return false;

            if (tip.IdleOnly && (idleState == null || !idleState.IsIdle))
                return false;

            if (tip.ActiveOnly && (idleState == null || !idleState.IsActive))
                return false;

            return true;
        }

        bool IsCombatOrPressureActive()
        {
            if (scriptedBlockActive)
                return true;

            if (player != null && (player.Rolling || player.Defend || player.isParried))
                return true;

            return PlayerInputReader.IsPrimaryHeld()
                || PlayerInputReader.IsSecondaryHeld()
                || PlayerInputReader.IsRollHeld()
                || PlayerInputReader.IsDefendKeyHeld();
        }

        void EnsurePresenter()
        {
            if (presenter == null)
                presenter = GetComponent<TipCardPresenter>();
            if (presenter == null)
                presenter = gameObject.AddComponent<TipCardPresenter>();
        }

        static TipRuntimeState GetState(string key)
        {
            if (!RuntimeState.TryGetValue(key, out TipRuntimeState state))
            {
                state = new TipRuntimeState();
                RuntimeState.Add(key, state);
            }

            return state;
        }

        static bool HasExceededDisplayLimit(TipDefinition tip, TipRuntimeState state)
        {
            if (tip.ShowOnlyOnce && state.DisplayCount > 0)
                return true;

            return state.DisplayCount >= tip.MaxDisplayCount;
        }

        static string BuildStateKey(string uniqueId, string areaKey)
        {
            if (string.IsNullOrWhiteSpace(areaKey))
                return uniqueId;

            return uniqueId + "@" + areaKey;
        }
    }
}
