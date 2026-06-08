#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Beavermania.Editor.Validation
{
    public static class InputReaderValidationRunner
    {
        const string ResultsPath = "AI_WORKFLOW/inputreader-validation-results.txt";

        [MenuItem("Beavermania/Validation/Run InputReader Lifecycle Tests")]
        public static void RunFromMenu()
        {
            RunTestsAndWriteResults();
        }

        public static void RunTestsAndWriteResults()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                testNames = new[]
                {
                    "Beavermania.Tests.Core.Input.InputReaderLifecycleTests"
                }
            };

            var callback = new ResultsCallback(report =>
            {
                WriteResults(report);
                UnityEngine.Object.DestroyImmediate(api);
            });

            api.RegisterCallbacks(callback);
            api.Execute(new ExecutionSettings(filter));
            Debug.Log("InputReader lifecycle tests started. Results will be written when the run completes.");
        }

        static void WriteResults(string body)
        {
            var path = Path.Combine(Application.dataPath, "..", ResultsPath);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, body);
            Debug.Log($"InputReader validation results written to {ResultsPath}");
            AssetDatabase.Refresh();
        }

        sealed class ResultsCallback : ICallbacks
        {
            readonly Action<string> _onRunFinished;
            readonly object _gate = new object();
            int _passed;
            int _failed;
            int _skipped;
            readonly System.Text.StringBuilder _details = new System.Text.StringBuilder();

            public ResultsCallback(Action<string> onRunFinished)
            {
                _onRunFinished = onRunFinished;
            }

            public string BuildReport()
            {
                lock (_gate)
                {
                    return string.Join(Environment.NewLine, new[]
                    {
                        "InputReader Lifecycle Test Run",
                        $"Passed: {_passed}",
                        $"Failed: {_failed}",
                        $"Skipped: {_skipped}",
                        string.Empty,
                        _details.ToString()
                    });
                }
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                _onRunFinished?.Invoke(BuildReport());
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                lock (_gate)
                {
                    switch (result.TestStatus)
                    {
                        case TestStatus.Passed:
                            _passed++;
                            break;
                        case TestStatus.Failed:
                            _failed++;
                            _details.AppendLine($"FAIL: {result.FullName}");
                            _details.AppendLine(result.Message);
                            if (!string.IsNullOrEmpty(result.StackTrace))
                                _details.AppendLine(result.StackTrace);
                            _details.AppendLine();
                            break;
                        case TestStatus.Skipped:
                            _skipped++;
                            _details.AppendLine($"SKIP: {result.FullName} — {result.Message}");
                            break;
                    }
                }
            }
        }
    }
}
#endif
