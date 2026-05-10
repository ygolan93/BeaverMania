using System.Collections;
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
        if (MusicSource == null)
        {
            MusicSource = GetComponent<AudioSource>();
        }

        if (MusicSource == null)
        {
            BuildSafeLogger.ErrorOnce("MusicPlaylist.MissingAudioSource", "No AudioSource component found on the GameObject.", this, missingField: nameof(MusicSource));
            return;
        }
        if (MusicClip == null || MusicClip.Length == 0)
        {
            BuildSafeLogger.ErrorOnce("MusicPlaylist.NoMusicClips", "No music clips assigned in the MusicClip array.", this, missingField: nameof(MusicClip));
            return;
        }
        StartPlaylist();
    }

    private void StartPlaylist()
    {
        if (MusicSource == null || MusicClip == null || MusicClip.Length == 0)
        {
            return;
        }

        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }
        playlistCoroutine = StartCoroutine(Playlist());
    }

    IEnumerator Playlist()
    {
        while (MusicSource != null && MusicClip != null && MusicClip.Length > 0)
        {
            if (currentSong < 0 || currentSong >= MusicClip.Length)
            {
                yield break;
            }

            AudioClip clip = MusicClip[currentSong];
            if (clip == null)
            {
                BuildSafeLogger.WarnOnce("MusicPlaylist.NullClip." + currentSong, "Null music clip at index: " + currentSong, this);
                yield break;
            }

            if (currentSong != previousSong || MusicSource.clip != clip)
            {
                PlayCurrentSong(clip);
                previousSong = currentSong;
            }
            else if (!MusicSource.isPlaying)
            {
                MusicSource.Play();
            }

            yield return new WaitForSeconds(Mathf.Max(0.05f, clip.length - MusicSource.time));
        }
    }

    private void PlayCurrentSong(AudioClip clip)
    {
        if (MusicSource == null || clip == null)
        {
            return;
        }

        MusicSource.clip = clip;
        MusicSource.Play();
    }

    public void StopMusic()
    {
        if (MusicSource != null)
        {
            MusicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (MusicSource != null)
        {
            MusicSource.Play();
        }
    }

    public void ChangeSong(int newSongIndex)
    {
        if (MusicClip != null && newSongIndex >= 0 && newSongIndex < MusicClip.Length)
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
