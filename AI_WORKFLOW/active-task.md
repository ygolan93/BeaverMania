# Active Task

## Task title
- Shadow Revenant boss battle optimization (charge, combos, aim-line, SFX, balance)

## Type
- Feature (scripts + config + prefab wiring)

## Owner
- Mixed — **Unity Play Mode verification pending**

## Current phase
- Implementation complete (Phases 1–7); Phase 8 verification checklist below

## Goal
- Occasional telegraphed boss charge with single-hit melee
- Limited one-follow-up combo chains (feature-flagged)
- Projectile aim-line/tracer during windup
- Boss + minion SFX via `ShadowRevenantAudioProfile` + `GameplayAudio` throttling
- Balance pass (maxHealth 4200, readable pressure)

## Changes made

### Phase 2 — Charge
- `ShadowRevenantState`: `ChargeWindup`, `ChargeActive`, `ChargeRecover` (appended after `Dead`)
- `ShadowRevenantChargeAttack.cs`: horizontal dash, obstruction stop, single SphereCast hit
- `ShadowRevenantController`: charge decision, windup/active/recover ticks

### Phase 3 — Combos
- `ShadowRevenantComboPlanner.cs`: one follow-up max; allowed pairs per plan; combo cooldown/chance
- Recover exits + phase end hook combo resolution

### Phase 4 — Aim line
- `ShadowRevenantProjectileAimLine.cs`: pooled LineRenderer child, obstruction ray, fire tracer VFX
- Runtime fallback creates `ProjectileAimLine` child if missing on prefab

### Phase 5 — Boss SFX
- `ShadowRevenantAudioProfile.cs` + asset at `Assets/Data/Audio/ShadowRevenant/`
- `ShadowRevenantAudio.cs`: event playback via `SfxEventDefinition` / `GameplayAudio`

### Phase 6 — Minion SFX
- `ShadowRevenantShadeAudio.cs`: spawn/attack/hit/death with attack cooldown
- `ShadowRevenantShadeMinion` + `ShadowRevenantPoolHub` pass audio profile from config

### Phase 7 — Balance
- `ShadowRevenantConfig.asset`: maxHealth 4200, multipliers normalized, charge/combo/aim fields

### Builder
- `ShadowRevenantCombatAssetsBuilder`: charge/tracer VFX, audio profile, shade audio component

## Manual Unity steps
- [ ] Run **Beavermania → Build → Shadow Revenant Combat Assets** (charge/tracer VFX refs, audio profile link)
- [ ] Assign `SfxEventDefinition` clips on `ShadowRevenantAudioProfile` (or embed AudioSource on VFX prefabs)
- [ ] Assign Sfx mixer group on boss/minion `AudioSource` components (match Wasp/Scorpion)
- [ ] Play Mode in `ShadowRevenantTestArena.unity` — full checklist below

## Play Mode validation checklist (Phase 8)
- [ ] Boss spawns/aggroes with SFX (after clips assigned)
- [ ] Boss stays above ground; HP bar visible in combat
- [ ] Projectile aim-line during windup; tracer on fire; damage/parry unchanged
- [ ] Fog telegraphs, activates, fades, returns to pool (10+ casts)
- [ ] Boss occasionally charges at medium range; windup → dash → single hit → recover
- [ ] One follow-up combo sometimes; never endless chains
- [ ] Boss hit / light-break / death SFX + VFX + remains
- [ ] Minion spawn / attack (cooldown) / hit / death SFX + VFX
- [ ] No stuck audio loops after boss death
- [ ] No NullReference spam; stable FPS; pooled hierarchy bounded

## Rollback
- Set `enableChargeAttack`, `enableCombos`, `enableProjectileAimLine` to false on config
- Remove/disable `ShadowRevenantAudio` components

## Handoff needed
- No
