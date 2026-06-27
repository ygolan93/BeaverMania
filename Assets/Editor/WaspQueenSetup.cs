using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BeavermaniaEditor
{
    // Build tooling for the Wasp Queen boss animation assets.
    // Configures the FBX importer, builds the AnimatorController, and creates the boss prefab.
    // The runtime boss controller + WaspQueenAnimationEvents bridge are owned by Codex and added separately.
    public static class WaspQueenSetup
    {
        const string Fbx = "Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx";
        const string ControllerPath = "Assets/Prefabs/WaspQueen/Animations/WaspQueen.controller";
        const string PrefabPath = "Assets/Prefabs/WaspQueen/WaspQueen.prefab";
        // Controller referenced by PF_WaspQueen_Boss.prefab (rebuilt in place to preserve its GUID).
        const string BossControllerPath = "Assets/Prefabs/NPC/WaspQueen/WaspQueen.controller";
        const float Fps = 30f;

        static string Logical(string n)
        {
            int i = n.IndexOf("WQ_");
            return i >= 0 ? n.Substring(i) : n;
        }

        // NOTE: ModelImporterClipAnimation treats AnimationEvent.time as a NORMALIZED 0..1
        // fraction of the clip and multiplies it by the clip length on import. So events are
        // specified here as clip fractions, never as seconds.
        static AnimationEvent[] EventsFor(string logical)
        {
            var specs = new List<(string fn, float frac)>();
            switch (logical)
            {
                case "WQ_Ranged_Fire": specs.Add(("FireProjectile", 0.45f)); break;
                case "WQ_AoE_Release": specs.Add(("ActivateAoE", 0.48f)); break;
                case "WQ_Charge_Dash": specs.Add(("EnableChargeHitbox", 0.08f)); break;
                case "WQ_Summon_Release": specs.Add(("SummonWasps", 0.55f)); break;
                case "WQ_Phase_Transition": specs.Add(("PhasePulse", 0.50f)); break;
                case "WQ_Death": specs.Add(("ExplodeFragments", 0.87f)); break;
                case "WQ_Intro_Roar": specs.Add(("QueenScream", 0.27f)); break;
            }
            var evts = new AnimationEvent[specs.Count];
            for (int i = 0; i < specs.Count; i++)
                evts[i] = new AnimationEvent { time = specs[i].frac, functionName = specs[i].fn };
            return evts;
        }

        [MenuItem("Beavermania/WaspQueen/1 Configure Importer")]
        public static void ConfigureImporter()
        {
            var imp = AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if (imp == null) { Debug.LogError("WaspQueen FBX importer not found at " + Fbx); return; }
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.importAnimation = true;
            imp.resampleCurves = true;
            imp.importConstraints = false;
            imp.importVisibility = false;
            imp.importCameras = false;
            imp.importLights = false;
            var defs = imp.defaultClipAnimations;
            var list = new List<ModelImporterClipAnimation>();
            foreach (var d in defs)
            {
                var c = d;
                string logical = Logical(d.name);
                c.name = logical;
                c.loopTime = (logical == "WQ_Idle_Combat" || logical == "WQ_Charge_Dash");
                c.events = EventsFor(logical);
                list.Add(c);
            }
            imp.clipAnimations = list.ToArray();
            EditorUtility.SetDirty(imp);
            imp.SaveAndReimport();
            Debug.Log("WaspQueen importer configured: " + list.Count + " clips (Generic, root motion off).");
        }

        static Dictionary<string, AnimationClip> LoadClips()
        {
            var dict = new Dictionary<string, AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Fbx))
            {
                var cl = o as AnimationClip;
                if (cl != null && !cl.name.StartsWith("__")) dict[Logical(cl.name)] = cl;
            }
            return dict;
        }

        static AnimatorStateTransition Chain(AnimatorState a, AnimatorState b, float exitTime)
        {
            var t = a.AddTransition(b);
            t.hasExitTime = true; t.exitTime = exitTime; t.hasFixedDuration = true; t.duration = 0.05f;
            return t;
        }

        static AnimatorStateTransition Trig(AnimatorState a, AnimatorState b, string trigger)
        {
            var t = a.AddTransition(b);
            t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.08f;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            return t;
        }

        [MenuItem("Beavermania/WaspQueen/2 Build Controller")]
        public static void BuildController()
        {
            var clips = LoadClips();
            if (clips.Count == 0) { Debug.LogError("No WaspQueen clips found; configure importer first."); return; }
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (var tr in new[] { "Intro", "RangedAttack", "PoisonAoE", "Charge", "Summon", "PhaseTransition", "Die", "HitLight" })
                ac.AddParameter(tr, AnimatorControllerParameterType.Trigger);
            foreach (var bp in new[] { "IsActive", "IsDead", "IsFlying", "IsEnraged" })
                ac.AddParameter(bp, AnimatorControllerParameterType.Bool);
            ac.AddParameter("Phase", AnimatorControllerParameterType.Int);
            ac.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            ac.AddParameter("DistanceToPlayer", AnimatorControllerParameterType.Float);
            var sm = ac.layers[0].stateMachine;
            AnimatorState St(string clip, float x, float y)
            {
                AnimationClip c; clips.TryGetValue(clip, out c);
                var s = sm.AddState(clip.Substring(3), new Vector3(x, y, 0));
                s.motion = c; s.writeDefaultValues = true; return s;
            }
            var idle = St("WQ_Idle_Combat", 300, 0);
            var intro = St("WQ_Intro_Roar", 0, -80);
            var rTel = St("WQ_Ranged_Telegraph", 600, -80);
            var rFire = St("WQ_Ranged_Fire", 800, -80);
            var aTel = St("WQ_AoE_Telegraph", 600, 0);
            var aRel = St("WQ_AoE_Release", 800, 0);
            var cTel = St("WQ_Charge_Telegraph", 600, 80);
            var cDash = St("WQ_Charge_Dash", 800, 80);
            var cRec = St("WQ_Charge_Recovery", 1000, 80);
            var sTel = St("WQ_Summon_Telegraph", 600, 160);
            var sRel = St("WQ_Summon_Release", 800, 160);
            var phase = St("WQ_Phase_Transition", 300, 200);
            var death = St("WQ_Death", 300, -160);
            sm.defaultState = idle;
            Trig(idle, intro, "Intro"); Chain(intro, idle, 0.92f);
            Trig(idle, rTel, "RangedAttack"); Chain(rTel, rFire, 0.95f); Chain(rFire, idle, 0.9f);
            Trig(idle, aTel, "PoisonAoE"); Chain(aTel, aRel, 0.95f); Chain(aRel, idle, 0.9f);
            Trig(idle, cTel, "Charge"); Chain(cTel, cDash, 0.95f); Chain(cDash, cRec, 0.95f); Chain(cRec, idle, 0.9f);
            Trig(idle, sTel, "Summon"); Chain(sTel, sRel, 0.95f); Chain(sRel, idle, 0.9f);
            Trig(idle, phase, "PhaseTransition"); Chain(phase, idle, 0.95f);
            var anyDie = sm.AddAnyStateTransition(death);
            anyDie.hasExitTime = false; anyDie.hasFixedDuration = true; anyDie.duration = 0.05f; anyDie.canTransitionToSelf = false;
            anyDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            Debug.Log("WaspQueen.controller built with 13 states.");
        }

        // Rebuilds the controller that PF_WaspQueen_Boss references (guid preserved by writing in place).
        // Uses the boss script's exact trigger names so the new WQ_* clips play off WaspQueenBoss SetTrigger calls.
        [MenuItem("Beavermania/WaspQueen/4 Build Boss Controller")]
        public static void BuildBossController()
        {
            var clips = LoadClips();
            if (clips.Count == 0) { Debug.LogError("No WaspQueen clips found; configure importer first."); return; }
            // Mutate the existing asset in place so the prefab's controller GUID is preserved.
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(BossControllerPath);
            if (ac == null) ac = AnimatorController.CreateAnimatorControllerAtPath(BossControllerPath);
            else ClearController(ac);
            foreach (var tr in new[] { "RangedAttack", "PoisonAoE", "Charge", "Summon", "PhaseTransition", "Die", "Sting", "Hit", "Intro" })
                ac.AddParameter(tr, AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            AnimatorState St(string clip, string stateName, float x, float y)
            {
                AnimationClip c; clips.TryGetValue(clip, out c);
                if (c == null) Debug.LogError("Missing clip for boss controller: " + clip);
                var s = sm.AddState(stateName, new Vector3(x, y, 0));
                s.motion = c; s.writeDefaultValues = true; return s;
            }
            var idle = St("WQ_Idle_Combat", "Idle", 300, 0);
            var intro = St("WQ_Intro_Roar", "Intro", 0, -80);
            var rTel = St("WQ_Ranged_Telegraph", "Ranged_Telegraph", 600, -80);
            var rFire = St("WQ_Ranged_Fire", "Ranged_Fire", 800, -80);
            var aTel = St("WQ_AoE_Telegraph", "AoE_Telegraph", 600, 0);
            var aRel = St("WQ_AoE_Release", "AoE_Release", 800, 0);
            var cTel = St("WQ_Charge_Telegraph", "Charge_Telegraph", 600, 80);
            var cDash = St("WQ_Charge_Dash", "Charge_Dash", 800, 80);
            var cRec = St("WQ_Charge_Recovery", "Charge_Recovery", 1000, 80);
            var sTel = St("WQ_Summon_Telegraph", "Summon_Telegraph", 600, 160);
            var sRel = St("WQ_Summon_Release", "Summon_Release", 800, 160);
            var phase = St("WQ_Phase_Transition", "Phase_Transition", 300, 200);
            var sting = St("WQ_Sting", "Sting", 600, 280);
            var hit = St("WQ_Hit_Light", "Hit", 0, 80);
            var death = St("WQ_Death", "Death", 300, -180);
            sm.defaultState = idle;
            Trig(idle, intro, "Intro"); Chain(intro, idle, 0.92f);
            Trig(idle, rTel, "RangedAttack"); Chain(rTel, rFire, 0.95f); Chain(rFire, idle, 0.9f);
            Trig(idle, aTel, "PoisonAoE"); Chain(aTel, aRel, 0.95f); Chain(aRel, idle, 0.9f);
            Trig(idle, cTel, "Charge"); Chain(cTel, cDash, 0.95f); Chain(cRec, idle, 0.9f);
            Trig(idle, sTel, "Summon"); Chain(sTel, sRel, 0.95f); Chain(sRel, idle, 0.9f);
            Trig(idle, phase, "PhaseTransition"); Chain(phase, idle, 0.95f);
            Trig(idle, sting, "Sting"); Chain(sting, idle, 0.9f);
            var anyHit = sm.AddAnyStateTransition(hit);
            anyHit.hasExitTime = false; anyHit.hasFixedDuration = true; anyHit.duration = 0.05f; anyHit.canTransitionToSelf = false;
            anyHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            Chain(hit, idle, 0.85f);
            var anyDie = sm.AddAnyStateTransition(death);
            anyDie.hasExitTime = false; anyDie.hasFixedDuration = true; anyDie.duration = 0.05f; anyDie.canTransitionToSelf = false;
            anyDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            Debug.Log("Boss WaspQueen.controller rebuilt at " + BossControllerPath + " with 15 states.");
        }

        // Clears parameters, states, any-state transitions, and sub-state-machines from layer 0
        // without deleting the controller asset (preserves its GUID for the prefab reference).
        static void ClearController(AnimatorController ac)
        {
            while (ac.parameters.Length > 0) ac.RemoveParameter(0);
            var sm = ac.layers[0].stateMachine;
            foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
            foreach (var s in sm.stateMachines) sm.RemoveStateMachine(s.stateMachine);
            foreach (var s in sm.states) sm.RemoveState(s.state);
        }

        static Transform AddChild(GameObject root, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        [MenuItem("Beavermania/WaspQueen/3 Build Prefab")]
        public static void BuildPrefab()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (model == null) { Debug.LogError("WaspQueen FBX model not found."); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.name = "WaspQueen";
            var anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            anim.applyRootMotion = false;
            AddChild(inst, "ProjectileSpawnPoint", new Vector3(0f, 1.0f, 0.6f));
            AddChild(inst, "AoEOrigin", new Vector3(0f, 0.05f, 0.2f));
            AddChild(inst, "WaspSpawnPoint_1", new Vector3(0.6f, 1.2f, -0.2f));
            AddChild(inst, "WaspSpawnPoint_2", new Vector3(-0.6f, 1.2f, -0.2f));
            AddChild(inst, "WaspSpawnPoint_3", new Vector3(0f, 1.6f, -0.3f));
            var hb = AddChild(inst, "ChargeHitbox", new Vector3(0f, 0.8f, 0.5f));
            var box = hb.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(1.2f, 1.2f, 1.4f);
            hb.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath);
            Object.DestroyImmediate(inst);
            Debug.Log("WaspQueen.prefab created at " + PrefabPath);
        }

        [MenuItem("Beavermania/WaspQueen/Verify Clips")]
        public static void VerifyClips()
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Fbx))
            {
                var cl = o as AnimationClip;
                if (cl == null || cl.name.StartsWith("__")) continue;
                var evs = AnimationUtility.GetAnimationEvents(cl);
                string s = cl.name + " len=" + cl.length.ToString("0.###") + "s loop=" + cl.isLooping + " events=";
                foreach (var e in evs) s += e.functionName + "@" + e.time.ToString("0.###") + "s ";
                Debug.Log("[WQ Verify] " + s);
                _verifyLines.Add(s);
            }
            System.IO.File.WriteAllLines("Temp/wq_verify.txt", _verifyLines.ToArray());
            _verifyLines.Clear();
        }
        static readonly List<string> _verifyLines = new List<string>();

        [MenuItem("Beavermania/WaspQueen/Build All")]
        public static void BuildAll()
        {
            ConfigureImporter();
            BuildController();
            BuildPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("WaspQueen Build All complete.");
        }
    }
}
