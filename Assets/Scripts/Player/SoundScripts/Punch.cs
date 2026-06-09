using UnityEngine;

namespace Beavermania.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class Punch : MonoBehaviour
    {
        [SerializeField] AudioClip[] audioClip;
        AudioSource punch;

        void Awake()
        {
            punch = GetComponent<AudioSource>();
            AudioSourceRouting.EnsureRoute(punch, AudioSourceRoute.Sfx);
        }

        void OnEnable()
        {
            AudioSourceRouting.EnsureRoute(punch, AudioSourceRoute.Sfx);
        }

        public void Hit()
        {
            if (!AudioSourceRouting.EnsureRoute(punch, AudioSourceRoute.Sfx))
                return;

            AudioClip clip = GetRandomClip();
            if (clip == null)
                return;

            punch.PlayOneShot(clip);
        }

        AudioClip GetRandomClip()
        {
            if (audioClip == null || audioClip.Length == 0)
                return null;

            int index = Random.Range(0, audioClip.Length);
            return audioClip[index];
        }
    }
}
