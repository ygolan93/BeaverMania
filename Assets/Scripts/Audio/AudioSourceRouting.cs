using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Beavermania.Audio
{
    public enum AudioSourceRoute
    {
        Music,
        Sfx,
        Enemy,
        UI,
    }

    public static class AudioSourceRouting
    {
        static readonly HashSet<int> LoggedMissingRoutes = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetWarnings()
        {
            LoggedMissingRoutes.Clear();
        }

        public static bool EnsureRoute(AudioSource source, AudioSourceRoute route, bool overwriteExisting = false)
        {
            if (source == null)
                return false;

            if (!overwriteExisting && source.outputAudioMixerGroup != null)
                return true;

            AudioMixerGroup group = ResolveGroup(route);
            if (group == null)
            {
                LogMissingRoute(source, route);
                return false;
            }

            source.outputAudioMixerGroup = group;
            return true;
        }

        static AudioMixerGroup ResolveGroup(AudioSourceRoute route)
        {
            AudioVolumeSettings settings = AudioVolumeSettings.GetRoutingCatalog();
            if (settings == null)
                return null;

            switch (route)
            {
                case AudioSourceRoute.Music:
                    return settings.MusicGroup;
                case AudioSourceRoute.Enemy:
                    return settings.EnemiesGroup != null ? settings.EnemiesGroup : settings.SfxGroup;
                case AudioSourceRoute.UI:
                    return settings.UiGroup != null ? settings.UiGroup : settings.SfxGroup;
                case AudioSourceRoute.Sfx:
                default:
                    return settings.SfxGroup;
            }
        }

        static void LogMissingRoute(AudioSource source, AudioSourceRoute route)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int sourceId = source.GetInstanceID();
            if (!LoggedMissingRoutes.Add(sourceId))
                return;

            Debug.LogWarning(
                $"[AudioSourceRouting] Could not resolve {route} mixer group for '{source.name}'. Playback will be skipped until AudioVolumeSettings is configured.",
                source);
#endif
        }
    }
}
