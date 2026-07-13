using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyCinematicLightEmitter
    {
        private static readonly List<StrategyCinematicLightEmitter> ActiveEmitters = new();

        internal static bool IsWorldPositionCovered(Vector3 world, float radiusScale)
        {
            float safeScale = Mathf.Clamp(radiusScale, 0.1f, 1.25f);
            for (int i = ActiveEmitters.Count - 1; i >= 0; i--)
            {
                StrategyCinematicLightEmitter emitter = ActiveEmitters[i];
                if (emitter == null || !emitter.isActiveAndEnabled)
                {
                    ActiveEmitters.RemoveAt(i);
                    continue;
                }

                if (!emitter.TryGetNightMaskLight(
                        out Vector3 center,
                        out float radius,
                        out _,
                        out _))
                {
                    continue;
                }

                float effectiveRadius = radius * safeScale;
                if ((world - center).sqrMagnitude <= effectiveRadius * effectiveRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private void RegisterCoverageEmitter()
        {
            if (configured && isActiveAndEnabled && !ActiveEmitters.Contains(this))
            {
                ActiveEmitters.Add(this);
            }
        }

        private void OnEnable()
        {
            RegisterCoverageEmitter();
        }

        private void OnDisable()
        {
            ActiveEmitters.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveEmitters.Remove(this);
        }
    }
}
