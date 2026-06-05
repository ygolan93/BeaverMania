# Active Task

## Task title
- Scorpion refactor Unity integration + prefab wiring

## Type
- Feature integration (ScriptableObject assets + prefab serialization)

## Owner
- **Cursor** — Play Mode boss-fight verification pending (user)

## Current phase
- Prefab + stats assets wired via Unity MCP; full arena combat checklist not automated

## Goal
- Wire refactored `ScorpionScript` + `ScorpionStatsData` on Scorpion prefabs
- Preserve existing combat tuning, VFX, audio, and collider references
- Validate boss in Level 1 Remastered scene

## Changes made (2026-06-05)

### ScriptableObject assets created
- `Assets/Data/NPC/Scorpion/ScorpionStatsData.asset` — arena/default tuning (8000 HP)
- `Assets/Data/NPC/Scorpion/ScorpionBossStatsData.asset` — boss variant tuning (150000 HP)

### Prefab wiring
- `Assets/Prefabs/Scorpion/Scorpion.prefab` — `statsData` → `ScorpionStatsData.asset`
- `Assets/Prefabs/Scorpion/ScorpionBoss.prefab` — `statsData` → `ScorpionBossStatsData.asset`
- Unity migrated inline stats to `legacy*` fallback fields on both prefabs (FormerlySerializedAs)

### Scene note
- `Level 1 - Remastered - Steam.unity` boss instance uses **`ScorpionBoss.prefab`** (renamed `ScorpionBoss`), with scene override `Player` reference wired.

## Manual Unity steps
- [ ] Play Mode: enter boss arena in `Level 1 - Remastered - Steam.unity`
- [ ] Full combat checklist below

## Play Mode validation checklist
- [ ] Scorpion detects player
- [ ] Charge / attack loop works
- [ ] Player damages Scorpion; HP bar updates
- [ ] Parry/stun loop works
- [ ] Death triggers explosion + drops
- [ ] No NullReferenceException / missing script warnings
- [ ] Animator params `Walk`, `Backwards`, `Attack`, `Stunned` OK
- [ ] Audio (Beat/Sting) on hit

## Result
- Integration: **Pass** (prefab/assets)
- Play Mode boss fight: **Needs user verification**
