using System;
using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyInGameCinematicResult
    {
        Completed,
        Cancelled,
        Failed
    }

    public readonly struct StrategyInGameCinematicFraming
    {
        public StrategyInGameCinematicFraming(
            Bounds worldBounds,
            Vector2 perSidePadding,
            float minimumOrthographicSize = 0f,
            float maximumOrthographicSize = 0f)
        {
            WorldBounds = worldBounds;
            PerSidePadding = new Vector2(
                Mathf.Max(0f, perSidePadding.x),
                Mathf.Max(0f, perSidePadding.y));
            MinimumOrthographicSize = Mathf.Max(0f, minimumOrthographicSize);
            MaximumOrthographicSize = Mathf.Max(0f, maximumOrthographicSize);
        }

        public Bounds WorldBounds { get; }
        public Vector2 PerSidePadding { get; }
        public float MinimumOrthographicSize { get; }
        public float MaximumOrthographicSize { get; }
    }

    public readonly struct StrategyInGameCinematicOptions
    {
        public StrategyInGameCinematicOptions(
            float openingDurationSeconds,
            float openingHoldSeconds,
            float cameraRestoreDurationSeconds,
            float targetAspectRatio,
            float minimumBarFraction,
            float reducedMotionOpeningDurationSeconds)
        {
            OpeningDurationSeconds = Mathf.Max(0f, openingDurationSeconds);
            OpeningHoldSeconds = Mathf.Max(0f, openingHoldSeconds);
            CameraRestoreDurationSeconds = Mathf.Max(0f, cameraRestoreDurationSeconds);
            TargetAspectRatio = Mathf.Max(1f, targetAspectRatio);
            MinimumBarFraction = Mathf.Clamp(minimumBarFraction, 0f, 0.24f);
            ReducedMotionOpeningDurationSeconds = Mathf.Max(
                0f,
                reducedMotionOpeningDurationSeconds);
        }

        public static StrategyInGameCinematicOptions Default => new(
            0.80f,
            0.18f,
            0.25f,
            2.39f,
            0.055f,
            0.14f);

        public float OpeningDurationSeconds { get; }
        public float OpeningHoldSeconds { get; }
        public float CameraRestoreDurationSeconds { get; }
        public float TargetAspectRatio { get; }
        public float MinimumBarFraction { get; }
        public float ReducedMotionOpeningDurationSeconds { get; }
    }

    public interface IStrategyInGameCinematicSequence
    {
        string DebugName { get; }

        bool TryPrepare(out StrategyInGameCinematicFraming framing);

        void Begin(StrategyInGameCinematicContext context);

        IEnumerator Play(StrategyInGameCinematicContext context);

        void Cleanup(
            StrategyInGameCinematicContext context,
            StrategyInGameCinematicResult result);
    }

    public sealed class StrategyInGameCinematicContext
    {
        private Func<bool> cancellationRequested;

        internal StrategyInGameCinematicContext(
            StrategyCameraController cameraController,
            Camera camera,
            Func<bool> isCancellationRequested)
        {
            CameraController = cameraController;
            Camera = camera;
            cancellationRequested = isCancellationRequested;
        }

        public StrategyCameraController CameraController { get; }
        public Camera Camera { get; }
        public bool IsCancellationRequested => cancellationRequested == null
            || cancellationRequested.Invoke();
        public float UnscaledTime => Time.unscaledTime;
        public float UnscaledDeltaTime => Mathf.Max(0f, Time.unscaledDeltaTime);

        public IEnumerator WaitForSecondsUnscaled(float durationSeconds)
        {
            float duration = Mathf.Max(0f, durationSeconds);
            float elapsed = 0f;
            while (elapsed < duration && !IsCancellationRequested)
            {
                yield return null;
                elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
            }
        }

        public IEnumerator AnimateUnscaled(
            float durationSeconds,
            Action<float> applyProgress,
            bool smoothStep = true)
        {
            if (applyProgress == null)
            {
                throw new ArgumentNullException(nameof(applyProgress));
            }

            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f)
            {
                if (!IsCancellationRequested)
                {
                    applyProgress.Invoke(1f);
                }

                yield break;
            }

            float elapsed = 0f;
            applyProgress.Invoke(0f);
            while (elapsed < duration && !IsCancellationRequested)
            {
                yield return null;
                elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
                float progress = Mathf.Clamp01(elapsed / duration);
                applyProgress.Invoke(smoothStep
                    ? progress * progress * (3f - 2f * progress)
                    : progress);
            }
        }

        internal void Invalidate()
        {
            cancellationRequested = null;
        }
    }
}
