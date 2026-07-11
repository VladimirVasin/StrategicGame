using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class StrategyCameraFeedbackController : MonoBehaviour
    {
        private const float ViewPadding = 0.12f;
        private const float MaximumOffset = 0.16f;

        private Camera strategyCamera;
        private Vector3 appliedOffset;
        private float remainingSeconds;
        private float totalSeconds;
        private float amplitude;
        private float noiseSeed;

        public static StrategyCameraFeedbackController Active { get; private set; }

        public void Configure(Camera camera)
        {
            strategyCamera = camera != null ? camera : GetComponent<Camera>();
            Active = this;
        }

        public static void Emit(Vector3 worldPosition, float strength, float duration)
        {
            StrategyCameraFeedbackController controller = Active;
            if (controller == null || !controller.IsSourceNearView(worldPosition))
            {
                return;
            }

            controller.amplitude = Mathf.Max(controller.amplitude, Mathf.Clamp(strength, 0f, MaximumOffset));
            controller.totalSeconds = Mathf.Max(controller.totalSeconds, Mathf.Max(0.04f, duration));
            controller.remainingSeconds = Mathf.Max(controller.remainingSeconds, Mathf.Max(0.04f, duration));
            controller.noiseSeed = Mathf.Repeat(
                worldPosition.x * 0.173f + worldPosition.y * 0.317f + Time.unscaledTime,
                1000f);
        }

        private void Awake()
        {
            strategyCamera = GetComponent<Camera>();
            Active = this;
        }

        private void OnDestroy()
        {
            RemoveAppliedOffset();
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            RemoveAppliedOffset();
        }

        private void LateUpdate()
        {
            if (remainingSeconds <= 0f || strategyCamera == null)
            {
                amplitude = 0f;
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.unscaledDeltaTime);
            float life = totalSeconds > 0f ? remainingSeconds / totalSeconds : 0f;
            float envelope = life * life;
            float time = Time.unscaledTime * 27f;
            float x = Mathf.PerlinNoise(noiseSeed, time) * 2f - 1f;
            float y = Mathf.PerlinNoise(noiseSeed + 13.7f, time + 4.3f) * 2f - 1f;
            appliedOffset = Vector3.ClampMagnitude(new Vector3(x, y, 0f), 1f) * amplitude * envelope;
            transform.position += appliedOffset;
        }

        private void RemoveAppliedOffset()
        {
            if (appliedOffset.sqrMagnitude <= 0f)
            {
                return;
            }

            transform.position -= appliedOffset;
            appliedOffset = Vector3.zero;
        }

        private bool IsSourceNearView(Vector3 worldPosition)
        {
            if (strategyCamera == null)
            {
                return false;
            }

            Vector3 viewport = strategyCamera.WorldToViewportPoint(worldPosition);
            return viewport.z > 0f
                && viewport.x >= -ViewPadding
                && viewport.x <= 1f + ViewPadding
                && viewport.y >= -ViewPadding
                && viewport.y <= 1f + ViewPadding;
        }
    }
}
