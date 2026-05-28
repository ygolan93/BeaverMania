using Beavermania.Audio;
using Beavermania.Core.GameFlow;
using Beavermania.UI.Tips;
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
        [SerializeField] Slider sfxVolumeSlider;
        [SerializeField] AudioSource Music;
        [SerializeField] Button restartLastCheckpointButton;
        [SerializeField] GameObject restartCheckpointConfirmationPanel;

        PauseController pauseController;
        bool loggedMissingPlayer;
        bool loggedMissingMusic;
        bool loggedMissingPauseMenu;

        void Awake()
        {
            pauseController = GetComponent<PauseController>();
            if (pauseController == null)
                pauseController = gameObject.AddComponent<PauseController>();
        }

        PauseController PauseController => pauseController;

        void Start()
        {
            if (PauseMenu == null)
                LogMissingReferenceError(nameof(PauseMenu), ref loggedMissingPauseMenu);

            if (Player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    Player = playerObject.GetComponent<BeaverPlayer>();

                if (Player == null)
                    LogMissingReferenceError(nameof(Player), ref loggedMissingPlayer);
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
            InitializeSfxVolumeSlider();
            InitializeTipsUi();
        }

        void InitializeTipsUi()
        {
            TipsService.EnsureForCanvas(gameObject, Player);
            TipsPauseMenuToggleInstaller.Ensure(PauseMenu);
        }

        void InitializeVolumeSlider()
        {
            if (volumeSlider == null)
                return;

            float savedMaster = AudioVolumeSettings.GetSavedMaster();
            volumeSlider.SetValueWithoutNotify(savedMaster);
            ApplyMasterVolume(savedMaster);
        }

        void InitializeSfxVolumeSlider()
        {
            EnsureSfxVolumeSliderReference();
            if (sfxVolumeSlider == null)
                return;

            float savedSfx = AudioVolumeSettings.GetSavedSfx();
            sfxVolumeSlider.SetValueWithoutNotify(savedSfx);
            ApplySfxVolume(savedSfx, false);
        }

        void EnsureSfxVolumeSliderReference()
        {
            if (sfxVolumeSlider != null)
                return;

            Transform searchRoot = PauseMenu != null ? PauseMenu.transform : transform;
            Transform existing = searchRoot.Find("Sfx Volume SLIDER");
            if (existing == null)
                existing = searchRoot.Find("MusicSfx Balance SLIDER");

            if (existing != null)
            {
                sfxVolumeSlider = existing.GetComponent<Slider>();
                if (sfxVolumeSlider != null)
                {
                    WireSfxVolumeSliderEvents();
                    return;
                }
            }

            if (volumeSlider == null)
                return;

            GameObject clone = Instantiate(volumeSlider.gameObject, volumeSlider.transform.parent);
            clone.name = "Sfx Volume SLIDER";
            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            RectTransform sourceRect = volumeSlider.GetComponent<RectTransform>();
            if (cloneRect != null && sourceRect != null)
                cloneRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -45f);

            sfxVolumeSlider = clone.GetComponent<Slider>();
            if (sfxVolumeSlider == null)
                return;

            WireSfxVolumeSliderEvents();
        }

        void WireSfxVolumeSliderEvents()
        {
            if (sfxVolumeSlider == null)
                return;

            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(_ => SfxVolume());
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

        public void SfxVolume()
        {
            if (sfxVolumeSlider == null)
                return;

            float value = Mathf.Clamp01(sfxVolumeSlider.value);
            ApplySfxVolume(value, true);
        }

        static void ApplySfxVolume(float linearVolume, bool save)
        {
            if (AudioVolumeSettings.Instance != null)
            {
                if (save)
                    AudioVolumeSettings.Instance.ApplySfxVolumeFromMenu(linearVolume);
                else
                    AudioVolumeSettings.Instance.ApplySfxVolume(linearVolume, false);
                return;
            }

            GameObject musicObject = GameObject.FindGameObjectWithTag("Music");
            if (musicObject == null)
                return;

            if (save)
                musicObject.SendMessage("ApplySfxVolumeFromMenu", linearVolume, SendMessageOptions.DontRequireReceiver);
            else
                musicObject.SendMessage("ApplySfxVolume", linearVolume, SendMessageOptions.DontRequireReceiver);
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

        void OnDisable()
        {
            if (pauseController == null)
                return;

            pauseController.ResumeIfPaused();
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

        void LogMissingReferenceError(string referenceName, ref bool logged)
        {
            if (logged)
                return;

            logged = true;
            Debug.LogError($"{nameof(UIMenu)} is missing required reference '{referenceName}'. Wire it on PlayerCanvas in the Inspector.", this);
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
