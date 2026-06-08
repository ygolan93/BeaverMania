using UnityEngine;
using Beavermania.Core.GameFlow;
using Beavermania.UI.Objectives;
using Beavermania.Player.Movement;

namespace Beavermania.Objects
{

    public class FirstBridgeChecker : MonoBehaviour
    {
        public NewConstructor Bridge;
        public ObjectiveUI Player;
        WayPoint wp;
        Carry playerLoad;
        int appliedLegacyFallbackIndex = -1;
        // Start is called before the first frame update
        void Start()
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<ObjectiveUI>();
            wp = Player.GetComponent<WayPoint>();
            playerLoad = Player.GetComponent<Carry>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Bridge == null)
                return;

            if (Bridge.isLocked == true)
            {
                int carriedLogs = playerLoad != null ? playerLoad.i : 0;
                if (ObjectiveSyncService.Instance != null)
                {
                    ObjectiveSyncService.Instance.OnBridgeConstructionLocked();
                    if (carriedLogs > 0)
                        ObjectiveSyncService.Instance.OnBridgeCompleted();
                }
                else if (wp != null)
                {
                    int fallbackIndex = carriedLogs > 0 ? 4 : 3;
                    if (fallbackIndex != appliedLegacyFallbackIndex && wp.TryApplyObjectiveIndexDirect(fallbackIndex))
                        appliedLegacyFallbackIndex = fallbackIndex;
                }

                if (carriedLogs > 0)
                    Destroy(gameObject);
            }
        }
    }
}
