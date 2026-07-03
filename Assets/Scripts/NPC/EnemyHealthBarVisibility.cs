using UnityEngine;

namespace Beavermania.NPC
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBarVisibility : MonoBehaviour
    {
        [SerializeField] Canvas healthBarCanvas;
        [SerializeField] Transform playerTransform;
        [SerializeField] float showDistance = 22f;
        [SerializeField] float engageDistance = 14f;
        [SerializeField] float recentlyDamagedDuration = 2.5f;
        [SerializeField] bool alwaysShow;
        [SerializeField] ShadowRevenantController shadowRevenant;

        const float VisibilityCheckInterval = 0.25f;

        float damagedVisibleUntil;
        float nextVisibilityCheck;
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

            nextVisibilityCheck = Time.time + Random.Range(0f, VisibilityCheckInterval);
        }

        void Start()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }

            ApplyVisibility(false);
        }

        void Update()
        {
            if (Time.time < nextVisibilityCheck) return;
            nextVisibilityCheck = Time.time + VisibilityCheckInterval;
            ApplyVisibility(ShouldShowHealthBar());
        }

        public void NotifyDamaged()
        {
            damagedVisibleUntil = Time.time + recentlyDamagedDuration;
        }

        public void EnableAlwaysShow()
        {
            alwaysShow = true;
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

            float distance = Vector3.Distance(playerTransform.position, transform.position);
            if (distance <= engageDistance)
                return true;

            if (wasp != null && wasp.PlayerDistance <= engageDistance)
                return true;

            return distance <= showDistance;
        }

        void ApplyVisibility(bool visible)
        {
            if (healthBarCanvas == null)
                return;

            if (healthBarCanvas.enabled != visible)
                healthBarCanvas.enabled = visible;
        }
    }
}
