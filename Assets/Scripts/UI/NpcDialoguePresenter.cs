using UnityEngine;
using Beavermania.NPC;

namespace Beavermania.UI
{
    /// <summary>
    /// Presenter for the shared NPC dialogue panel nested in PlayerCanvas.
    /// Traders point their DialoguePanel reference at this panel's root;
    /// each time the panel is shown the active trader is resolved, its
    /// dialogue data is applied, and the dialogue is restarted so the
    /// panel can serve any trader.
    /// </summary>
    public class NpcDialoguePresenter : MonoBehaviour
    {
        [SerializeField] Dialogue dialogue;

        void OnEnable()
        {
            if (dialogue == null)
            {
                Debug.LogError($"{nameof(NpcDialoguePresenter)} on '{name}' has no {nameof(dialogue)} assigned.", this);
                return;
            }

            var trader = FindSessionActiveTrader();
            if (trader != null)
            {
                dialogue.Merchant = trader;

                // Always apply the trader's data, even when null, so a trader
                // without its own lines never inherits the previous trader's.
                dialogue.SetDialogueData(trader.DialogueData);
                if (trader.DialogueData == null)
                    Debug.LogWarning($"{nameof(NpcDialoguePresenter)}: trader '{trader.name}' has no DialogueData assigned; the shared panel has no lines to show.", trader);
            }
            else
            {
                // No active trader: clear both the merchant and the dialogue data
                // so nothing from a previous session is ever reused or replayed.
                dialogue.Merchant = null;
                dialogue.SetDialogueData(null);
            }

            dialogue.RestartDialogue();
        }

        static Trader FindSessionActiveTrader()
        {
            var traders = FindObjectsOfType<Trader>(true);
            for (int i = 0; i < traders.Length; i++)
            {
                if (traders[i] != null && traders[i].IsTraderSessionActive())
                    return traders[i];
            }

            return null;
        }
    }
}
