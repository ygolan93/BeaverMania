using Beavermania.Audio;
using Beavermania.Core.Input;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    public class PauseController : MonoBehaviour
    {
        public bool ActivePause { get; private set; }

        GameObject pauseMenu;
        GameObject question;
        BeaverPlayer player;
        bool isBound;
        bool loggedMissingPauseMenu;
        bool loggedMissingPlayer;

        public void Bind(GameObject pauseMenu, GameObject question, BeaverPlayer player)
        {
            this.pauseMenu = pauseMenu;
            this.question = question;
            this.player = player;
            isBound = pauseMenu != null && player != null;

            if (pauseMenu == null)
                LogMissingReference(nameof(pauseMenu), ref loggedMissingPauseMenu);
            if (player == null)
                LogMissingReference(nameof(player), ref loggedMissingPlayer);

            ActivePause = false;
            ApplyPauseState();
        }

        public void Pause()
        {
            if (!isBound)
                return;

            SetPaused(true);
        }

        public void ResumeIfPaused()
        {
            if (!ActivePause)
                return;

            SetPaused(false);
        }

        public void HideQuestion()
        {
            SetQuestionVisible(false);
        }

        public void ChangeBolean()
        {
            if (!isBound)
                return;

            SetPaused(!ActivePause);
        }

        void Update()
        {
            if (!isBound)
                return;

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
            if (!isBound)
                return;

            if (ActivePause)
            {
                SetMenuVisible(true);
                GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.PauseMenu, true);
                PauseBackgroundMusic();
                player.ShowCursor();
                return;
            }

            SetMenuVisible(false);
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.PauseMenu, false);
            SetQuestionVisible(false);
            ResumeBackgroundMusic();

            if (!player.IsGameplayInputLocked())
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
            SetMenuVisible(false);
            SetQuestionVisible(false);
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.PauseMenu, false);
            ResumeBackgroundMusic();

            if (player != null && !player.IsGameplayInputLocked())
                player.HideCursor();
        }

        void LogMissingReference(string referenceName, ref bool logged)
        {
            if (logged)
                return;

            logged = true;
            Debug.LogError($"{nameof(PauseController)} requires a valid {referenceName} reference. Assign it on UIMenu (PlayerCanvas).", this);
        }
    }
}
