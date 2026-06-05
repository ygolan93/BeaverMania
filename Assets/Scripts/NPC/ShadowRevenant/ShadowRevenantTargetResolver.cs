using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.NPC
{
    public static class ShadowRevenantTargetResolver
    {
        const string PlayerTag = "Player";

        public static bool TryResolve(out IShadowRevenantTarget target, out Transform transform, Object context = null)
        {
            target = null;
            transform = null;

            GameObject playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
            if (playerObject == null)
            {
                if (context != null)
                    Debug.LogWarning($"{nameof(ShadowRevenantTargetResolver)} could not find GameObject tagged '{PlayerTag}'.", context);

                return false;
            }

            BeaverPlayer player = playerObject.GetComponent<BeaverPlayer>();
            if (player == null)
            {
                if (context != null)
                    Debug.LogWarning($"{nameof(ShadowRevenantTargetResolver)} found '{PlayerTag}' but no {nameof(BeaverPlayer)}.", context);

                return false;
            }

            transform = playerObject.transform;
            target = EnsureAdapter(playerObject, player);
            return target != null;
        }

        public static IShadowRevenantTarget ResolveFromTransform(Transform sourceTransform, Object context = null)
        {
            if (sourceTransform == null)
                return null;

            IShadowRevenantTarget existing = sourceTransform.GetComponentInParent<IShadowRevenantTarget>();
            if (existing != null)
                return existing;

            existing = sourceTransform.GetComponentInChildren<IShadowRevenantTarget>();
            if (existing != null)
                return existing;

            BeaverPlayer player = sourceTransform.GetComponent<BeaverPlayer>();
            if (player == null)
                player = sourceTransform.GetComponentInParent<BeaverPlayer>();
            if (player == null)
                player = sourceTransform.GetComponentInChildren<BeaverPlayer>();

            if (player == null)
            {
                if (context != null)
                    Debug.LogWarning($"{nameof(ShadowRevenantTargetResolver)} transform has no {nameof(BeaverPlayer)} or {nameof(IShadowRevenantTarget)}.", context);

                return null;
            }

            return EnsureAdapter(player.gameObject, player);
        }

        static ShadowRevenantPlayerTargetAdapter EnsureAdapter(GameObject playerObject, BeaverPlayer player)
        {
            var adapter = playerObject.GetComponent<ShadowRevenantPlayerTargetAdapter>();
            if (adapter == null)
                adapter = playerObject.AddComponent<ShadowRevenantPlayerTargetAdapter>();

            return adapter;
        }
    }
}
