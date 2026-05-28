using Beavermania.Display;
using UnityEngine;

namespace Beavermania.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioScript : MonoBehaviour
    {
        [SerializeField] AudioClip[] audioClip;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioSource audioEffects;
        [SerializeField] FootstepVfxEmitter footstepVfxEmitter;

        void Awake()
        {
            if (footstepVfxEmitter == null)
                footstepVfxEmitter = GetComponentInParent<FootstepVfxEmitter>();
            if (footstepVfxEmitter == null)
                footstepVfxEmitter = gameObject.AddComponent<FootstepVfxEmitter>();
        }

        public void SwordSwing3()
        {
            if (audioSource == null || audioClip == null || audioClip.Length <= 18)
                return;

            audioSource.clip = audioClip[18];
            audioSource.volume = 0.95f;
            GameplayAudio.TryPlayOneShot(audioSource, audioClip[18], "player.swordswing", 0.12f, 0.95f, 1f);
        }
        public void SwordSwing2()
        {
            if (audioSource == null || audioClip == null || audioClip.Length <= 17)
                return;

            audioSource.clip = audioClip[17];
            audioSource.volume = 0.95f;
            GameplayAudio.TryPlayOneShot(audioSource, audioClip[17], "player.swordswing", 0.12f, 0.95f, 1f);
        }
        public void SwordSwing1()
        {
            if (audioSource == null || audioClip == null || audioClip.Length <= 16)
                return;

            audioSource.clip = audioClip[16];
            audioSource.volume = 0.95f;
            GameplayAudio.TryPlayOneShot(audioSource, audioClip[16], "player.swordswing", 0.12f, 0.95f, 1f);
        }
        public void Error()
        {
            audioEffects.clip = audioClip[15];
            audioEffects.volume = 1f;
            audioEffects.pitch = 1f;
            audioEffects.PlayOneShot(audioClip[15]);
        }
        public void PickItem()
        {
            audioEffects.clip = audioClip[14];
            audioEffects.volume = 1f;
            audioEffects.pitch = 1f;
            audioEffects.PlayOneShot(audioClip[14]);
        }
        public void SwitchItem()
        {
            audioSource.clip = audioClip[13];
            audioSource.volume = 1f;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(audioClip[13]);
        }


        public void ArrowShoot()
        {
            audioEffects.clip = audioClip[12];
            audioEffects.volume = 1f;
            audioEffects.pitch = 1f;
            audioEffects.PlayOneShot(audioClip[12]);
        }
        public void ArrowDraw()
        {
            audioSource.clip = audioClip[11];
            audioSource.volume = 1f;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(audioClip[11]);
        }
        public void Drink()
        {
            audioSource.clip = audioClip[10];
            audioSource.volume = 1f;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(audioClip[10]);
        }
        public void Eat()
        {
            audioEffects.clip = audioClip[9];
            audioEffects.volume = 1f;
            audioEffects.pitch = 1f;
            audioEffects.PlayOneShot(audioClip[9]);
        }
        public void Roll()
        {
            audioSource.clip = audioClip[8];
            audioSource.volume = 1f;
            audioSource.pitch = 0.8f;
            audioSource.PlayOneShot(audioClip[8]);
        }
        public void PickUp2()
        {
            audioSource.clip = audioClip[7];
            audioSource.volume = 1f;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(audioClip[7]);
        }
        public void PickUp1()
        {
            audioSource.clip = audioClip[6];
            audioSource.volume = 1f;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(audioClip[6]);
        }
        public void Coin()
        {
            if (audioEffects == null || audioClip == null || audioClip.Length <= 5)
                return;

            audioEffects.clip = audioClip[5];
            audioEffects.volume = 1f;
            GameplayAudio.TryPlayOneShot(audioEffects, audioClip[5], "player.coin", 0.08f, 1f, 1f);
        }
        public void Heal()
        {
            audioEffects.clip = audioClip[4];
            audioEffects.volume = 1f;
            audioEffects.pitch = 1f;
            audioEffects.PlayOneShot(audioClip[4]);
        }
        public void Slide()
        {
            audioEffects.clip = audioClip[3];
            audioEffects.volume = 1f;
            audioEffects.pitch = 0.7f;
            audioEffects.PlayOneShot(audioClip[3]);
        }
        public void Wind()
        {
            if (audioSource == null || audioClip == null || audioClip.Length <= 2)
                return;

            audioSource.clip = audioClip[2];
            audioSource.volume = 0.9f;
            GameplayAudio.TryPlayOneShot(audioSource, audioClip[2], "player.wind", 0.15f, 0.9f, 1f);
        }
        public void Step()
        {
            if (footstepVfxEmitter != null)
                footstepVfxEmitter.PlayStep();

            if (audioSource == null || audioClip == null || audioClip.Length <= 1)
                return;

            audioSource.clip = audioClip[1];
            audioSource.volume = 0.6f;
            audioSource.pitch = 0.8f;
            audioSource.PlayOneShot(audioClip[1]);
        }

        public void Jump()
        {
            if (audioEffects == null || audioClip == null || audioClip.Length == 0)
                return;

            audioEffects.clip = audioClip[0];
            audioEffects.volume = 0.65f;
            GameplayAudio.TryPlayOneShot(audioEffects, audioClip[0], "player.jump", 0.2f, 0.55f, 0.8f);
        }



    }
}
