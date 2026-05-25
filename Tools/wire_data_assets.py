#!/usr/bin/env python3
"""Wire ScriptableObject references into prefabs and scenes."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

DIALOGUE_SCRIPT_GUID = "d3c53d8ec40a9cf4081d0219df78e083"
SHOP_SCRIPT_GUID = "ef1034112eefcab43943d67651513d78"
PLAYER_SCRIPT_GUID = "8c489014c99e2174688db9b08f27a14e"

DIALOGUE_BY_GO_NAME = {
    "FirstTraderDialogue": "fd19deed4727491b9e563df133c54d0e",
    "BeavertownDialogue": "c9581441312b46b8a5bf2b70f5f8fc8a",
    "SecondTraderDialogue": "267790fbc2b5459eb31cd22f6b72c917",
    "FarmerDialogue": "6c79e200a1554643b07f7e4bb56f1d1c",
    "GuardDialogue": "baf2b1f4983141069ee261ea591a597d",
    "WatchtowerDialogue": "79f2cfb928044ea3a35d146af8d061ba",
    "CampDialogue": "a5a95eef33b1469ba59db348a7494d62",
    "SurvivorDialogue": "c35ce8e7637747e58bc6b74341d99ba2",
    "BeavusDialogue": "563dd627ce1245efb3a6eb108f9ac1dc",
    "BossDialogue": "fdd678ca5e5b4d379d46ec159d47b897",
}

DEFAULT_SHOP_GUID = "ab9379e0791a4120b0df519c28b3de32"
CAMP_SHOP_GUID = "8a548dd3a4b2467a867a1b599a161140"
COMBAT_BALANCE_GUID = "4bb4a81501974e79857a438e1623725b"

CAMP_SHOP_GO_SUBSTRINGS = (
    "FirstTrader Shop",
    "CampTrader",
    "Camp Shop",
    "CampDialogue",
)

TARGET_FILES = [
    ROOT / "Assets" / "Prefabs" / "Objects" / "UI" / "PlayerCanvas.prefab",
    ROOT / "Assets" / "Prefabs" / "Objects" / "UI" / "TraderPanel.prefab",
    ROOT / "Assets" / "Prefabs" / "Objects" / "UI" / "Combat Panel.prefab",
    ROOT / "Assets" / "Prefabs" / "Objects" / "UI" / "GuardPanel.prefab",
    ROOT / "Assets" / "Scenes" / "Tutorial.unity",
    ROOT / "Assets" / "Prefabs" / "OtterPlayer" / "Player Updated.prefab",
    ROOT / "Assets" / "Prefabs" / "OtterPlayer" / "OtterAnimations" / "OtterPlayer.prefab",
]


def build_go_name_map(text: str) -> dict[str, str]:
    mapping = {}
    for match in re.finditer(r"--- !u!1 &(\d+)\nGameObject:(.*?)(?=\n--- !u!)", text, re.DOTALL):
        name_match = re.search(r"\n  m_Name: (.+)", match.group(2))
        if name_match:
            mapping[match.group(1)] = name_match.group(1).strip()
    return mapping


def so_ref(guid: str) -> str:
    return f"{{fileID: 11400000, guid: {guid}, type: 2}}"


def upsert_field(block: str, field: str, value_line: str) -> str:
    pattern = rf"^  {re.escape(field)}:.*$"
    if re.search(pattern, block, flags=re.MULTILINE):
        return re.sub(pattern, f"  {value_line}", block, count=1, flags=re.MULTILINE)
    if field == "dialogueData":
        insert_after = re.search(r"^  panel:.*$", block, flags=re.MULTILINE)
        if insert_after:
            pos = insert_after.end()
            return block[:pos] + f"\n  {value_line}" + block[pos:]
    insert_after = re.search(r"^  Player:.*$", block, flags=re.MULTILINE)
    if insert_after and field == "pricingData":
        pos = insert_after.end()
        return block[:pos] + f"\n  {value_line}" + block[pos:]
    if field == "pricingData":
        insert_after = re.search(r"^  bowStarterArrows:.*$", block, flags=re.MULTILINE)
        if insert_after:
            pos = insert_after.end()
            return block[:pos] + f"\n  {value_line}" + block[pos:]

    insert_after = re.search(r"^  rb:.*$", block, flags=re.MULTILINE)
    if insert_after and field == "combatBalance":
        pos = insert_after.end()
        return block[:pos] + f"\n  {value_line}" + block[pos:]
    return block.rstrip() + f"\n  {value_line}\n"


def iter_monobehaviour_blocks(text: str):
    for match in re.finditer(r"--- !u!114 &\d+\nMonoBehaviour:\n", text):
        start = match.start()
        end = text.find("\n--- !u!", match.end())
        if end < 0:
            end = len(text)
        yield text[start:end]


def patch_dialogue_components(text: str) -> tuple[str, int]:
    go_names = build_go_name_map(text)
    count = 0
    for block in iter_monobehaviour_blocks(text):
        if DIALOGUE_SCRIPT_GUID not in block:
            continue
        if f"guid: {DIALOGUE_SCRIPT_GUID}" not in block:
            continue
        go_id_match = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
        if not go_id_match:
            continue
        go_name = go_names.get(go_id_match.group(1), "")
        asset_guid = DIALOGUE_BY_GO_NAME.get(go_name)
        if not asset_guid:
            continue
        new_block = upsert_field(block, "dialogueData", f"dialogueData: {so_ref(asset_guid)}")
        if new_block != block:
            text = text.replace(block, new_block, 1)
            count += 1
    return text, count


def patch_shop_components(text: str) -> tuple[str, int]:
    go_names = build_go_name_map(text)
    count = 0
    for block in iter_monobehaviour_blocks(text):
        if f"guid: {SHOP_SCRIPT_GUID}" not in block:
            continue
        go_id_match = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
        go_name = go_names.get(go_id_match.group(1), "") if go_id_match else ""
        use_camp = any(s in go_name for s in CAMP_SHOP_GO_SUBSTRINGS)
        if not use_camp and "hammerPrice: 80" in block:
            use_camp = True
        guid = CAMP_SHOP_GUID if use_camp else DEFAULT_SHOP_GUID
        new_block = upsert_field(block, "pricingData", f"pricingData: {so_ref(guid)}")
        if new_block != block:
            text = text.replace(block, new_block, 1)
            count += 1
    return text, count


def patch_player_components(text: str) -> tuple[str, int]:
    count = 0
    for block in iter_monobehaviour_blocks(text):
        if f"guid: {PLAYER_SCRIPT_GUID}" not in block:
            continue
        new_block = upsert_field(block, "combatBalance", f"combatBalance: {so_ref(COMBAT_BALANCE_GUID)}")
        if new_block != block:
            text = text.replace(block, new_block, 1)
            count += 1
    return text, count


def main():
    for path in TARGET_FILES:
        if not path.exists():
            print(f"SKIP missing {path}")
            continue
        text = path.read_text(encoding="utf-8")
        text, d_count = patch_dialogue_components(text)
        text, s_count = patch_shop_components(text)
        text, p_count = patch_player_components(text)
        path.write_text(text, encoding="utf-8")
        print(f"{path.name}: dialogue={d_count} shop={s_count} player={p_count}")


if __name__ == "__main__":
    main()
