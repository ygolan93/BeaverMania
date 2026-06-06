# Active Task

## Task title
- Combat balance pass — five-tier enemy hierarchy

## Type
- Mixed (ScriptableObject tuning + script migration + Unity Play Mode verification)

## Owner
- **Cursor** — implementation complete
- **User** — full feel validation in Level 1 Remastered

## Current phase
- Data assets tuned; Wasp SO migration done; boss projectile mitigation added; MCP spot-check passed

## Goal
- Balance player vs enemy combat across difficulty tiers (easiest → hardest):
  1. Wasps
  2. Shadow Revenant Shade Minions
  3. Scorpions
  4. Shadow Revenant Mini Boss
  5. Scorpion Boss
- Early enemies tuned for bare hands (50 dmg); bosses for hammer (700) + bow (2000) + boost

## Changes made (2026-06-06)

### Phase 0 — Data hygiene
- Reverted mistaken `BoostChargeController` on enemy prefabs (Scorpion, ScorpionBoss, ShadowRevenant)
- Synced `Assets/Resources/Beavermania/Combat/BoostChargeSettings_Default.asset` with Data copy
- Reverted accidental Scorpion Boss 1.5M HP local edit

### Phase 1 — Wasp ScriptableObject migration
- New `WaspStatsData.cs` + `Assets/Data/NPC/Wasp/WaspStats_Default.asset`
- Resources fallback: `Assets/Resources/Beavermania/NPC/Wasp/WaspStats_Default.asset`
- `NPC_Basic.cs` reads stats via SO with legacy fallback chain
- `LVL1 Wasp.prefab` wired to `WaspStats_Default`

### Phase 2 — Tier tuning (HP hierarchy)
| Tier | Asset | HP | Notes |
|------|-------|-----|-------|
| Wasp | `WaspStats_Default` | **450** | ~9 bare-hand hits |
| Shade | `ShadowRevenantConfig` | **600** | dmg **24** |
| Scorpion | `ScorpionStatsData` | **8000** | unchanged |
| Shadow Revenant | `ShadowRevenantConfig` | **100000** | proj/charge/fog slightly tuned |
| Scorpion Boss | `ScorpionBossStatsData` | **150000** | jaw **20**, sting **65**, attackDistance **7** |

HP order: 450 < 600 < 8000 << 100000 < 150000 ✓

### Phase 3 — Boss projectile mitigation
- `ScorpionStatsData.projectileDamageMultiplier` — boss asset **0.35** (arrow ~700 effective)
- `ShadowRevenantConfig.playerProjectileDamageMultiplier` — **0.4** (arrow ~800 effective at normal mult)
- `ScorpionScript` + `ShadowRevenantController` apply multiplier when damage source is `Projectile`

## Unity MCP verification (spot-check)
- Play Mode Level 1: wasps report **450 HP**; scorpions **8000**; ScorpionBoss **150000** sting **65**
- Needs full manual TTK/threat matrix per tier

## Manual Play Mode validation checklist
- [ ] Wasp (bare hands): 6–10 hits to kill; survivable in groups
- [ ] Shade minion (bare hands): harder than wasp, easier than scorpion
- [ ] Scorpion (hammer): ~10–15 hits; sting telegraphed and survivable
- [ ] Shadow Revenant (hammer + bow + boost): ~90–150 s; bow not instant delete
- [ ] Scorpion Boss (full kit): longest fight; sting scary but fair
- [ ] No console errors from balance changes

## Result
- Script/data integration: **Pass**
- Full combat feel: **Needs user Play Mode verification**

## Out of scope
- Arrow/FireBreath global damage retune (boss multipliers used instead)
- Level 1 scene layout / spawn count changes
- Trader/shop economy
