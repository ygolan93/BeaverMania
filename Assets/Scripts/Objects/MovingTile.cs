using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingTile : MonoBehaviour
{
    [SerializeField] Rigidbody Tile;
    [SerializeField] float TileSpeed;
    [SerializeField] float MovementTime = 10f;
    float InitialTimer;
    bool Move = true;
    Rigidbody Player;
    Vector3 PosStart;
    public Vector3 MoveTo;
    private void Start()
    {
        InitialTimer = MovementTime;
    }



    // Update is called once per frame
    void FixedUpdate()
    {
        if (Move == true)
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
        if (Move == false)
        {
            Tile.velocity = new Vector3(0, 0, 0);
            MovementTime -= Time.deltaTime;
            if (MovementTime <= 0)
            {
                MovementTime = InitialTimer;
                Move = true;
            }
        }
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Damage"))
        {
            Move = false;
        }
    }

    private void OnTriggerStay(Collider OBJ)
    {

        if (OBJ.gameObject.CompareTag("Bridge"))
        {
            var Child = OBJ.GetComponent<Transform>();
            Child.transform.parent = Tile.transform;

        }
        if (OBJ.gameObject.CompareTag("Player"))
        {
            var Child = OBJ.GetComponent<Rigidbody>();
            Child.transform.parent = Tile.transform;
        }


    }

    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Bridge") || OBJ.gameObject.CompareTag("Player"))
        {
            var Child = OBJ.transform;
            Child.parent = null;
        }
    }



}
