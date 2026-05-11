using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    public const string MusicVolumeKey = "settings.audio.musicVolume";
    public const string FullscreenKey = "settings.video.fullscreen";
    public const string QualityKey = "settings.video.quality";

    [SerializeField] AudioSource musicSource;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Dropdown qualityDropdown;

    void OnEnable()
    {
        LoadAndApplySettings();
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool enabled)
    {
        ApplyFullscreen(enabled);
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetQuality(int index)
    {
        index = ClampQualityIndex(index);
        ApplyQuality(index);
        PlayerPrefs.SetInt(QualityKey, index);
        PlayerPrefs.Save();
    }

    void LoadAndApplySettings()
    {
        var musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, GetDefaultMusicVolume());
        var fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        var quality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());

        ApplyMusicVolume(musicVolume);
        ApplyFullscreen(fullscreen);
        ApplyQuality(quality);
    }

    void ApplyMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        var source = GetMusicSource();
        if (source != null)
        {
            source.volume = value;
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(value);
        }
    }

    void ApplyFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(enabled);
        }
    }

    void ApplyQuality(int index)
    {
        index = ClampQualityIndex(index);
        QualitySettings.SetQualityLevel(index, true);

        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(index);
        }
    }

    float GetDefaultMusicVolume()
    {
        var source = GetMusicSource();
        return source != null ? source.volume : 1f;
    }

    AudioSource GetMusicSource()
    {
        if (musicSource != null)
        {
            return musicSource;
        }

        var musicObject = GameObject.FindGameObjectWithTag("Music");
        if (musicObject == null)
        {
            return null;
        }

        musicSource = musicObject.GetComponent<AudioSource>();
        return musicSource;
    }

    static int ClampQualityIndex(int index)
    {
        return Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
    }
}
