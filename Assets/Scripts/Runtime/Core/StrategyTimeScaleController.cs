using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyTimeScaleController : MonoBehaviour
    {
        private const float NormalScale = 1f;
        private const float DoubleScale = 2f;
        private const float TripleScale = 3f;

        private float baseFixedDeltaTime;
        private int pauseLockCount;

        public float CurrentScale { get; private set; } = NormalScale;
        public bool IsPausedByLock => pauseLockCount > 0;

        public void Configure()
        {
            baseFixedDeltaTime = Time.fixedDeltaTime / Mathf.Max(Time.timeScale, 0.0001f);
            SetTimeScale(NormalScale);
        }

        private void Awake()
        {
            if (baseFixedDeltaTime <= 0f)
            {
                baseFixedDeltaTime = Time.fixedDeltaTime;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                SetTimeScale(NormalScale);
            }
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                SetTimeScale(DoubleScale);
            }
            else if (keyboard.f3Key.wasPressedThisFrame)
            {
                SetTimeScale(TripleScale);
            }
        }

        private void OnDisable()
        {
            if (baseFixedDeltaTime > 0f)
            {
                pauseLockCount = 0;
                Time.timeScale = NormalScale;
                Time.fixedDeltaTime = baseFixedDeltaTime;
            }
        }

        public void PushPauseLock(string reason)
        {
            pauseLockCount++;
            ApplyEffectiveTimeScale();
            StrategyDebugLogger.Info(
                "Time",
                "PauseLockPushed",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("locks", pauseLockCount),
                StrategyDebugLogger.F("currentScale", CurrentScale));
        }

        public void PopPauseLock(string reason)
        {
            if (pauseLockCount <= 0)
            {
                StrategyDebugLogger.Warn(
                    "Time",
                    "PauseLockPopRejected",
                    StrategyDebugLogger.F("reason", reason));
                return;
            }

            pauseLockCount--;
            ApplyEffectiveTimeScale();
            StrategyDebugLogger.Info(
                "Time",
                "PauseLockPopped",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("locks", pauseLockCount),
                StrategyDebugLogger.F("currentScale", CurrentScale));
        }

        private void SetTimeScale(float scale)
        {
            float previousScale = CurrentScale;
            CurrentScale = Mathf.Max(NormalScale, scale);
            ApplyEffectiveTimeScale();
            if (!Mathf.Approximately(previousScale, CurrentScale))
            {
                StrategyDebugLogger.Info(
                    "Time",
                    "ScaleChanged",
                    StrategyDebugLogger.F("previous", previousScale),
                    StrategyDebugLogger.F("current", CurrentScale),
                    StrategyDebugLogger.F("fixedDeltaTime", Time.fixedDeltaTime));
            }
        }

        private void ApplyEffectiveTimeScale()
        {
            float fixedScale = Mathf.Max(NormalScale, CurrentScale);
            Time.timeScale = pauseLockCount > 0 ? 0f : CurrentScale;
            Time.fixedDeltaTime = baseFixedDeltaTime * fixedScale;
        }
    }
}
