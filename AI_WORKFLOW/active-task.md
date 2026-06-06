# Active Task

## Task title
- Weapon ScriptableObject refactor

## Type
- Mixed (ScriptableObject + scripts + player prefab wiring + Play Mode verification)

## Owner
- **Cursor** — implementation complete
- **User** — Play Mode parity verification

## Current phase
- Scripts, WeaponData assets, and Player.prefab wiring complete; CI build blocked locally (UnityEngine refs unavailable in this environment)

## Goal
- Migrate per-weapon combat stats from `PlayerCombatBalanceData` / string arsenal switches into `WeaponData` SO + `Weapon` runtime component without gameplay changes.

## Changes made (2026-06-06)

### New types
- `Assets/Scripts/Data/Combat/WeaponData.cs` — weapon stats, category enum, legacy ID, capability flags
- `Assets/Scripts/Player/Combat/Weapon.cs` — owned loadout, equip/cycle, legacy bootstrap

### Default assets
- `Assets/Data/Combat/Weapons/*_Default.asset` (Bare Hands, Bow, Hammers, Armor Set)
- `Assets/Resources/Beavermania/Combat/Weapons/*_Default.asset` (runtime fallback)

### Integrations
- `BeaverPlayerBehaviour` — `weaponLoadout` ref, `EquippedWeaponData`, `ApplyEquippedWeaponVisuals`, legacy `Arsenal` sync preserved; removed dead `GroundAttack` field
- `AnimatedAttack` — ground/air damage from equipped `WeaponData`; roll attack stays on combat balance
- `SwordEffects` — fire breath + swing trails gated on weapon capability/category
- `PlayerCombatBalanceData` — weapon fields removed (keeps roll attack, health/stamina, non-weapon tuning)

### Prefab
- `Assets/Prefabs/Player/Otter_Shapekeys/Player.prefab` — `Weapon` component + catalog SO refs + `weaponLoadout` wired

## Manual Play Mode validation checklist
- [ ] Bare hands ground (50) / air kick (20) vs wasps unchanged
- [ ] Hammer melee (~700, 2m radius)
- [ ] Bow melee + arrow shot stamina (30)
- [ ] Armor set ground slash (200) + hurricane kick air (350)
- [ ] Fire breath HP cost + sword glare armor-only
- [ ] Shop pickup + arsenal browse cycles; legacy string `Arsenal` still populated
- [ ] Wasp hit VFX/SFX still correct per weapon
- [ ] Roll attack still 200 dmg

## Result
- Script/data/prefab integration: **Pass (pending Unity compile in Editor)**
- Full combat feel: **Needs user Play Mode verification**

## Out of scope
- Shop.cs / NPC_Basic.cs string API changes
- UI, save system, animation clip edits
