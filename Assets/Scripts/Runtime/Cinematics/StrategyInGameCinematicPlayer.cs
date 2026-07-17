using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyInGameCinematicPlayer : MonoBehaviour
    {
        private const string PauseReason = "InGameCinematic";
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";
        private StrategyCameraController cameraController;
        private StrategyTimeScaleController timeScale;
        private StrategyInputRouter inputRouter;
        private Camera strategyCamera;
        private StrategyCinematicLetterboxView letterbox;
        private StrategyInputContextHandle inputContext;
        private IStrategyInGameCinematicSequence activeSequence;
        private StrategyInGameCinematicContext activeContext;
        private StrategyInGameCinematicOptions activeOptions;
        private Action<StrategyInGameCinematicResult> completedCallback;
        private Coroutine playbackRoutine;
        private Exception sequenceException;
        private Vector3 returnCameraCenter;
        private float returnCameraSize;
        private bool configured;
        private bool isPlaying;
        private bool isCompleting;
        private bool cancellationRequested;
        private bool pauseHeld;
        private bool cleanupDelivered;
        public bool IsPlaying => isPlaying;
        public bool CanPlay => configured
            && isActiveAndEnabled
            && !isPlaying
            && !isCompleting
            && cameraController != null
            && strategyCamera != null
            && inputRouter != null
            && inputRouter.IsAvailable
            && inputRouter.ActiveContextCount == 0
            && (timeScale == null || !timeScale.IsPausedByLock);
        public void Configure(
            StrategyCameraController strategyCameraController,
            StrategyTimeScaleController timeScaleController,
            StrategyInputRouter router)
        {
            Cancel(false);
            cameraController = strategyCameraController;
            timeScale = timeScaleController;
            inputRouter = router;
            strategyCamera = cameraController != null
                ? cameraController.GetComponent<Camera>()
                : null;
            configured = cameraController != null
                && strategyCamera != null
                && timeScale != null
                && inputRouter != null;
            StrategyDebugLogger.Info(
                "InGameCinematic",
                "Configured",
                StrategyDebugLogger.F("ready", configured));
        }
        public bool TryPlay(
            IStrategyInGameCinematicSequence sequence,
            StrategyInGameCinematicOptions options,
            Action<StrategyInGameCinematicResult> onCompleted)
        {
            if (!CanPlay || IsMissingSequence(sequence))
            {
                return false;
            }

            StrategyInGameCinematicFraming framing;
            try
            {
                if (!sequence.TryPrepare(out framing))
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                StrategyDebugLogger.Warn(
                    "InGameCinematic",
                    "PrepareFailed",
                    StrategyDebugLogger.F("sequence", GetDebugName(sequence)),
                    StrategyDebugLogger.F("error", exception.Message));
                return false;
            }

            if (!cameraController.TryGetView(out returnCameraCenter, out returnCameraSize))
            {
                return false;
            }

            activeOptions = ResolveOptions(options);
            EnsureLetterbox();
            letterbox.Configure(
                activeOptions.TargetAspectRatio,
                activeOptions.MinimumBarFraction);
            letterbox.HideImmediate();

            activeSequence = sequence;
            completedCallback = onCompleted;
            cancellationRequested = false;
            cleanupDelivered = false;
            sequenceException = null;
            isPlaying = true;
            activeContext = new StrategyInGameCinematicContext(
                cameraController,
                strategyCamera,
                () => cancellationRequested || !isPlaying);

            HoldInput();
            if (inputContext == null || inputContext.IsDisposed)
            {
                ResetPlaybackState();
                return false;
            }

            HoldUiInput();
            PushPauseLock();
            float targetSize = StrategyInGameCinematicMath.CalculateTargetOrthographicSize(
                framing,
                strategyCamera.aspect,
                letterbox.SafeHeightFraction);
            Coroutine startedRoutine = StartCoroutine(PlayRoutine(
                framing.WorldBounds.center,
                targetSize));
            if (isPlaying)
            {
                playbackRoutine = startedRoutine;
            }
            StrategyDebugLogger.Info(
                "InGameCinematic",
                "Started",
                StrategyDebugLogger.F("sequence", GetDebugName(sequence)),
                StrategyDebugLogger.F("focus", framing.WorldBounds.center),
            StrategyDebugLogger.F("size", targetSize));
            return true;
        }
        public bool Cancel(bool notifyCompletion = false)
        {
            if (!isPlaying)
            {
                return false;
            }

            if (isCompleting)
            {
                CompleteCameraReturnImmediately();
                return true;
            }

            cancellationRequested = true;
            StopPlaybackRoutine();
            CompletePlayback(StrategyInGameCinematicResult.Cancelled, notifyCompletion);
            return true;
        }
        public bool Cancel(
            IStrategyInGameCinematicSequence sequence,
            bool notifyCompletion = false)
        {
            return !IsMissingSequence(sequence)
                && ReferenceEquals(activeSequence, sequence)
                && Cancel(notifyCompletion);
        }
        private IEnumerator PlayRoutine(Vector3 focusCenter, float focusSize)
        {
            if (!TryBeginSequence())
            {
                yield return null;
                playbackRoutine = null;
                CompletePlayback(StrategyInGameCinematicResult.Failed, true);
                yield break;
            }

            float openingDuration = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0
                ? activeOptions.ReducedMotionOpeningDurationSeconds
                : activeOptions.OpeningDurationSeconds;
            cameraController.FocusOnAnimated(focusCenter, focusSize, openingDuration);
            yield return AnimateLetterboxIn(openingDuration);
            if (cancellationRequested || !isPlaying)
            {
                yield break;
            }

            if (activeOptions.OpeningHoldSeconds > 0f)
            {
                yield return activeContext.WaitForSecondsUnscaled(activeOptions.OpeningHoldSeconds);
            }

            if (cancellationRequested || !isPlaying)
            {
                yield break;
            }

            IEnumerator sequenceRoutine = null;
            try
            {
                sequenceRoutine = activeSequence.Play(activeContext);
            }
            catch (Exception exception)
            {
                sequenceException = exception;
            }

            if (sequenceRoutine != null && sequenceException == null)
            {
                yield return RunSequenceSafely(sequenceRoutine);
            }

            StrategyInGameCinematicResult result = cancellationRequested
                ? StrategyInGameCinematicResult.Cancelled
                : sequenceException != null
                    ? StrategyInGameCinematicResult.Failed
                    : StrategyInGameCinematicResult.Completed;
            if (sequenceException != null)
            {
                StrategyDebugLogger.Warn(
                    "InGameCinematic",
                    "SequenceFailed",
                    StrategyDebugLogger.F("sequence", GetDebugName(activeSequence)),
                    StrategyDebugLogger.F("error", sequenceException.Message));
            }

            playbackRoutine = null;
            CompletePlayback(result, true);
        }

        private IEnumerator AnimateLetterboxIn(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                letterbox.SetReveal(1f);
                yield break;
            }

            float elapsed = 0f;
            letterbox.SetReveal(0f);
            while (elapsed < durationSeconds && !cancellationRequested)
            {
                yield return null;
                elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
                float progress = Mathf.Clamp01(elapsed / durationSeconds);
                float eased = progress * progress * (3f - 2f * progress);
                letterbox.SetReveal(eased);
            }
        }
        private IEnumerator RunSequenceSafely(IEnumerator root)
        {
            Stack<IEnumerator> stack = new();
            stack.Push(root);
            try
            {
                while (stack.Count > 0 && !cancellationRequested)
                {
                    IEnumerator current = stack.Peek();
                    bool moved = false;
                    object yielded = null;
                    Exception caught = null;
                    try
                    {
                        moved = current.MoveNext();
                        if (moved)
                        {
                            yielded = current.Current;
                        }
                    }
                    catch (Exception exception)
                    {
                        caught = exception;
                    }

                    if (caught != null)
                    {
                        sequenceException = caught;
                        break;
                    }

                    if (!moved)
                    {
                        DisposeEnumerator(stack.Pop());
                        continue;
                    }

                    if (yielded is IEnumerator nested)
                    {
                        stack.Push(nested);
                        continue;
                    }

                    yield return yielded;
                }
            }
            finally
            {
                while (stack.Count > 0)
                {
                    DisposeEnumerator(stack.Pop());
                }
            }
        }
        private void EnsureLetterbox()
        {
            if (letterbox != null)
            {
                return;
            }

            GameObject viewObject = new(
                "In-Game Cinematic Letterbox",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            viewObject.transform.SetParent(transform, false);
            letterbox = viewObject.AddComponent<StrategyCinematicLetterboxView>();
        }

        private void StopPlaybackRoutine()
        {
            if (playbackRoutine == null)
            {
                return;
            }

            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        private void ResetPlaybackState()
        {
            ReleaseInput();
            ReleasePauseLock();
            ReleaseUiShield();
            RestoreUiSelectionIfUnclaimed();
            activeContext?.Invalidate();
            activeSequence = null;
            activeContext = null;
            completedCallback = null;
            cameraReturnRoutine = null;
            completionResult = default;
            sequenceException = null;
            cancellationRequested = false;
            cleanupDelivered = false;
            isPlaying = false;
            isCompleting = false;
            returnCameraCenter = default;
            returnCameraSize = 0f;
        }

        private static StrategyInGameCinematicOptions ResolveOptions(
            StrategyInGameCinematicOptions options)
        {
            return options.TargetAspectRatio > 0f
                ? options
                : StrategyInGameCinematicOptions.Default;
        }

        private void OnDisable()
        {
            Cancel(false);
            letterbox?.HideImmediate();
        }

        private void OnDestroy()
        {
            Cancel(false);
        }
    }
}
