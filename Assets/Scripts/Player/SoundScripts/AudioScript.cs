using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class AudioScript : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClip;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource audioSource2;

    public void SwordSwing3()
    {
        audioSource.clip = audioClip[18];
        audioSource.volume = 1f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[18]);
    }
    public void SwordSwing2()
    {
        audioSource.clip = audioClip[17];
        audioSource.volume = 0.7f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[17]);
    }
    public void SwordSwing1()
    {
        audioSource.clip = audioClip[16];
        audioSource.volume = 0.6f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[16]);
    }
    public void Error()
    {
        audioSource2.clip = audioClip[15];
        audioSource2.volume = 6f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[15]);
    }
    public void PickItem()
    {
        audioSource2.clip = audioClip[14];
        audioSource2.volume = 6f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[14]);
    }
    public void SwitchItem()
    {
        audioSource2.clip = audioClip[13];
        audioSource2.volume = 6f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[13]);
    }


    public void ArrowShoot()
    {
        audioSource2.clip = audioClip[12];
        audioSource2.volume = 6f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[12]);
    }
    public void ArrowDraw()
    {
        audioSource2.clip = audioClip[11];
        audioSource2.volume = 6f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[11]);
    }
    public void Drink()
    {
        audioSource2.clip = audioClip[10];
        audioSource2.volume = 6f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[10]);
    }
    public void Eat()
    {
        audioSource2.clip = audioClip[9];
        audioSource2.volume = 5f;
        audioSource2.pitch = 1f;
        audioSource2.PlayOneShot(audioClip[9]);
    }
    public void Roll()
    {
        audioSource.clip = audioClip[8];
        audioSource.volume = 5f;
        audioSource2.pitch = 0.8f;
        audioSource.PlayOneShot(audioClip[8]);
    }
    public void PickUp2()
    {
        audioSource.clip = audioClip[7];
        audioSource.volume = 7f;
        audioSource2.pitch = 1f;
        audioSource.PlayOneShot(audioClip[7]);
    }
    public void PickUp1()
    {
        audioSource.clip = audioClip[6];
        audioSource.volume = 7f;
        audioSource2.pitch = 1f;
        audioSource.PlayOneShot(audioClip[6]);
    }
    public void Coin()
    {
        audioSource.clip = audioClip[5];
        audioSource.volume = 7f;
        audioSource2.pitch = 1f;
        audioSource.PlayOneShot(audioClip[5]);
    }
    public void Heal()
    {
        audioSource.clip = audioClip[4];
        audioSource.volume = 1f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[4]);
    }
    public void Slide()
    {
        audioSource.clip = audioClip[3];
        audioSource.volume = 2f;
        audioSource.pitch = 0.7f;
        audioSource.PlayOneShot(audioClip[3]);
    }
    public void Wind()
    {
        audioSource.clip = audioClip[2];
        audioSource.volume = 0.3f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[2]);
    }
    public void Step()
    {
        audioSource.clip = audioClip[1];
        audioSource.volume = 0.6f;
        audioSource.pitch = 0.8f;
        audioSource.PlayOneShot(audioClip[1]);
    }
    public void Jump()
    {
        audioSource2.clip = audioClip[0];
        audioSource2.volume =0.2f;
        audioSource2.pitch = 0.8f;
        audioSource2.PlayOneShot(audioClip[0]);
    }



}
