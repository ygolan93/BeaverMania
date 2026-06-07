using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    /// <summary>
    /// Previously scanned every collider at scene load to attach <see cref="TriggerVolumeVisualHider"/>.
    /// That full-scene scan blocked Play Mode for ~20s on Level 1 Remastered.
    /// Add <see cref="TriggerVolumeVisualHider"/> directly on debug trigger prefabs/scene objects instead.
    /// </summary>
    public static class GameplayTriggerVisualBootstrap
    {
    }
}
