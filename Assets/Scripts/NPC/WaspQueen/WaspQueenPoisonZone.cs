using Beavermania.Player;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class WaspQueenPoisonZone : MonoBehaviour
    {
        [SerializeField] SphereCollider zoneCollider;
        [SerializeField] Transform telegraphRoot;
        [SerializeField] Transform activeRoot;
        [SerializeField] bool scaleVisualsToRadius = true;
        [Tooltip("Radius (world units) the active fog visual reads well at when activeRoot localScale is 1.")]
        [SerializeField] float activeVisualBaseRadius = 3f;

        BeaverPlayerBehaviour player;
        float telegraphRemaining;
        float activeDurationRemaining;
        float tickInterval;
        float nextDamageTime;
        float damagePerTick;
        bool released = true;
        bool damagePhaseActive;

        public bool IsActive => !released;

        public void Activate(
            Vector3 position,
            float radius,
            float telegraphDuration,
            float activeDuration,
            float tickDamage,
            float damageRate,
            BeaverPlayerBehaviour playerTarget)
        {
            CacheReferences();
            released = false;
            damagePhaseActive = false;
            player = playerTarget;
            telegraphRemaining = Mathf.Max(0f, telegraphDuration);
            activeDurationRemaining = Mathf.Max(0.05f, activeDuration);
            tickInterval = Mathf.Max(0.05f, damageRate);
            nextDamageTime = Time.time;
            damagePerTick = Mathf.Max(0f, tickDamage);

            gameObject.SetActive(true);
            transform.position = position;

            float resolvedRadius = Mathf.Max(0.1f, radius);
            if (zoneCollider != null)
            {
                zoneCollider.enabled = false;
                zoneCollider.isTrigger = true;
                zoneCollider.radius = resolvedRadius;
            }

            ApplyRadiusScaling(resolvedRadius);

            SetVisualState(showTelegraph: telegraphRemaining > 0f, showActive: telegraphRemaining <= 0f);
            if (telegraphRemaining <= 0f)
                BeginDamagePhase();
        }

        void Awake()
        {
            CacheReferences();
        }

        void Update()
        {
            if (released)
                return;

            if (!damagePhaseActive)
            {
                telegraphRemaining -= Time.deltaTime;
                if (telegraphRemaining <= 0f)
                    BeginDamagePhase();
                return;
            }

            activeDurationRemaining -= Time.deltaTime;
            if (activeDurationRemaining <= 0f)
                Deactivate();
        }

        void OnTriggerStay(Collider other)
        {
            if (released || !damagePhaseActive || other == null || player == null)
                return;

            if (!other.transform.IsChildOf(player.transform) && other.GetComponentInParent<BeaverPlayerBehaviour>() != player)
                return;

            if (Time.time < nextDamageTime)
                return;

            nextDamageTime = Time.time + tickInterval;
            if (player.Rolling || player.isParried)
                return;

            player.TakeDamage(damagePerTick);
        }

        public void Deactivate()
        {
            if (released)
                return;

            released = true;
            damagePhaseActive = false;
            if (zoneCollider != null)
                zoneCollider.enabled = false;

            Destroy(gameObject);
        }

        void BeginDamagePhase()
        {
            damagePhaseActive = true;
            if (zoneCollider != null)
                zoneCollider.enabled = true;

            SetVisualState(showTelegraph: false, showActive: true);
        }

        void SetVisualState(bool showTelegraph, bool showActive)
        {
            if (telegraphRoot != null)
                telegraphRoot.gameObject.SetActive(showTelegraph);

            if (activeRoot != null)
                activeRoot.gameObject.SetActive(showActive);
        }

        void ApplyRadiusScaling(float radius)
        {
            if (!scaleVisualsToRadius)
                return;

            if (activeRoot != null)
            {
                float baseRadius = Mathf.Max(0.01f, activeVisualBaseRadius);
                activeRoot.localScale = Vector3.one * (radius / baseRadius);
            }

            if (telegraphRoot != null)
            {
                float diameter = radius * 2f;
                float thickness = telegraphRoot.localScale.y > 0f ? telegraphRoot.localScale.y : 0.04f;
                telegraphRoot.localScale = new Vector3(diameter, thickness, diameter);
            }
        }

        void CacheReferences()
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<SphereCollider>();
        }
    }
}
