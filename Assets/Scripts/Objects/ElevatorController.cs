using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    public Transform topPosition;    // The top position of the elevator.
    public Transform bottomPosition; // The bottom position of the elevator.
    public float speed = 2.0f;       // Elevator movement speed.
    public LiftButton Button1;
    public LiftButton Button2;
    [SerializeField] AudioSource noise;
    private bool isMovingUp = true;  // Flag to track the elevator's direction.
    public bool isMoving = false;   // Flag to prevent multiple movements.

    private void Update()
    {
        // Check if the elevator is not moving to prevent multiple movements.
        if (!isMoving)
        {
            // Check if the player is in the elevator and pressing the elevator button.
            if (Button1.isPushed==true || Button2.isPushed == true)
            {
                noise.Play();
                // Toggle the elevator direction.
                isMovingUp = !isMovingUp;
                // Start moving the elevator.
                StartCoroutine(MoveElevator(isMovingUp ? topPosition.position : bottomPosition.position));
            }
        }
    }

    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            OBJ.gameObject.transform.parent = transform;
        }
    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            OBJ.gameObject.transform.parent = null;
        }
    }

    private IEnumerator MoveElevator(Vector3 targetPosition)
    {
        isMoving = true;

        // Calculate the distance to the target position.
        float distance = Vector3.Distance(transform.position, targetPosition);

        // Calculate the time it takes to reach the target position.
        float journeyTime = distance / speed;

        // Calculate the initial position of the elevator.
        Vector3 startPosition = transform.position;

        // Interpolate the elevator's position to move it smoothly.
        float startTime = Time.time;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);

        while (Time.time < startTime + journeyTime)
        {
            float distanceCovered = (Time.time - startTime) * speed;
            float fractionOfJourney = distanceCovered / journeyLength;
            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            yield return null;
        }

        // Snap the elevator to the exact target position.
        transform.position = targetPosition;

        isMoving = false;
        noise.Stop();

    }
}
