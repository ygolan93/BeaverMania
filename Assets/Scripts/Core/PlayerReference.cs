using UnityEngine;

public static class PlayerReference
{
    static Behaviour cachedPlayer;

    public static bool TryGetPlayer(out Behaviour player)
    {
        if (cachedPlayer != null)
        {
            player = cachedPlayer;
            return true;
        }

        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null || !playerObject.TryGetComponent(out cachedPlayer))
        {
            player = null;
            return false;
        }

        player = cachedPlayer;
        return true;
    }
}
