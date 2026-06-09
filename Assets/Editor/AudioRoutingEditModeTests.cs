#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Beavermania.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Beavermania.Tests.Audio
{
    public sealed class AudioRoutingEditModeTests
    {
        const string MixerPath = "Assets/Scripts/SceneScripts/NewAudioMixer.mixer";
        const string AudioMixerFieldName = "audioMixer";
        const string MusicGroupFieldName = "musicGroup";
        const string SfxGroupFieldName = "sfxGroup";
        const string EnemiesGroupFieldName = "enemiesGroup";
        const string UiGroupFieldName = "uiGroup";
        const string InstanceBackingFieldName = "<Instance>k__BackingField";
        const float Tolerance = 0.0001f;

        AudioMixer mixer;
        AudioVolumeSettings settings;
        GameObject settingsGameObject;
        GameObject sourceGameObject;
        AudioVolumeSettings previousInstance;
        float originalMaster;
        float originalMusic;
        float originalSfx;
        float originalEnemies;
        float originalListenerVolume;

        [SetUp]
        public void SetUp()
        {
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer, Is.Not.Null, $"Expected mixer asset at '{MixerPath}'.");

            CaptureMixerState();
            originalListenerVolume = AudioListener.volume;
            previousInstance = GetRegisteredInstance();

            settingsGameObject = new GameObject("AudioVolumeSettingsTestHost");
            settings = settingsGameObject.AddComponent<AudioVolumeSettings>();
            SetObjectReference(settings, AudioMixerFieldName, mixer);
            RegisterInstance(settings);
        }

        [TearDown]
        public void TearDown()
        {
            RestoreMixerState();
            AudioListener.volume = originalListenerVolume;

            PlayerPrefs.DeleteKey(AudioVolumeSettings.MasterVolumePrefKey);
            PlayerPrefs.DeleteKey(AudioVolumeSettings.MusicVolumePrefKey);
            PlayerPrefs.DeleteKey(AudioVolumeSettings.SfxVolumePrefKey);
            PlayerPrefs.Save();

            if (sourceGameObject != null)
                Object.DestroyImmediate(sourceGameObject);

            if (settingsGameObject != null)
                Object.DestroyImmediate(settingsGameObject);

            RegisterInstance(previousInstance);
        }

        [Test]
        public void ApplyMusicVolume_ChangesOnlyMusicVolume()
        {
            float masterBefore = GetMixerValue("MasterVolume");
            float sfxBefore = GetMixerValue("SfxVolume");
            float enemiesBefore = GetMixerValue("EnemiesVolume");

            settings.ApplyMusicVolume(0.25f, save: false);

            Assert.That(GetMixerValue("MusicVolume"), Is.EqualTo(AudioVolumeSettings.LinearToDecibels(0.25f)).Within(Tolerance));
            Assert.That(GetMixerValue("MasterVolume"), Is.EqualTo(masterBefore).Within(Tolerance));
            Assert.That(GetMixerValue("SfxVolume"), Is.EqualTo(sfxBefore).Within(Tolerance));
            Assert.That(GetMixerValue("EnemiesVolume"), Is.EqualTo(enemiesBefore).Within(Tolerance));
        }

        [Test]
        public void ApplySfxVolume_ChangesSfxAndEnemiesOnly()
        {
            float masterBefore = GetMixerValue("MasterVolume");
            float musicBefore = GetMixerValue("MusicVolume");

            settings.ApplySfxVolume(0.5f, save: false);

            float expectedDb = AudioVolumeSettings.LinearToDecibels(0.5f);
            Assert.That(GetMixerValue("SfxVolume"), Is.EqualTo(expectedDb).Within(Tolerance));
            Assert.That(GetMixerValue("EnemiesVolume"), Is.EqualTo(expectedDb).Within(Tolerance));
            Assert.That(GetMixerValue("MasterVolume"), Is.EqualTo(masterBefore).Within(Tolerance));
            Assert.That(GetMixerValue("MusicVolume"), Is.EqualTo(musicBefore).Within(Tolerance));
        }

        [Test]
        public void ApplyMasterVolume_DoesNotMutateAudioListenerVolume()
        {
            AudioListener.volume = 0.42f;

            settings.ApplyMasterVolume(0.8f, save: false);

            Assert.That(AudioListener.volume, Is.EqualTo(0.42f).Within(Tolerance));
            Assert.That(GetMixerValue("MasterVolume"), Is.EqualTo(AudioVolumeSettings.LinearToDecibels(0.8f)).Within(Tolerance));
        }

        [Test]
        public void EnsureRoute_AssignsMusicRoute_WhenSourceIsUnassigned()
        {
            AudioSource source = CreateUnassignedSource();

            bool routed = AudioSourceRouting.EnsureRoute(source, AudioSourceRoute.Music);

            Assert.That(routed, Is.True);
            Assert.That(source.outputAudioMixerGroup, Is.EqualTo(GetMixerGroup("Music")));
        }

        [Test]
        public void EnsureRoute_AssignsSfxRoute_WhenSourceIsUnassigned()
        {
            AudioSource source = CreateUnassignedSource();

            bool routed = AudioSourceRouting.EnsureRoute(source, AudioSourceRoute.Sfx);

            Assert.That(routed, Is.True);
            Assert.That(source.outputAudioMixerGroup, Is.EqualTo(GetMixerGroup("SFX")));
        }

        [Test]
        public void EnsureRoute_AssignsEnemyRoute_WhenConfigured()
        {
            AudioSource source = CreateUnassignedSource();

            bool routed = AudioSourceRouting.EnsureRoute(source, AudioSourceRoute.Enemy);

            Assert.That(routed, Is.True);
            Assert.That(source.outputAudioMixerGroup, Is.EqualTo(GetMixerGroup("Enemies")));
        }

        [Test]
        public void EnsureRoute_AssignsUiRoute_WhenConfigured()
        {
            AudioSource source = CreateUnassignedSource();

            bool routed = AudioSourceRouting.EnsureRoute(source, AudioSourceRoute.UI);

            Assert.That(routed, Is.True);
            Assert.That(source.outputAudioMixerGroup, Is.EqualTo(GetMixerGroup("UI")));
        }

        [Test]
        public void EnsureRoute_EnemyFallsBackToSfx_WhenEnemiesGroupIsMissing()
        {
            AudioMixerGroup sfxGroup = GetMixerGroup("SFX");
            SetObjectReference(settings, AudioMixerFieldName, null);
            SetObjectReference(settings, MusicGroupFieldName, null);
            SetObjectReference(settings, SfxGroupFieldName, sfxGroup);
            SetObjectReference(settings, EnemiesGroupFieldName, null);
            SetObjectReference(settings, UiGroupFieldName, null);
            AudioSource source = CreateUnassignedSource();

            bool routed = AudioSourceRouting.EnsureRoute(source, AudioSourceRoute.Enemy);

            Assert.That(routed, Is.True);
            Assert.That(source.outputAudioMixerGroup, Is.EqualTo(sfxGroup));
        }

        AudioSource CreateUnassignedSource()
        {
            if (sourceGameObject != null)
                Object.DestroyImmediate(sourceGameObject);

            sourceGameObject = new GameObject("AudioRouteSource");
            return sourceGameObject.AddComponent<AudioSource>();
        }

        void CaptureMixerState()
        {
            originalMaster = GetMixerValue("MasterVolume");
            originalMusic = GetMixerValue("MusicVolume");
            originalSfx = GetMixerValue("SfxVolume");
            originalEnemies = GetMixerValue("EnemiesVolume");
        }

        void RestoreMixerState()
        {
            if (mixer == null)
                return;

            mixer.SetFloat("MasterVolume", originalMaster);
            mixer.SetFloat("MusicVolume", originalMusic);
            mixer.SetFloat("SfxVolume", originalSfx);
            mixer.SetFloat("EnemiesVolume", originalEnemies);
        }

        float GetMixerValue(string parameterName)
        {
            Assert.That(mixer.GetFloat(parameterName, out float value), Is.True, $"Expected exposed mixer parameter '{parameterName}'.");
            return value;
        }

        AudioMixerGroup GetMixerGroup(string groupName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(MixerPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AudioMixerGroup group && group.name == groupName)
                    return group;
            }

            Assert.Fail($"Expected mixer group '{groupName}' in '{MixerPath}'.");
            return null;
        }

        static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Expected serialized property '{propertyName}'.");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static AudioVolumeSettings GetRegisteredInstance()
        {
            FieldInfo field = typeof(AudioVolumeSettings).GetField(InstanceBackingFieldName, BindingFlags.Static | BindingFlags.NonPublic);
            return field != null ? field.GetValue(null) as AudioVolumeSettings : null;
        }

        static void RegisterInstance(AudioVolumeSettings instance)
        {
            FieldInfo field = typeof(AudioVolumeSettings).GetField(InstanceBackingFieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected AudioVolumeSettings instance backing field.");
            field.SetValue(null, instance);
        }

        [Test]
        public void AllowlistedPrefabs_HaveRoutedEnabledAudioSources()
        {
            List<AudioRoutingPrefabAudit.AudioRoutingPrefabIssue> issues = AudioRoutingPrefabAudit.ScanAllowlist();

            if (issues.Count > 0)
            {
                for (int i = 0; i < issues.Count; i++)
                    Debug.LogError(issues[i].ToString(), AssetDatabase.LoadAssetAtPath<Object>(issues[i].PrefabPath));
            }

            Assert.That(issues, Is.Empty, "Expected zero audio routing issues in allowlisted prefabs.");
        }
    }
}
#endif
