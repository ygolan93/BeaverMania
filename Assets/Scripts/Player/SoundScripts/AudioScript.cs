using Beavermania.Display;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioScript : MonoBehaviour
    {
        const int JumpClipIndex = 0;
        const string GenericJumpChannel = "sfx.jump";
        const string PlayerJumpChannel = "player.jump";
        const float GenericJumpCooldown = 0.2f;
        const float GenericJumpVolumeScale = 0.55f;
        const float GenericJumpPitch = 0.8f;
        const float PlayerJumpCooldown = 0.16f;
        const float PlayerJumpVolumeScale = 0.42f;
        const float PlayerJumpPitchMin = 0.78f;
        const float PlayerJumpPitchMax = 0.84f;

        [SerializeField] AudioClip[] audioClip;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioSource audioEffects;
        [SerializeField] FootstepVfxEmitter footstepVfxEmitter;

        void Awake()
        {
            EnsureRoutes();

            if (footstepVfxEmitter != null)
                return;

            footstepVfxEmitter = GetComponentInParent<FootstepVfxEmitter>();
            if (footstepVfxEmitter != null)
                return;

            BeaverPlayer player = GetComponentInParent<BeaverPlayer>();
            if (player != null)
                footstepVfxEmitter = player.gameObject.AddComponent<FootstepVfxEmitter>();
        }

        void OnEnable()
        {
            EnsureRoutes();
        }

        public void SwordSwing3()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(18, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 0.95f;
            GameplayAudio.TryPlayOneShot(source, clip, "player.swordswing", 0.12f, 0.95f, 1f);
        }

        public void SwordSwing2()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(17, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 0.95f;
            GameplayAudio.TryPlayOneShot(source, clip, "player.swordswing", 0.12f, 0.95f, 1f);
        }

        public void SwordSwing1()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(16, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 0.95f;
            GameplayAudio.TryPlayOneShot(source, clip, "player.swordswing", 0.12f, 0.95f, 1f);
        }

        public void Error()
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(15, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void PickItem()
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(14, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void SwitchItem()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(13, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void ArrowShoot()
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(12, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void ArrowDraw()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(11, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void Drink()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(10, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void Eat()
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(9, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void Roll()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(8, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 0.8f;
            source.PlayOneShot(clip);
        }

        public void PickUp2()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(7, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void PickUp1()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(6, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void Coin()
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(5, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            GameplayAudio.TryPlayOneShot(source, clip, "player.coin", 0.08f, 1f, 1f);
        }

        public void Heal()
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(4, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }

        public void Slide()
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(3, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 1f;
            source.pitch = 0.7f;
            source.PlayOneShot(clip);
        }

        public void Wind()
        {
            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(2, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 0.9f;
            GameplayAudio.TryPlayOneShot(source, clip, "player.wind", 0.15f, 0.9f, 1f);
        }

        public void Step()
        {
            if (footstepVfxEmitter != null)
                footstepVfxEmitter.PlayStep();

            AudioSource source = ResolvePrimarySource();
            if (source == null || !TryGetClip(1, out AudioClip clip))
                return;

            source.clip = clip;
            source.volume = 0.6f;
            source.pitch = 0.8f;
            source.PlayOneShot(clip);
        }

        public void Jump()
        {
            TryPlayJumpSfx(GenericJumpChannel, GenericJumpCooldown, GenericJumpVolumeScale, GenericJumpPitch);
        }

        public bool PlayPlayerJumpSfx()
        {
            return TryPlayJumpSfx(
                PlayerJumpChannel,
                PlayerJumpCooldown,
                PlayerJumpVolumeScale,
                SamplePitch(PlayerJumpPitchMin, PlayerJumpPitchMax));
        }

        void EnsureRoutes()
        {
            AudioSourceRouting.EnsureRoute(ResolvePrimaryComponent(), AudioSourceRoute.Sfx);
            AudioSourceRouting.EnsureRoute(audioEffects, AudioSourceRoute.Sfx);
        }

        AudioSource ResolvePrimarySource()
        {
            AudioSource source = ResolvePrimaryComponent();
            return AudioSourceRouting.EnsureRoute(source, AudioSourceRoute.Sfx) ? source : null;
        }

        AudioSource ResolveEffectsSource()
        {
            return AudioSourceRouting.EnsureRoute(audioEffects, AudioSourceRoute.Sfx) ? audioEffects : null;
        }

        AudioSource ResolvePrimaryComponent()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            return audioSource;
        }

        bool TryGetClip(int index, out AudioClip clip)
        {
            clip = null;
            if (audioClip == null || index < 0 || index >= audioClip.Length)
                return false;

            clip = audioClip[index];
            return clip != null;
        }

        bool TryPlayJumpSfx(string channel, float minInterval, float volumeScale, float pitch)
        {
            AudioSource source = ResolveEffectsSource();
            if (source == null || !TryGetClip(JumpClipIndex, out AudioClip clip))
                return false;

            source.clip = clip;
            source.volume = 1f;
            return GameplayAudio.TryPlayOneShot(source, clip, channel, minInterval, volumeScale, pitch);
        }

        static float SamplePitch(float minPitch, float maxPitch)
        {
            return minPitch >= maxPitch ? minPitch : Random.Range(minPitch, maxPitch);
        }
    }
}
