#if UNITY_EDITOR
using System.Reflection;
using Beavermania.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Beavermania.Tests.Audio
{
    public sealed class GameplayAudioThrottleTests
    {
        const string MixerPath = "Assets/Scripts/SceneScripts/NewAudioMixer.mixer";
        const string AudioMixerFieldName = "audioMixer";
        const string MusicGroupFieldName = "musicGroup";
        const string SfxGroupFieldName = "sfxGroup";
        const string EnemiesGroupFieldName = "enemiesGroup";
        const string UiGroupFieldName = "uiGroup";
        const string InstanceBackingFieldName = "<Instance>k__BackingField";
        const string PlayerJumpChannel = "player.jump";
        const string SwordSwingChannel = "player.swordswing";
        const string WindChannel = "player.wind";

        AudioMixer mixer;
        AudioVolumeSettings settings;
        AudioVolumeSettings previousInstance;
        GameObject settingsGameObject;
        GameObject sourceAGameObject;
        GameObject sourceBGameObject;
        AudioClip clip;

        [SetUp]
        public void SetUp()
        {
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer, Is.Not.Null, $"Expected mixer asset at '{MixerPath}'.");

            GameplayAudio.ClearAllThrottles();
            previousInstance = GetRegisteredInstance();

            settingsGameObject = new GameObject("GameplayAudioThrottleTestsHost");
            settings = settingsGameObject.AddComponent<AudioVolumeSettings>();
            SetObjectReference(settings, AudioMixerFieldName, mixer);
            SetObjectReference(settings, MusicGroupFieldName, GetMixerGroup("Music"));
            SetObjectReference(settings, SfxGroupFieldName, GetMixerGroup("SFX"));
            SetObjectReference(settings, EnemiesGroupFieldName, GetMixerGroup("Enemies"));
            SetObjectReference(settings, UiGroupFieldName, GetMixerGroup("UI"));
            RegisterInstance(settings);

            clip = AudioClip.Create("GameplayAudioThrottleClip", 4410, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            GameplayAudio.ClearAllThrottles();
            RegisterInstance(previousInstance);

            if (clip != null)
                Object.DestroyImmediate(clip);

            if (sourceAGameObject != null)
                Object.DestroyImmediate(sourceAGameObject);

            if (sourceBGameObject != null)
                Object.DestroyImmediate(sourceBGameObject);

            if (settingsGameObject != null)
                Object.DestroyImmediate(settingsGameObject);
        }

        [Test]
        public void TryPlayOneShot_ThrottlesRepeatedPlayerJumpKey()
        {
            AudioSource source = CreateRoutedSource("JumpSource", ref sourceAGameObject);

            bool first = GameplayAudio.TryPlayOneShot(source, clip, PlayerJumpChannel, 0.16f, 0.42f, 0.8f);
            bool second = GameplayAudio.TryPlayOneShot(source, clip, PlayerJumpChannel, 0.16f, 0.42f, 0.8f);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        }

        [Test]
        public void TryPlayOneShot_AllowsPlayerJumpAndSwordSwingInSameFrame()
        {
            AudioSource jumpSource = CreateRoutedSource("JumpSource", ref sourceAGameObject);
            AudioSource swordSource = CreateRoutedSource("SwordSource", ref sourceBGameObject);

            bool jumpPlayed = GameplayAudio.TryPlayOneShot(jumpSource, clip, PlayerJumpChannel, 0.16f, 0.42f, 0.8f);
            bool swordPlayed = GameplayAudio.TryPlayOneShot(swordSource, clip, SwordSwingChannel, 0.12f, 0.95f, 1f);

            Assert.That(jumpPlayed, Is.True);
            Assert.That(swordPlayed, Is.True);
        }

        [Test]
        public void TryPlayOneShot_AllowsPlayerJumpAndWindInSameFrame()
        {
            AudioSource jumpSource = CreateRoutedSource("JumpSource", ref sourceAGameObject);
            AudioSource windSource = CreateRoutedSource("WindSource", ref sourceBGameObject);

            bool jumpPlayed = GameplayAudio.TryPlayOneShot(jumpSource, clip, PlayerJumpChannel, 0.16f, 0.42f, 0.8f);
            bool windPlayed = GameplayAudio.TryPlayOneShot(windSource, clip, WindChannel, 0.15f, 0.9f, 1f);

            Assert.That(jumpPlayed, Is.True);
            Assert.That(windPlayed, Is.True);
        }

        [Test]
        public void ClearChannel_AllowsImmediateReplayOfPlayerJump()
        {
            AudioSource source = CreateRoutedSource("JumpSource", ref sourceAGameObject);

            bool first = GameplayAudio.TryPlayOneShot(source, clip, PlayerJumpChannel, 0.16f, 0.42f, 0.8f);
            bool throttled = GameplayAudio.TryPlayOneShot(source, clip, PlayerJumpChannel, 0.16f, 0.42f, 0.8f);
            GameplayAudio.ClearChannel(PlayerJumpChannel);
            bool replayed = GameplayAudio.TryPlayOneShot(source, clip, PlayerJumpChannel, 0.16f, 0.42f, 0.8f);

            Assert.That(first, Is.True);
            Assert.That(throttled, Is.False);
            Assert.That(replayed, Is.True);
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

        static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Expected serialized property '{propertyName}'.");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

        static AudioSource CreateRoutedSource(string sourceName, ref GameObject host)
        {
            if (host != null)
                Object.DestroyImmediate(host);

            host = new GameObject(sourceName);
            AudioSource source = host.AddComponent<AudioSource>();
            bool routed = AudioSourceRouting.EnsureRoute(source, AudioSourceRoute.Sfx);
            Assert.That(routed, Is.True, $"Expected AudioSource '{sourceName}' to resolve an SFX route.");
            return source;
        }
    }
}
#endif
