#!/usr/bin/env python3
"""Generate ScriptableObject .asset files from prefab Dialogue/Shop data."""
import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets" / "Data"

TRADER_DIALOGUE_GUID = "f7e8d9c0b1a2436587091e2d3f4a5b6c"
SHOP_PRICING_GUID = "e6d5c4b3a2918075647382910abcdef0"
COMBAT_BALANCE_GUID = "d4c3b2a1908f7e6d5c4b3a2918075640"


def new_guid():
    return uuid.uuid4().hex


def yaml_quote(s: str) -> str:
    s = s.replace("\\", "\\\\").replace("'", "''")
    return f"'{s}'"


def write_meta(asset_path: Path, guid: str):
    meta = asset_path.with_suffix(asset_path.suffix + ".meta")
    meta.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def write_dialogue_asset(path: Path, name: str, lines: list[str], is_boss: bool = False, trader_id: str = ""):
    guid = new_guid()
    path.parent.mkdir(parents=True, exist_ok=True)
    line_block = "\n".join(f"  - {yaml_quote(line)}" for line in lines)
    content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {TRADER_DIALOGUE_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  traderId: {yaml_quote(trader_id)}
  displayName: {yaml_quote(name.replace("DialogueData", "").replace("Data", ""))}
  dialogueLines:
{line_block}
  isBossDialogue: {1 if is_boss else 0}
  textSpeed: 0
  advanceObjectiveOnEnd: 1
  hasShop: 1
"""
    path.write_text(content, encoding="utf-8")
    write_meta(path, guid)
    return guid


def write_shop_asset(path: Path, name: str, prices: dict):
    guid = new_guid()
    path.parent.mkdir(parents=True, exist_ok=True)
    content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SHOP_PRICING_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  nutPrice: {prices['nutPrice']}
  applePrice: {prices['applePrice']}
  accessoryPrice: {prices['accessoryPrice']}
  goldBrickPrice: {prices['goldBrickPrice']}
  hammerPrice: {prices['hammerPrice']}
  bowAndArrowPrice: {prices['bowAndArrowPrice']}
  arrowBundlePrice: {prices['arrowBundlePrice']}
  swordAndShieldPrice: {prices['swordAndShieldPrice']}
  arrowsPerBundle: {prices['arrowsPerBundle']}
  bowStarterArrows: {prices['bowStarterArrows']}
"""
    path.write_text(content, encoding="utf-8")
    write_meta(path, guid)
    return guid


def write_combat_asset(path: Path):
    guid = new_guid()
    path.parent.mkdir(parents=True, exist_ok=True)
    content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {COMBAT_BALANCE_GUID}, type: 3}}
  m_Name: DefaultPlayerCombatBalanceData
  m_EditorClassIdentifier: 
  maxHealth: 1000
  maxStamina: 100
  rollAttackDamage: 200
  scorpionLightDamage: 15
  scorpionHeavyDamage: 30
  shroomHealPerTick: 2
  appleHealAmount: 500
  stoneThrowStaminaCost: 20
  attackRange: 0.5
  groundBeat: 0.3
  airBeat: 0.2
"""
    path.write_text(content, encoding="utf-8")
    write_meta(path, guid)
    return guid


def parse_unity_lines_block(block: str) -> list[str]:
    """Parse Unity YAML lines: list with optional multiline continuations."""
    if not block.startswith("lines:"):
        return []

    entries: list[str] = []
    current: list[str] = []
    in_entry = False

    for raw in block.splitlines()[1:]:
        if raw.startswith("  - "):
            if in_entry and current:
                entries.append(" ".join(part.strip() for part in current if part.strip()))
            current = [raw[4:]]
            in_entry = True
        elif in_entry and (raw.startswith("    ") or raw.startswith("\t")):
            current.append(raw.strip())
        elif in_entry and raw and not raw.startswith(" "):
            break
        elif in_entry and raw == "":
            continue

    if in_entry and current:
        entries.append(" ".join(part.strip() for part in current if part.strip()))

    cleaned = []
    for entry in entries:
        val = entry.strip()
        if (val.startswith("'") and val.endswith("'")) or (val.startswith('"') and val.endswith('"')):
            val = val[1:-1]
        val = val.replace("''", "'")
        try:
            val = val.encode("utf-8").decode("unicode_escape")
        except UnicodeDecodeError:
            pass
        cleaned.append(val)
    return cleaned


def extract_dialogue_by_gameobject_name(prefab_text: str, go_name: str) -> tuple[list[str], bool]:
    """Find Dialogue component block after a GameObject with m_Name: go_name."""
    pattern = rf"m_Name: {re.escape(go_name)}\b"
    best_lines: list[str] = []
    best_is_boss = False

    for match in re.finditer(pattern, prefab_text):
        chunk = prefab_text[match.start() : match.start() + 150000]
        lines_idx = chunk.find("lines:")
        if lines_idx < 0:
            continue

        end_idx = len(chunk)
        for marker in ("textSpeed:", "isBoss:", "Merchant:"):
            pos = chunk.find(marker, lines_idx + 6)
            if pos > lines_idx and pos < end_idx:
                end_idx = pos

        block = chunk[lines_idx:end_idx]
        parsed = parse_unity_lines_block(block)
        if len(parsed) > len(best_lines):
            best_lines = parsed
            best_is_boss = bool(re.search(r"isBoss: 1", chunk[lines_idx : end_idx + 50]))

    return best_lines, best_is_boss


def main():
    mappings = [
        ("FirstTraderDialogueData.asset", "FirstTraderDialogue", "first_trader"),
        ("OldBeaverDialogueData.asset", "BeavertownDialogue", "old_beaver"),
        ("SecondTraderDialogueData.asset", "SecondTraderDialogue", "second_trader"),
        ("FarmerDialogueData.asset", "FarmerDialogue", "farmer"),
        ("GuardDialogueData.asset", "GuardDialogue", "guard"),
        ("WatchtowerGuardDialogueData.asset", "WatchtowerDialogue", "watchtower_guard"),
        ("MilitaryTraderDialogueData.asset", "CampDialogue", "military_trader"),
        ("WoundedSoldierDialogueData.asset", "SurvivorDialogue", "wounded_soldier"),
        ("BeavusDialogueData.asset", "BeavusDialogue", "beavus"),
        ("BossDialogueData.asset", "BossDialogue", "boss"),
    ]

    prefab_sources = [
        ROOT / "Assets" / "Prefabs" / "Objects" / "UI" / "PlayerCanvas.prefab",
        ROOT / "Assets" / "Prefabs" / "Objects" / "UI" / "TraderPanel.prefab",
        ROOT / "Assets" / "Prefabs" / "Objects" / "UI" / "Combat Panel.prefab",
        ROOT / "Assets" / "Scenes" / "Tutorial.unity",
    ]

    combined_text = ""
    for p in prefab_sources:
        if p.exists():
            combined_text += p.read_text(encoding="utf-8", errors="replace") + "\n"

    dialogue_guids = {}
    for asset_name, go_name, trader_id in mappings:
        lines, is_boss = extract_dialogue_by_gameobject_name(combined_text, go_name)
        if not lines and go_name == "SecondTraderDialogue":
            lines, is_boss = extract_dialogue_by_game_object_from_scene(combined_text, go_name)
        if not lines and go_name == "CampDialogue":
            # try Tutorial CampDialogue name in scene
            lines, is_boss = extract_dialogue_by_gameobject_name(combined_text, "CampDialogue")
        path = ASSETS / "Dialogue" / asset_name
        if lines:
            dialogue_guids[asset_name] = write_dialogue_asset(
                path, asset_name.replace(".asset", ""), lines, is_boss, trader_id
            )
            print(f"Created {path} ({len(lines)} lines)")
        else:
            print(f"WARN: No lines for {go_name}, skipping {asset_name}")

    default_prices = dict(
        nutPrice=3,
        applePrice=5,
        accessoryPrice=60,
        goldBrickPrice=150,
        hammerPrice=40,
        bowAndArrowPrice=120,
        arrowBundlePrice=25,
        swordAndShieldPrice=150,
        arrowsPerBundle=10,
        bowStarterArrows=5,
    )
    camp_prices = dict(
        nutPrice=3,
        applePrice=5,
        accessoryPrice=60,
        goldBrickPrice=150,
        hammerPrice=80,
        bowAndArrowPrice=100,
        arrowBundlePrice=10,
        swordAndShieldPrice=200,
        arrowsPerBundle=20,
        bowStarterArrows=7,
    )

    shop_guids = {
        "DefaultShopPricingData.asset": write_shop_asset(
            ASSETS / "Shop" / "DefaultShopPricingData.asset", "DefaultShopPricingData", default_prices
        ),
        "CampShopPricingData.asset": write_shop_asset(
            ASSETS / "Shop" / "CampShopPricingData.asset", "CampShopPricingData", camp_prices
        ),
    }
    write_combat_asset(ASSETS / "Combat" / "DefaultPlayerCombatBalanceData.asset")
    print("Done. Dialogue GUIDs:", dialogue_guids)
    print("Shop GUIDs:", shop_guids)


def extract_dialogue_by_game_object_from_scene(text, go_name):
    return extract_dialogue_by_gameobject_name(text, go_name)


if __name__ == "__main__":
    main()
