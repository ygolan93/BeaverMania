using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlaylist : MonoBehaviour
{
    public AudioSource MusicSource;
    [SerializeField] AudioClip[] MusicClip;
    private int currentSong = 0;
    private int previousSong = -1;
    private Coroutine playlistCoroutine;

    private void Start()
    {
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
        MusicSource.Pause();
    }

    public void ResumeMusic()
    {
        MusicSource.Play();
    }

    public void ChangeSong(int newSongIndex)
    {
        if (newSongIndex >= 0 && newSongIndex < MusicClip.Length)
        {
            currentSong = newSongIndex;
            StartPlaylist();
        }
        else
        {
            Debug.LogWarning("Invalid song index: " + newSongIndex);
        }
    }
}
