using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyDebugLogger : MonoBehaviour
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly object SyncRoot = new();
        private const int FlushLineThreshold = 32;
        private const float FlushIntervalSeconds = 0.5f;

        private bool configured;
        private bool fileWriteDisabled;
        private string canonicalLogPath;
        private string logPath;
        private StreamWriter writer;
        private int processId;
        private int pendingFlushLines;
        private long activeByteCount;
        private float nextFlushTime;

        public static StrategyDebugLogger Active { get; private set; }
        public static string LogPath => Active != null ? Active.logPath : string.Empty;

        public void Configure()
        {
            if (configured)
            {
                return;
            }

            Active = this;
            canonicalLogPath = ResolveLogPath();
            logPath = canonicalLogPath;
            processId = ResolveProcessId();
            configured = true;
            fileWriteDisabled = false;

            if (!StrategyLogFileRotation.TryOpenSession(
                    canonicalLogPath,
                    DateTime.UtcNow,
                    processId,
                    Utf8NoBom,
                    out writer,
                    out logPath))
            {
                fileWriteDisabled = true;
                DisposeWriter();
                return;
            }

            activeByteCount = ResolveWriterLength();
            nextFlushTime = Time.realtimeSinceStartup + FlushIntervalSeconds;

            Application.logMessageReceived -= HandleUnityLogMessage;
            Application.logMessageReceived += HandleUnityLogMessage;

            Info(
                "Session",
                "Start",
                F("scene", SceneManager.GetActiveScene().name),
                F("unity", Application.unityVersion),
                F("platform", Application.platform),
                F("log", logPath));
        }

        public static LogField F(string key, object value)
        {
            return new LogField(key, value);
        }

        public static void Info(string system, string eventName, params LogField[] fields)
        {
            Write("Info", system, eventName, fields);
        }

        public static void Warn(string system, string eventName, params LogField[] fields)
        {
            Write("Warn", system, eventName, fields);
        }

        public static void Error(string system, string eventName, params LogField[] fields)
        {
            Write("Error", system, eventName, fields);
        }

        private static void Write(string level, string system, string eventName, params LogField[] fields)
        {
            if (Active == null || !Active.configured || Active.fileWriteDisabled)
            {
                return;
            }

            Active.WriteLine(BuildLine(level, system, eventName, fields));
        }

        private static string BuildLine(string level, string system, string eventName, LogField[] fields)
        {
            StringBuilder builder = new StringBuilder(192);
            builder.Append(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
            builder.Append(" frame=").Append(Time.frameCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(" t=").Append(Time.realtimeSinceStartup.ToString("0.###", CultureInfo.InvariantCulture));
            builder.Append(" scale=").Append(Time.timeScale.ToString("0.###", CultureInfo.InvariantCulture));
            builder.Append(" level=").Append(level);
            builder.Append(" system=").Append(SanitizeToken(system));
            builder.Append(" event=").Append(SanitizeToken(eventName));

            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(fields[i].Key))
                    {
                        continue;
                    }

                    builder.Append(' ');
                    builder.Append(SanitizeToken(fields[i].Key));
                    builder.Append('=');
                    builder.Append(FormatValue(fields[i].Value));
                }
            }

            return builder.ToString();
        }

        private void WriteLine(string line)
        {
            try
            {
                lock (SyncRoot)
                {
                    if (writer == null)
                    {
                        RecoverWithFallback();
                        if (writer == null)
                        {
                            return;
                        }
                    }

                    long lineByteCount = Utf8NoBom.GetByteCount(line)
                        + Utf8NoBom.GetByteCount(writer.NewLine);
                    float now = Time.realtimeSinceStartup;
                    if (activeByteCount > 0L
                        && activeByteCount + lineByteCount > StrategyLogFileRotation.MaxActiveBytes
                        && !FlushAndRotate(now, true))
                    {
                        return;
                    }

                    writer.WriteLine(line);
                    activeByteCount += lineByteCount;
                    pendingFlushLines++;
                    bool sizeReached = StrategyLogFileRotation.ShouldRotate(activeByteCount);
                    if (sizeReached || pendingFlushLines >= FlushLineThreshold || now >= nextFlushTime)
                    {
                        FlushAndRotate(now, sizeReached);
                    }
                }
            }
            catch
            {
                DisposeWriter();
                RecoverWithFallback();
            }
        }

        private bool FlushAndRotate(float now, bool rotationRequested)
        {
            writer.Flush();
            pendingFlushLines = 0;
            nextFlushTime = now + FlushIntervalSeconds;
            activeByteCount = ResolveWriterLength();
            if (!rotationRequested && !StrategyLogFileRotation.ShouldRotate(activeByteCount))
            {
                return true;
            }

            writer.Dispose();
            writer = null;
            if (!StrategyLogFileRotation.TryRotateAndReopen(
                    logPath,
                    canonicalLogPath,
                    DateTime.UtcNow,
                    processId,
                    Utf8NoBom,
                    out writer,
                    out logPath))
            {
                fileWriteDisabled = true;
                activeByteCount = 0L;
                return false;
            }

            activeByteCount = ResolveWriterLength();
            return true;
        }

        private void RecoverWithFallback()
        {
            if (!StrategyLogFileRotation.TryOpenFallback(
                    canonicalLogPath,
                    DateTime.UtcNow,
                    processId,
                    Utf8NoBom,
                    out writer,
                    out logPath))
            {
                fileWriteDisabled = true;
                activeByteCount = 0L;
                return;
            }

            fileWriteDisabled = false;
            activeByteCount = ResolveWriterLength();
            pendingFlushLines = 0;
            nextFlushTime = Time.realtimeSinceStartup + FlushIntervalSeconds;
        }

        private void HandleUnityLogMessage(string condition, string stackTrace, LogType type)
        {
            if (fileWriteDisabled)
            {
                return;
            }

            string level = type == LogType.Error || type == LogType.Assert || type == LogType.Exception
                ? "Error"
                : type == LogType.Warning
                    ? "Warn"
                    : "Info";

            if (type == LogType.Log)
            {
                WriteLine(BuildLine(level, "Unity", "Message", new[] { F("message", condition) }));
                return;
            }

            WriteLine(BuildLine(
                level,
                "Unity",
                type.ToString(),
                new[]
                {
                    F("message", condition),
                    F("stack", Truncate(stackTrace, 900))
                }));
        }

        private void OnDisable()
        {
            if (configured && !fileWriteDisabled)
            {
                Info("Session", "End");
                FlushWriter();
            }

            Application.logMessageReceived -= HandleUnityLogMessage;
            if (Active == this)
            {
                Active = null;
            }

            DisposeWriter();
        }

        private void FlushWriter()
        {
            try
            {
                lock (SyncRoot)
                {
                    writer?.Flush();
                    activeByteCount = ResolveWriterLength();
                    pendingFlushLines = 0;
                    nextFlushTime = Time.realtimeSinceStartup + FlushIntervalSeconds;
                }
            }
            catch
            {
                fileWriteDisabled = true;
            }
        }

        private void DisposeWriter()
        {
            try
            {
                lock (SyncRoot)
                {
                    writer?.Dispose();
                    writer = null;
                    pendingFlushLines = 0;
                    activeByteCount = 0L;
                }
            }
            catch
            {
                writer = null;
                activeByteCount = 0L;
            }
        }

        private long ResolveWriterLength()
        {
            try
            {
                return writer?.BaseStream.Length ?? 0L;
            }
            catch
            {
                return activeByteCount;
            }
        }

        private static string ResolveLogPath()
        {
#if UNITY_EDITOR
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.persistentDataPath;
            return Path.Combine(projectRoot, "debug.log");
#else
            return Path.Combine(Application.persistentDataPath, "debug.log");
#endif
        }

        private static int ResolveProcessId()
        {
            try
            {
                return System.Diagnostics.Process.GetCurrentProcess().Id;
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            switch (value)
            {
                case string text:
                    return Quote(text);
                case bool boolean:
                    return boolean ? "true" : "false";
                case float number:
                    return number.ToString("0.###", CultureInfo.InvariantCulture);
                case double number:
                    return number.ToString("0.###", CultureInfo.InvariantCulture);
                case int number:
                    return number.ToString(CultureInfo.InvariantCulture);
                case long number:
                    return number.ToString(CultureInfo.InvariantCulture);
                case Vector2Int vector:
                    return Quote(vector.x + "," + vector.y);
                case Vector2 vector:
                    return Quote(vector.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + vector.y.ToString("0.###", CultureInfo.InvariantCulture));
                case Vector3 vector:
                    return Quote(vector.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + vector.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + vector.z.ToString("0.###", CultureInfo.InvariantCulture));
                case Bounds bounds:
                    return Quote(
                        "center="
                        + FormatValue(bounds.center).Trim('"')
                        + ";size="
                        + FormatValue(bounds.size).Trim('"'));
                case Enum enumValue:
                    return enumValue.ToString();
                default:
                    return Quote(value.ToString());
            }
        }

        private static string Quote(string text)
        {
            if (text == null)
            {
                return "null";
            }

            return "\""
                + text
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                + "\"";
        }

        private static string SanitizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "Unknown";
            }

            return token.Replace(' ', '_').Replace('=', '_');
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        public readonly struct LogField
        {
            public LogField(string key, object value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }
            public object Value { get; }
        }
    }
}
