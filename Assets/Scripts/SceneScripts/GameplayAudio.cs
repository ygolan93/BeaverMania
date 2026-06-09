using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.Audio
{
    /// <summary>
    /// Lightweight one-shot helper with per-channel cooldowns. Does not replace <see cref="AudioScript"/> yet.
    /// </summary>
    public static class GameplayAudio
    {
        static readonly Dictionary<string, float> LastPlayTimeByChannel = new Dictionary<string, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SubscribeSceneClear()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearAllThrottles();
        }

        public static bool TryPlayOneShot(
            AudioSource source,
            AudioClip clip,
            string channel,
            float minInterval = 0.08f,
            float volumeScale = 1f,
            float pitch = 1f)
        {
            if (source == null || clip == null || string.IsNullOrEmpty(channel))
                return false;

            if (source.outputAudioMixerGroup == null)
                return false;

            float now = Time.unscaledTime;
            if (LastPlayTimeByChannel.TryGetValue(channel, out float lastPlay) && now - lastPlay < minInterval)
                return false;

            LastPlayTimeByChannel[channel] = now;
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            return true;
        }

        public static bool WasChannelPlayedWithin(string channel, float withinSeconds)
        {
            if (string.IsNullOrEmpty(channel) || withinSeconds <= 0f)
                return false;

            if (!LastPlayTimeByChannel.TryGetValue(channel, out float lastPlay))
                return false;

            return Time.unscaledTime - lastPlay < withinSeconds;
        }

        public static void ClearChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return;

            LastPlayTimeByChannel.Remove(channel);
        }

        public static void ClearAllThrottles()
        {
            LastPlayTimeByChannel.Clear();
        }
    }
}
