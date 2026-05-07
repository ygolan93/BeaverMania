public enum ServiceLifetime
{
    // Survives scene transitions; use for app-level settings/services.
    Persistent,

    // Bound to gameplay scenes; use for checkpoint, HUD, and player state.
    Scene
}
