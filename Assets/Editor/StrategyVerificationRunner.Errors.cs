using System;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const string SmokeErrorCountKey = "ProjectUnknown.PlayModeSmokeErrorCount";
        private const string SmokeFirstErrorKey = "ProjectUnknown.PlayModeSmokeFirstError";

        private static void ResetSmokeErrorCapture()
        {
            SessionState.SetInt(SmokeErrorCountKey, 0);
            SessionState.EraseString(SmokeFirstErrorKey);
            RestoreSmokeErrorCapture();
        }

        private static void RestoreSmokeErrorCapture()
        {
            Application.logMessageReceived -= HandleSmokeLog;
            Application.logMessageReceived += HandleSmokeLog;
        }

        private static void HandleSmokeLog(string condition, string stackTrace, LogType type)
        {
            if ((type != LogType.Error && type != LogType.Assert && type != LogType.Exception)
                || IsKnownHeadlessEditorError(condition, stackTrace))
            {
                return;
            }

            int count = SessionState.GetInt(SmokeErrorCountKey, 0) + 1;
            SessionState.SetInt(SmokeErrorCountKey, count);
            if (count == 1)
            {
                string firstError = string.IsNullOrWhiteSpace(condition) ? type.ToString() : condition.Trim();
                SessionState.SetString(
                    SmokeFirstErrorKey,
                    firstError.Length <= 512 ? firstError : firstError.Substring(0, 512));
            }
        }

        private static bool IsKnownHeadlessEditorError(string condition, string stackTrace)
        {
            string message = condition ?? string.Empty;
            string trace = stackTrace ?? string.Empty;
            bool searchIndexStartupFailure = Application.isBatchMode && message.StartsWith(
                    "ArgumentOutOfRangeException",
                    StringComparison.Ordinal)
                && trace.Contains("UnityEditor.Search.SearchDatabase", StringComparison.Ordinal)
                && trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup", StringComparison.Ordinal);
            bool missingHeadlessView = Application.isBatchMode && message.StartsWith(
                    "No graphic device is available",
                    StringComparison.Ordinal)
                && trace.Contains("UnityEditor.EditorApplicationLayout:SetPlaymodeLayout", StringComparison.Ordinal);
            return searchIndexStartupFailure || missingHeadlessView;
        }

        private static int GetSmokeErrorCount()
        {
            return SessionState.GetInt(SmokeErrorCountKey, 0);
        }

        private static void ApplySmokeErrors(ref bool passed, ref string result)
        {
            int errorCount = GetSmokeErrorCount();
            if (!passed || errorCount <= 0)
            {
                return;
            }

            string firstError = SessionState.GetString(SmokeFirstErrorKey, "unknown error");
            passed = false;
            result = $"FAIL: Unity emitted {errorCount} unexpected error(s); first: {firstError}";
        }

        private static void CleanupSmokeErrorCapture()
        {
            Application.logMessageReceived -= HandleSmokeLog;
            SessionState.EraseInt(SmokeErrorCountKey);
            SessionState.EraseString(SmokeFirstErrorKey);
        }
    }
}
