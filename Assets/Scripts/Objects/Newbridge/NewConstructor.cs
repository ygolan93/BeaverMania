using UnityEngine;
using UnityEngine.UI;
using Beavermania.Audio;
using Beavermania.Core.GameFlow;
using Beavermania.Core.Input;
using Beavermania.Data.Tips;
using Beavermania.UI.Tips;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Objects
{
    public class NewConstructor : MonoBehaviour
    {
        const string BridgeLockTipId = "bridge.lock-construction";
        const string BridgeCarryLogsTipId = "bridge.carry-logs";
        const string BridgeBuildTipId = "bridge.build-with-logs";
        const string BridgeExtendTipId = "bridge.extend-with-nut";

        public BeaverPlayer Player;
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
        [SerializeField] float bridgeTipRepeatAttemptSeconds = 2f;
        float nextBridgeTipAttemptTime;
        string bridgeTipAreaKey;
        bool playerInBridgeRange;

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
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<BeaverPlayer>();
            isLocked = false;
            BridgeText = "press left ctrl for bridge lock and construction";
            BridgeUI.text = BridgeText;
            bridgeTipAreaKey = "bridge:" + GetInstanceID();
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
        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            playerInBridgeRange = false;
            if (BridgeUI != null)
            {
                BridgeUI.enabled = false;
                BridgeUI.text = string.Empty;
            }
        }

        public void OnTriggerStay(Collider OBJ)
        {
            if (OBJ.gameObject.CompareTag("Player"))
            {
                playerInBridgeRange = true;
                UpdateBridgeProgressUi();
                if (ObjectiveSyncService.Instance != null)
                    ObjectiveSyncService.Instance.OnPlayerNearBridgeFrame();

                TryShowBridgeTip();

                if (PlayerInputReader.WasRollPressed())
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

                    if (ObjectiveSyncService.Instance != null)
                        ObjectiveSyncService.Instance.OnBridgeConstructionLocked();
                }
 
            }

            if (PlayerInputReader.IsRollHeld() && PartCount >= BridgeLimit)
            {
                Player.Plattering = "Oh man! I need a nut";
                Player.ChangeSpeech = 3;
            }

        }

        void TryShowBridgeTip()
        {
            if (Time.unscaledTime < nextBridgeTipAttemptTime)
                return;

            nextBridgeTipAttemptTime = Time.unscaledTime + Mathf.Max(0.25f, bridgeTipRepeatAttemptSeconds);

            if (!isLocked)
            {
                TipsService.TryShowTip(new TipRequest(
                    BridgeLockTipId,
                    "Hold LCtrl here to lock bridge construction.",
                    priority: 20,
                    cooldownSeconds: 12f,
                    maxDisplayCount: 2,
                    showOnlyOnce: false,
                    displaySeconds: 4f), bridgeTipAreaKey);
                return;
            }

            if (PartCount >= BridgeLimit)
            {
                TipsService.TryShowTip(new TipRequest(
                    BridgeExtendTipId,
                    "Use a nut here to extend the bridge.",
                    priority: 15,
                    cooldownSeconds: 12f,
                    maxDisplayCount: 2,
                    showOnlyOnce: false,
                    displaySeconds: 4f), bridgeTipAreaKey);
                return;
            }

            if (Player != null && Player.Load != null && Player.Load.i > 0)
            {
                TipsService.TryShowTip(new TipRequest(
                    BridgeBuildTipId,
                    "Drop logs into this bridge frame to build.",
                    priority: 25,
                    cooldownSeconds: 8f,
                    maxDisplayCount: 3,
                    showOnlyOnce: false,
                    displaySeconds: 4f), bridgeTipAreaKey);
                return;
            }

            TipsService.TryShowTip(new TipRequest(
                BridgeCarryLogsTipId,
                "Bring logs here after locking the bridge frame.",
                priority: 10,
                cooldownSeconds: 14f,
                maxDisplayCount: 2,
                showOnlyOnce: false,
                idleOnly: true,
                displaySeconds: 4f), bridgeTipAreaKey);
        }

        void UpdateBridgeProgressUi()
        {
            if (BridgeUI == null || !playerInBridgeRange)
                return;

            BridgeUI.enabled = true;
            int carried = Player != null && Player.Load != null ? Player.Load.i : 0;

            if (!isLocked)
            {
                BridgeUI.text = "Hold LCtrl to lock bridge frame";
                return;
            }

            if (PartCount >= BridgeLimit)
            {
                BridgeUI.text = $"Bridge complete ({PartCount}/{BridgeLimit}) — use a nut to extend";
                return;
            }

            BridgeUI.text = $"Carried logs: {carried}/9 | Placed: {PartCount}/{BridgeLimit}";
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
}
