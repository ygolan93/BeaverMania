using Beavermania.NPC;
using Beavermania.Objects;
using UnityEngine;

namespace Beavermania.Player.AI
{
    /// <summary>
    /// Attaches AutoPlayer and optional scene adapters on load when prefabs/scenes are not wired manually.
    /// </summary>
    public static class AutoPlayerSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            EnsureInstallerOnPlayer();
            EnsureTraderAdapters();
            EnsureTreeMarkers();
            EnsureBridgeMarkers();
        }

        static void EnsureInstallerOnPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            if (player.GetComponent<AutoPlayerRuntimeInstaller>() == null)
                player.AddComponent<AutoPlayerRuntimeInstaller>();
        }

        static void EnsureTraderAdapters()
        {
            var traders = Object.FindObjectsOfType<Trader>(true);
            for (int i = 0; i < traders.Length; i++)
            {
                Trader trader = traders[i];
                if (trader == null)
                    continue;

                if (trader.GetComponent<TraderAutoInteractAdapter>() == null)
                    trader.gameObject.AddComponent<TraderAutoInteractAdapter>();
            }
        }

        static void EnsureTreeMarkers()
        {
            var spawners = Object.FindObjectsOfType<LogSpawner>(true);
            for (int i = 0; i < spawners.Length; i++)
            {
                LogSpawner spawner = spawners[i];
                if (spawner == null || spawner.GetComponent<AutoChoppableTree>() != null)
                    continue;

                spawner.gameObject.AddComponent<AutoChoppableTree>();
            }

            var grows = Object.FindObjectsOfType<Grow>(true);
            for (int i = 0; i < grows.Length; i++)
            {
                Grow grow = grows[i];
                if (grow == null || grow.GetComponent<AutoChoppableTree>() != null)
                    continue;

                grow.gameObject.AddComponent<AutoChoppableTree>();
            }
        }

        static void EnsureBridgeMarkers()
        {
            var constructors = Object.FindObjectsOfType<NewConstructor>(true);
            for (int i = 0; i < constructors.Length; i++)
            {
                NewConstructor constructor = constructors[i];
                if (constructor == null || constructor.GetComponent<AutoBridgeBuildPoint>() != null)
                    continue;

                constructor.gameObject.AddComponent<AutoBridgeBuildPoint>();
            }
        }
    }
}
