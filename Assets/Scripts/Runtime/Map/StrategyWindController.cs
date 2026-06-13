using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWindController : MonoBehaviour
    {
        [SerializeField] private Vector2 planarDirection = new Vector2(1f, 0.18f);
        [SerializeField] private float windMain = 0.62f;
        [SerializeField] private float windPulseMagnitude = 0.24f;
        [SerializeField] private float windPulseFrequency = 0.42f;
        [SerializeField] private float windTurbulence = 0.34f;

        private WindZone windZone;
        private float weatherMainBoost;
        private float weatherPulseBoost;
        private float weatherTurbulenceBoost;

        public static StrategyWindController Active { get; private set; }
        public WindZone WindZone => windZone;
        public Vector2 PlanarDirection => GetNormalizedPlanarDirection();

        public void ConfigureDefault()
        {
            Active = this;
            EnsureWindZone();
            ApplyWindSettings();
        }

        public void SetWeatherInfluence(float mainBoost, float pulseBoost, float turbulenceBoost)
        {
            weatherMainBoost = Mathf.Max(0f, mainBoost);
            weatherPulseBoost = Mathf.Max(0f, pulseBoost);
            weatherTurbulenceBoost = Mathf.Max(0f, turbulenceBoost);
            EnsureWindZone();
            ApplyWindSettings();
        }

        private void Awake()
        {
            Active = this;
            EnsureWindZone();
            ApplyWindSettings();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void EnsureWindZone()
        {
            if (windZone != null)
            {
                return;
            }

            windZone = GetComponent<WindZone>();
            if (windZone == null)
            {
                windZone = gameObject.AddComponent<WindZone>();
            }
        }

        private void ApplyWindSettings()
        {
            if (windZone == null)
            {
                return;
            }

            windZone.mode = WindZoneMode.Directional;
            windZone.windMain = Mathf.Max(0f, windMain + weatherMainBoost);
            windZone.windPulseMagnitude = Mathf.Max(0f, windPulseMagnitude + weatherPulseBoost);
            windZone.windPulseFrequency = Mathf.Max(0.01f, windPulseFrequency);
            windZone.windTurbulence = Mathf.Max(0f, windTurbulence + weatherTurbulenceBoost);

            Vector2 direction = GetNormalizedPlanarDirection();
            Vector3 forward = new Vector3(direction.x, direction.y, 0f);
            transform.rotation = Quaternion.LookRotation(forward, Vector3.forward);
        }

        private Vector2 GetNormalizedPlanarDirection()
        {
            if (planarDirection.sqrMagnitude < 0.0001f)
            {
                return Vector2.right;
            }

            return planarDirection.normalized;
        }
    }
}
