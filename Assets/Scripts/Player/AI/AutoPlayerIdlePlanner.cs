using Beavermania.NPC;
using Beavermania.Player;
using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerIdlePlanner : MonoBehaviour
    {
        [SerializeField] AutoPlayerPerception perception;
        [SerializeField] AutoPlayerActionAdapter adapter;
        [SerializeField] AutoPlayerShopAdapter shopAdapter;
        [SerializeField] AutoPlayerTaskMemory memory;
        [SerializeField] float keepMovingWeight = 1f;
        [SerializeField] float buildBridgeWeight = 4f;
        [SerializeField] float visitTraderWeight = 2.5f;
        [SerializeField] float switchArsenalWeight = 1.8f;
        [SerializeField] float shopVisitCooldown = 25f;

        IdlePlannerResult _lastResult;

        public IdlePlannerResult LastResult => _lastResult;

        void Awake()
        {
            if (perception == null)
                perception = GetComponent<AutoPlayerPerception>();
            if (adapter == null)
                adapter = GetComponent<AutoPlayerActionAdapter>();
            if (shopAdapter == null)
                shopAdapter = GetComponent<AutoPlayerShopAdapter>();
            if (memory == null)
                memory = GetComponent<AutoPlayerTaskMemory>();
        }

        public IdlePlannerResult Evaluate(bool bridgePipelineActive)
        {
            var result = new IdlePlannerResult
            {
                Action = IdleActionKind.KeepMoving,
                Target = null,
                Score = keepMovingWeight,
                ArsenalEntryName = string.Empty
            };

            if (bridgePipelineActive)
            {
                result.Action = IdleActionKind.BuildBridge;
                result.Score = buildBridgeWeight * 2f;
                _lastResult = result;
                return result;
            }

            float bridgeScore = ScoreBridgeWork();
            float traderScore = ScoreTraderVisit();
            float arsenalScore = ScoreArsenalSwitch(out string arsenalName);
            float moveScore = keepMovingWeight;

            result.Score = moveScore;
            result.Action = IdleActionKind.KeepMoving;

            if (bridgeScore > result.Score)
            {
                result.Score = bridgeScore;
                result.Action = IdleActionKind.BuildBridge;
                result.Target = perception != null ? perception.NearestBridge ?? perception.NearestTree ?? perception.NearestLog : null;
            }

            if (traderScore > result.Score)
            {
                result.Score = traderScore;
                result.Action = IdleActionKind.VisitTrader;
                result.Target = perception != null ? perception.NearestInteractable : null;
            }

            if (arsenalScore > result.Score)
            {
                result.Score = arsenalScore;
                result.Action = IdleActionKind.SwitchArsenal;
                result.ArsenalEntryName = arsenalName;
            }

            _lastResult = result;
            return result;
        }

        float ScoreBridgeWork()
        {
            if (perception == null || adapter == null)
                return 0f;

            float score = 0f;
            if (perception.NearestBridge != null)
                score += buildBridgeWeight;

            if (adapter.IsCarryingLogs)
                score += buildBridgeWeight * 0.75f;

            if (perception.NearestLog != null && adapter.CanCarryMore)
                score += buildBridgeWeight * 0.5f;

            if (perception.NearestTree != null && adapter.CanCarryMore)
                score += buildBridgeWeight * 0.35f;

            return score;
        }

        float ScoreTraderVisit()
        {
            if (perception == null || shopAdapter == null || memory == null)
                return 0f;

            if (Time.time < memory.NextShopVisitAllowedTime)
                return 0f;

            if (perception.NearestInteractable == null)
                return 0f;

            if (!shopAdapter.PlayerNeedsShopVisit())
                return 0f;

            if (!shopAdapter.CanAffordAnyUsefulPurchase())
                return 0f;

            float dist = perception.NearestInteractableDistance;
            float proximity = Mathf.Clamp01(1f - dist / Mathf.Max(perception.CombatDetectionRadius, 1f));
            return visitTraderWeight * (0.5f + proximity);
        }

        float ScoreArsenalSwitch(out string entryName)
        {
            entryName = string.Empty;
            if (adapter == null || adapter.Player == null)
                return 0f;

            BeaverPlayerBehaviour player = adapter.Player;

            if (perception != null && perception.NearestTree != null && player.OwnsArsenalItem("Hammers"))
            {
                if (!IsEquipped(player, "Hammers"))
                {
                    entryName = "Hammers";
                    return switchArsenalWeight * 1.2f;
                }
            }

            if (player.OwnsArsenalItem("Bow") && player.arrowMunition > 0 && !IsEquipped(player, "Bow"))
            {
                entryName = "Bow";
                return switchArsenalWeight;
            }

            if (!player.OwnsArsenalItem("Hammers") && !player.OwnsArsenalItem("Bow"))
            {
                entryName = "Bare Hands";
                return switchArsenalWeight * 0.25f;
            }

            return 0f;
        }

        static bool IsEquipped(BeaverPlayerBehaviour player, string entryName)
        {
            if (player == null)
                return false;

            string active = player.GetActiveArsenalEntryName();
            if (string.IsNullOrEmpty(active))
                return false;

            return active == entryName;
        }

        public Trader GetTraderFromTarget(Transform target)
        {
            if (target == null)
                return null;

            return target.GetComponent<Trader>() ?? target.GetComponentInParent<Trader>();
        }
    }
}
