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

        public float CurrentScale { get; private set; } = NormalScale;

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
                Time.timeScale = NormalScale;
                Time.fixedDeltaTime = baseFixedDeltaTime;
            }
        }

        private void SetTimeScale(float scale)
        {
            float previousScale = CurrentScale;
            CurrentScale = Mathf.Max(NormalScale, scale);
            Time.timeScale = CurrentScale;
            Time.fixedDeltaTime = baseFixedDeltaTime * CurrentScale;
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
    }
}
