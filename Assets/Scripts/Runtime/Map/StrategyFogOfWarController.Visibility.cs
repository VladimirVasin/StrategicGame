using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFogOfWarController
    {
        private void UpdateNightVisionTuning()
        {
            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            nightVisionPressure = EvaluateNightVisionPressure(snapshot);
            exploredAlpha = Mathf.Lerp(DayExploredAlpha, NightExploredAlpha, nightVisionPressure);

            if (snapshot.Phase == loggedVisionPhase)
            {
                return;
            }

            loggedVisionPhase = snapshot.Phase;
            StrategyDebugLogger.Info(
                "Fog",
                "VisionPhaseChanged",
                StrategyDebugLogger.F("phase", snapshot.PhaseLabel),
                StrategyDebugLogger.F("nightPressure", nightVisionPressure),
                StrategyDebugLogger.F("campRadius", GetCurrentRevealRadius(CampRevealRadius, RevealSourceKind.Camp)),
                StrategyDebugLogger.F("residentRadius", GetCurrentRevealRadius(ResidentRevealRadius, RevealSourceKind.Resident)),
                StrategyDebugLogger.F("buildingRadius", GetCurrentRevealRadius(BuildingRevealRadius, RevealSourceKind.Building)),
                StrategyDebugLogger.F("exploredAlpha", exploredAlpha));
        }

        private static float EvaluateNightVisionPressure(StrategyCalendarSnapshot snapshot)
        {
            switch (snapshot.Phase)
            {
                case StrategyTimeOfDayPhase.Dusk:
                    return Smooth01(snapshot.PhaseProgress);
                case StrategyTimeOfDayPhase.Night:
                    return 1f;
                case StrategyTimeOfDayPhase.Dawn:
                    return 1f - Smooth01(snapshot.PhaseProgress);
                default:
                    return 0f;
            }
        }

        private float GetCurrentRevealRadius(RevealSource source)
        {
            return GetCurrentRevealRadius(source.Radius, source.Kind);
        }

        private float GetCurrentRevealRadius(float radius, RevealSourceKind kind)
        {
            float multiplier = GetNightRevealMultiplier(kind) * GetWeatherFogRevealMultiplier(kind);
            float minimumRadius = GetCurrentMinimumRevealRadius(kind);
            return Mathf.Max(minimumRadius, radius * multiplier);
        }

        private float GetCurrentEdgeSoftness(RevealSource source)
        {
            float multiplier = GetNightRevealMultiplier(source.Kind) * GetWeatherFogRevealMultiplier(source.Kind);
            return Mathf.Lerp(0.75f, RevealEdgeSoftness, multiplier);
        }

        private float GetCurrentMinimumRevealRadius(RevealSourceKind kind)
        {
            return Mathf.Lerp(GetMinimumRevealRadius(kind), GetWeatherFogMinimumRevealRadius(kind), weatherFogPressure);
        }

        private float GetNightRevealMultiplier(RevealSourceKind kind)
        {
            switch (kind)
            {
                case RevealSourceKind.Camp:
                    return Mathf.Lerp(1f, NightCampRevealMultiplier, nightVisionPressure);
                case RevealSourceKind.Resident:
                    return Mathf.Lerp(1f, NightResidentRevealMultiplier, nightVisionPressure);
                case RevealSourceKind.Building:
                    return Mathf.Lerp(1f, NightBuildingRevealMultiplier, nightVisionPressure);
                default:
                    return 1f;
            }
        }

        private static float GetMinimumRevealRadius(RevealSourceKind kind)
        {
            switch (kind)
            {
                case RevealSourceKind.Camp:
                    return MinimumCampRevealRadius;
                case RevealSourceKind.Resident:
                    return MinimumResidentRevealRadius;
                case RevealSourceKind.Building:
                    return MinimumBuildingRevealRadius;
                default:
                    return 1f;
            }
        }

        private static float EvaluateRevealStrength(float distance, float radius, float edgeSoftness)
        {
            float innerRadius = Mathf.Max(0f, radius - 0.75f);
            float strength = 1f - Mathf.InverseLerp(innerRadius, radius + edgeSoftness, distance);
            return Mathf.Clamp01(strength);
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private enum RevealSourceKind
        {
            Camp,
            Resident,
            Building
        }

        private readonly struct RevealSource
        {
            public RevealSource(Vector2 cellCenter, float radius, RevealSourceKind kind)
            {
                CellCenter = cellCenter;
                Radius = radius;
                Kind = kind;
            }

            public Vector2 CellCenter { get; }
            public float Radius { get; }
            public RevealSourceKind Kind { get; }
        }
    }
}
