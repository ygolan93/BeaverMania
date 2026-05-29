using Beavermania.Player;
using Beavermania.UI.Objectives;
using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Core.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ObjectiveSyncService : MonoBehaviour
    {
        public static ObjectiveSyncService Instance { get; private set; }

        [SerializeField] string[] stageObjectives =
        {
            "Talk to the trader",
            "Collect logs from trees",
            "Bring logs to the bridge frame",
            "Build and cross the bridge",
            "Continue your journey"
        };

        [SerializeField] GameProgressionStage currentStage = GameProgressionStage.TalkToTrader;

        BeaverPlayer player;
        WayPoint wayPoint;
        ObjectiveUI objectiveUi;
        PlayerHudState hudState;

        public GameProgressionStage CurrentStage => currentStage;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            ResolvePlayerReferences();
            ApplyStage(currentStage, false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OnTraderDialogueCompleted()
        {
            if (currentStage == GameProgressionStage.TalkToTrader)
                ApplyStage(GameProgressionStage.CollectLogs, true);
        }

        public void OnLogCollected()
        {
            if (currentStage <= GameProgressionStage.TalkToTrader)
                ApplyStage(GameProgressionStage.CollectLogs, true);
        }

        public void OnPlayerNearBridgeFrame()
        {
            if (currentStage == GameProgressionStage.CollectLogs)
                ApplyStage(GameProgressionStage.DeliverLogsToBridge, true);
        }

        public void OnBridgeConstructionLocked()
        {
            if (currentStage <= GameProgressionStage.DeliverLogsToBridge)
                ApplyStage(GameProgressionStage.BuildBridge, true);
        }

        public void OnBridgeCompleted()
        {
            ApplyStage(GameProgressionStage.ContinueJourney, true);
        }

        public void ApplyStage(GameProgressionStage stage, bool refreshHudOverride)
        {
            currentStage = stage;
            ResolvePlayerReferences();

            int index = (int)stage;
            string objective = index >= 0 && index < stageObjectives.Length
                ? stageObjectives[index]
                : string.Empty;

            if (wayPoint != null)
            {
                wayPoint.i = index;
                ActivateWaypointLocation(wayPoint, index);
            }

            if (objectiveUi != null)
            {
                objectiveUi.i = index;
                if (objectiveUi.Objective != null && index >= 0 && index < objectiveUi.Objective.Length)
                    objective = objectiveUi.Objective[index];
            }

            if (hudState != null)
            {
                if (refreshHudOverride)
                {
                    hudState.ObjectiveTextOverrideActive = false;
                    hudState.ObjectiveTextOverride = null;
                }

                hudState.ObjectiveText = objective;
            }
        }

        void ResolvePlayerReferences()
        {
            if (player != null && wayPoint != null)
                return;

            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                return;

            player = playerObject.GetComponent<BeaverPlayer>();
            wayPoint = playerObject.GetComponent<WayPoint>();
            objectiveUi = playerObject.GetComponent<ObjectiveUI>();
            hudState = playerObject.GetComponent<PlayerHudState>();
        }

        static void ActivateWaypointLocation(WayPoint wp, int index)
        {
            if (wp.Locations == null || wp.Locations.Length == 0)
                return;

            for (int i = 0; i < wp.Locations.Length; i++)
            {
                if (wp.Locations[i] != null)
                    wp.Locations[i].gameObject.SetActive(i == index);
            }
        }
    }
}
