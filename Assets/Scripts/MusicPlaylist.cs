using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlaylist : MonoBehaviour
{
    public AudioSource MusicSource;
    [SerializeField] AudioClip[] MusicClip;

    public void OutroSong()
    {
        MusicSource.clip = MusicClip[0];
        MusicSource.PlayOneShot(MusicClip[0]);
    }
    public void BeatNuts()
    {
        MusicSource.clip = MusicClip[1];
        MusicSource.PlayOneShot(MusicClip[1]);
    }
    public void StopMusic()
    {

        MusicSource.Pause();
    }
    public void ResumeMusic()
    {
        MusicSource.Play();
    }

}
