using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class NewConstructor : MonoBehaviour
{
    public Behavior Player;
    public int PartCount = 0;
    public int BridgeLimit = 8;
    [SerializeField] GameObject BridgePart;
    public AudioSource WoodKnock;
    public Rigidbody Bridge;
    public Material Cel;
    public Material Synthi;
    MeshRenderer Ramp;
    public Text BridgeUI;
    string BridgeText;
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
        BridgeText = "press left ctrl for bridge lock and construction";
        BridgeUI.text = BridgeText;
    }

    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.tag == "Player")
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                isLocked = true;
                Bridge.isKinematic = true;
                Ramp.material = Synthi;
                BridgeText = "Locked";
                BridgeUI.text = BridgeText;
            }
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                Destroy(gameObject);
            }
        }
        if (OBJ.gameObject.tag == "Seed" && isLocked == true)
        {
            Destroy(OBJ.gameObject);
            BridgeLimit += BridgeLimit;
        }

        if (OBJ.gameObject.tag == "Part" && isLocked == true && PartCount<BridgeLimit)
        {
            PartCount++;
            WoodKnock.Play();
            if (Player.CurrentHealth < Player.MaxHealth)
            {
                Player.CurrentHealth += 50;
                Player.HealthBar.SetHealth(Player.CurrentHealth);
            }
            X = 4.45f;
            var newPart = Instantiate(BridgePart, Bridge.transform.position + Bridge.transform.up * -X * PartCount + Bridge.transform.forward * -0.5f, Bridge.transform.rotation);
            newPart.transform.parent = Bridge.transform;

            Destroy(OBJ.gameObject);
        }
    }
}





