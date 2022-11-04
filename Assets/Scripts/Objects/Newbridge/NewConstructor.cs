using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewConstructor : MonoBehaviour
{
    public Behavior Player;
    public int PartCount = 0;
    public int BridgeLimit = 20;
    public bool Standing = false;
    [SerializeField] GameObject BridgePart;
    public AudioSource WoodKnock;
    public Rigidbody Bridge;
    //public string BridgeUI;
    float X;
    private void Awake()
    {
        Bridge = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        if (Standing == true)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                Bridge.constraints = RigidbodyConstraints.FreezeAll;
                //BridgeUI = "Locked";
            }
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                Destroy(gameObject);
            }

        }
    }

    public void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
            Standing = false;
    }

    public void OnCollisionStay(Collision collision)
    {

        if (collision.gameObject.tag == "Player")
            Standing = true;

        if (collision.gameObject.tag == "Part"/*&& PartCount <= BridgeLimit*/)
        {
                WoodKnock.Play();
                if (Player.CurrentHealth < Player.MaxHealth)
                {
                    Player.CurrentHealth += 50;
                    Player.HealthBar.SetHealth(Player.CurrentHealth);
                }
            PartCount++;
            X = 4.4f;
          var newPart = Instantiate(BridgePart, Bridge.transform.position + Bridge.transform.up * -X * PartCount + Bridge.transform.forward*-0.5f, Bridge.transform.rotation);
            newPart.transform.parent = Bridge.transform;

            Destroy(collision.gameObject);
            }
        }

    }
   
 
