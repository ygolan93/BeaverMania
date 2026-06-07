using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Beavermania.Core.Input
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "Beavermania/Input/Input Reader")]
#if ENABLE_INPUT_SYSTEM
    public sealed class InputReader : ScriptableObject, BeaverInputSystem.IGameplayActions, BeaverInputSystem.IUIActions
    {
        enum InputMode
        {
            None,
            Gameplay,
            UI
        }

        BeaverInputSystem _actions;
        InputMode _activeMode;
        Vector2 _currentMove;
        bool _isSprinting;

        public event Action<Vector2> MoveEvent;
        public event Action JumpEvent;
        public event Action PauseEvent;
        public event Action ResumeEvent;
        public event Action<bool> SprintEvent;

        void OnEnable()
        {
            EnsureActions();
            ClearCallbacksAndDisableMaps();
            SetGameplay();
        }

        void OnDisable()
        {
            ClearCallbacksAndDisableMaps();
        }

        void OnDestroy()
        {
            ClearCallbacksAndDisableMaps();
            ReleaseActions();
        }

        void ReleaseActions()
        {
            if (_actions == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _actions = null;
                return;
            }
#endif

            _actions.Dispose();
            _actions = null;
        }

        public void SetGameplay()
        {
            RebindForMode(InputMode.Gameplay);
        }

        public void SetUI()
        {
            RebindForMode(InputMode.UI);
        }

        void EnsureActions()
        {
            _actions ??= new BeaverInputSystem();
        }

        void RebindForMode(InputMode mode)
        {
            EnsureActions();
            ClearCallbacksAndDisableMaps();

            switch (mode)
            {
                case InputMode.Gameplay:
                    _actions.Gameplay.SetCallbacks(this);
                    _actions.Gameplay.Enable();
                    break;
                case InputMode.UI:
                    _actions.UI.SetCallbacks(this);
                    _actions.UI.Enable();
                    break;
            }

            _activeMode = mode;
        }

        void ClearCallbacksAndDisableMaps()
        {
            if (_actions == null)
            {
                _activeMode = InputMode.None;
                ResetTransientState();
                return;
            }

            _actions.Gameplay.Disable();
            _actions.UI.Disable();
            _actions.Gameplay.SetCallbacks(null);
            _actions.UI.SetCallbacks(null);
            _activeMode = InputMode.None;
            ResetTransientState();
        }

        void ResetTransientState()
        {
            if (_currentMove != Vector2.zero)
            {
                _currentMove = Vector2.zero;
                MoveEvent?.Invoke(_currentMove);
            }

            if (_isSprinting)
            {
                _isSprinting = false;
                SprintEvent?.Invoke(false);
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (_activeMode != InputMode.Gameplay)
                return;

            _currentMove = context.ReadValue<Vector2>();
            MoveEvent?.Invoke(_currentMove);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (_activeMode != InputMode.Gameplay || !context.performed)
                return;

            JumpEvent?.Invoke();
        }

        public void OnPause(InputAction.CallbackContext context)
        {
            if (_activeMode != InputMode.Gameplay || !context.performed)
                return;

            SetUI();
            PauseEvent?.Invoke();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (_activeMode != InputMode.Gameplay)
                return;

            if (context.performed)
            {
                if (_isSprinting)
                    return;

                _isSprinting = true;
                SprintEvent?.Invoke(true);
                return;
            }

            if (!context.canceled || !_isSprinting)
                return;

            _isSprinting = false;
            SprintEvent?.Invoke(false);
        }

        public void OnResume(InputAction.CallbackContext context)
        {
            if (_activeMode != InputMode.UI || !context.performed)
                return;

            SetGameplay();
            ResumeEvent?.Invoke();
        }
    }
#else
    public sealed class InputReader : ScriptableObject
    {
        public event Action<Vector2> MoveEvent;
        public event Action JumpEvent;
        public event Action PauseEvent;
        public event Action ResumeEvent;
        public event Action<bool> SprintEvent;

        public void SetGameplay()
        {
        }

        public void SetUI()
        {
        }
    }
#endif
}
