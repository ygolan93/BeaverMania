using System;
using Beavermania.Core.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Objectives
{

    public class WayPoint : MonoBehaviour
    {
        const float LookRotationEpsilon = 0.0001f;
        const int DefaultLocationCount = 21;
        static bool s_loggedMarkCanvasIssue;

        public Image Mark;
        private Transform target;
        public Transform[] Locations;
        public Transform Arrow;
        public int i;
        Camera cachedMainCamera;

        void Awake()
        {
            EnsureLocationTransforms();
            TryResolveMarkFromCanvas();
        }

        void Start()
        {
            cachedMainCamera = Camera.main;
            if (Arrow != null)
                Arrow.gameObject.SetActive(false);
            if (Locations == null || Locations.Length == 0)
            {
                Debug.LogError("[WayPoint] Locations is still invalid after Awake.", this);
                return;
            }

            if (!AdvanceToIndex(i))
                Debug.LogError("[WayPoint] Index i is out of bounds.", this);
        }

        public bool AdvanceToNext()
        {
            return AdvanceToIndex(i + 1);
        }

        public bool AdvanceToIndex(int index)
        {
            if (Locations == null || Locations.Length == 0)
            {
                Debug.LogWarning("[WayPoint] Cannot advance: Locations is invalid.", this);
                return false;
            }

            if (index < 0 || index >= Locations.Length)
            {
                Debug.LogWarning($"[WayPoint] Cannot advance to index {index}; valid range is 0..{Locations.Length - 1}.", this);
                return false;
            }

            if (Locations[index] == null)
            {
                Debug.LogWarning($"[WayPoint] Cannot advance to index {index}; Locations[{index}] is null.", this);
                return false;
            }

            if (i == index && target == Locations[index])
                return false;

            for (int locationIndex = 0; locationIndex < Locations.Length; locationIndex++)
            {
                if (Locations[locationIndex] == null)
                    continue;

                if (locationIndex == 21 || locationIndex == 22)
                    continue;

                Locations[locationIndex].gameObject.SetActive(locationIndex == index);
            }

            i = index;
            target = Locations[index];
            return true;
        }

        void Update()
        {
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

            AdvanceToIndex(newIndex);
        }

        void EnsureLocationTransforms()
        {
            if (Locations == null || Locations.Length < DefaultLocationCount)
            {
                var prev = Locations;
                Locations = new Transform[DefaultLocationCount];
                if (prev != null)
                {
                    for (int c = 0; c < prev.Length && c < DefaultLocationCount; c++)
                        Locations[c] = prev[c];
                }
            }
            for (int idx = 0; idx < Locations.Length; idx++)
            {
                if (Locations[idx] != null)
                    continue;
                var holder = new GameObject($"WaypointTarget_{idx}");
                float yaw = (360f / DefaultLocationCount) * idx;
                Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 3f;
                holder.transform.SetPositionAndRotation(transform.position + offset, Quaternion.identity);
                Locations[idx] = holder.transform;
            }
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
