using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class Punch : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClip;
    private AudioSource punch;

    

    private void Awake()
    {
         punch = GetComponent<AudioSource>();
    }
    public void Hit()
    {
            AudioClip clip = GetRandomClip();
            punch.PlayOneShot(clip);
    }


     AudioClip GetRandomClip()
    {
            int index = Random.Range(0, audioClip.Length - 1);
            return audioClip[index];

    }


}

