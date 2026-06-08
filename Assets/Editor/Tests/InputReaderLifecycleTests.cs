#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Reflection;
using Beavermania.Core.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Beavermania.Tests.Core.Input
{
    public sealed class InputReaderLifecycleTests : InputTestFixture
    {
        const BindingFlags InstanceBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        Keyboard _keyboard;
        InputReader _reader;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _reader = ScriptableObject.CreateInstance<InputReader>();
        }

        public override void TearDown()
        {
            if (_reader != null)
            {
                UnityEngine.Object.DestroyImmediate(_reader);
                _reader = null;
            }

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator EnableDisableCycle_DoesNotDuplicateJumpEvent()
        {
            int jumpCount = 0;
            _reader.JumpEvent += OnJump;
            InvokeLifecycleMethod("OnDisable");
            yield return null;
            InvokeLifecycleMethod("OnEnable");
            yield return null;

            Press(_keyboard.spaceKey);
            yield return null;

            Assert.That(jumpCount, Is.EqualTo(1));

            _reader.JumpEvent -= OnJump;
            Release(_keyboard.spaceKey);
            yield return null;
            yield break;

            void OnJump()
            {
                jumpCount++;
            }
        }

        [UnityTest]
        public IEnumerator RepeatedSetGameplay_DoesNotDuplicateJumpEvent()
        {
            int jumpCount = 0;
            _reader.JumpEvent += OnJump;
            _reader.SetGameplay();
            _reader.SetGameplay();
            yield return null;

            Press(_keyboard.spaceKey);
            yield return null;

            Assert.That(jumpCount, Is.EqualTo(1));

            _reader.JumpEvent -= OnJump;
            Release(_keyboard.spaceKey);
            yield return null;
            yield break;

            void OnJump()
            {
                jumpCount++;
            }
        }

        [UnityTest]
        public IEnumerator PauseInput_SwitchesToUi_AndNextPressResumesGameplay()
        {
            int pauseCount = 0;
            int resumeCount = 0;
            _reader.PauseEvent += OnPause;
            _reader.ResumeEvent += OnResume;

            Press(_keyboard.escapeKey);
            yield return null;
            Release(_keyboard.escapeKey);
            yield return null;
            Press(_keyboard.escapeKey);
            yield return null;

            Assert.That(pauseCount, Is.EqualTo(1));
            Assert.That(resumeCount, Is.EqualTo(1));

            _reader.PauseEvent -= OnPause;
            _reader.ResumeEvent -= OnResume;
            Release(_keyboard.escapeKey);
            yield return null;
            yield break;

            void OnPause()
            {
                pauseCount++;
            }

            void OnResume()
            {
                resumeCount++;
            }
        }

        [UnityTest]
        public IEnumerator OnDisable_StopsJumpAndPauseCallbacks()
        {
            int jumpCount = 0;
            int pauseCount = 0;
            _reader.JumpEvent += OnJump;
            _reader.PauseEvent += OnPause;
            InvokeLifecycleMethod("OnDisable");
            yield return null;

            Press(_keyboard.spaceKey);
            yield return null;
            Release(_keyboard.spaceKey);
            yield return null;
            Press(_keyboard.escapeKey);
            yield return null;

            Assert.That(jumpCount, Is.EqualTo(0));
            Assert.That(pauseCount, Is.EqualTo(0));

            _reader.JumpEvent -= OnJump;
            _reader.PauseEvent -= OnPause;
            Release(_keyboard.escapeKey);
            yield return null;
            yield break;

            void OnJump()
            {
                jumpCount++;
            }

            void OnPause()
            {
                pauseCount++;
            }
        }

        void InvokeLifecycleMethod(string methodName)
        {
            MethodInfo method = typeof(InputReader).GetMethod(methodName, InstanceBindingFlags);
            Assert.That(method, Is.Not.Null, $"Expected private lifecycle method '{methodName}' to exist.");
            method.Invoke(_reader, null);
        }
    }
}
#endif
