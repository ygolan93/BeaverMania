using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beavermania.Core.GameFlow;
using Beavermania.UI.Objectives;

namespace Beavermania.Player
{

    public class OBJProgressUponDeath : MonoBehaviour
    {
        public GameObject OBJ;
        public ObjectiveUI Player;
        [SerializeField] int advanceToObjectiveIndex = -1;
        // Start is called before the first frame update
        void Start()
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<ObjectiveUI>();
        }

        // Update is called once per frame
        void Update()
        {
            if (OBJ == null)
            {
                var objectiveService = ObjectiveSyncService.Instance;
                if (objectiveService != null)
                {
                    if (advanceToObjectiveIndex >= 0)
                        objectiveService.TrySetObjectiveIndex(advanceToObjectiveIndex, ObjectiveAdvanceReason.ObjectiveTargetDestroyed);
                    else
                        objectiveService.TryAdvanceObjective(1, ObjectiveAdvanceReason.ObjectiveTargetDestroyed);
                }
                else if (Player != null)
                {
                    if (advanceToObjectiveIndex >= 0 && Player.currentPoint != null)
                    {
                        Player.currentPoint.TryApplyObjectiveIndexDirect(advanceToObjectiveIndex);
                        if (Player.TryGetObjectiveText(advanceToObjectiveIndex, out string objectiveText))
                            Player.ApplyObjectiveMirror(advanceToObjectiveIndex, objectiveText);
                    }
                    else
                    {
                        Player.UpdateObjective();
                    }
                }

                Destroy(gameObject);
            }
        }
    }
}
