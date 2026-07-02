using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCinematicVisualController
    {
        private const float NightMaskEmitterPadding = 14f;

        private readonly List<StrategyPlacedBuilding> cinematicBuildingQuery = new();
        private readonly List<StrategyRoadsideLightSource> roadsideLightQuery = new();
        private readonly List<StrategyCinematicLightEmitter> cinematicVisibleEmitters = new();
        private readonly List<StrategyCinematicLightEmitter> nightMaskEmitters = new();

        private void ScanLightEmitters()
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            emitters.Clear();
            CopyCinematicBuildings(cinematicBuildingQuery);
            for (int i = 0; i < cinematicBuildingQuery.Count; i++)
            {
                StrategyPlacedBuilding building = cinematicBuildingQuery[i];
                if (building == null)
                {
                    continue;
                }

                if (!building.TryGetComponent(out StrategyCinematicLightEmitter emitter))
                {
                    emitter = building.gameObject.AddComponent<StrategyCinematicLightEmitter>();
                }

                emitter.ConfigureForBuilding(building);
                emitters.Add(emitter);
            }

            ScanCampfireEmitters();
            ScanRoadsideLightEmitters();
        }

        private void ScanCampfireEmitters()
        {
            StrategyCampfireAnimator[] campfires = Object.FindObjectsByType<StrategyCampfireAnimator>();
            for (int i = 0; i < campfires.Length; i++)
            {
                StrategyCampfireAnimator campfire = campfires[i];
                if (campfire == null)
                {
                    continue;
                }

                if (!campfire.TryGetComponent(out StrategyCinematicLightEmitter emitter))
                {
                    emitter = campfire.gameObject.AddComponent<StrategyCinematicLightEmitter>();
                }

                emitter.ConfigureForCampfire(campfire);
                emitters.Add(emitter);
            }
        }

        private void ScanRoadsideLightEmitters()
        {
            StrategyRoadsideLightSource.CopyActiveSources(roadsideLightQuery);
            for (int i = 0; i < roadsideLightQuery.Count; i++)
            {
                StrategyRoadsideLightSource roadsideLight = roadsideLightQuery[i];
                if (roadsideLight == null)
                {
                    continue;
                }

                if (!roadsideLight.TryGetComponent(out StrategyCinematicLightEmitter emitter))
                {
                    emitter = roadsideLight.gameObject.AddComponent<StrategyCinematicLightEmitter>();
                }

                emitter.ConfigureForRoadsideLight(roadsideLight);
                emitters.Add(emitter);
            }
        }

        private void RefreshEmitterLods(Rect view)
        {
            cinematicVisibleEmitters.Clear();
            nightMaskEmitters.Clear();
            if (emitters.Count == 0)
            {
                return;
            }

            Rect expanded = ExpandRect(view, EmitterLodPadding);
            Rect maskExpanded = ExpandRect(view, NightMaskEmitterPadding);
            Vector3 center = new(view.center.x, view.center.y, 0f);
            for (int i = emitters.Count - 1; i >= 0; i--)
            {
                StrategyCinematicLightEmitter emitter = emitters[i];
                if (emitter == null)
                {
                    emitters.RemoveAt(i);
                    continue;
                }

                bool visible = emitter.RefreshCinematicVisibility(expanded);
                if (visible)
                {
                    cinematicVisibleEmitters.Add(emitter);
                }

                if (TryGetEmitterMaskLightInView(emitter, maskExpanded))
                {
                    nightMaskEmitters.Add(emitter);
                }
            }

            for (int slot = 0; slot < MaxActivePointLights; slot++)
            {
                StrategyCinematicLightEmitter best = null;
                float bestScore = float.NegativeInfinity;
                for (int i = 0; i < cinematicVisibleEmitters.Count; i++)
                {
                    StrategyCinematicLightEmitter emitter = cinematicVisibleEmitters[i];
                    if (emitter == null || emitter.CinematicPointLightAllowed)
                    {
                        continue;
                    }

                    float score = emitter.GetCinematicLightScore(center);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = emitter;
                    }
                }

                if (best == null)
                {
                    break;
                }

                best.EnableCinematicPointLight();
            }
        }

        private static void CopyCinematicBuildings(List<StrategyPlacedBuilding> target)
        {
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (chunks == null || chunks.CopyActiveBuildingComponents(target) <= 0)
            {
                target.Clear();
                IReadOnlyList<StrategyPlacedBuilding> active = StrategyPlacedBuilding.ActiveBuildings;
                for (int i = 0; i < active.Count; i++)
                {
                    if (active[i] != null)
                    {
                        target.Add(active[i]);
                    }
                }
            }
        }

        private static bool TryGetEmitterMaskLightInView(
            StrategyCinematicLightEmitter emitter,
            Rect view)
        {
            return emitter.TryGetNightMaskLight(
                    out Vector3 center,
                    out float radius,
                    out _)
                && CircleIntersectsRect(center, radius, view);
        }

        private static bool CircleIntersectsRect(Vector3 center, float radius, Rect rect)
        {
            float nearestX = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
            float nearestY = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
            float dx = center.x - nearestX;
            float dy = center.y - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
