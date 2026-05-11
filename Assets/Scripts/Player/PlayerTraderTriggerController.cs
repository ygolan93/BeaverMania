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
    Collider suppressedCollider;
    Trader suppressedTrader;

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
        if (IsSuppressedTrader(traderCollider, trader))
        {
            return;
        }

        if (activeTrader == trader)
        {
            if (!owner.isAtTrader)
            {
                SuppressTraderUntilExit(traderCollider, trader);
                ClearActiveTraderState();
                return;
            }
            else
            {
                if (!activeSkipPressed && skipPressed)
                {
                    ExitActiveTrader();
                    return;
                }

                activeSkipPressed = skipPressed;
                return;
            }
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
        var trader = ResolveTrader(traderCollider);
        if (activeCollider == traderCollider || activeTrader == trader)
        {
            ExitActiveTrader();
        }

        if (suppressedCollider == traderCollider || suppressedTrader == trader)
        {
            ClearSuppressedTraderState();
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


    bool IsSuppressedTrader(Collider traderCollider, Trader trader)
    {
        return suppressedCollider == traderCollider || suppressedTrader == trader;
    }

    void SuppressTraderUntilExit(Collider traderCollider, Trader trader)
    {
        suppressedCollider = traderCollider;
        suppressedTrader = trader;
    }

    void ClearSuppressedTraderState()
    {
        suppressedCollider = null;
        suppressedTrader = null;
    }

    void ExitActiveTrader()
    {
        if (activeTrader == null)
        {
            return;
        }

        ClearActiveTraderState();
        owner.ExitTraderInteraction();
    }

    void ClearActiveTraderState()
    {
        activeTrader = null;
        activeCollider = null;
        activeSkipPressed = false;
    }
}
