using UnityEngine;

namespace Beavermania.NPC
{
    [RequireComponent(typeof(AudioSource))]
    public class NPC_Audio : MonoBehaviour
    {
        [SerializeField] AudioClip[] audioClip;
        [SerializeField] AudioSource ActionSource;

        void Awake()
        {
            ResolveActionSource();
        }

        void OnEnable()
        {
            ResolveActionSource();
        }

        public void Beat()
        {
            PlayClip(0, 0.4f, 1f);
        }

        public void Sting()
        {
            PlayClip(1);
        }

        public void Breath()
        {
            PlayClip(2);
        }

        public void Crawling()
        {
            PlayClip(3);
        }

        public void LiteSwordDamage()
        {
            PlayClip(4, 0.1f, 1f);
        }

        public void HeavySwordDamage()
        {
            PlayClip(5, 1f, 0.8f);
        }

        void PlayClip(int clipIndex, float? volume = null, float? pitch = null)
        {
            AudioSource source = ResolveActionSource();
            if (source == null || !TryGetClip(clipIndex, out AudioClip clip))
                return;

            source.clip = clip;
            if (volume.HasValue)
                source.volume = volume.Value;
            if (pitch.HasValue)
                source.pitch = pitch.Value;
            source.PlayOneShot(clip);
        }

        AudioSource ResolveActionSource()
        {
            if (ActionSource == null)
                ActionSource = GetComponent<AudioSource>();

            return Beavermania.Audio.AudioSourceRouting.EnsureRoute(ActionSource, Beavermania.Audio.AudioSourceRoute.Enemy)
                ? ActionSource
                : null;
        }

        bool TryGetClip(int index, out AudioClip clip)
        {
            clip = null;
            if (audioClip == null || index < 0 || index >= audioClip.Length)
                return false;

            clip = audioClip[index];
            return clip != null;
        }
    }
}
