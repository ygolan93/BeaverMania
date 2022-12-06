using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class NPC_Audio : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClip;
    [SerializeField] AudioSource ActionSource;
    [SerializeField] AudioSource BuzzSource;

    public void Beat()
    {
        ActionSource.clip = audioClip[0];
        ActionSource.volume = 0.4f;
        ActionSource.pitch = 1f;
        ActionSource.PlayOneShot(audioClip[0]);
    }
    public void Sting()
    {
        ActionSource.clip = audioClip[1];
        ActionSource.volume = 0.7f;
        ActionSource.pitch = 1f;
        ActionSource.PlayOneShot(audioClip[1]);
    }
    public void StopBuzzing()
    {
        BuzzSource.Stop();
    }
    public void Buzz()
    {
        BuzzSource.Play();
    }

}
