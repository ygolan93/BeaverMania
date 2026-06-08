using System;
using System.Collections.Generic;
using Beavermania.Core.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Objectives
{

    public class WayPoint : MonoBehaviour
    {
        const float LookRotationEpsilon = 0.0001f;
        static bool s_loggedMarkCanvasIssue;

        public Image Mark;
        private Transform target;
        public Transform[] Locations;
        public Transform Arrow;
        public int i;
        Camera cachedMainCamera;
        ObjectiveUI cachedObjectiveUi;
        Beavermania.Player.PlayerHudState cachedHudState;
        readonly HashSet<int> loggedMissingTargetIndices = new();

        public Transform CurrentTarget => target;

        void Awake()
        {
            EnsureLocationArrayInitialized();
            TryResolveMarkFromCanvas();
            cachedObjectiveUi = GetComponent<ObjectiveUI>();
            cachedHudState = GetComponent<Beavermania.Player.PlayerHudState>();
        }

        void Start()
        {
            cachedMainCamera = Camera.main;
            if (Arrow != null)
                Arrow.gameObject.SetActive(false);

            if (!TryGetLocationTarget(i, out Transform initialTarget))
            {
                Debug.LogWarning($"[WayPoint] Failed to resolve initial waypoint target for index {i}.", this);
                return;
            }

            ApplyObjectiveIndexDirect(i, initialTarget);
            MirrorLocalObjectiveState(i);
        }

        public bool AdvanceToNext()
        {
            var objectiveService = Beavermania.Core.GameFlow.ObjectiveSyncService.Instance;
            if (objectiveService != null)
            {
                bool advanced = objectiveService.TryAdvanceObjective(1, Beavermania.Core.GameFlow.ObjectiveAdvanceReason.LegacyWaypointAdvanceRequest);
                if (advanced || !objectiveService.ShouldUseLegacyObjectiveFallback())
                    return advanced;
            }

            return TryAdvanceToNextDirect();
        }

        public bool AdvanceToIndex(int index)
        {
            var objectiveService = Beavermania.Core.GameFlow.ObjectiveSyncService.Instance;
            if (objectiveService != null)
            {
                bool advanced = objectiveService.TrySetObjectiveIndex(index, Beavermania.Core.GameFlow.ObjectiveAdvanceReason.LegacyWaypointAdvanceRequest);
                if (advanced || !objectiveService.ShouldUseLegacyObjectiveFallback())
                    return advanced;
            }

            return TryApplyObjectiveIndexDirect(index);
        }

        internal bool TryAdvanceToNextDirect()
        {
            return TryApplyObjectiveIndexDirect(i + 1);
        }

        internal bool TryApplyObjectiveIndexDirect(int index)
        {
            if (!TryGetLocationTarget(index, out Transform nextTarget))
                return false;

            if (i == index && target == nextTarget)
                return true;

            ApplyObjectiveIndexDirect(index, nextTarget);
            MirrorLocalObjectiveState(index);
            return true;
        }

        internal bool TryGetLocationTarget(int index, out Transform nextTarget)
        {
            nextTarget = null;
            if (index < 0)
            {
                Debug.LogWarning($"[WayPoint] Cannot advance to index {index}; index must be non-negative.", this);
                return false;
            }

            EnsureLocationSlotCapacity(index + 1);
            if (Locations == null || index >= Locations.Length)
            {
                LogMissingTarget(index, $"[WayPoint] Cannot advance to index {index}; waypoint storage is unavailable.");
                return false;
            }

            Transform location = Locations[index];
            if (!IsActiveLocationReference(location))
            {
                Locations[index] = null;
                if (TryResolveSceneWaypointTarget(index, out Transform resolvedTarget))
                    Locations[index] = resolvedTarget;

                location = Locations[index];
            }

            if (!IsActiveLocationReference(location))
            {
                LogMissingTarget(index, $"[WayPoint] Cannot advance to index {index}; no scene waypoint target named '{index}' was found.");
                return false;
            }

            nextTarget = location;
            return true;
        }

        void ApplyObjectiveIndexDirect(int index, Transform nextTarget)
        {
            i = index;
            target = nextTarget;
        }

        void RefreshActiveWaypointTarget()
        {
            if (IsActiveLocationReference(target))
                return;

            target = null;
            if (TryGetLocationTarget(i, out Transform resolvedTarget))
                target = resolvedTarget;
        }

        static bool IsActiveLocationReference(Transform location)
        {
            return location;
        }

        void MirrorLocalObjectiveState(int index)
        {
            if (cachedObjectiveUi == null)
                cachedObjectiveUi = GetComponent<ObjectiveUI>();

            if (cachedHudState == null)
                cachedHudState = GetComponent<Beavermania.Player.PlayerHudState>();

            if (cachedObjectiveUi == null)
                return;

            string objectiveText = string.Empty;
            if (cachedObjectiveUi.TryGetObjectiveText(index, out string resolvedObjectiveText))
                objectiveText = resolvedObjectiveText ?? string.Empty;

            cachedObjectiveUi.ApplyObjectiveMirror(index, objectiveText);
            if (cachedHudState != null)
                cachedHudState.SetObjectiveText(objectiveText);
        }

        void Update()
        {
            RefreshActiveWaypointTarget();

            if (Arrow != null)
            {
                if (PlayerInputReader.IsWaypointCompassHeld())
                    Arrow.gameObject.SetActive(true);
                else
                    Arrow.gameObject.SetActive(false);
            }

            if (target != null && Arrow != null)
            {
                Vector3 toTarget = target.position - transform.position;
                if (toTarget.sqrMagnitude > LookRotationEpsilon)
                    Arrow.gameObject.transform.rotation = Quaternion.LookRotation(toTarget);
            }

            if (target != null && Mark != null && Mark.canvas != null)
            {
                float minX = Mark.GetPixelAdjustedRect().width / 2;
                float maxX = Screen.width - minX;
                float minY = Mark.GetPixelAdjustedRect().height / 2;
                float maxY = Screen.height - minY;
                if (cachedMainCamera == null)
                    cachedMainCamera = Camera.main;
                if (cachedMainCamera == null)
                    return;
                Vector2 pos = cachedMainCamera.WorldToScreenPoint(target.position + new Vector3(0, 2, 0));

                if (Vector3.Dot((target.position - transform.position), transform.forward) < 0)
                    Mark.enabled = false;
                else
                    Mark.enabled = true;

                pos.x = Mathf.Clamp(pos.x, minX, maxX);
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
                Mark.transform.position = pos;
            }
            else if (target != null && Mark != null && Mark.canvas == null && !s_loggedMarkCanvasIssue)
            {
                s_loggedMarkCanvasIssue = true;
                Debug.LogWarning("[WayPoint] Mark Image is not under an active Canvas; screen clamping skipped. Assign a UI Image on a Canvas or add WayPointMark to PlayerCanvas.", this);
            }
        }

        private void OnTriggerEnter(Collider OBJ)
        {
            if (!OBJ.gameObject.CompareTag("WayPoint"))
                return;

            OBJ.gameObject.SetActive(false);

            if (!int.TryParse(OBJ.gameObject.name, out int newIndex))
            {
                Debug.LogWarning("Failed to parse waypoint name: " + OBJ.gameObject.name, this);
                return;
            }

            var objectiveService = Beavermania.Core.GameFlow.ObjectiveSyncService.Instance;
            if (objectiveService != null)
            {
                bool advanced = objectiveService.TrySetObjectiveIndex(newIndex, Beavermania.Core.GameFlow.ObjectiveAdvanceReason.WaypointTrigger);
                if (advanced || !objectiveService.ShouldUseLegacyObjectiveFallback())
                    return;
            }

            TryApplyObjectiveIndexDirect(newIndex);
        }

        void EnsureLocationArrayInitialized()
        {
            if (Locations == null)
                Locations = Array.Empty<Transform>();
        }

        void EnsureLocationSlotCapacity(int requiredLength)
        {
            EnsureLocationArrayInitialized();
            if (requiredLength <= Locations.Length)
                return;

            Array.Resize(ref Locations, requiredLength);
        }

        bool TryResolveSceneWaypointTarget(int index, out Transform resolvedTarget)
        {
            resolvedTarget = null;
            string targetName = index.ToString();
            var sceneWaypoints = FindObjectsOfType<Transform>(true);
            for (int sceneIndex = 0; sceneIndex < sceneWaypoints.Length; sceneIndex++)
            {
                Transform sceneWaypoint = sceneWaypoints[sceneIndex];
                if (sceneWaypoint == null)
                    continue;

                GameObject sceneWaypointObject = sceneWaypoint.gameObject;
                if (sceneWaypointObject == null || !sceneWaypointObject.CompareTag("WayPoint"))
                    continue;

                if (!string.Equals(sceneWaypointObject.name, targetName, StringComparison.Ordinal))
                    continue;

                resolvedTarget = sceneWaypoint;
                return true;
            }

            return false;
        }

        void LogMissingTarget(int index, string message)
        {
            if (loggedMissingTargetIndices.Add(index))
                Debug.LogWarning(message, this);
        }

        void TryResolveMarkFromCanvas()
        {
            if (Mark != null)
                return;

            Mark = FindWaypointMarkImage();
        }

        static Image FindWaypointMarkImage()
        {
            foreach (var graphic in FindObjectsOfType<Image>(true))
            {
                if (graphic == null)
                    continue;
                var n = graphic.gameObject.name;
                if (string.Equals(n, "WayPointMark", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "WaypointMark", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "Mark", StringComparison.OrdinalIgnoreCase))
                {
                    return graphic;
                }
            }

            return null;
        }
    }
}
