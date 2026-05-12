using Beavermania.Core.Input;
using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    public class PauseController : MonoBehaviour
    {
        public bool ActivePause = false;

        GameObject pauseMenu;
        GameObject question;
        global::Behaviour player;

        public void Bind(GameObject pauseMenu, GameObject question, global::Behaviour player)
        {
            this.pauseMenu = pauseMenu;
            this.question = question;
            this.player = player;

            ActivePause = false;
            ApplyPauseState();
        }

        public void Pause()
        {
            SetPaused(true);
        }

        public void ResumeIfPaused()
        {
            if (ActivePause)
                SetPaused(false);
        }

        public void HideQuestion()
        {
            SetQuestionVisible(false);
        }

        public void ChangeBolean()
        {
            SetPaused(!ActivePause);
        }

        void Update()
        {
            if (PlayerInputReader.WasPausePressed())
                ChangeBolean();
        }

        void SetPaused(bool paused)
        {
            ActivePause = paused;
            ApplyPauseState();
        }

        void ApplyPauseState()
        {
            if (ActivePause)
            {
                SetMenuVisible(true);
                GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.PauseMenu, true);

                if (player != null)
                    player.ShowCursor();

                return;
            }

            SetMenuVisible(false);
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.PauseMenu, false);
            SetQuestionVisible(false);

            if (player != null && !player.IsGameplayInputLocked())
                player.HideCursor();
        }

        void SetMenuVisible(bool visible)
        {
            if (pauseMenu != null)
                pauseMenu.SetActive(visible);
        }

        void SetQuestionVisible(bool visible)
        {
            if (question != null)
                question.SetActive(visible);
        }

        void OnDisable()
        {
            if (!ActivePause)
                return;

            ActivePause = false;
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.PauseMenu, false);
        }
    }
}
