using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlaylist : MonoBehaviour
{
    // GameMusic.prefab persists music only; it is not a runtime bootstrap owner.
    public AudioSource MusicSource;
    [SerializeField] AudioClip[] MusicClip;
    private int currentSong = 0;
    private int previousSong = -1;
    private Coroutine playlistCoroutine;

    private void Start()
    {
        if (MusicSource == null)
        {
            MusicSource = GetComponent<AudioSource>();
        }

        if (MusicSource == null)
        {
            BuildSafeLogger.ErrorOnce("MusicPlaylist.MissingAudioSource", "No AudioSource component found on the GameObject.", this, missingField: nameof(MusicSource));
            return;
        }
        if (MusicClip.Length == 0)
        {
            BuildSafeLogger.ErrorOnce("MusicPlaylist.NoMusicClips", "No music clips assigned in the MusicClip array.", this, missingField: nameof(MusicClip));
            return;
        }
        StartPlaylist();
    }

    private void StartPlaylist()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }
        playlistCoroutine = StartCoroutine(Playlist());
    }

    IEnumerator Playlist()
    {
        while (true)
        {
            if (currentSong != previousSong)
            {
                PlayCurrentSong();
                previousSong = currentSong;
            }

            // Check if the current clip has finished playing
            if (!MusicSource.isPlaying)
            {
                // Replay the current song
                MusicSource.Play();
            }

            yield return null; // Wait until the next frame to recheck
        }
    }

    private void PlayCurrentSong()
    {
        MusicSource.clip = MusicClip[currentSong];
        MusicSource.Play();
    }

    public void StopMusic()
    {
        if (MusicSource == null)
        {
            return;
        }

        MusicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (MusicSource == null)
        {
            return;
        }

        MusicSource.Play();
    }

    public void ChangeSong(int newSongIndex)
    {
        if (MusicSource == null || MusicClip == null || MusicClip.Length == 0)
        {
            return;
        }

        if (newSongIndex >= 0 && newSongIndex < MusicClip.Length)
        {
            currentSong = newSongIndex;
            StartPlaylist();
        }
        else
        {
            BuildSafeLogger.WarnOnce("MusicPlaylist.InvalidSongIndex." + newSongIndex, "Invalid song index: " + newSongIndex, this);
        }
    }
}
