using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWindSway : MonoBehaviour
    {
        private StrategyWindController windController;
        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private Quaternion baseLocalRotation;
        private float phase;
        private float bendDegrees;
        private float offsetAmplitude;
        private float stretchAmplitude;

        public void Configure(
            StrategyWindController controller,
            float swayPhase,
            float maxBendDegrees,
            float maxOffsetAmplitude,
            float maxStretchAmplitude)
        {
            windController = controller;
            phase = swayPhase;
            bendDegrees = maxBendDegrees;
            offsetAmplitude = maxOffsetAmplitude;
            stretchAmplitude = maxStretchAmplitude;
            CaptureBaseTransform();
        }

        private void Awake()
        {
            CaptureBaseTransform();
        }

        private void Update()
        {
            if (windController == null)
            {
                windController = StrategyWindController.Active;
            }

            WindZone windZone = windController != null ? windController.WindZone : null;
            if (windController == null || windZone == null)
            {
                return;
            }

            Vector2 direction = windController.PlanarDirection;
            float time = Time.time;
            float pulseSpeed = Mathf.Max(0.05f, windZone.windPulseFrequency) * 2.2f;
            float turbulence = Mathf.Max(0f, windZone.windTurbulence);
            float pulse = Mathf.Sin(time * pulseSpeed + phase);
            float gust = Mathf.PerlinNoise(phase * 0.37f, time * (0.18f + turbulence * 0.22f)) - 0.5f;
            float strength = Mathf.Max(0f, windZone.windMain + windZone.windPulseMagnitude * pulse + turbulence * gust);
            float flutter = Mathf.Sin(time * (pulseSpeed * 2.7f + 0.35f) + phase * 1.7f) * turbulence;
            float sway = (pulse + flutter * 0.38f) * strength;
            float lean = direction.x * strength * 0.32f;

            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, (sway + lean) * bendDegrees);
            transform.localPosition = baseLocalPosition + new Vector3(direction.x * sway * offsetAmplitude, 0f, 0f);

            float stretch = 1f + Mathf.Abs(sway) * stretchAmplitude;
            transform.localScale = new Vector3(
                baseLocalScale.x * (1f + Mathf.Abs(sway) * stretchAmplitude * 0.35f),
                baseLocalScale.y * stretch,
                baseLocalScale.z);
        }

        private void CaptureBaseTransform()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalScale = transform.localScale;
            baseLocalRotation = transform.localRotation;
        }
    }
}
