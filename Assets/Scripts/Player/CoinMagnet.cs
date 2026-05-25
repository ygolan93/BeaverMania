using Beavermania.Objects;
using UnityEngine;

namespace Beavermania.Player
{
    /// <summary>
    /// Detects coins in <see cref="magnetRadius"/> and starts/continues magnet pull on each coin.
    /// Coins with <see cref="CurrencySpin.PullFromAnyDistance"/> pull via <see cref="TryApplyMagnet"/> instead.
    /// </summary>
    public class CoinMagnet : MonoBehaviour
    {
        const int OverlapBufferSize = 48;
        const float MaxCollectDistance = 1f;

        [Header("Magnet")]
        [SerializeField] float magnetRadius = 5f;
        [SerializeField] float pullSpeed = 8f;
        [SerializeField] float maxPullSpeed = 18f;
        [SerializeField] float acceleration = 20f;
        [Tooltip("Pickup radius at the collection point. Must stay much smaller than magnetRadius.")]
        [SerializeField] float collectDistance = 0.35f;
        [Tooltip("World-space Y added to the player root when no collect point is assigned.")]
        [SerializeField] float verticalOffset = 1f;
        [SerializeField] Transform coinCollectPoint;
        [Tooltip("Legacy alias; uses coinCollectPoint when set.")]
        [SerializeField] Transform collectionTarget;
        [SerializeField] LayerMask coinLayerMask = ~0;

        [Header("Debug")]
        [SerializeField] bool drawDebugGizmo = true;

        static readonly Collider[] OverlapBuffer = new Collider[OverlapBufferSize];
        Transform runtimeCollectPoint;

        void Awake()
        {
            if (coinCollectPoint == null && collectionTarget != null)
                coinCollectPoint = collectionTarget;

            magnetRadius = Mathf.Max(0f, magnetRadius);
            pullSpeed = Mathf.Max(0.01f, pullSpeed);
            maxPullSpeed = Mathf.Max(pullSpeed, maxPullSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            collectDistance = Mathf.Clamp(collectDistance, 0.05f, MaxCollectDistance);

            if (collectDistance >= magnetRadius)
                collectDistance = Mathf.Min(MaxCollectDistance, magnetRadius * 0.15f);

            runtimeCollectPoint = coinCollectPoint != null ? coinCollectPoint : CreateFallbackCollectPoint();
        }

        void Update()
        {
            if (magnetRadius <= 0f || pullSpeed <= 0f || runtimeCollectPoint == null)
                return;

            Vector3 magnetCenter = ResolveMagnetDetectionCenter();

            int hitCount = Physics.OverlapSphereNonAlloc(
                magnetCenter,
                magnetRadius,
                OverlapBuffer,
                coinLayerMask,
                QueryTriggerInteraction.Collide);

            for (var i = 0; i < hitCount; i++)
            {
                Collider col = OverlapBuffer[i];
                if (col == null)
                    continue;

                CurrencySpin coin = col.GetComponentInParent<CurrencySpin>();
                if (coin == null || coin.PullFromAnyDistance)
                    continue;

                TryApplyMagnet(coin);
            }
        }

        public void TryApplyMagnet(CurrencySpin coin)
        {
            if (coin == null || runtimeCollectPoint == null || pullSpeed <= 0f)
                return;

            if (!coin.PullFromAnyDistance && !IsWithinMagnetRadius(coin.transform.position))
                return;

            coin.ContinueMagnetPull(
                runtimeCollectPoint,
                pullSpeed,
                maxPullSpeed,
                acceleration,
                collectDistance);
        }

        bool IsWithinMagnetRadius(Vector3 worldPosition)
        {
            if (magnetRadius <= 0f)
                return false;

            return Vector3.Distance(worldPosition, ResolveMagnetDetectionCenter()) <= magnetRadius;
        }

        Transform CreateFallbackCollectPoint()
        {
            var fallback = new GameObject("CoinCollectPoint_Fallback").transform;
            fallback.SetParent(transform, false);
            fallback.localPosition = Vector3.up * verticalOffset;
            return fallback;
        }

        Vector3 ResolveMagnetDetectionCenter()
        {
            return transform.position + Vector3.up * (verticalOffset * 0.5f);
        }

        void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmo)
                return;

            Vector3 magnetCenter = ResolveMagnetDetectionCenter();
            Vector3 anchor = runtimeCollectPoint != null
                ? runtimeCollectPoint.position
                : transform.position + Vector3.up * verticalOffset;

            Gizmos.color = new Color(1f, 0.82f, 0.15f, 0.9f);
            Gizmos.DrawWireSphere(magnetCenter, magnetRadius);

            Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.9f);
            Gizmos.DrawWireSphere(anchor, collectDistance);
        }
    }
}
