using UnityEngine;

[DisallowMultipleComponent]
public class PlayerTraderTriggerController : MonoBehaviour
{
    Behaviour owner;
    Collider cachedCollider;
    Trader cachedTrader;
    Collider activeCollider;
    Trader activeTrader;
    bool activeSkipPressed;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    public void HandleTriggerStay(Collider traderCollider)
    {
        var trader = ResolveTrader(traderCollider);
        if (trader == null)
        {
            return;
        }

        var skipPressed = trader.skipPressed;
        if (activeTrader == trader)
        {
            if (!activeSkipPressed && skipPressed)
            {
                ExitActiveTrader();
                return;
            }

            activeSkipPressed = skipPressed;
            return;
        }

        if (skipPressed)
        {
            return;
        }

        activeTrader = trader;
        activeCollider = traderCollider;
        activeSkipPressed = false;
        owner.EnterTraderInteraction(traderCollider);
    }

    public void HandleTriggerExit(Collider traderCollider)
    {
        if (activeCollider == traderCollider || activeTrader == ResolveTrader(traderCollider))
        {
            ExitActiveTrader();
        }
    }

    Trader ResolveTrader(Collider traderCollider)
    {
        if (traderCollider == null || !traderCollider.gameObject.CompareTag("Trader"))
        {
            return null;
        }

        if (cachedCollider != traderCollider)
        {
            cachedCollider = traderCollider;
            cachedTrader = traderCollider.gameObject.GetComponent<Trader>();
        }

        return cachedTrader;
    }

    void ExitActiveTrader()
    {
        if (activeTrader == null)
        {
            return;
        }

        activeTrader = null;
        activeCollider = null;
        activeSkipPressed = false;
        owner.ExitTraderInteraction();
    }
}
