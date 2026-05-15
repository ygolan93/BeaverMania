using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Beavermania.Debugging
{
    /// <summary>Session-scoped NDJSON logs for agent debug mode (session 378ac9).</summary>
    public static class AgentDebugLog
    {
        const string SessionId = "378ac9";
        const string LogFileName = "debug-378ac9.log";

        static string LogPath =>
            Path.Combine(Application.dataPath, "..", LogFileName);

        public static void Write(string hypothesisId, string location, string message, string dataJson = "{}")
        {
            try
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string line =
                    $"{{\"sessionId\":\"{SessionId}\",\"hypothesisId\":\"{hypothesisId}\",\"location\":\"{Escape(location)}\",\"message\":\"{Escape(message)}\",\"data\":{dataJson},\"timestamp\":{ts.ToString(CultureInfo.InvariantCulture)}}}\n";
                File.AppendAllText(LogPath, line);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AgentDebugLog] write failed: {ex.Message}");
            }
        }

        static string Escape(string s) =>
            string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        public static string Vec3(Vector3 v) =>
            $"{{\"x\":{v.x.ToString(CultureInfo.InvariantCulture)},\"y\":{v.y.ToString(CultureInfo.InvariantCulture)},\"z\":{v.z.ToString(CultureInfo.InvariantCulture)}}}";
    }
}
