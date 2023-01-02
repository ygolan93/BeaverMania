using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlaylist : MonoBehaviour
{
    public AudioSource MusicSource;
    [SerializeField] AudioClip[] MusicClip;
    float TrackTimer = 0;
    int Song = 0;
    private void Start()
    {
        MusicSource = GetComponent<AudioSource>();
        StartCoroutine(Playlist());
    }

    IEnumerator Playlist()
    {
        for (int i = 0; i < MusicClip.Length; i++)
        {
            MusicSource.clip = MusicClip[i];
            MusicSource.Play();
            yield return new WaitForSeconds(MusicSource.clip.length);
        }
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
