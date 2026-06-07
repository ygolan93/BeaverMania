using UnityEngine;

namespace Beavermania.Player
{
    public static class PlayerReferenceCache
    {
        static BeaverPlayerBehaviour cachedPlayer;
        static float lastFailedLookupTime = -999f;
        const float FailedLookupCooldown = 0.5f;

        public static BeaverPlayerBehaviour GetPlayer()
        {
            if (cachedPlayer != null)
                return cachedPlayer;

            if (Time.unscaledTime - lastFailedLookupTime < FailedLookupCooldown)
                return null;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                lastFailedLookupTime = Time.unscaledTime;
                return null;
            }

            cachedPlayer = playerObject.GetComponent<BeaverPlayerBehaviour>();
            if (cachedPlayer == null)
                lastFailedLookupTime = Time.unscaledTime;

            return cachedPlayer;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            cachedPlayer = null;
            lastFailedLookupTime = -999f;
        }
    }
}
