using UnityEngine;

public sealed class CombatFeedbackEmitter : MonoBehaviour
{
    [SerializeField] TimedActiveEffect hitEffect = new TimedActiveEffect();
    [SerializeField] TimedActiveEffect slashHitEffect = new TimedActiveEffect();
    [SerializeField] TimedActiveEffect deathEffect = new TimedActiveEffect();
    [SerializeField] TimedActiveEffect hazardEffect = new TimedActiveEffect();

    public void EmitHit() => hitEffect?.Activate(this);

    public void EmitSlashHit() => slashHitEffect?.Activate(this);

    public void EmitDeath() => deathEffect?.Activate(this);

    public void EmitHazard() => hazardEffect?.Activate(this);

    public void ResetFeedback()
    {
        hitEffect?.DeactivateImmediate(this);
        slashHitEffect?.DeactivateImmediate(this);
        deathEffect?.DeactivateImmediate(this);
        hazardEffect?.DeactivateImmediate(this);
    }
}
