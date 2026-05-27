#if UNITY_EDITOR
using Beavermania.Data.Player;
using UnityEditor;
using UnityEngine;

namespace Beavermania.Player.AI.Editor
{
    [CustomEditor(typeof(AutoPlayerPlaystyleProfile))]
    public class AutoPlayerPlaystyleProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var profile = (AutoPlayerPlaystyleProfile)target;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Sessions: {profile.sessionCount}  Samples: {profile.totalSamples}");
            EditorGUILayout.LabelField($"Engage radius: {profile.learnedEngageRadius:F1}  Sprint dist: {profile.learnedSprintDistance:F1}");
            EditorGUILayout.LabelField($"Combat buckets: {profile.combatBuckets.Count}");

            if (GUILayout.Button("Clear Profile Data"))
            {
                profile.combatBuckets.Clear();
                profile.totalSamples = 0;
                profile.sessionCount = 0;
                profile.combatSampleCount = 0;
                profile.bridgeActivityCount = 0;
                profile.exploreActivityCount = 0;
                EditorUtility.SetDirty(profile);
            }
        }
    }

    [CustomEditor(typeof(AutoPlayerManualPlayRecorder))]
    public class AutoPlayerManualPlayRecorderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var recorder = (AutoPlayerManualPlayRecorder)target;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Recording: {recorder.IsRecording}  Samples: {recorder.SessionSampleCount}");

            if (GUILayout.Button("Start Recording"))
                recorder.StartRecording();
            if (GUILayout.Button("Stop Recording"))
                recorder.StopRecording();
            if (GUILayout.Button("Clear Session"))
                recorder.ClearSession();
            if (GUILayout.Button("Bake Session To Profile"))
                recorder.BakeSessionToProfile();
        }
    }
}
#endif
