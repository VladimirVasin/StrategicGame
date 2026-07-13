using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyNightLightTaskController
    {
        private static void RefreshCinematicSources()
        {
            StrategyCinematicVisualController visuals = Object.FindAnyObjectByType<StrategyCinematicVisualController>();
            visuals?.RefreshSceneLightingNow();
            EnsureRoadsideNightLightSources();
        }

        private static void EnsureRoadsideNightLightSources()
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            StrategyRoadsideLightSource[] roadsideLights = Object.FindObjectsByType<StrategyRoadsideLightSource>();
            for (int i = 0; i < roadsideLights.Length; i++)
            {
                StrategyRoadsideLightSource roadsideLight = roadsideLights[i];
                if (roadsideLight == null)
                {
                    continue;
                }

                if (!roadsideLight.TryGetComponent(out StrategyCinematicLightEmitter emitter))
                {
                    emitter = roadsideLight.gameObject.AddComponent<StrategyCinematicLightEmitter>();
                }

                emitter.ConfigureForRoadsideLight(roadsideLight);
                if (!roadsideLight.TryGetComponent(out StrategyNightLightSource source))
                {
                    source = roadsideLight.gameObject.AddComponent<StrategyNightLightSource>();
                }

                source.ConfigureForRoadside(roadsideLight);
            }
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
