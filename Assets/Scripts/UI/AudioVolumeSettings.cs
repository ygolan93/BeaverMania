using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Beavermania.Audio
{
    public class AudioVolumeSettings : MonoBehaviour
    {
        public const string MasterVolumePrefKey = "Beavermania.MasterVolume";
        public const string MusicVolumePrefKey = "Beavermania.MusicVolume";
        public const string SfxVolumePrefKey = "Beavermania.SfxVolume";
        public const string MusicSfxBalancePrefKey = "Beavermania.MusicSfxBalance";

        const string MasterVolumeParam = "MasterVolume";
        const string MusicVolumeParam = "MusicVolume";
        const string SfxVolumeParam = "SfxVolume";

        public const float DefaultMasterLinear = 1f;
        public const float DefaultMusicLinear = 0.8f;
        public const float DefaultSfxLinear = 1f;
        public const float DefaultMusicSfxBalance = 0.5f;
        public const float MusicSfxBalanceSwing = 0.25f;

        [SerializeField] AudioMixer audioMixer;

        public static AudioVolumeSettings Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            ApplyAllFromPlayerPrefs();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this)
                Instance = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != this || audioMixer == null)
                return;

            ApplyAllFromPlayerPrefs();
        }

        public void ApplyAllFromPlayerPrefs()
        {
            if (audioMixer == null)
                return;

            ApplyMasterVolume(GetSavedMaster(), false);
            ApplyMusicVolume(GetSavedMusic(), false);
            ApplySfxVolume(GetSavedSfx(), false);
            LogAppliedMixerVolumes();
        }

        public static float GetSavedMaster()
        {
            return GetSavedLinearVolume(MasterVolumePrefKey, DefaultMasterLinear);
        }

        public static float GetSavedMusic()
        {
            return GetSavedLinearVolume(MusicVolumePrefKey, DefaultMusicLinear);
        }

        public static float GetSavedSfx()
        {
            return GetSavedLinearVolume(SfxVolumePrefKey, DefaultSfxLinear);
        }

        public static float GetSavedMusicSfxBalance()
        {
            return GetSavedLinearVolume(MusicSfxBalancePrefKey, DefaultMusicSfxBalance);
        }

        public static void GetLinearVolumesForBalance(float balance, out float musicLinear, out float sfxLinear)
        {
            balance = Mathf.Clamp01(balance);
            float bias = (balance - DefaultMusicSfxBalance) * 2f;
            musicLinear = Mathf.Clamp01(DefaultMusicLinear + bias * MusicSfxBalanceSwing);
            sfxLinear = Mathf.Clamp01(DefaultSfxLinear - bias * MusicSfxBalanceSwing);
        }

        public void ApplyMusicSfxBalance(float balance, bool save = true)
        {
            balance = Mathf.Clamp01(balance);
            GetLinearVolumesForBalance(balance, out float musicLinear, out float sfxLinear);
            ApplyMusicVolume(musicLinear, false);
            ApplySfxVolume(sfxLinear, false);

            if (save)
            {
                PlayerPrefs.SetFloat(MusicSfxBalancePrefKey, balance);
                PlayerPrefs.Save();
            }
        }

        public void ApplyMusicSfxBalanceFromPrefs(bool save = false)
        {
            ApplyMusicSfxBalance(GetSavedMusicSfxBalance(), save);
        }

        public void ApplyMusicSfxBalanceFromMenu(float balance)
        {
            ApplyMusicSfxBalance(balance, true);
            LogAppliedMixerVolumes();
        }

        static float GetSavedLinearVolume(string prefKey, float defaultLinear)
        {
            if (!PlayerPrefs.HasKey(prefKey))
                return defaultLinear;

            return Mathf.Clamp01(PlayerPrefs.GetFloat(prefKey));
        }

        public void ApplyMasterVolumeFromMenu(float linearVolume)
        {
            ApplyMasterVolume(linearVolume, true);
            LogAppliedMixerVolumes();
        }

        public void ApplyMasterVolume(float linearVolume, bool save = true)
        {
            linearVolume = Mathf.Clamp01(linearVolume);
            SetMixerParameter(MasterVolumeParam, linearVolume);

            if (save)
            {
                PlayerPrefs.SetFloat(MasterVolumePrefKey, linearVolume);
                PlayerPrefs.Save();
            }

            AudioListener.volume = linearVolume;
        }

        public void ApplyMusicVolume(float linearVolume, bool save = true)
        {
            linearVolume = Mathf.Clamp01(linearVolume);
            SetMixerParameter(MusicVolumeParam, linearVolume);

            if (save)
            {
                PlayerPrefs.SetFloat(MusicVolumePrefKey, linearVolume);
                PlayerPrefs.Save();
            }
        }

        public void ApplySfxVolume(float linearVolume, bool save = true)
        {
            linearVolume = Mathf.Clamp01(linearVolume);
            SetMixerParameter(SfxVolumeParam, linearVolume);

            if (save)
            {
                PlayerPrefs.SetFloat(SfxVolumePrefKey, linearVolume);
                PlayerPrefs.Save();
            }
        }

        public void ApplySfxVolumeFromMenu(float linearVolume)
        {
            ApplySfxVolume(linearVolume, true);
            LogAppliedMixerVolumes();
        }

        void SetMixerParameter(string parameterName, float linearVolume)
        {
            if (audioMixer == null)
                return;

            audioMixer.SetFloat(parameterName, LinearToDecibels(linearVolume));
        }

        public static float LinearToDecibels(float linearVolume)
        {
            return linearVolume <= 0.0001f ? -80f : Mathf.Log10(linearVolume) * 20f;
        }

        public void LogAppliedMixerVolumes()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (audioMixer == null)
                return;

            LogMixerParameter(MasterVolumeParam, GetSavedMaster());
            LogMixerParameter(MusicVolumeParam, GetSavedMusic());
            LogMixerParameter(SfxVolumeParam, GetSavedSfx());
#endif
        }

        void LogMixerParameter(string parameterName, float savedLinear)
        {
            if (audioMixer.GetFloat(parameterName, out float decibels))
            {
                Debug.Log(
                    $"[AudioVolumeSettings] {parameterName}: savedLinear={savedLinear:F2}, appliedDb={decibels:F2}",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    $"[AudioVolumeSettings] Could not read mixer parameter '{parameterName}'.",
                    this);
            }
        }
    }
}