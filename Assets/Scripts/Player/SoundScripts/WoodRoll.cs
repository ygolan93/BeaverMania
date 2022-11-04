using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class WoodRoll : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClip;
    private AudioSource audioSource;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Wood()
    {
        AudioClip clip = GetClip();
        audioSource.PlayOneShot(clip);
    }

    private AudioClip GetClip()
    {
        int index = 0;
        return audioClip[index];
    }
}
