using System;
using Beavermania.Objects;
using Beavermania.Player;
using Beavermania.UI.Objectives;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Core.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ObjectiveSyncService : MonoBehaviour
    {
        public static ObjectiveSyncService Instance { get; private set; }

        [SerializeField] int fallbackObjectiveIndex;
        [SerializeField] bool allowLegacyObjectiveFallback = true;
        [SerializeField] bool mirrorLegacyFields = true;

        const float ChopZoneMarkerRadius = 35f;

        readonly ObjectiveProgressionState objectiveState = new();
        ObjectiveStateSnapshot currentSnapshot;

        bool loggedChopZoneFallback;

        BeaverPlayer player;
        WayPoint wayPoint;
        ObjectiveUI objectiveUi;
        PlayerHudState hudState;
        bool loggedObjectiveConfigMismatch;

        public event Action<ObjectiveStateSnapshot> ObjectiveChanged;

        public int CurrentObjectiveIndex => objectiveState.IsInitialized ? objectiveState.CurrentObjectiveIndex : fallbackObjectiveIndex;

        public string CurrentObjectiveText => currentSnapshot.CurrentObjectiveText ?? string.Empty;

        public int CurrentWaypointIndex => currentSnapshot.CurrentWaypointIndex;

        public Transform CurrentWaypointTarget => currentSnapshot.CurrentWaypointTarget;

        public GameProgressionStage CurrentStage => ResolveCurrentLegacyStage();

        public bool AllowLegacyObjectiveFallback => allowLegacyObjectiveFallback;

        public bool MirrorLegacyFields => mirrorLegacyFields;

        public bool CanAuthoritativelyApplyObjective => HasAuthoritativeBindings();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            RefreshBindingsAndReapply();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            RefreshBindingsAndReapply();
        }

        public bool OnTraderDialogueCompleted()
        {
            return TryAdvanceObjective(1, ObjectiveAdvanceReason.DialogueCompleted);
        }

        public bool OnLogCollected()
        {
            return TryAdvanceResolvedLegacyStage(GameProgressionStage.CollectLogs, ObjectiveAdvanceReason.LogCollected);
        }

        public bool OnChopTreeDestroyed(Transform choppedTree)
        {
            EnsureStateInitialized();

            if (!TryResolveObjectiveText(CurrentObjectiveIndex, out string objectiveText)
                || !IsChopTreeObjective(objectiveText))
                return false;

            if (choppedTree == null)
                return false;

            Transform expectedTarget = CurrentWaypointTarget;
            if (expectedTarget == null && wayPoint != null)
                wayPoint.TryGetLocationTarget(CurrentObjectiveIndex, out expectedTarget);

            if (expectedTarget != null && !DoesChoppedTreeMatchWaypointTarget(choppedTree, expectedTarget))
                return false;

            return TryAdvanceObjective(1, ObjectiveAdvanceReason.ObjectiveTargetDestroyed);
        }

        public bool OnPlayerNearBridgeFrame()
        {
            return TryAdvanceResolvedLegacyStage(GameProgressionStage.DeliverLogsToBridge, ObjectiveAdvanceReason.PlayerNearBridgeFrame);
        }

        public bool OnBridgeConstructionLocked()
        {
            return TryAdvanceResolvedLegacyStage(GameProgressionStage.BuildBridge, ObjectiveAdvanceReason.BridgeConstructionLocked);
        }

        public bool OnBridgeCompleted()
        {
            return TryAdvanceResolvedLegacyStage(GameProgressionStage.ContinueJourney, ObjectiveAdvanceReason.BridgeCompleted);
        }

        public bool TrySetObjectiveIndex(int index, ObjectiveAdvanceReason reason)
        {
            if (index < 0)
                return false;

            EnsureStateInitialized();
            if (objectiveState.IsInitialized && index <= objectiveState.CurrentObjectiveIndex)
                return false;

            int previousObjectiveIndex = objectiveState.IsInitialized
                ? objectiveState.CurrentObjectiveIndex
                : index;

            objectiveState.TrySetCurrentObjectiveIndex(index);
            ApplyCurrentState(previousObjectiveIndex, reason, clearHudOverride: true, notifyListeners: true);
            return true;
        }

        public bool TryAdvanceObjective(int delta, ObjectiveAdvanceReason reason)
        {
            if (delta <= 0)
                return false;

            EnsureStateInitialized();
            return TrySetObjectiveIndex(CurrentObjectiveIndex + delta, reason);
        }

        public bool TryApplyLegacyStage(GameProgressionStage stage, ObjectiveAdvanceReason reason)
        {
            EnsureStateInitialized();

            if (TryResolveLegacyStageObjectiveIndex(stage, out int objectiveIndex))
                return TrySetObjectiveIndex(objectiveIndex, reason);

            return allowLegacyObjectiveFallback
                && !HasAuthoritativeBindings()
                && TrySetObjectiveIndex((int)stage, reason);
        }

        bool TryAdvanceResolvedLegacyStage(GameProgressionStage stage, ObjectiveAdvanceReason reason)
        {
            EnsureStateInitialized();

            if (!TryResolveLegacyStageObjectiveIndex(stage, out int objectiveIndex))
                return allowLegacyObjectiveFallback
                    && !HasAuthoritativeBindings()
                    && TrySetObjectiveIndex((int)stage, reason);

            if (objectiveIndex > CurrentObjectiveIndex + 1)
                return false;

            return TrySetObjectiveIndex(objectiveIndex, reason);
        }

        public void RefreshBindingsAndReapply()
        {
            ResolvePlayerReferences(force: true);
            EnsureStateInitialized();
            ApplyCurrentState(CurrentObjectiveIndex, ObjectiveAdvanceReason.RefreshBindings, clearHudOverride: false, notifyListeners: false);
        }

        public bool ShouldUseLegacyObjectiveFallback()
        {
            return allowLegacyObjectiveFallback && !HasAuthoritativeBindings();
        }

        void EnsureStateInitialized()
        {
            if (objectiveState.IsInitialized)
                return;

            int initialObjectiveIndex = ResolveInitialObjectiveIndex();
            objectiveState.Initialize(initialObjectiveIndex);
            currentSnapshot = BuildSnapshot(initialObjectiveIndex, initialObjectiveIndex, ObjectiveAdvanceReason.Bootstrap);
        }

        int ResolveInitialObjectiveIndex()
        {
            if (objectiveUi != null && objectiveUi.i >= 0)
                return objectiveUi.i;

            if (wayPoint != null && wayPoint.i >= 0)
                return wayPoint.i;

            return fallbackObjectiveIndex;
        }

        void ApplyCurrentState(int previousObjectiveIndex, ObjectiveAdvanceReason reason, bool clearHudOverride, bool notifyListeners)
        {
            ResolvePlayerReferences(force: false);
            ValidateBindingsConsistency();

            currentSnapshot = BuildSnapshot(previousObjectiveIndex, CurrentObjectiveIndex, reason);

            if (hudState != null && clearHudOverride)
            {
                hudState.ObjectiveTextOverrideActive = false;
                hudState.ObjectiveTextOverride = null;
            }

            if (mirrorLegacyFields)
                MirrorLegacyComponents(currentSnapshot);

            if (notifyListeners)
                ObjectiveChanged?.Invoke(currentSnapshot);
        }

        ObjectiveStateSnapshot BuildSnapshot(int previousObjectiveIndex, int objectiveIndex, ObjectiveAdvanceReason reason)
        {
            string objectiveText = string.Empty;
            if (!TryResolveObjectiveText(objectiveIndex, out objectiveText) && allowLegacyObjectiveFallback)
                objectiveText = ResolveLegacyObjectiveText();

            int waypointIndex = -1;
            Transform waypointTarget = null;
            if (TryResolveWaypointTarget(objectiveIndex, out waypointTarget))
            {
                waypointIndex = objectiveIndex;
            }
            else if (allowLegacyObjectiveFallback && wayPoint != null)
            {
                waypointIndex = wayPoint.i;
                waypointTarget = wayPoint.CurrentTarget;
            }

            return new ObjectiveStateSnapshot(
                previousObjectiveIndex,
                objectiveIndex,
                objectiveText ?? string.Empty,
                waypointIndex,
                waypointTarget,
                reason);
        }

        bool TryResolveObjectiveText(int index, out string objectiveText)
        {
            objectiveText = null;
            if (objectiveUi == null)
                return false;

            return objectiveUi.TryGetObjectiveText(index, out objectiveText);
        }

        bool TryResolveWaypointTarget(int index, out Transform waypointTarget)
        {
            waypointTarget = null;
            if (wayPoint == null)
                return false;

            return wayPoint.TryGetLocationTarget(index, out waypointTarget);
        }

        string ResolveLegacyObjectiveText()
        {
            if (objectiveUi != null && !string.IsNullOrEmpty(objectiveUi.Instruction))
                return objectiveUi.Instruction;

            if (hudState != null && !string.IsNullOrEmpty(hudState.ObjectiveText))
                return hudState.ObjectiveText;

            return string.Empty;
        }

        void MirrorLegacyComponents(in ObjectiveStateSnapshot snapshot)
        {
            if (wayPoint != null && snapshot.CurrentWaypointIndex >= 0)
                wayPoint.TryApplyObjectiveIndexDirect(snapshot.CurrentWaypointIndex);

            if (objectiveUi != null)
                objectiveUi.ApplyObjectiveMirror(snapshot.CurrentObjectiveIndex, snapshot.CurrentObjectiveText);

            if (hudState != null)
                hudState.SetObjectiveText(snapshot.CurrentObjectiveText);
        }

        GameProgressionStage ResolveCurrentLegacyStage()
        {
            if (!TryResolveObjectiveText(CurrentObjectiveIndex, out string objectiveText))
                return (GameProgressionStage)Mathf.Clamp(CurrentObjectiveIndex, 0, (int)GameProgressionStage.ContinueJourney);

            if (IsBridgeProgressionObjective(objectiveText))
                return GameProgressionStage.BuildBridge;

            if (IsLogCollectionObjective(objectiveText))
                return GameProgressionStage.CollectLogs;

            if (ContainsKeyword(objectiveText, "trader"))
                return GameProgressionStage.TalkToTrader;

            return GameProgressionStage.ContinueJourney;
        }

        bool TryResolveLegacyStageObjectiveIndex(GameProgressionStage stage, out int objectiveIndex)
        {
            objectiveIndex = -1;
            if (objectiveUi == null || objectiveUi.Objective == null || objectiveUi.Objective.Length == 0)
                return false;

            switch (stage)
            {
                case GameProgressionStage.TalkToTrader:
                    return TryFindNextObjectiveIndex(0, IsTraderObjective, out objectiveIndex);

                case GameProgressionStage.CollectLogs:
                    return TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsLogCollectionObjective, out objectiveIndex);

                case GameProgressionStage.DeliverLogsToBridge:
                    return TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsBridgeLockObjective, out objectiveIndex)
                        || TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsBridgeCarryObjective, out objectiveIndex)
                        || TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsBridgeConstructionObjective, out objectiveIndex)
                        || TryFindObjectiveIndex(CurrentObjectiveIndex, IsBridgeConstructionObjective, out objectiveIndex);

                case GameProgressionStage.BuildBridge:
                    return TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsLogCollectionObjective, out objectiveIndex)
                        || TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsBridgeCarryObjective, out objectiveIndex)
                        || TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsBridgeConstructionObjective, out objectiveIndex)
                        || TryFindObjectiveIndex(CurrentObjectiveIndex, IsBridgeConstructionObjective, out objectiveIndex);

                case GameProgressionStage.ContinueJourney:
                    return TryFindNextObjectiveIndex(CurrentObjectiveIndex + 1, IsPostBridgeObjective, out objectiveIndex);

                default:
                    return false;
            }
        }

        bool TryFindNextObjectiveIndex(int startIndex, Func<string, bool> predicate, out int objectiveIndex)
        {
            return TryFindObjectiveIndex(Mathf.Max(0, startIndex), predicate, out objectiveIndex);
        }

        bool TryFindObjectiveIndex(int startIndex, Func<string, bool> predicate, out int objectiveIndex)
        {
            objectiveIndex = -1;
            if (objectiveUi == null || objectiveUi.Objective == null || predicate == null)
                return false;

            for (int index = Mathf.Max(0, startIndex); index < objectiveUi.Objective.Length; index++)
            {
                string objectiveText = objectiveUi.Objective[index];
                if (predicate(objectiveText))
                {
                    objectiveIndex = index;
                    return true;
                }
            }

            return false;
        }

        static bool IsTraderObjective(string objectiveText)
        {
            return ContainsKeyword(objectiveText, "trader");
        }

        static bool IsLogCollectionObjective(string objectiveText)
        {
            return ContainsKeyword(objectiveText, "log")
                && !IsBridgeCarryObjective(objectiveText);
        }

        static bool IsChopTreeObjective(string objectiveText)
        {
            return ContainsKeyword(objectiveText, "chop")
                && ContainsKeyword(objectiveText, "tree");
        }

        bool DoesChoppedTreeMatchWaypointTarget(Transform choppedTree, Transform expectedTarget)
        {
            if (choppedTree == expectedTarget)
                return true;

            if (!IsWaypointChopZoneMarker(expectedTarget))
                return false;

            if (!HasLogSpawner(choppedTree))
                return false;

            float distance = Vector3.Distance(choppedTree.position, expectedTarget.position);
            if (distance > ChopZoneMarkerRadius)
                return false;

            if (!loggedChopZoneFallback)
            {
                loggedChopZoneFallback = true;
                Debug.Log(
                    $"[{nameof(ObjectiveSyncService)}] Accepted nearby chop tree '{choppedTree.name}' for zone marker '{expectedTarget.name}' ({distance:F1}m).",
                    this);
            }

            return true;
        }

        static bool HasLogSpawner(Transform transform)
        {
            return transform != null && transform.GetComponent<LogSpawner>() != null;
        }

        static bool IsWaypointChopZoneMarker(Transform target)
        {
            if (target == null || HasLogSpawner(target))
                return false;

            if (target.CompareTag("Objective"))
                return true;

            string name = target.name;
            return name.IndexOf("TreesToCut", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("WP", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cliff", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsBridgeProgressionObjective(string objectiveText)
        {
            return IsBridgeLockObjective(objectiveText)
                || IsBridgeCarryObjective(objectiveText)
                || IsBridgeConstructionObjective(objectiveText);
        }

        static bool IsBridgeLockObjective(string objectiveText)
        {
            return ContainsKeyword(objectiveText, "lock it with left control")
                || ContainsKeyword(objectiveText, "lock bridge")
                || ContainsKeyword(objectiveText, "lock ");
        }

        static bool IsBridgeCarryObjective(string objectiveText)
        {
            return ContainsKeyword(objectiveText, "bridge")
                && (ContainsKeyword(objectiveText, "carry")
                    || ContainsKeyword(objectiveText, "bring")
                    || ContainsKeyword(objectiveText, "deliver"));
        }

        static bool IsBridgeConstructionObjective(string objectiveText)
        {
            return ContainsKeyword(objectiveText, "cliff")
                || ContainsKeyword(objectiveText, "construct")
                || ContainsKeyword(objectiveText, "build")
                || (ContainsKeyword(objectiveText, "bridge")
                    && !ContainsKeyword(objectiveText, "log")
                    && !ContainsKeyword(objectiveText, "carry")
                    && !ContainsKeyword(objectiveText, "bring")
                    && !ContainsKeyword(objectiveText, "deliver")
                    && !ContainsKeyword(objectiveText, "lock"));
        }

        static bool IsPostBridgeObjective(string objectiveText)
        {
            return !string.IsNullOrWhiteSpace(objectiveText)
                && !IsBridgeProgressionObjective(objectiveText)
                && !IsLogCollectionObjective(objectiveText);
        }

        static bool ContainsKeyword(string text, string keyword)
        {
            return !string.IsNullOrWhiteSpace(text)
                && text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool HasAuthoritativeBindings()
        {
            return objectiveUi != null
                && objectiveUi.Objective != null
                && objectiveUi.Objective.Length > 0
                && wayPoint != null;
        }

        void ValidateBindingsConsistency()
        {
            if (objectiveUi == null || objectiveUi.Objective == null || wayPoint == null || wayPoint.Locations == null)
                return;

            if (objectiveUi.Objective.Length <= wayPoint.Locations.Length)
                return;

            if (loggedObjectiveConfigMismatch)
                return;

            loggedObjectiveConfigMismatch = true;
            Debug.LogWarning(
                $"[{nameof(ObjectiveSyncService)}] Objective text count ({objectiveUi.Objective.Length}) exceeds waypoint slot count ({wayPoint.Locations.Length}). Objective text will still update, but some later waypoint transitions may need additional scene targets.",
                this);
        }

        void ResolvePlayerReferences(bool force)
        {
            if (!force && player != null && wayPoint != null && objectiveUi != null && hudState != null)
                return;

            player = null;
            wayPoint = null;
            objectiveUi = null;
            hudState = null;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                return;

            player = playerObject.GetComponent<BeaverPlayer>();
            wayPoint = playerObject.GetComponent<WayPoint>();
            objectiveUi = playerObject.GetComponent<ObjectiveUI>();
            hudState = playerObject.GetComponent<PlayerHudState>();
        }
    }
}
