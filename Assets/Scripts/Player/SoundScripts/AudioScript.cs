using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class AudioScript : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClip;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource audioSource2;


    // Sound clips that are conditioned with animations

    public void BlahBlah()
    {
        audioSource.clip = audioClip[7];
        audioSource.volume = 7f;
        audioSource2.pitch = 1f;
        audioSource.PlayOneShot(audioClip[6]);
    }
    public void Coin()
    {
        audioSource.clip = audioClip[6];
        audioSource.volume = 7f;
        audioSource2.pitch = 1f;
        audioSource.PlayOneShot(audioClip[6]);
    }
    public void Heal()
    {
        audioSource2.clip = audioClip[5];
        audioSource2.volume = 1f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[5]);
    }
    private void Slide()
    {
        audioSource.clip = audioClip[4];
        audioSource.volume = 2f;
        audioSource.pitch = 0.7f;
        audioSource.PlayOneShot(audioClip[4]);
    }
    private void Yodel()
    {
        audioSource2.clip = audioClip[3];
        audioSource2.volume = 1f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[3]);
    }
    private void Wind()
    {
        audioSource.clip = audioClip[2];
        audioSource.volume = 0.3f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[2]);
    }
    private void Step()
    {
        audioSource.clip = audioClip[1];
        audioSource.volume = 0.4f;
        audioSource.pitch = 0.8f;
        audioSource.PlayOneShot(audioClip[1]);
    }
    public void Jump()
    {
        audioSource2.clip = audioClip[0];
        audioSource2.volume =0.15f;
        audioSource2.pitch = 0.8f;
        audioSource2.PlayOneShot(audioClip[0]);
    }



}
