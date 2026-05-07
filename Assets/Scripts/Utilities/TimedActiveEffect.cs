using System.Collections;
using UnityEngine;

[System.Serializable]
public sealed class TimedActiveEffect
{
    [SerializeField] GameObject target;
    [SerializeField, Min(0f)] float duration = 0.08f;

    Coroutine deactivateRoutine;

    public GameObject Target => target;

    public void Activate(MonoBehaviour owner) => Activate(owner, duration);

    public void Activate(MonoBehaviour owner, float activeSeconds)
    {
        if (owner == null || target == null)
        {
            return;
        }

        if (deactivateRoutine != null)
        {
            owner.StopCoroutine(deactivateRoutine);
        }

        target.SetActive(true);
        deactivateRoutine = owner.StartCoroutine(DeactivateAfter(activeSeconds));
    }

    public void DeactivateImmediate(MonoBehaviour owner)
    {
        if (owner != null && deactivateRoutine != null)
        {
            owner.StopCoroutine(deactivateRoutine);
        }

        deactivateRoutine = null;

        if (target != null)
        {
            target.SetActive(false);
        }
    }

    IEnumerator DeactivateAfter(float activeSeconds)
    {
        if (activeSeconds > 0f)
        {
            yield return new WaitForSeconds(activeSeconds);
        }

        if (target != null)
        {
            target.SetActive(false);
        }

        deactivateRoutine = null;
    }
}
