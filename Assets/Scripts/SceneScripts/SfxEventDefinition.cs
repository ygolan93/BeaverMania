using UnityEngine;

namespace Beavermania.Audio
{
    /// <summary>
    /// Designer-facing SFX definition for gradual migration off clip-index facades.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxEvent", menuName = "Beavermania/Audio/Sfx Event")]
    public sealed class SfxEventDefinition : ScriptableObject
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitchMin = 1f;
        [Range(0.5f, 1.5f)] public float pitchMax = 1f;
        [Min(0f)] public float minInterval = 0.08f;

        public float SamplePitch()
        {
            return pitchMin >= pitchMax ? pitchMin : Random.Range(pitchMin, pitchMax);
        }

        public bool TryPlay(AudioSource source, string channelOverride = null)
        {
            if (clip == null)
                return false;

            string channel = string.IsNullOrEmpty(channelOverride) ? name : channelOverride;
            return GameplayAudio.TryPlayOneShot(source, clip, channel, minInterval, volume, SamplePitch());
        }
    }
}
