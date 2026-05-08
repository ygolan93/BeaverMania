using UnityEngine;

public sealed class DebugDamageTrigger : MonoBehaviour
{
    [SerializeField] DebugQAConfig config;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;

    void Update()
    {
        if (config == null || !config.enableDamageTrigger || !Input.GetKeyDown(config.damageTriggerKey))
        {
            return;
        }

        if (PlayerReference.TryGetPlayer(out Behaviour player) && player != null)
        {
            player.TakeDamage(new DamageEvent
            {
                Amount = Mathf.Max(0f, config.debugDamageAmount),
                Source = gameObject,
                Point = player.transform.position,
                Type = DamageType.Generic,
                CanStun = false
            });
        }
    }
#else
    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;
#endif
}
