using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantRemains : MonoBehaviour
    {
        [SerializeField] float lifetimeSeconds = 45f;

        float remaining;

        public void ConfigureLifetime(float seconds)
        {
            lifetimeSeconds = Mathf.Max(0.1f, seconds);
            remaining = lifetimeSeconds;
        }

        void OnEnable()
        {
            remaining = Mathf.Max(0.1f, lifetimeSeconds);
        }

        void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                Destroy(gameObject);
        }
    }
}
