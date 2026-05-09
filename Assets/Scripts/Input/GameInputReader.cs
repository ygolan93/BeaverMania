using System;
using UnityEngine;

[DefaultExecutionOrder(-1100)]
public class GameInputReader : MonoBehaviour
{
    public static GameInputReader Instance { get; private set; }

    public InputMode Mode { get; private set; } = InputMode.Gameplay;
    public bool IsGameplayInputEnabled { get { return Mode == InputMode.Gameplay; } }
    public bool IsUiInputEnabled { get { return Mode == InputMode.Ui; } }

    public bool PausePressed { get; private set; }
    public bool PrimaryHeld { get; private set; }
    public bool PrimaryPressed { get; private set; }
    public bool PrimaryReleased { get; private set; }
    public bool SecondaryHeld { get; private set; }
    public bool SecondaryPressed { get; private set; }
    public bool SecondaryReleased { get; private set; }
    public bool InteractPressed { get; private set; }
    public Vector2 MovementVector { get; private set; }
    public Vector2 ScrollDelta { get; private set; }

    public event Action PausePressedEvent;
    public event Action PrimaryPressedEvent;
    public event Action PrimaryReleasedEvent;
    public event Action SecondaryPressedEvent;
    public event Action SecondaryReleasedEvent;
    public event Action InteractPressedEvent;

    public static GameInputReader GetOrCreate()
    {
        return RuntimeServices.GetRequired<GameInputReader>(ServiceLifetime.Persistent);
    }

    internal void EnableGameplayInput()
    {
        Mode = InputMode.Gameplay;
    }

    internal void DisableGameplayInput()
    {
        Mode = InputMode.Disabled;
        ClearGameplayInput();
    }

    internal void EnableUiInput()
    {
        Mode = InputMode.Ui;
        ClearGameplayInput();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (!RuntimeServices.Register(this, ServiceLifetime.Persistent))
        {
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            RuntimeServices.Unregister(this);
        }
    }

    private void Update()
    {
        PollLegacyInput();
        DispatchEvents();
    }

    private void PollLegacyInput()
    {
        PausePressed = Mode != InputMode.Disabled && UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        if (Mode != InputMode.Gameplay)
        {
            ClearGameplayInput();
            return;
        }

        PrimaryHeld = UnityEngine.Input.GetKey(KeyCode.Mouse0);
        PrimaryPressed = UnityEngine.Input.GetKeyDown(KeyCode.Mouse0);
        PrimaryReleased = UnityEngine.Input.GetKeyUp(KeyCode.Mouse0);
        SecondaryHeld = UnityEngine.Input.GetKey(KeyCode.Mouse1);
        SecondaryPressed = UnityEngine.Input.GetKeyDown(KeyCode.Mouse1);
        SecondaryReleased = UnityEngine.Input.GetKeyUp(KeyCode.Mouse1);
        InteractPressed = UnityEngine.Input.GetKeyDown(KeyCode.F);
        MovementVector = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
        ScrollDelta = UnityEngine.Input.mouseScrollDelta;
    }

    private void ClearGameplayInput()
    {
        PrimaryHeld = false;
        PrimaryPressed = false;
        PrimaryReleased = false;
        SecondaryHeld = false;
        SecondaryPressed = false;
        SecondaryReleased = false;
        InteractPressed = false;
        MovementVector = Vector2.zero;
        ScrollDelta = Vector2.zero;
    }

    private void DispatchEvents()
    {
        if (PausePressed && PausePressedEvent != null)
        {
            PausePressedEvent();
        }

        if (PrimaryPressed && PrimaryPressedEvent != null)
        {
            PrimaryPressedEvent();
        }

        if (PrimaryReleased && PrimaryReleasedEvent != null)
        {
            PrimaryReleasedEvent();
        }

        if (SecondaryPressed && SecondaryPressedEvent != null)
        {
            SecondaryPressedEvent();
        }

        if (SecondaryReleased && SecondaryReleasedEvent != null)
        {
            SecondaryReleasedEvent();
        }

        if (InteractPressed && InteractPressedEvent != null)
        {
            InteractPressedEvent();
        }
    }
}
