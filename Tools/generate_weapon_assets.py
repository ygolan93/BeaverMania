#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT_GUID = "24d9b57ebe58486aaa170e06f0a80991"

weapons = [
    ("BareHands_Default", "7d55fcfac023418eb89d67f90b16ba48", "9c77fd4325284364906548f1aab50394", {
        "displayName": "Bare Hands", "legacyArsenalId": "Bare Hands", "category": 0,
        "groundMeleeDamage": 50, "airMeleeDamage": 20, "groundAttackRadius": 0.7, "airAttackRadius": 2.5,
        "groundAttackOriginYOffset": 0, "bowShotStaminaCost": 0, "fireBreathHealthCostPercent": 0,
        "supportsFireBreath": 0, "supportsSwordGlare": 0,
    }),
    ("Bow_Default", "1ba56638a127400cb78776fcec4c2f01", "8717e5a6425a4ae396f8763739796c08", {
        "displayName": "Bow", "legacyArsenalId": "Bow", "category": 2,
        "groundMeleeDamage": 50, "airMeleeDamage": 20, "groundAttackRadius": 1.0, "airAttackRadius": 2.5,
        "groundAttackOriginYOffset": 0, "bowShotStaminaCost": 30, "fireBreathHealthCostPercent": 0,
        "supportsFireBreath": 0, "supportsSwordGlare": 0,
    }),
    ("Hammers_Default", "c262b2ff23a34d479a19c8777c3be256", "875e8c2ede164c37bf538fcc7419505b", {
        "displayName": "Hammers", "legacyArsenalId": "Hammers", "category": 1,
        "groundMeleeDamage": 700, "airMeleeDamage": 20, "groundAttackRadius": 2.0, "airAttackRadius": 2.5,
        "groundAttackOriginYOffset": 0, "bowShotStaminaCost": 0, "fireBreathHealthCostPercent": 0,
        "supportsFireBreath": 0, "supportsSwordGlare": 0,
    }),
    ("ArmorSet_Default", "ceef7d2283b240ec95143f951d88c0dd", "061e66e7c0a24feaa3b7508a66f5e7ec", {
        "displayName": "Armor Set", "legacyArsenalId": "ArmorSet", "category": 3,
        "groundMeleeDamage": 200, "airMeleeDamage": 350, "groundAttackRadius": 4.0, "airAttackRadius": 4.0,
        "groundAttackOriginYOffset": 0.5, "bowShotStaminaCost": 0, "fireBreathHealthCostPercent": 20,
        "supportsFireBreath": 1, "supportsSwordGlare": 1,
    }),
]


def write_asset(path, guid, name, fields):
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
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  displayName: {fields['displayName']}
  legacyArsenalId: {fields['legacyArsenalId']}
  category: {fields['category']}
  groundMeleeDamage: {fields['groundMeleeDamage']}
  airMeleeDamage: {fields['airMeleeDamage']}
  groundAttackRadius: {fields['groundAttackRadius']}
  airAttackRadius: {fields['airAttackRadius']}
  groundAttackOriginYOffset: {fields['groundAttackOriginYOffset']}
  bowShotStaminaCost: {fields['bowShotStaminaCost']}
  fireBreathHealthCostPercent: {fields['fireBreathHealthCostPercent']}
  supportsFireBreath: {fields['supportsFireBreath']}
  supportsSwordGlare: {fields['supportsSwordGlare']}
"""
    path.write_text(content, encoding="utf-8")
    meta = path.with_suffix(path.suffix + ".meta")
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


if __name__ == "__main__":
    for name, data_guid, res_guid, fields in weapons:
        write_asset(ROOT / "Assets/Data/Combat/Weapons" / f"{name}.asset", data_guid, name, fields)
        write_asset(ROOT / "Assets/Resources/Beavermania/Combat/Weapons" / f"{name}.asset", res_guid, name, fields)
    print("Created weapon assets")
