using UnityEngine;

public class NewConstructor : MonoBehaviour
{
    public Behavior Player;
    public int PartCount = 0;
    public int BridgeLimit = 20;
    [SerializeField] GameObject BridgePart;
    public AudioSource WoodKnock;
    public Rigidbody Bridge;
    public Material Cel;
    public Material Synthi;
    MeshRenderer Ramp;
    public string BridgeUI;
    bool isLocked = false;

    //public string BridgeUI;
    float X;
    private void Awake()
    {
        Ramp = GetComponent<MeshRenderer>();
        Ramp.material = Cel;
        Bridge = GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        isLocked = false;
    }

    public void OnTriggerStay(Collider OBJ)
    {

        if (OBJ.gameObject.tag == "Player")
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                isLocked = true;
                Bridge.isKinematic = true;
                BridgeUI = "Locked";
                Ramp.material = Synthi;

            }
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                Destroy(gameObject);
            }
        }

        if (OBJ.gameObject.tag == "Part"&&isLocked==true)
        {
                WoodKnock.Play();
                if (Player.CurrentHealth < Player.MaxHealth)
                {
                    Player.CurrentHealth += 50;
                    Player.HealthBar.SetHealth(Player.CurrentHealth);
                }
            PartCount++;
            X = 4.5f;
          var newPart = Instantiate(BridgePart, Bridge.transform.position + Bridge.transform.up * -X * PartCount + Bridge.transform.forward*-0.5f, Bridge.transform.rotation);
            newPart.transform.parent = Bridge.transform;

            Destroy(OBJ.gameObject);
            }
        }

    }



   
 
