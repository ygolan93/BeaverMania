using UnityEngine;
using UnityEngine.UI;
public class NewConstructor : MonoBehaviour
{
    public Behaviour Player;
    public int PartCount = 0;
    public int BridgeLimit = 8;
    [SerializeField] GameObject BridgePart;
    [SerializeField] GameObject BridgeLink;
    [SerializeField] GameObject Log;
    //[SerializeField] GameObject Step;
    public AudioScript Construction;
    public Rigidbody Bridge;
    [SerializeField] BoxCollider movingCollider;
    [SerializeField] MeshCollider staticCollider;
    [SerializeField] Transform PartsParent;
    public Material Cel;
    public Material Synthi;
    [SerializeField] MeshRenderer[] Ramps;
    public Text BridgeUI;
    string BridgeText;
    public bool isLocked = false;
    //public string BridgeUI;
    float X;
    public BoxCollider[] partsColliders;
    bool invokeLock = false;
    Vector3 lockPos;
    private void Awake()
    {
        //Ramp = GetComponent<MeshRenderer>();
        movingCollider.enabled = true;
        staticCollider.enabled = false;
        for (int i = 0; i < Ramps.Length; i++)
        {
            Ramps[i].material = Cel;
        }
        Bridge = GetComponent<Rigidbody>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        isLocked = false;
        BridgeText = "press left ctrl for bridge lock and construction";
        BridgeUI.text = BridgeText;
    }

    [System.Obsolete]
    public void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Part") && isLocked == true)
        {
            if (PartCount < BridgeLimit)
            {
                BridgeText = "";
                PartCount++;
                X = 2.53f;
                var newPart = Instantiate(BridgePart, PartsParent.transform.position - PartCount * X * PartsParent.transform.forward, Bridge.transform.rotation * Quaternion.Euler(-90, 180, 0));
                newPart.transform.parent = PartsParent;
                Destroy(OBJ.gameObject);
                Construction.Jump();
                partsColliders = PartsParent.GetComponentsInChildren<BoxCollider>();
                MergeColliders(partsColliders);

                if (Player.CurrentHealth < Player.MaxHealth)
                {
                    Player.CurrentHealth += 50;
                    Player.HealthBar.SetHealth(Player.CurrentHealth);
                }
            }

        }

        if (OBJ.gameObject.CompareTag("Seed") && isLocked == true && PartCount >= 9)
        {
            Destroy(OBJ.gameObject);
            BridgeLimit += 9;
            var newPart = Instantiate(BridgeLink, PartsParent.transform.position - PartCount * X * PartsParent.transform.forward, Bridge.transform.rotation * Quaternion.Euler(-90, 0, 0));
            newPart.transform.parent = PartsParent;
            Player.Plattering = "TADA!";
            Player.ChangeSpeech = 2;
        }
    }
    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (invokeLock==false)
                {
                    lockPos = transform.position - new Vector3(0, 0.05f, 0);
                    transform.position = lockPos;
                    invokeLock = true;
                    Construction.Step();
                    Player.Plattering = "Zing!";
                    Player.ChangeSpeech = 2;
                }
                isLocked = true;
                movingCollider.enabled = false;
                staticCollider.enabled = true;
                Bridge.gameObject.layer = 9;
                Bridge.isKinematic = true;
                for (int i = 0; i < Ramps.Length; i++)
                {
                    Ramps[i].material = Synthi;
                }
                BridgeUI.enabled = false;
                BridgeUI.text = BridgeText;

            }
 
        }

        if (Input.GetKey(KeyCode.LeftControl) && PartCount >= BridgeLimit)
        {
            Player.Plattering = "Oh man! I need a nut";
            Player.ChangeSpeech = 3;
        }

    }

    void MergeColliders(BoxCollider[] bridgeParts)
    {
        float partY= BridgePart.GetComponent<BoxCollider>().size.y;
        bridgeParts[0].enabled = true;
        if (PartCount>1)
        {
            bridgeParts[0].size += new Vector3(0, partY, 0);
            bridgeParts[0].center -= new Vector3(0, partY / 2, 0);
        }

        for (int i = 1; i < bridgeParts.Length; i++)
        {
            bridgeParts[i].enabled = false;
        }
        if (bridgeParts.Length==8)
        {
            Debug.Log("Reached bridge limit! Use a nut to extend");
        }
        
    }
}





