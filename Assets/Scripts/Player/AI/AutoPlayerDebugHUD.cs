using UnityEngine;

namespace Beavermania.Player.AI
{
    public class AutoPlayerDebugHUD : MonoBehaviour
    {
        [SerializeField] bool showDebugHud = true;
        [SerializeField] bool drawDestinationGizmo = true;

        AutoPlayerBrain _brain;
        AutoPlayerMovementAgent _movement;
        AutoPlayerActionAdapter _adapter;
        AutoPlayerTaskMemory _memory;
        AutoPlayerTerrainSense _terrain;
        AutoPlayerIdlePlanner _idlePlanner;

        string _statusLine = string.Empty;

        public bool ShowDebugHud
        {
            get => showDebugHud;
            set => showDebugHud = value;
        }

        public void SyncFromBrain(
            AutoPlayerBrain brain,
            AutoPlayerMovementAgent movement,
            AutoPlayerActionAdapter adapter,
            AutoPlayerTaskMemory memory)
        {
            _brain = brain;
            _movement = movement;
            _adapter = adapter;
            _memory = memory;
            if (_terrain == null)
                _terrain = GetComponent<AutoPlayerTerrainSense>();
            if (_idlePlanner == null)
                _idlePlanner = GetComponent<AutoPlayerIdlePlanner>();

            if (brain == null)
            {
                _statusLine = "AutoPlayer: (no brain)";
                return;
            }

            string targetName = brain.CurrentTarget != null ? brain.CurrentTarget.name : "none";
            float dist = movement != null ? movement.DistanceToDestination : 0f;
            float stuck = movement != null ? movement.StuckTimer : 0f;
            int logs = adapter != null ? adapter.CarriedLogCount : 0;
            string idleAction = _idlePlanner != null ? _idlePlanner.LastResult.Action.ToString() : "n/a";
            string terrainFlags = "n/a";
            if (_terrain != null)
            {
                TerrainProbeSnapshot snap = _terrain.Snapshot;
                terrainFlags =
                    $"walk={snap.IsOnWalkableGround} cliff={snap.CliffAhead} haz={snap.HazardAhead}";
            }

            string arsenal = adapter != null ? adapter.GetActiveArsenalName() : "n/a";
            string combatMode = brain.CombatMode.ToString();
            string retaliate = brain.IsRetaliating ? "yes" : "no";
            string camLock = brain.CameraOrbitLocked ? "locked" : "free";
            string stamina = brain.RecoveringStamina ? "recovering" : "ok";
            float staminaPct = adapter != null ? adapter.StaminaRatio() * 100f : 100f;

            _statusLine =
                $"AutoPlayer {(brain.AutoPlayerEnabled ? "ON" : "OFF")}\n" +
                $"State: {brain.State}  Priority: {brain.CurrentPriority}\n" +
                $"Target: {targetName}  Dist: {dist:F1}  Stuck: {stuck:F1}s\n" +
                $"Logs: {logs}  Arsenal: {arsenal}\n" +
                $"Combat: {combatMode}  Retaliate: {retaliate}  Cam: {camLock}\n" +
                $"Stamina: {staminaPct:F0}% ({stamina})\n" +
                $"Idle: {idleAction}  Terrain: {terrainFlags}";
        }

        void OnGUI()
        {
            if (!showDebugHud || _brain == null || !_brain.AutoPlayerEnabled)
                return;

            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Box(new Rect(10f, 10f, 340f, 132f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(18f, 16f, 320f, 122f), _statusLine);
        }

        void OnDrawGizmos()
        {
            if (!drawDestinationGizmo || _movement == null || !_movement.HasDestination)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_movement.CurrentDestination, 0.35f);
            Gizmos.DrawLine(transform.position, _movement.CurrentDestination);
        }
    }
}
