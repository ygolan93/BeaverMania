using UnityEngine;

namespace Beavermania.Display
{
    public static class TerrainPerformanceBootstrap
    {
        const float TreeDistance = 2500f;
        const float DetailObjectDistance = 40f;
        const float BasemapDistance = 600f;
        const float HeightmapPixelError = 8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyTerrainPerformanceSettings()
        {
#if UNITY_EDITOR
            // Terrain distance tuning is serialized on scene terrains; skip runtime mutation in Editor
            // to avoid Play Mode stalls from touching many terrain tiles at once.
            return;
#endif

            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
                return;

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null)
                    continue;

                terrain.treeDistance = TreeDistance;
                terrain.treeBillboardDistance = 50f;
                terrain.treeCrossFadeLength = 5f;
                terrain.treeMaximumFullLODCount = 50;
                terrain.detailObjectDistance = DetailObjectDistance;
                terrain.detailObjectDensity = 0.65f;
                terrain.basemapDistance = BasemapDistance;
                terrain.heightmapPixelError = HeightmapPixelError;
            }
        }
    }
}
