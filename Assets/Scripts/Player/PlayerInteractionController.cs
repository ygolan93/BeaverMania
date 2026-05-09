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
        owner.FreeLook.m_LookAt = owner.Root;
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
            owner.FreeLook.enabled = false;
            owner.CamForTraders.enabled = true;
            owner.CamForTraders.m_LookAt = trader.transform;
            return;
        }

        ExitTrader();
    }

    public void ExitTrader()
    {
        owner.isAtTrader = false;
        GameFlowController.GetOrCreate().SetPlaying();
        owner.CamForTraders.enabled = false;
        owner.CamForTraders.m_LookAt = null;
        owner.FreeLook.enabled = true;
        owner.FreeLook.m_LookAt.rotation = Quaternion.Slerp(owner.transform.rotation, SafeRotation.LookRotationOrCurrent(owner.Root.position, owner.transform.rotation), 0.5f);
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
