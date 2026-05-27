using Beavermania.Core.Input;
using Beavermania.NPC;
using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class TraderAutoInteractAdapter : MonoBehaviour, IAutoInteractable
    {
        [SerializeField] Trader trader;
        [SerializeField] float interactionRangePadding = 0.5f;

        void Awake()
        {
            if (trader == null)
                trader = GetComponent<Trader>();
        }

        public bool CanAutoInteract(GameObject interactor)
        {
            if (trader == null || interactor == null)
                return false;

            if (trader.IsTraderSessionActive())
                return false;

            float range = trader.GetOfferPanelDistance() + interactionRangePadding;
            float dist = Vector3.Distance(interactor.transform.position, transform.position);
            return dist <= range;
        }

        public void AutoInteract(GameObject interactor)
        {
            if (!CanAutoInteract(interactor))
                return;

            PlayerInputOverride.PulseWorldInteract();
        }

        public bool IsAutoInteractionBusy()
        {
            return trader != null && trader.IsTraderSessionActive();
        }
    }
}
