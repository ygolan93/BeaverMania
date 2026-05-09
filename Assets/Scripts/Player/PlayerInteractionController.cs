using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractionController : MonoBehaviour
{
    Behaviour owner;
    GameInputReader inputReader;

    public void Initialize(Behaviour behaviour, GameInputReader reader)
    {
        owner = behaviour;
        inputReader = reader;
    }

    public void ShowCursor()
    {
        CursorStateService.GetOrCreate().ShowCursor();
    }

    public void HideCursor()
    {
        owner.TrySetFreeLookLookAt(owner.Root);
        CursorStateService.GetOrCreate().HideCursor();
        var gameFlow = GameFlowController.Instance;
        if (gameFlow == null || gameFlow.State == GameFlowState.Playing)
        {
            GameFlowController.GetOrCreate().SetPlaying();
        }
    }

    public void EnterTrader(Collider trader)
    {
        var skip = trader.gameObject.GetComponent<Trader>().skipPressed;
        if (!skip)
        {
            ShowCursor();
            owner.isAtTrader = true;
            GameFlowController.GetOrCreate().SetShop();
            owner.TrySetFreeLookEnabled(false);
            owner.TrySetTraderCameraEnabled(true);
            owner.TrySetTraderCameraLookAt(trader.transform);
            return;
        }

        ExitTrader();
    }

    public void ExitTrader()
    {
        owner.isAtTrader = false;
        GameFlowController.GetOrCreate().SetPlaying();
        owner.TrySetTraderCameraEnabled(false);
        owner.TrySetTraderCameraLookAt(null);
        owner.TrySetFreeLookEnabled(true);
        owner.TryRotateFreeLookLookAtTowardRoot();
    }

    GameInputReader GetInputReader()
    {
        if (inputReader == null)
        {
            inputReader = GameInputReader.GetOrCreate();
        }

        return inputReader;
    }
}
