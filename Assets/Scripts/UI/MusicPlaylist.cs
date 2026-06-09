using System.Collections;
using UnityEngine;

namespace Beavermania.Audio
{
    public class MusicPlaylist : MonoBehaviour
    {
        public AudioSource MusicSource;
        [SerializeField] AudioClip[] MusicClip;
        [SerializeField] float crossfadeDuration = 0.5f;

        int currentClipIndex;
        Coroutine playlistLoopCoroutine;
        Coroutine crossfadeCoroutine;
        float baseVolume = 1f;
        bool musicPausedByGameplay;
        bool manualTransitionInProgress;

        void Awake()
        {
            if (ShouldAbortAsDuplicateInstance())
            {
                enabled = false;
                return;
            }

            ResolveMusicSource();
            EnsureMusicRouting();
        }

        void Start()
        {
            if (!enabled || ShouldAbortAsDuplicateInstance())
                return;

            ResolveMusicSource();
            if (MusicSource == null || !EnsureMusicRouting())
            {
                LogPlaylistWarning("GameMusic is missing a routed Music AudioSource.");
                return;
            }

            if (!HasAnyValidClip())
            {
                LogPlaylistWarning("No music clips assigned to MusicPlaylist.");
                return;
            }

            baseVolume = MusicSource.volume;

            if (MusicSource.isPlaying && MusicSource.clip != null)
            {
                SyncCurrentClipIndexFromPlayingClip();
                EnsurePlaylistLoopRunning();
                return;
            }

            currentClipIndex = FindFirstValidClipIndex(0);
            PlayClipAtIndex(currentClipIndex);
            EnsurePlaylistLoopRunning();
        }

        void ResolveMusicSource()
        {
            if (MusicSource == null)
                MusicSource = GetComponent<AudioSource>();
        }

        bool EnsureMusicRouting()
        {
            if (MusicSource == null)
                return false;

            return AudioSourceRouting.EnsureRoute(MusicSource, AudioSourceRoute.Music, overwriteExisting: true);
        }

        bool ShouldAbortAsDuplicateInstance()
        {
            GameObject[] musicObjects = GameObject.FindGameObjectsWithTag("Music");
            for (int i = 0; i < musicObjects.Length; i++)
            {
                GameObject other = musicObjects[i];
                if (other == gameObject)
                    continue;

                if (other.scene.name == "DontDestroyOnLoad")
                    return true;

                AudioSource otherSource = other.GetComponent<AudioSource>();
                if (otherSource != null && otherSource.isPlaying)
                    return true;
            }

            return false;
        }

        void SyncCurrentClipIndexFromPlayingClip()
        {
            AudioClip playingClip = MusicSource.clip;
            for (int i = 0; i < MusicClip.Length; i++)
            {
                if (MusicClip[i] == playingClip)
                {
                    currentClipIndex = i;
                    return;
                }
            }
        }

        bool HasAnyValidClip()
        {
            if (MusicClip == null || MusicClip.Length == 0)
                return false;

            for (int i = 0; i < MusicClip.Length; i++)
            {
                if (MusicClip[i] != null)
                    return true;
            }

            return false;
        }

        int FindFirstValidClipIndex(int startIndex)
        {
            if (MusicClip == null || MusicClip.Length == 0)
                return -1;

            for (int offset = 0; offset < MusicClip.Length; offset++)
            {
                int index = (startIndex + offset) % MusicClip.Length;
                if (MusicClip[index] != null)
                    return index;
            }

            return -1;
        }

        int GetNextValidClipIndex(int fromIndex)
        {
            if (MusicClip == null || MusicClip.Length == 0)
                return -1;

            for (int offset = 1; offset <= MusicClip.Length; offset++)
            {
                int index = (fromIndex + offset) % MusicClip.Length;
                if (MusicClip[index] != null)
                    return index;
            }

            return -1;
        }

        void EnsurePlaylistLoopRunning()
        {
            if (playlistLoopCoroutine != null)
                return;

            playlistLoopCoroutine = StartCoroutine(PlaylistLoop());
        }

        IEnumerator PlaylistLoop()
        {
            while (true)
            {
                if (MusicSource == null)
                    yield break;

                if (musicPausedByGameplay || manualTransitionInProgress || crossfadeCoroutine != null)
                {
                    yield return null;
                    continue;
                }

                while (MusicSource.isPlaying)
                    yield return null;

                if (musicPausedByGameplay || manualTransitionInProgress || crossfadeCoroutine != null)
                {
                    yield return null;
                    continue;
                }

                int nextIndex = GetNextValidClipIndex(currentClipIndex);
                if (nextIndex < 0)
                {
                    yield return null;
                    continue;
                }

                currentClipIndex = nextIndex;
                PlayClipAtIndex(currentClipIndex);
            }
        }

        void PlayClipAtIndex(int clipIndex)
        {
            if (MusicSource == null || !EnsureMusicRouting() || MusicClip == null || clipIndex < 0 || clipIndex >= MusicClip.Length)
                return;

            AudioClip clip = MusicClip[clipIndex];
            if (clip == null)
                return;

            currentClipIndex = clipIndex;
            MusicSource.clip = clip;
            MusicSource.volume = baseVolume;
            MusicSource.Play();
        }

        public void StopMusic()
        {
            if (MusicSource == null || musicPausedByGameplay)
                return;

            musicPausedByGameplay = true;
            MusicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (MusicSource == null || !musicPausedByGameplay)
                return;

            musicPausedByGameplay = false;
            if (MusicSource.clip == null)
            {
                int resumeIndex = FindFirstValidClipIndex(currentClipIndex);
                if (resumeIndex >= 0)
                    PlayClipAtIndex(resumeIndex);
                return;
            }

            if (!MusicSource.isPlaying)
                MusicSource.Play();
        }

        public void ChangeSong(int newSongIndex)
        {
            if (MusicSource == null)
                return;

            if (newSongIndex < 0 || newSongIndex >= MusicClip.Length || MusicClip[newSongIndex] == null)
            {
                Debug.LogWarning("Invalid song index: " + newSongIndex);
                return;
            }

            if (newSongIndex == currentClipIndex && MusicSource.isPlaying)
                return;

            currentClipIndex = newSongIndex;
            if (crossfadeCoroutine != null)
                StopCoroutine(crossfadeCoroutine);

            crossfadeCoroutine = StartCoroutine(CrossfadeToCurrentSong());
        }

        IEnumerator CrossfadeToCurrentSong()
        {
            manualTransitionInProgress = true;
            float duration = Mathf.Max(0.01f, crossfadeDuration);
            float elapsed = 0f;
            float startVolume = MusicSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                MusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            PlayClipAtIndex(currentClipIndex);
            elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                MusicSource.volume = Mathf.Lerp(0f, baseVolume, t);
                yield return null;
            }

            MusicSource.volume = baseVolume;
            manualTransitionInProgress = false;
            crossfadeCoroutine = null;
            EnsurePlaylistLoopRunning();
        }

        static void LogPlaylistWarning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[MusicPlaylist] " + message);
#endif
        }
    }
}
