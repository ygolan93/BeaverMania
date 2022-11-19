using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedPlant : MonoBehaviour
{
    public GameObject GrowingTree;
    public Behavior Player;
    [SerializeField] Rigidbody Seed;
    public bool grounded;
    public bool planted;
    float GrowTimer=5f;
    public string GrowDisplay;
    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
    }
    private void OnCollisionStay(Collision OBJ)
    {
       if (OBJ.gameObject.CompareTag("Isle"))
        {
            grounded = true;
        }
       if (OBJ.gameObject== Player)
        {

            if (!Input.anyKey)
            {
                Destroy(gameObject);
                Player.NutCount++;
            }
        }
    }

    private void Update()
    {
        if ((Player.transform.position-Seed.position).magnitude<3)
        {
            planted = true;
        }

        if (planted==true)
        {
            GrowTimer -= Time.deltaTime;
            GrowDisplay = Mathf.Round(GrowTimer) + "";
            if (GrowTimer <= 0)
            {
                Instantiate(GrowingTree, transform.position + new Vector3(0, -0.4f, 0), Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }


    private void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Isle"))
        {
            grounded = false;
        }
    }
}
