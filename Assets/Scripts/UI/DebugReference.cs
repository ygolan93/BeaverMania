using UnityEngine;
using TMPro;
using Beavermania.Player;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using Beavermania.UI.Objectives;

namespace Beavermania.UI
{
    public class DebugReference : MonoBehaviour
    {
        [SerializeField] public BeaverPlayer Player;
        [SerializeField] public PlayerHudState PlayerHudState;

        public TextMeshProUGUI ObjectiveText;
        public TextMeshProUGUI DisplayText;
        public TextMeshProUGUI StaminaText;
        public TextMeshProUGUI LogCountText;
        public TextMeshProUGUI HealingDisplay;
        public TextMeshProUGUI CurrencyCount;
        public TextMeshProUGUI SeedCount;
        public TextMeshProUGUI GobletCount;
        public TextMeshProUGUI AppleCount;
        public TextMeshProUGUI ArrowMunition;

        bool loggedMissingPlayer;

        void Start()
        {
            BindPlayerHudState();
        }

        void BindPlayerHudState()
        {
            if (Player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    Player = playerObject.GetComponent<BeaverPlayer>();

                if (Player == null)
                {
                    LogMissingReference(nameof(Player), ref loggedMissingPlayer);
                    return;
                }
            }

            if (PlayerHudState == null)
                PlayerHudState = Player.GetComponent<PlayerHudState>();

            if (PlayerHudState == null)
            {
                PlayerHudState = Player.gameObject.AddComponent<PlayerHudState>();
                var objective = ResolvePlayerObjective();
                PlayerHudState.CopyFrom(Player, objective != null, objective != null ? objective.Instruction : null);
            }
        }

        ObjectiveUI ResolvePlayerObjective()
        {
            if (Player == null)
                return null;

            if (Player.PlayerObjective != null)
                return Player.PlayerObjective;

            return Player.GetComponent<ObjectiveUI>();
        }

        void Update()
        {
            if (PlayerHudState == null)
                BindPlayerHudState();

            if (PlayerHudState == null)
                return;

            if (Player != null)
            {
                var objective = ResolvePlayerObjective();
                PlayerHudState.CopyFrom(Player, objective != null, objective != null ? objective.Instruction : null);
            }

            SetText(ObjectiveText, PlayerHudState.ObjectiveText);
            SetText(DisplayText, PlayerHudState.DebugText);
            SetText(StaminaText, PlayerHudState.StaminaText);
            SetText(LogCountText, PlayerHudState.LogCount);
            SetText(CurrencyCount, PlayerHudState.Wallet);
            SetText(HealingDisplay, PlayerHudState.HealingText);
            SetText(SeedCount, PlayerHudState.SeedText);
            SetText(GobletCount, PlayerHudState.GobletText);
            SetText(AppleCount, PlayerHudState.AppleText);
            SetText(ArrowMunition, PlayerHudState.ArrowText);
        }

        static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        void LogMissingReference(string referenceName, ref bool logged)
        {
            if (logged)
                return;

            logged = true;
    #if DEVELOPMENT_BUILD
            Debug.LogWarning($"{nameof(DebugReference)} could not resolve {referenceName} fallback.", this);
    #endif
        }

    }
}
