using Beavermania.Audio;
using Beavermania.Core.GameFlow;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.UI.Menus
{
    public class UIMenu : MonoBehaviour
    {
        const string MasterVolumeKey = AudioVolumeSettings.MasterVolumePrefKey;

        [SerializeField] public GameObject PauseMenu;
        [SerializeField] public GameObject Question;
        [SerializeField] public BeaverPlayer Player;
        [SerializeField] Slider volumeSlider;
        [SerializeField] AudioSource Music;
        [SerializeField] Button restartLastCheckpointButton;
        [SerializeField] GameObject restartCheckpointConfirmationPanel;

        PauseController pauseController;
        bool loggedMissingPlayer;
        bool loggedMissingMusic;

        PauseController PauseController
        {
            get
            {
                if (pauseController == null)
                    pauseController = GetComponent<PauseController>();

                if (pauseController == null)
                    pauseController = gameObject.AddComponent<PauseController>();

                return pauseController;
            }
        }

        private void Start()
        {
            if (Player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    Player = playerObject.GetComponent<BeaverPlayer>();

                if (Player == null)
                    LogMissingReference(nameof(Player), ref loggedMissingPlayer);
            }

            if (Player != null && Player.seekMusic == true && Music == null)
            {
                GameObject musicObject = GameObject.FindGameObjectWithTag("Music");
                if (musicObject != null)
                    Music = musicObject.GetComponent<AudioSource>();

                if (Music == null)
                    LogMissingReference(nameof(Music), ref loggedMissingMusic);
            }

            PauseController.Bind(PauseMenu, Question, Player);
            EnsurePauseMenuOpenListener();
            RefreshRestartCheckpointButtonState();
            InitializeVolumeSlider();
        }

        void InitializeVolumeSlider()
        {
            if (volumeSlider == null)
                return;

            float savedMaster = AudioVolumeSettings.GetSavedMaster();
            volumeSlider.SetValueWithoutNotify(savedMaster);
            ApplyMasterVolume(savedMaster);
        }

        public void Pause()
        {
            PauseController.Pause();
            RefreshRestartCheckpointButtonState();
        }

        public void ChangeBolean()
        {
            PauseController.ChangeBolean();
            if (PauseController.ActivePause)
                RefreshRestartCheckpointButtonState();
        }

        public void RestartCheckpointFromMenu()
        {
            if (Player == null)
                return;

            if (Player.Lives <= 1)
                return;

            Player.StartCoroutine(RunRestartCheckpointAfterPauseUiChain(PauseController, Player));
        }

        public void TryShowRestartCheckpointConfirmation()
        {
            if (Player == null || Player.Lives <= 1)
                return;

            if (restartCheckpointConfirmationPanel != null)
                restartCheckpointConfirmationPanel.SetActive(true);
            if (Question != null)
                Question.SetActive(true);
        }

        public void ExecuteConfirmedRestartCheckpointFlow()
        {
            if (Player == null || Player.Lives <= 1)
                return;

            Player.StartCoroutine(RunRestartCheckpointAfterPauseUiChain(PauseController, Player));
            PauseController.ChangeBolean();
            if (restartCheckpointConfirmationPanel != null)
                restartCheckpointConfirmationPanel.SetActive(false);
            if (Question != null)
                Question.SetActive(false);
        }

        public void RefreshRestartCheckpointButtonState()
        {
            if (restartLastCheckpointButton == null)
                return;

            restartLastCheckpointButton.interactable = Player != null && Player.Lives > 1;
        }

        void EnsurePauseMenuOpenListener()
        {
            if (PauseMenu == null)
                return;

            if (PauseMenu.GetComponent<UIMenuPauseMenuOpenHook>() == null)
            {
                var hook = PauseMenu.AddComponent<UIMenuPauseMenuOpenHook>();
                hook.Initialize(this);
            }
        }

        static IEnumerator RunRestartCheckpointAfterPauseUiChain(PauseController pauseController, BeaverPlayer player)
        {
            yield return new WaitForEndOfFrame();

            if (pauseController == null || player == null)
                yield break;

            pauseController.HideQuestion();
            pauseController.ResumeIfPaused();
            player.RestartCheckpoint();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void MainMenu()
        {
            SceneRestartController.LoadSceneSingle("Menu");
        }

        public void Volume()
        {
            if (volumeSlider == null)
                return;

            float value = Mathf.Clamp01(volumeSlider.value);
            ApplyMasterVolume(value);
        }

        static void ApplyMasterVolume(float linearVolume)
        {
            if (AudioVolumeSettings.Instance != null)
            {
                AudioVolumeSettings.Instance.ApplyMasterVolumeFromMenu(linearVolume);
                return;
            }

            GameObject musicObject = GameObject.FindGameObjectWithTag("Music");
            if (musicObject == null)
                return;

            musicObject.SendMessage("ApplyMasterVolumeFromMenu", linearVolume, SendMessageOptions.DontRequireReceiver);
        }

        void LogMissingReference(string referenceName, ref bool logged)
        {
            if (logged)
                return;

            logged = true;
#if DEVELOPMENT_BUILD
            Debug.LogWarning($"{nameof(UIMenu)} could not resolve {referenceName} fallback.", this);
#endif
        }
    }

    sealed class UIMenuPauseMenuOpenHook : MonoBehaviour
    {
        UIMenu owner;

        public void Initialize(UIMenu owner)
        {
            this.owner = owner;
        }

        void OnEnable()
        {
            owner?.RefreshRestartCheckpointButtonState();
        }
    }
}
