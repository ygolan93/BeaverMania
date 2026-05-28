using Beavermania.Core.Input;
using UnityEngine;

namespace Beavermania.UI.Tips
{
    [DisallowMultipleComponent]
    public sealed class PlayerIdleStateTracker : MonoBehaviour
    {
        [SerializeField] float idleSecondsRequired = 6f;
        [SerializeField] float movementDistanceThreshold = 0.08f;
        [SerializeField] float activeInputGraceSeconds = 0.35f;

        Vector3 lastPosition;
        float idleTimer;
        float lastActiveInputTime;

        public bool IsIdle => idleTimer >= idleSecondsRequired;
        public bool IsActive => Time.unscaledTime - lastActiveInputTime <= activeInputGraceSeconds;

        void OnEnable()
        {
            lastPosition = transform.position;
            idleTimer = 0f;
            lastActiveInputTime = Time.unscaledTime;
        }

        void Update()
        {
            bool moved = (transform.position - lastPosition).sqrMagnitude > movementDistanceThreshold * movementDistanceThreshold;
            bool inputActive = PlayerInputReader.IsAnyKeyHeld();

            if (moved || inputActive)
            {
                idleTimer = 0f;
                if (inputActive)
                    lastActiveInputTime = Time.unscaledTime;
            }
            else
            {
                idleTimer += Time.unscaledDeltaTime;
            }

            lastPosition = transform.position;
        }
    }
}
