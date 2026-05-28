using Beavermania.Data.Tips;
using UnityEngine;

namespace Beavermania.UI.Tips
{
    [DisallowMultipleComponent]
    public sealed class TipTriggerZone : MonoBehaviour
    {
        [SerializeField] TipDefinition tip;
        [SerializeField] string areaKey;
        [SerializeField] bool triggerOnEnter = true;
        [SerializeField] bool triggerWhileInside;
        [SerializeField] float repeatAttemptSeconds = 2f;

        float nextRepeatTime;

        void OnTriggerEnter(Collider other)
        {
            if (!triggerOnEnter || !IsPlayer(other))
                return;

            TryTrigger();
        }

        void OnTriggerStay(Collider other)
        {
            if (!triggerWhileInside || !IsPlayer(other) || Time.unscaledTime < nextRepeatTime)
                return;

            TryTrigger();
            nextRepeatTime = Time.unscaledTime + Mathf.Max(0.25f, repeatAttemptSeconds);
        }

        void TryTrigger()
        {
            if (tip == null)
                return;

            string resolvedAreaKey = string.IsNullOrWhiteSpace(areaKey) ? name : areaKey;
            TipsService.TryShowTip(tip, resolvedAreaKey);
        }

        static bool IsPlayer(Collider other)
        {
            return other != null && other.CompareTag("Player");
        }
    }
}
