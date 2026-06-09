using UnityEngine;

namespace Beavermania.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class WoodRoll : MonoBehaviour
    {
        [SerializeField] AudioClip[] audioClip;
        AudioSource audioSource;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            AudioSourceRouting.EnsureRoute(audioSource, AudioSourceRoute.Sfx);
        }

        void OnEnable()
        {
            AudioSourceRouting.EnsureRoute(audioSource, AudioSourceRoute.Sfx);
        }

        void Wood()
        {
            if (!AudioSourceRouting.EnsureRoute(audioSource, AudioSourceRoute.Sfx))
                return;

            AudioClip clip = GetClip();
            if (clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }

        AudioClip GetClip()
        {
            if (audioClip == null || audioClip.Length == 0)
                return null;

            return audioClip[0];
        }
    }
}
