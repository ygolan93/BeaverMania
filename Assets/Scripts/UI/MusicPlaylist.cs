using System.Collections;
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
    bool musicPaused;

    private void Start()
    {
        if (MusicSource == null)
        {
            MusicSource = GetComponent<AudioSource>();
        }

        if (!ValidatePlaylist())
        {
            return;
        }

        ValidateDuplicateMusicInstances();
        StartPlaylist();
    }

    internal void WarnIfDuplicateMusicObjectExists()
    {
        var musicObjects = GameObject.FindGameObjectsWithTag(MusicTag);
        for (var i = 0; i < musicObjects.Length; i++)
        {
            var musicObject = musicObjects[i];
            if (musicObject != null && musicObject != gameObject && musicObject.activeInHierarchy)
            {
                BuildSafeLogger.WarnOnce("MusicPlaylist.DuplicateActiveMusic", "Another active object tagged Music exists: " + musicObject.name, this, missingTag: MusicTag);
                return;
            }
        }
    }

    private void StartPlaylist()
    {
        if (!HasPlaylistSource())
        {
            return;
        }

        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }

        musicPaused = false;
        playlistCoroutine = StartCoroutine(Playlist());
    }

    IEnumerator Playlist()
    {
        while (HasPlaylistSource())
        {
            if (!TryGetCurrentClip(out var clip))
            {
                yield break;
            }

            if (currentSong != previousSong || MusicSource.clip != clip)
            {
                PlayCurrentSong(clip);
                previousSong = currentSong;
            }
            else if (!MusicSource.isPlaying && !musicPaused)
            {
                MusicSource.Play();
            }

            yield return new WaitWhile(() => HasPlaylistSource() && currentSong == previousSong && (MusicSource.isPlaying || musicPaused));
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
        if (!HasPlayableMusic())
        {
            return;
        }

        musicPaused = true;
        MusicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (!HasPlayableMusic())
        {
            return;
        }

        musicPaused = false;
        MusicSource.Play();
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

    bool ValidatePlaylist()
    {
        bool valid = true;

        if (MusicSource == null)
        {
            BuildSafeLogger.ErrorOnce("MusicPlaylist.MissingAudioSource", "No AudioSource component found on the GameObject.", this, missingField: nameof(MusicSource));
            valid = false;
        }

        if (MusicClip == null || MusicClip.Length == 0)
        {
            BuildSafeLogger.ErrorOnce("MusicPlaylist.NoMusicClips", "No music clips assigned in the MusicClip array.", this, missingField: nameof(MusicClip));
            return false;
        }

        for (int i = 0; i < MusicClip.Length; i++)
        {
            if (MusicClip[i] == null)
            {
                BuildSafeLogger.WarnOnce("MusicPlaylist.NullClip." + i, "Null music clip at index: " + i, this, missingField: nameof(MusicClip));
            }
        }

        return valid;
    }

    bool HasPlaylistSource()
    {
        return MusicSource != null && MusicClip != null && MusicClip.Length > 0;
    }

    bool HasPlayableMusic()
    {
        return HasPlaylistSource() && MusicSource.clip != null;
    }

    bool TryGetCurrentClip(out AudioClip clip)
    {
        clip = null;
        if (MusicClip == null || currentSong < 0 || currentSong >= MusicClip.Length)
        {
            return false;
        }

        clip = MusicClip[currentSong];
        if (clip == null)
        {
            BuildSafeLogger.WarnOnce("MusicPlaylist.NullClip." + currentSong, "Null music clip at index: " + currentSong, this, missingField: nameof(MusicClip));
            return false;
        }

        return true;
    }

    void ValidateDuplicateMusicInstances()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var playlists = FindObjectsOfType<MusicPlaylist>();
        var taggedMusicObjects = GameObject.FindGameObjectsWithTag("Music");
        if (playlists.Length > 1 || taggedMusicObjects.Length > 1)
        {
            BuildSafeLogger.WarnOnce(
                "MusicPlaylist.DuplicatePersistentMusic",
                "Duplicate active music detected. MusicPlaylist count=" + playlists.Length + "; Music tag count=" + taggedMusicObjects.Length + ".",
                this);
        }
#endif
    }
}
