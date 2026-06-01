# Active Task

## Task title
- Shadow Revenant fog lifetime + death feedback fix

## Type
- Bugfix (scripts + prefab)

## Owner
- Cursor-owned — **Unity Play Mode verification pending**

## Current phase
- Implementation complete

## Goal
- Dread Fog always clears within ~windup+duration+fade seconds and returns to pool.
- Boss death shows death VFX + remains; boss visual hides without silent vanish.

## Changes made
- **Fog:** `poolLifetimeRemaining` fail-safe from telegraph start (`fogWindup + fogDuration + fogFadeOutTime`); `Update` always counts down; particle stop on pool return; explicit `pendingFogCast.DeactivateToPool()` on death.
- **Death:** `SpawnRemains`, `HideBossAfterDeath` (visual/colliders/HP bar off, boss root stays for state); death VFX at grounded position + 1.2m up.
- **Assets:** `ShadowRevenantRemains` script/prefab; config `remainsPrefab`, `remainsLifetime`; builder creates/assigns remains.

## Manual Unity steps
- [ ] Run **Beavermania → Build → Shadow Revenant Combat Assets** to refresh death VFX burst if needed.
- [ ] Play Mode in `ShadowRevenantTestArena.unity` — fog expiry + boss death checklist.

## Handoff needed
- No
