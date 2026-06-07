using UnityEngine;

namespace Beavermania.NPC
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBarVisibility : MonoBehaviour
    {
        const float RefreshIntervalSeconds = 0.1f;

        [SerializeField] Canvas healthBarCanvas;
        [SerializeField] Transform playerTransform;
        [SerializeField] float showDistance = 22f;
        [SerializeField] float engageDistance = 14f;
        [SerializeField] float recentlyDamagedDuration = 2.5f;
        [SerializeField] bool alwaysShow;
        [SerializeField] ShadowRevenantController shadowRevenant;

        float damagedVisibleUntil;
        float refreshTimer;
        bool visibilityDirty = true;
        bool lastAppliedVisibility;
        NPC_Basic wasp;

        void Awake()
        {
            if (healthBarCanvas == null)
                healthBarCanvas = GetComponentInChildren<Canvas>(true);

            if (shadowRevenant == null)
                shadowRevenant = GetComponent<ShadowRevenantController>();

            wasp = GetComponentInParent<NPC_Basic>();
            if (wasp == null)
                wasp = GetComponent<NPC_Basic>();
        }

        void Start()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }

            lastAppliedVisibility = false;
            ApplyVisibility(false);
        }

        void Update()
        {
            refreshTimer -= Time.deltaTime;
            if (!visibilityDirty && refreshTimer > 0f)
                return;

            refreshTimer = RefreshIntervalSeconds;
            visibilityDirty = false;
            ApplyVisibility(ShouldShowHealthBar());
        }

        public void NotifyDamaged()
        {
            damagedVisibleUntil = Time.time + recentlyDamagedDuration;
            visibilityDirty = true;
        }

        public void EnableAlwaysShow()
        {
            alwaysShow = true;
            visibilityDirty = true;
            ApplyVisibility(true);
        }

        bool ShouldShowHealthBar()
        {
            if (shadowRevenant != null)
            {
                ShadowRevenantState state = shadowRevenant.State;
                return state != ShadowRevenantState.Dormant && state != ShadowRevenantState.Dead;
            }

            if (alwaysShow)
                return true;

            if (Time.time < damagedVisibleUntil)
                return true;

            if (playerTransform == null)
                return false;

            float distanceSq = (playerTransform.position - transform.position).sqrMagnitude;
            float engageDistanceSq = engageDistance * engageDistance;
            if (distanceSq <= engageDistanceSq)
                return true;

            if (wasp != null && wasp.PlayerDistance <= engageDistance)
                return true;

            float showDistanceSq = showDistance * showDistance;
            return distanceSq <= showDistanceSq;
        }

        void ApplyVisibility(bool visible)
        {
            if (healthBarCanvas == null)
                return;

            if (lastAppliedVisibility == visible && healthBarCanvas.enabled == visible)
                return;

            lastAppliedVisibility = visible;
            healthBarCanvas.enabled = visible;
        }
    }
}
