using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class NPC_Audio : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClip;
    [SerializeField] AudioSource audioSource1;
    [SerializeField] AudioSource audioSource2;

    public void Beat()
    {
        audioSource1.clip = audioClip[0];
        audioSource1.volume = 0.4f;
        audioSource1.pitch = 1f;
        audioSource1.PlayOneShot(audioClip[0]);
    }
    public void Sting()
    {
        audioSource1.clip = audioClip[1];
        audioSource1.volume = 0.4f;
        audioSource1.pitch = 1f;
        audioSource1.PlayOneShot(audioClip[1]);
    }
    public void Buzz()
    {
        audioSource2.clip = audioClip[2];
        audioSource2.volume = 0.02f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[2]);
    }

}
