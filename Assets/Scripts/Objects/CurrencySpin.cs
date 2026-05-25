using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using Beavermania.Player;

namespace Beavermania.Objects
{
    [DefaultExecutionOrder(100)]
    public class CurrencySpin : MonoBehaviour
    {
        enum CoinState
        {
            Idle,
            MagnetPulled,
            Collected
        }

        const float MinTravelBeforeDistanceCollect = 0.15f;

        [Header("Magnet Mode")]
        [Tooltip("When enabled, this coin is pulled from any distance (enemy drops). When disabled, only the player's magnet radius applies.")]
        [SerializeField] bool pullFromAnyDistance;

        BeaverPlayer player;
        CoinMagnet playerMagnet;
        CoinState state = CoinState.Idle;
        Transform pullTarget;
        float basePullSpeed;
        float maxPullSpeed;
        float pullAcceleration;
        float collectDistance;
        float currentPullSpeed;
        float pulledTravelAccum;
        bool magnetActiveThisFrame;

        public bool PullFromAnyDistance => pullFromAnyDistance;

        void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                return;

            player = playerObject.GetComponent<BeaverPlayer>();
            playerMagnet = playerObject.GetComponent<CoinMagnet>();
        }

        void Update()
        {
            if (state == CoinState.Collected)
                return;

            transform.Rotate(0f, 0f, 170f * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (state != CoinState.Collected && pullFromAnyDistance && playerMagnet != null)
                playerMagnet.TryApplyMagnet(this);

            if (state == CoinState.MagnetPulled)
            {
                if (!magnetActiveThisFrame)
                    EndMagnetPull();
                else
                    TickMagnetPull();
            }

            magnetActiveThisFrame = false;
        }

        public bool CanBeMagnetized => state == CoinState.Idle && isActiveAndEnabled && gameObject.activeInHierarchy;

        public void SetPullFromAnyDistance(bool value) => pullFromAnyDistance = value;

        public static void ConfigureEnemyDrop(GameObject spawnedObject)
        {
            if (spawnedObject == null)
                return;

            CurrencySpin coin = spawnedObject.GetComponent<CurrencySpin>();
            if (coin == null)
                coin = spawnedObject.GetComponentInChildren<CurrencySpin>();

            if (coin != null)
                coin.SetPullFromAnyDistance(true);
        }

        public void ContinueMagnetPull(
            Transform target,
            float pullSpeed,
            float maxSpeed,
            float acceleration,
            float pickupDistance)
        {
            if (state == CoinState.Collected || target == null)
                return;

            magnetActiveThisFrame = true;
            pullTarget = target;
            basePullSpeed = Mathf.Max(0.01f, pullSpeed);
            maxPullSpeed = Mathf.Max(basePullSpeed, maxSpeed);
            pullAcceleration = Mathf.Max(0f, acceleration);
            collectDistance = Mathf.Max(0.05f, pickupDistance);

            if (state == CoinState.Idle)
            {
                currentPullSpeed = basePullSpeed;
                pulledTravelAccum = 0f;
                state = CoinState.MagnetPulled;
            }
        }

        void TickMagnetPull()
        {
            if (pullTarget == null)
            {
                EndMagnetPull();
                return;
            }

            Vector3 targetPosition = pullTarget.position;
            Vector3 previousPosition = transform.position;

            currentPullSpeed = Mathf.Min(
                currentPullSpeed + pullAcceleration * Time.deltaTime,
                maxPullSpeed);

            transform.position = Vector3.MoveTowards(
                previousPosition,
                targetPosition,
                currentPullSpeed * Time.deltaTime);

            pulledTravelAccum += Vector3.Distance(previousPosition, transform.position);
            TryCollectWhenReached(targetPosition);
        }

        void EndMagnetPull()
        {
            if (state != CoinState.MagnetPulled)
                return;

            state = CoinState.Idle;
            pullTarget = null;
            currentPullSpeed = 0f;
            pulledTravelAccum = 0f;
        }

        void TryCollectWhenReached(Vector3 targetPosition)
        {
            if (pulledTravelAccum < MinTravelBeforeDistanceCollect)
                return;

            if (Vector3.Distance(transform.position, targetPosition) > collectDistance)
                return;

            TryCollect();
        }

        void TryCollect()
        {
            if (state == CoinState.Collected)
                return;

            state = CoinState.Collected;
            pullTarget = null;
            currentPullSpeed = 0f;
            pulledTravelAccum = 0f;

            Vector3 feedbackPosition = transform.position;

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    player = playerObject.GetComponent<BeaverPlayer>();
            }

            if (player != null)
            {
                player.Currency += 1;
                player.PlayCoinPickupFeedback(feedbackPosition);
            }

            Destroy(gameObject);
        }
    }
}
