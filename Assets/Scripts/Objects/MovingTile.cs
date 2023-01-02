using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingTile : MonoBehaviour
{
    [SerializeField] Rigidbody Tile;
    [SerializeField] float TileSpeed;
    [SerializeField] float MovementTime = 10f;
    float InitialTimer;
    public Vector3 MoveTo;
    private void Start()
    {
        InitialTimer = MovementTime;
    }



    // Update is called once per frame
    void FixedUpdate()
    {
            MovementTime -= Time.deltaTime;
            if (MovementTime > InitialTimer * 0.5f)
            {
                Tile.velocity = MoveTo.normalized * TileSpeed;
            }
            if (MovementTime <= InitialTimer * 0.5f)
            {
                Tile.velocity = -MoveTo.normalized * TileSpeed;
            }
            if (MovementTime <= 0)
            {
                MovementTime = InitialTimer;
            }
    }

    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player")|| OBJ.gameObject.CompareTag("Bridge"))
        {
            OBJ.transform.parent = Tile.transform;
        }
    }

    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Bridge") || OBJ.gameObject.CompareTag("Player"))
        {
            OBJ.transform.parent = null;
        }
    }



}
