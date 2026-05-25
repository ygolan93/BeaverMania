using Beavermania.Audio;
using Beavermania.Core.Input;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    public class PauseController : MonoBehaviour
    {
        public bool ActivePause = false;

        GameObject pauseMenu;
        GameObject question;
        BeaverPlayer player;

        public void Bind(GameObject pauseMenu, GameObject question, BeaverPlayer player)
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
                PauseBackgroundMusic();

                if (player != null)
                    player.ShowCursor();

                return;
            }

            SetMenuVisible(false);
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.PauseMenu, false);
            SetQuestionVisible(false);
            ResumeBackgroundMusic();

            if (player != null && !player.IsGameplayInputLocked())
                player.HideCursor();
        }

        void PauseBackgroundMusic()
        {
            MusicPlaylist playlist = ResolveMusicPlaylist();
            if (playlist != null)
                playlist.StopMusic();
        }

        void ResumeBackgroundMusic()
        {
            MusicPlaylist playlist = ResolveMusicPlaylist();
            if (playlist != null)
                playlist.ResumeMusic();
        }

        MusicPlaylist ResolveMusicPlaylist()
        {
            if (player != null && player.Music != null)
                return player.Music;

            GameObject musicObject = GameObject.FindGameObjectWithTag("Music");
            if (musicObject == null)
                return null;

            return musicObject.GetComponent<MusicPlaylist>();
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
