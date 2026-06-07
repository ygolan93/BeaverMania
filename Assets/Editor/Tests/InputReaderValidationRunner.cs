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

            var callback = new ResultsCallback();
            api.RegisterCallbacks(callback);
            api.Execute(new ExecutionSettings(filter));

            if (!callback.WaitForCompletion(TimeSpan.FromMinutes(3)))
            {
                WriteResults("InputReader lifecycle test run timed out after 3 minutes.");
                return;
            }

            WriteResults(callback.BuildReport());
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
            readonly object _gate = new object();
            bool _complete;
            int _passed;
            int _failed;
            int _skipped;
            readonly System.Text.StringBuilder _details = new System.Text.StringBuilder();

            public bool WaitForCompletion(TimeSpan timeout)
            {
                var deadline = DateTime.UtcNow + timeout;
                while (DateTime.UtcNow < deadline)
                {
                    lock (_gate)
                    {
                        if (_complete)
                            return true;
                    }

                    System.Threading.Thread.Sleep(250);
                }

                return false;
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
                lock (_gate)
                {
                    _complete = true;
                }
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
