using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class AudioScript : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClip;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource audioEffects;

    public void SwordSwing3()
    {
        audioSource.clip = audioClip[18];
        audioSource.volume = 0.4f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[18]);
    }
    public void SwordSwing2()
    {
        audioSource.clip = audioClip[17];
        audioSource.volume = 0.4f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[17]);
    }
    public void SwordSwing1()
    {
        audioSource.clip = audioClip[16];
        audioSource.volume = 0.4f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[16]);
    }
    public void Error()
    {
        audioEffects.clip = audioClip[15];
        audioEffects.volume = 6f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[15]);
    }
    public void PickItem()
    {
        audioEffects.clip = audioClip[14];
        audioEffects.volume = 6f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[14]);
    }
    public void SwitchItem()
    {
        audioEffects.clip = audioClip[13];
        audioEffects.volume = 6f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[13]);
    }


    public void ArrowShoot()
    {
        audioEffects.clip = audioClip[12];
        audioEffects.volume = 6f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[12]);
    }
    public void ArrowDraw()
    {
        audioEffects.clip = audioClip[11];
        audioEffects.volume = 6f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[11]);
    }
    public void Drink()
    {
        audioEffects.clip = audioClip[10];
        audioEffects.volume = 6f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[10]);
    }
    public void Eat()
    {
        audioEffects.clip = audioClip[9];
        audioEffects.volume = 5f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[9]);
    }
    public void Roll()
    {
        audioSource.clip = audioClip[8];
        audioSource.volume = 5f;
        audioSource.pitch = 0.8f;
        audioSource.PlayOneShot(audioClip[8]);
    }
    public void PickUp2()
    {
        audioSource.clip = audioClip[7];
        audioSource.volume = 7f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[7]);
    }
    public void PickUp1()
    {
        audioSource.clip = audioClip[6];
        audioSource.volume = 7f;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(audioClip[6]);
    }
    public void Coin()
    {
        audioEffects.clip = audioClip[5];
        audioEffects.volume = 7f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[5]);
    }
    public void Heal()
    {
        audioEffects.clip = audioClip[4];
        audioEffects.volume = 1f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[4]);
    }
    public void Slide()
    {
        audioEffects.clip = audioClip[3];
        audioEffects.volume = 2f;
        audioEffects.pitch = 0.7f;
        audioEffects.PlayOneShot(audioClip[3]);
    }
    public void Wind()
    {
        audioEffects.clip = audioClip[2];
        audioEffects.volume = 0.3f;
        audioEffects.pitch = 1f;
        audioEffects.PlayOneShot(audioClip[2]);
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
        audioEffects.clip = audioClip[0];
        audioEffects.volume =0.2f;
        audioEffects.pitch = 0.8f;
        audioEffects.PlayOneShot(audioClip[0]);
    }



}
