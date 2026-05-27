using Cinemachine;
using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerCameraLock : MonoBehaviour
    {
        const float DirectionEpsilon = 1e-6f;

        [SerializeField] BeaverPlayerBehaviour player;
        [SerializeField] float aimSmoothSpeed = 6f;

        float _cachedXAxisMaxSpeed;
        float _cachedYAxisMaxSpeed;
        bool _orbitLocked;

        public bool OrbitLocked => _orbitLocked;

        void Awake()
        {
            if (player == null)
                player = GetComponent<BeaverPlayerBehaviour>();
        }

        public void SetOrbitLocked(bool locked)
        {
            CinemachineFreeLook freeLook = player != null ? player.FreeLook : null;
            if (freeLook == null)
                return;

            if (player != null && player.isAtTrader)
            {
                if (!locked && _orbitLocked)
                {
                    freeLook.m_XAxis.m_MaxSpeed = _cachedXAxisMaxSpeed;
                    freeLook.m_YAxis.m_MaxSpeed = _cachedYAxisMaxSpeed;
                    _orbitLocked = false;
                }
                return;
            }

            if (locked)
            {
                if (_orbitLocked)
                    return;

                _cachedXAxisMaxSpeed = freeLook.m_XAxis.m_MaxSpeed;
                _cachedYAxisMaxSpeed = freeLook.m_YAxis.m_MaxSpeed;
                freeLook.m_XAxis.m_MaxSpeed = 0f;
                freeLook.m_YAxis.m_MaxSpeed = 0f;
                _orbitLocked = true;
                return;
            }

            if (!_orbitLocked)
                return;

            freeLook.m_XAxis.m_MaxSpeed = _cachedXAxisMaxSpeed;
            freeLook.m_YAxis.m_MaxSpeed = _cachedYAxisMaxSpeed;
            _orbitLocked = false;
        }

        public void AimAtTarget(Transform target)
        {
            if (target == null || player == null || player.FreeLook == null)
                return;

            if (player.isAtTrader)
                return;

            CinemachineFreeLook freeLook = player.FreeLook;
            Transform follow = freeLook.Follow != null ? freeLook.Follow : player.transform;
            Vector3 toTarget = target.position - follow.position;
            if (toTarget.sqrMagnitude < DirectionEpsilon)
                return;

            float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float followYaw = follow.eulerAngles.y;
            float yawDelta = Mathf.DeltaAngle(followYaw, targetYaw);
            float nextX = freeLook.m_XAxis.Value + yawDelta;
            freeLook.m_XAxis.Value = Mathf.Lerp(freeLook.m_XAxis.Value, nextX, Time.deltaTime * aimSmoothSpeed);

            Vector3 fullDir = target.position - freeLook.transform.position;
            if (fullDir.sqrMagnitude > DirectionEpsilon)
            {
                fullDir.Normalize();
                float pitch = Mathf.Asin(Mathf.Clamp(fullDir.y, -1f, 1f)) * Mathf.Rad2Deg;
                float clampedPitch = Mathf.Clamp(pitch, freeLook.m_YAxis.m_MinValue, freeLook.m_YAxis.m_MaxValue);
                freeLook.m_YAxis.Value = Mathf.Lerp(
                    freeLook.m_YAxis.Value,
                    clampedPitch,
                    Time.deltaTime * aimSmoothSpeed);
            }
        }

        void OnDisable()
        {
            SetOrbitLocked(false);
        }
    }
}
