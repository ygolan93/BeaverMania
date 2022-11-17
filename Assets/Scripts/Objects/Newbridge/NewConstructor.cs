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
    public Material Cel;
    public Material Synthi;
    MeshRenderer Ramp;
    public string BridgeUI;
    //public string BridgeUI;
    float X;
    private void Awake()
    {
        //Cel = Resources.Load<Material>("RampMATCel");
        //Synthi = Resources.Load<Material>("RampMatSynthi");
        Ramp = GetComponent<MeshRenderer>();
        Ramp.material = Cel;
        Bridge = GetComponent<Rigidbody>();
        

    }
    private void Update()
    {
        if (Standing == true)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                Bridge.isKinematic = true;
                BridgeUI = "Locked";
                Ramp.material = Synthi;

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
   
 
