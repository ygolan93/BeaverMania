using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanButton : MonoBehaviour
{
    public float pushSpeed;
    public Fan Rotor;
    public Material unPressed;
    public Material Pressed;
    public Renderer mat;
    public Transform initialPos;
    public Transform pushPos;
    public bool isPushed;
    public float switchOff;
    [SerializeField] AudioSource click;
    [SerializeField] AudioClip clickClip;
    [SerializeField] GameObject pressedButton;
    // Start is called before the first frame update
    private void Start()
    {
        mat = pressedButton.GetComponent<Renderer>();
        pressedButton.transform.position = initialPos.position;
    }

    private void Update()
    {
        if (isPushed == true)
        {
            PushButton();
        }
        if (isPushed == false)
        {
            RetractButton();
        }

        if (Rotor.turnON==true)
        {
            mat.material = Pressed;
        }
        if (Rotor.turnON==false)
        {
            mat.material = unPressed;
        }

    }
    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            click.clip = clickClip;
            click.PlayOneShot(clickClip);
            isPushed = true;
            if (Rotor.turnON==false)
            {
                Rotor.turnON = true;
            }
        }
    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            click.clip = clickClip;
            click.PlayOneShot(clickClip);
            isPushed = true;
            if (Rotor.turnON == false)
            {
                Rotor.turnON = true;
            }
        }
    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            isPushed = false;
        }
    }
    private void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            isPushed = false;
        }
    }


    void PushButton()
    {
        pressedButton.transform.position = Vector3.Lerp(pressedButton.transform.position, pushPos.position, pushSpeed);
    }
    void RetractButton()
    {
        pressedButton.transform.position = Vector3.Lerp(pressedButton.transform.position, initialPos.position, pushSpeed);

    }
}
