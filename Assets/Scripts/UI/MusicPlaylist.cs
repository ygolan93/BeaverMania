using System.Collections;
using UnityEngine;

namespace Beavermania.Audio
{
    public class MusicPlaylist : MonoBehaviour
    {
        public AudioSource MusicSource;
        [SerializeField] AudioClip[] MusicClip;
        [SerializeField] float crossfadeDuration = 0.5f;
        int currentSong = 0;
        int previousSong = -1;
        Coroutine playlistCoroutine;
        Coroutine crossfadeCoroutine;
        float baseVolume = 1f;
        bool musicPausedByGameplay;

        private void Start()
        {
            if (ShouldAbortAsDuplicateInstance())
                return;

            MusicSource = GetComponent<AudioSource>();
            if (MusicSource == null)
            {
                Debug.LogError("No AudioSource component found on the GameObject.");
                return;
            }
            if (MusicClip.Length == 0)
            {
                Debug.LogError("No music clips assigned in the MusicClip array.");
                return;
            }

            baseVolume = MusicSource.volume;
            if (MusicSource.isPlaying && MusicSource.clip != null)
            {
                SyncCurrentSongIndexFromPlayingClip();
                previousSong = currentSong;
                StartPlaylist();
                return;
            }

            StartPlaylist();
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

        void SyncCurrentSongIndexFromPlayingClip()
        {
            AudioClip playingClip = MusicSource.clip;
            for (int i = 0; i < MusicClip.Length; i++)
            {
                if (MusicClip[i] == playingClip)
                {
                    currentSong = i;
                    return;
                }
            }
        }

        private void StartPlaylist()
        {
            if (playlistCoroutine != null)
                StopCoroutine(playlistCoroutine);

            playlistCoroutine = StartCoroutine(Playlist());
        }

        IEnumerator Playlist()
        {
            while (true)
            {
                if (musicPausedByGameplay)
                {
                    yield return null;
                    continue;
                }

                if (currentSong != previousSong)
                {
                    PlayCurrentSong();
                    previousSong = currentSong;
                }

                if (!musicPausedByGameplay && !MusicSource.isPlaying && MusicSource.clip != null)
                {
                    float clipLength = MusicSource.clip.length;
                    if (clipLength > 0f && MusicSource.time >= clipLength - 0.1f)
                        MusicSource.Play();
                }

                yield return null;
            }
        }

        private void PlayCurrentSong()
        {
            MusicSource.clip = MusicClip[currentSong];
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
            if (!MusicSource.isPlaying)
                MusicSource.Play();
        }

        public void ChangeSong(int newSongIndex)
        {
            if (MusicSource == null)
                return;

            if (newSongIndex >= 0 && newSongIndex < MusicClip.Length)
            {
                if (newSongIndex == currentSong)
                    return;

                currentSong = newSongIndex;
                if (crossfadeCoroutine != null)
                    StopCoroutine(crossfadeCoroutine);

                crossfadeCoroutine = StartCoroutine(CrossfadeToCurrentSong());
            }
            else
            {
                Debug.LogWarning("Invalid song index: " + newSongIndex);
            }
        }

        IEnumerator CrossfadeToCurrentSong()
        {
            float duration = Mathf.Max(0.01f, crossfadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                MusicSource.volume = Mathf.Lerp(baseVolume, 0f, t);
                yield return null;
            }

            PlayCurrentSong();
            previousSong = currentSong;
            elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                MusicSource.volume = Mathf.Lerp(0f, baseVolume, t);
                yield return null;
            }

            MusicSource.volume = baseVolume;
            crossfadeCoroutine = null;

            if (playlistCoroutine == null)
                StartPlaylist();
        }
    }
}
