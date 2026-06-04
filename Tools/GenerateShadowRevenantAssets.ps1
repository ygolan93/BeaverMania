# Generates Shadow Revenant test arena assets (YAML) for Unity import.
$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path (Join-Path $root "Assets"))) { $root = Split-Path $PSScriptRoot -Parent }
$assets = Join-Path $root "Assets"
$dataDir = Join-Path $assets "Data\NPC\ShadowRevenant"
$prefabDir = Join-Path $assets "Prefabs\NPC\ShadowRevenant"
$scenePath = Join-Path $assets "Scenes\ShadowRevenantTestArena.unity"

function New-Guid32 { return ([guid]::NewGuid().ToString("N")).ToLower() }

function Write-Meta($path, $guid, $type = "DefaultImporter") {
    $folder = $null
    if ($type -eq "folder") {
        $content = @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
    }
    elseif ($type -eq "script") {
        $content = @"
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
    }
    elseif ($type -eq "prefab") {
        $content = @"
fileFormatVersion: 2
guid: $guid
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
    }
    elseif ($type -eq "material") {
        $content = @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
    }
    elseif ($type -eq "controller") {
        $content = @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 9100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
    }
    else {
        $content = @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
    }
    Set-Content -Path ($path + ".meta") -Value $content -NoNewline
}

New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
New-Item -ItemType Directory -Force -Path $prefabDir | Out-Null

$g = @{
    DataFolder = New-Guid32
    PrefabFolder = New-Guid32
    Config = New-Guid32
    Material = New-Guid32
    Controller = New-Guid32
    Projectile = New-Guid32
    Fog = New-Guid32
    Shade = New-Guid32
    Boss = New-Guid32
}

Write-Meta (Join-Path $assets "Data\NPC") $g.DataFolder "folder"
Write-Meta (Join-Path $assets "Data\NPC\ShadowRevenant") $g.PrefabFolder "folder"
Write-Meta (Join-Path $assets "Prefabs\NPC") (New-Guid32) "folder"
Write-Meta $prefabDir (New-Guid32) "folder"

$configPath = Join-Path $dataDir "ShadowRevenantConfig.asset"
$matPath = Join-Path $prefabDir "ShadowRevenantPlaceholder.mat"
$controllerPath = Join-Path $prefabDir "ShadowRevenant.controller"
$projectilePath = Join-Path $prefabDir "ShadowRevenantProjectile.prefab"
$fogPath = Join-Path $prefabDir "ShadowRevenantDreadFogZone.prefab"
$shadePath = Join-Path $prefabDir "ShadowRevenantShadeMinion.prefab"
$bossPath = Join-Path $prefabDir "ShadowRevenant.prefab"

Write-Meta $configPath $g.Config
Write-Meta $matPath $g.Material "material"
Write-Meta $controllerPath $g.Controller "controller"
Write-Meta $projectilePath $g.Projectile "prefab"
Write-Meta $fogPath $g.Fog "prefab"
Write-Meta $shadePath $g.Shade "prefab"
Write-Meta $bossPath $g.Boss "prefab"

$configYaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: acb0e34192624e9c8862e434012c1fab, type: 3}
  m_Name: ShadowRevenantConfig
  m_EditorClassIdentifier: 
  maxHealth: 3200
  normalDamageMultiplier: 1
  phasedDamageMultiplier: 0
  lightBrokenDamageMultiplier: 1.5
  aggroRange: 42
  leashRange: 70
  faceTurnSpeed: 8
  strafeSpeed: 4
  phaseCooldown: 8
  phaseDuration: 2.4
  phaseWindup: 0.35
  phaseTriggerRange: 18
  teleportMinRadius: 9
  teleportMaxRadius: 17
  teleportRaycastHeight: 12
  teleportClearanceRadius: 1.2
  teleportValidationAttempts: 12
  teleportGroundMask:
    serializedVersion: 2
    m_Bits: 1
  teleportObstructionMask:
    serializedVersion: 2
    m_Bits: 0
  projectilePrefab: {fileID: 7000007, guid: $($g.Projectile), type: 3}
  projectileDamage: 24
  projectileSpeed: 24
  projectileLifetime: 5
  projectileCooldown: 2.2
  projectileWindup: 0.45
  projectileRecover: 0.45
  projectileRange: 36
  fogPrefab: {fileID: 8000004, guid: $($g.Fog), type: 3}
  fogRadius: 5
  fogDuration: 5
  fogDamagePerTick: 10
  fogTickInterval: 0.75
  fogSlowPercent: 0.3
  fogCooldown: 7
  fogWindup: 0.55
  fogRecover: 0.6
  fogRange: 24
  shadeMinionPrefab: {fileID: 9000005, guid: $($g.Shade), type: 3}
  maxActiveMinions: 3
  summonCount: 2
  summonCooldown: 12
  summonWindup: 0.8
  summonRecover: 0.7
  shadeMoveSpeed: 6
  shadeDamage: 12
  shadeDamageCooldown: 1.2
  shadeLifetime: 18
  lightBreakVulnerableDuration: 4
  lightBreakStaggerSeconds: 0.45
  projectilePrewarmCount: 8
  projectileMaxActive: 24
  fogPrewarmCount: 4
  fogMaxActive: 8
  shadePrewarmCount: 3
  shadeMaxActive: 6
  deathDropPrefabs: []
  hitVfxPrefab: {fileID: 0}
  deathVfxPrefab: {fileID: 0}
  phaseVfxPrefab: {fileID: 0}
  lightBreakVfxPrefab: {fileID: 0}
"@

$materialYaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: ShadowRevenantPlaceholder
  m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _MainTex:
        m_Texture: {fileID: 0}
        m_Scale: {x: 1, y: 1}
        m_Offset: {x: 0, y: 0}
    m_Ints: []
    m_Floats: []
    m_Colors:
    - _Color: {r: 0.08, g: 0.07, b: 0.12, a: 1}
"@

$controllerYaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1102 &-7428654321098765432
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Idle
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions: []
  m_StateMachineBehaviours: []
  m_Position: {x: 300, y: 0, z: 0}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {fileID: 0}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: 
--- !u!1107 &-7428654321098765431
AnimatorStateMachine:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Base Layer
  m_ChildStates:
  - serializedVersion: 1
    m_State: {fileID: -7428654321098765432}
    m_Position: {x: 300, y: 0, z: 0}
  m_ChildStateMachines: []
  m_AnyStateTransitions: []
  m_EntryTransitions: []
  m_StateMachineTransitions: {}
  m_StateMachineBehaviours: []
  m_AnyStatePosition: {x: 50, y: 20, z: 0}
  m_EntryPosition: {x: 50, y: 120, z: 0}
  m_ExitPosition: {x: 800, y: 120, z: 0}
  m_ParentStateMachinePosition: {x: 800, y: 20, z: 0}
  m_DefaultState: {fileID: -7428654321098765432}
--- !u!91 &9100000
AnimatorController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: ShadowRevenant
  serializedVersion: 5
  m_AnimatorParameters:
  - m_Name: Phased
    m_Type: 4
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {fileID: 9100000}
  - m_Name: Attack
    m_Type: 9
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {fileID: 9100000}
  - m_Name: Stagger
    m_Type: 9
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {fileID: 9100000}
  - m_Name: Summon
    m_Type: 9
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {fileID: 9100000}
  - m_Name: Dead
    m_Type: 9
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {fileID: 9100000}
  m_AnimatorLayers:
  - serializedVersion: 5
    m_Name: Base Layer
    m_StateMachine: {fileID: -7428654321098765431}
    m_Mask: {fileID: 0}
    m_Motions: []
    m_Behaviours: []
    m_BlendingMode: 0
    m_SyncedLayerIndex: -1
    m_DefaultWeight: 0
    m_IKPass: 0
    m_SyncedLayerAffectsTiming: 0
    m_Controller: {fileID: 9100000}
"@

$projectileYaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &7000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 7000002}
  - component: {fileID: 7000003}
  - component: {fileID: 7000004}
  - component: {fileID: 7000005}
  - component: {fileID: 7000006}
  - component: {fileID: 7000007}
  m_Layer: 0
  m_Name: ShadowRevenantProjectile
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &7000002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 7000001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 0.45, y: 0.45, z: 0.45}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!33 &7000003
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 7000001}
  m_Mesh: {fileID: 10207, guid: 0000000000000000e000000000000000, type: 0}
--- !u!23 &7000004
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 7000001}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 2100000, guid: $($g.Material), type: 2}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 1
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {fileID: 0}
--- !u!135 &7000005
SphereCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 7000001}
  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_Enabled: 1
  serializedVersion: 2
  m_Radius: 0.5
  m_Center: {x: 0, y: 0, z: 0}
--- !u!54 &7000006
Rigidbody:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 7000001}
  serializedVersion: 2
  m_Mass: 1
  m_Drag: 0
  m_AngularDrag: 0
  m_UseGravity: 0
  m_IsKinematic: 0
  m_Interpolate: 0
  m_Constraints: 0
  m_CollisionDetection: 0
--- !u!114 &7000007
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 7000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4c0ab742291a4fddb6b16673e15bdf35, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  projectileRigidbody: {fileID: 7000006}
  projectileCollider: {fileID: 7000005}
  impactVfxPrefab: {fileID: 0}
"@

# Fog, Shade, Boss YAML - written in continuation file due to size
Write-Host "GUIDs:" ($g | ConvertTo-Json)
Set-Content $configPath $configYaml -NoNewline
Set-Content $matPath $materialYaml -NoNewline
Set-Content $controllerPath $controllerYaml -NoNewline
Set-Content $projectilePath $projectileYaml -NoNewline
Write-Host "Wrote config, material, controller, projectile. Boss/fog/shade/scene require builder completion."
