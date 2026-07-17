using System;
using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyInGameCinematicPlayer
    {
        private Coroutine cameraReturnRoutine;
        private StrategyInGameCinematicResult completionResult;

        private void CompletePlayback(
            StrategyInGameCinematicResult result,
            bool notifyCompletion)
        {
            if (!isPlaying || isCompleting)
            {
                return;
            }

            isCompleting = true;
            completionResult = result;
            DeliverSequenceCleanup(result);

            // A synchronous callback may acquire the next modal. Release only this
            // player's ownership first, with no rendered frame between the owners.
            ReleaseInput();
            ReleasePauseLock();
            ReleaseUiShield();
            InvokeCompletionCallback(result, notifyCompletion);
            if (this == null || !isPlaying || !isCompleting)
            {
                return;
            }

            float restoreDuration = result == StrategyInGameCinematicResult.Completed
                ? activeOptions.CameraRestoreDurationSeconds
                : 0f;
            if (cameraController != null)
            {
                cameraController.RestoreViewAnimated(
                    returnCameraCenter,
                    returnCameraSize,
                    restoreDuration);
            }

            if (restoreDuration <= 0f || cameraController == null)
            {
                FinishPlayback(result);
                return;
            }

            ReclaimUnclaimedReturnOwnership();
            cameraReturnRoutine = StartCoroutine(
                FinishCameraReturnAfterDelay(restoreDuration, result));
        }

        private void InvokeCompletionCallback(
            StrategyInGameCinematicResult result,
            bool notifyCompletion)
        {
            Action<StrategyInGameCinematicResult> callback = completedCallback;
            if (!notifyCompletion || callback == null)
            {
                return;
            }

            try
            {
                callback.Invoke(result);
            }
            catch (Exception exception)
            {
                StrategyDebugLogger.Warn(
                    "InGameCinematic",
                    "CompletionCallbackFailed",
                    StrategyDebugLogger.F("error", exception.Message));
            }
        }

        private void ReclaimUnclaimedReturnOwnership()
        {
            bool inputUnclaimed = inputRouter != null
                && inputRouter.IsAvailable
                && inputRouter.ActiveContextCount == 0;
            if (inputUnclaimed)
            {
                ResumeUiShieldForCameraReturn();
                HoldInput();
            }

            if (timeScale != null && !timeScale.IsPausedByLock)
            {
                PushPauseLock();
            }
        }

        private IEnumerator FinishCameraReturnAfterDelay(
            float durationSeconds,
            StrategyInGameCinematicResult result)
        {
            float finishAt = Time.unscaledTime + Mathf.Max(0f, durationSeconds);
            while (isPlaying && isCompleting && Time.unscaledTime < finishAt)
            {
                yield return null;
            }

            if (!isPlaying || !isCompleting)
            {
                yield break;
            }

            cameraReturnRoutine = null;
            cameraController?.RestoreViewAnimated(
                returnCameraCenter,
                returnCameraSize,
                0f);
            FinishPlayback(result);
        }

        private void CompleteCameraReturnImmediately()
        {
            StopCameraReturnRoutine();
            cameraController?.RestoreViewAnimated(
                returnCameraCenter,
                returnCameraSize,
                0f);
            FinishPlayback(completionResult);
        }

        private void FinishPlayback(StrategyInGameCinematicResult result)
        {
            ReleaseInput();
            ReleasePauseLock();
            ReleaseUiShield();
            RestoreUiSelectionIfUnclaimed();
            letterbox?.HideImmediate();
            StrategyDebugLogger.Info(
                "InGameCinematic",
                "Finished",
                StrategyDebugLogger.F("result", result));
            ResetPlaybackState();
        }

        private void StopCameraReturnRoutine()
        {
            if (cameraReturnRoutine == null)
            {
                return;
            }

            StopCoroutine(cameraReturnRoutine);
            cameraReturnRoutine = null;
        }

        private void DeliverSequenceCleanup(StrategyInGameCinematicResult result)
        {
            if (cleanupDelivered || IsMissingSequence(activeSequence))
            {
                return;
            }

            cleanupDelivered = true;
            try
            {
                activeSequence.Cleanup(activeContext, result);
            }
            catch (Exception exception)
            {
                StrategyDebugLogger.Warn(
                    "InGameCinematic",
                    "CleanupFailed",
                    StrategyDebugLogger.F("sequence", GetDebugName(activeSequence)),
                    StrategyDebugLogger.F("error", exception.Message));
            }
        }

        private void HoldInput()
        {
            if (inputContext == null && inputRouter != null && inputRouter.IsAvailable)
            {
                inputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.All,
                    StrategyCancelMode.Swallow);
            }
        }

        private void ReleaseInput()
        {
            inputContext?.Dispose();
            inputContext = null;
            inputRouter?.ReleaseContexts(this);
        }

        private void PushPauseLock()
        {
            if (pauseHeld || timeScale == null)
            {
                return;
            }

            timeScale.PushPauseLock(PauseReason);
            pauseHeld = true;
        }

        private void ReleasePauseLock()
        {
            if (!pauseHeld)
            {
                return;
            }

            if (timeScale != null && timeScale.isActiveAndEnabled && timeScale.IsPausedByLock)
            {
                timeScale.PopPauseLock(PauseReason);
            }

            pauseHeld = false;
        }
    }
}
