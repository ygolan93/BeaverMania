using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiftButton : MonoBehaviour
{
    public float pushSpeed;
    public ElevatorController elevator;
    public Material unPressed;
    public Material Pressed;
    public Renderer mat;
    public Transform initialPos;
    public Transform pushPos;
    public bool isPushed;
    public float switchOff;
    [SerializeField] AudioSource click;
    [SerializeField] GameObject pressedButton;
    // Start is called before the first frame update
    private void Start()
    {
        mat = pressedButton.GetComponent<Renderer>();
        pressedButton.transform.position = initialPos.position;
    }

    private void Update()
    {
        if (isPushed==true)
        {
            PushButton();
        }
        if (isPushed == false)
        {
            RetractButton();
        }
        if (elevator.isMoving==true)
        {
            mat.material = Pressed;
        }
        if (elevator.isMoving==false)
        {
            mat.material = unPressed;
        }
    }
    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player")|| OBJ.gameObject.CompareTag("Damage"))
        {
            click.Play();
            isPushed = true;
        }
    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player") || OBJ.gameObject.CompareTag("Damage"))
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
