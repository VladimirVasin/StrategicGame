using System;
using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyInGameCinematicPlayer
    {
        private bool TryBeginSequence()
        {
            try
            {
                activeSequence.Begin(activeContext);
                return true;
            }
            catch (Exception exception)
            {
                sequenceException = exception;
                StrategyDebugLogger.Warn(
                    "InGameCinematic",
                    "BeginFailed",
                    StrategyDebugLogger.F("sequence", GetDebugName(activeSequence)),
                    StrategyDebugLogger.F("error", exception.Message));
                return false;
            }
        }

        private static bool IsMissingSequence(IStrategyInGameCinematicSequence sequence)
        {
            return sequence == null
                || sequence is UnityEngine.Object unityObject && unityObject == null;
        }

        private static string GetDebugName(IStrategyInGameCinematicSequence sequence)
        {
            return IsMissingSequence(sequence) || string.IsNullOrWhiteSpace(sequence.DebugName)
                ? "Unnamed"
                : sequence.DebugName;
        }

        private static void DisposeEnumerator(IEnumerator enumerator)
        {
            if (enumerator is not IDisposable disposable)
            {
                return;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception)
            {
                // Playback cleanup still owns the authoritative scene teardown.
            }
        }
    }
}
