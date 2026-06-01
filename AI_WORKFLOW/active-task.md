# Active Task

## Task title
- Shadow Revenant test arena rebuild (`ShadowRevenantTestArena.unity`)

## Type
- Feature / Unity wiring

## Owner
- Mixed (assets generated; Unity Play Mode verification pending)

## Current phase
- Verification

## Goal
- Fully restore playable Shadow Revenant boss in the test arena with config, pooled ability prefabs, placeholder visuals, and scene instance.

## Scope
- `Assets/Scenes/ShadowRevenantTestArena.unity` — boss prefab instance at ~(0, 0, 12)
- `Assets/Prefabs/NPC/ShadowRevenant/*` — boss, projectile, fog, shade, animator, material
- `Assets/Data/NPC/ShadowRevenant/ShadowRevenantConfig.asset`
- `Assets/Editor/ShadowRevenantTestArenaBuilder.cs` — menu/batch rebuild utility

## Relevant files/assets
- `Assets/Prefabs/NPC/ShadowRevenant/ShadowRevenant.prefab`
- `Assets/Data/NPC/ShadowRevenant/ShadowRevenantConfig.asset`
- `Assets/Scripts/NPC/ShadowRevenant/ShadowRevenantController.cs`
- `Assets/Scripts/NPC/ShadowRevenant/ShadowRevenantTestSceneBinder.cs`

## Risk level
- Medium (YAML-generated prefabs; Unity reimport may adjust serialization)

## Current status
- Config asset, ability prefabs, boss prefab, and scene instance added.
- Placeholder capsule visuals + minimal `ShadowRevenant.controller` (Phased, Attack, Stagger, Summon, Dead).
- **World HP bar added:** `HealthBarAnchor` → `Sphere` (`RotateUI`) → world Canvas → Slider; combat-only visibility.
- **Floor sink fix:** `ShadowRevenantConfig` hover/ground fields; `ShadowRevenantController` `ResolveHoverPosition` + `MaintainHoverHeight`; Rigidbody gravity off + kinematic; horizontal strafe via `MovePosition`; teleport uses hover Y.
- Prefab: `useGravity=0`, `isKinematic=1`. Builder aligned.
- **Combat prefab references repaired:** `shadeMinionPrefab` → `ShadowRevenantShadeMinion` (fileID 9000007); VFX prefabs assigned (`Hit/Death/Phase/LightBreak` + `PooledOneShotVfx`); projectile/fog unchanged; `deathDropPrefabs` intentionally empty.
- Shade minion: sphere body + green eye spheres (`ShadowRevenantShadeEye.mat`).
- VFX are lightweight `HurtEffect` clones (tune via **Beavermania → Build → Shadow Revenant Combat Assets** or replace particles in Editor).
- **Needs Unity Play Mode verification** (agent cannot run Editor here).

## Next action
- Owner: User / Unity Editor
- Action: Open `ShadowRevenantTestArena`, confirm `ShadowRevenant` has no missing scripts, enter Play Mode.

## Required verification
- Unity Play Mode: aggro, projectile/fog/shade, player damage, boss death, no console errors.
- Optional: **Beavermania → Build → Shadow Revenant Test Arena** to regenerate via `PrefabUtility` if any reference breaks after reimport.

## Handoff needed
- No Codex handoff unless Play Mode reveals script bugs.

## Play Mode checklist
- [ ] Boss **does not sink** through floor at spawn or while dormant (hover height stable)
- [ ] Strafe/chase: horizontal movement only; Y stays at ground + `hoverHeight`
- [ ] Phase teleport: lands around player above floor
- [ ] HUD wires via `ShadowRevenantTestSceneBinder` (no binder warning)
- [ ] Boss dormant: world HP bar **hidden**
- [ ] After aggro: HP bar **visible**, billboards toward player camera
- [ ] Melee/ranged reduces boss HP slider fill
- [ ] Walk toward boss → leaves Dormant, strafes/attacks
- [ ] At least two of: projectile hit, fog slow, shade damage
- [ ] Shade summon: minions spawn (no Type Mismatch / null pool errors)
- [ ] Hit / phase / light-break / death VFX visible when abilities trigger
- [ ] `ShadowRevenantConfig`: no Type Mismatch on shade minion; all VFX fields assigned
- [ ] Boss death: HP bar hidden; no `RotateUI` / visibility console errors
