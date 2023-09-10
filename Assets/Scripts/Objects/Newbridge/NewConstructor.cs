using UnityEngine;
using UnityEngine.UI;
public class NewConstructor : MonoBehaviour
{
    public Behavior Player;
    public int PartCount = 0;
    public int BridgeLimit = 8;
    [SerializeField] GameObject BridgePart;
    [SerializeField] GameObject BridgeLink;
    [SerializeField] GameObject Log;
    [SerializeField] GameObject Step;
    public AudioSource WoodKnock;
    public Rigidbody Bridge;
    public Material Cel;
    public Material Synthi;
    MeshRenderer Ramp;
    public Text BridgeUI;
    string BridgeText;
    public bool isLocked = false;
    //public string BridgeUI;
    float X;
    public BoxCollider[] partsColliders;
    private void Awake()
    {
        Ramp = GetComponent<MeshRenderer>();
        Ramp.material = Cel;
        Bridge = GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        isLocked = false;
        BridgeText = "press left ctrl for bridge lock and construction";
        BridgeUI.text = BridgeText;
        Step.SetActive(false);
    }

    public void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Part" && isLocked == true && PartCount < BridgeLimit)
        {
            PartCount++;
            X = 4.4331f;
            Step.SetActive(true);
            var newPart = Instantiate(BridgePart, Bridge.transform.position + Bridge.transform.up * -X * PartCount + Bridge.transform.forward * -0.5f, Bridge.transform.rotation);
            newPart.transform.parent = Bridge.transform;
            Destroy(OBJ.gameObject);
            WoodKnock.Play();
            partsColliders = GetComponentsInChildren<BoxCollider>();
            //BoxCollider partCol = newPart.GetComponent<BoxCollider>();
            MergeColliders(partsColliders);
            
            if (Player.CurrentHealth < Player.MaxHealth)
            {
                Player.CurrentHealth += 50;
                Player.HealthBar.SetHealth(Player.CurrentHealth);
            }

        }

        if (OBJ.gameObject.tag == "Seed" && isLocked == true && PartCount >= 9)
        {
            Destroy(OBJ.gameObject);
            BridgeLimit += 9;
            var newPart = Instantiate(BridgeLink, Bridge.transform.position + Bridge.transform.up * -X * PartCount + Bridge.transform.forward * -0.5f, Bridge.transform.rotation);
            newPart.transform.parent = Bridge.transform;
            //BoxCollider partCol = newPart.GetComponent<BoxCollider>();
            //MergeColliders(partCol);
        }
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
            if (Input.GetKeyDown(KeyCode.Delete) && PartCount == 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    Instantiate(Log, Bridge.transform.position + new Vector3(0, i, 0), Bridge.transform.rotation);
                }
                Destroy(gameObject);
            }
        }

    }

    void MergeColliders(BoxCollider[] bridgeParts)
    {
        for (int i = 0; i < bridgeParts.Length; i++)
        {
            if (i>1)
            {
                bridgeParts[i].enabled = false;
            }
        }
        if (bridgeParts.Length>2)
        {
            bridgeParts[1].size += new Vector3(0, 4.4331f, 0);
            bridgeParts[1].center -= new Vector3(0, 4.4331f / 2, 0);
        }


        //Bounds bounds1 = bridgeParts[1].bounds;
        //Bounds bounds2 = bridgeParts[bridgeParts.Length].bounds;
        //Bounds combinedBounds = new Bounds(Vector3.Lerp(bounds1.min, bounds2.min, 0.21f), Vector3.Lerp(bounds1.max, bounds2.max, 0.21f));
        //bridgeParts[1].size = combinedBounds.size;
        //bridgeParts[1].center = combinedBounds.center;

    }
}





