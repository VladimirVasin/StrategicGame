using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyNightLightTaskController
    {
        private const float AmbientTorchAdultShare = 0.70f;
        private const float AmbientTorchEndProgress = 0.98f;
        private const float AmbientTorchStartJitter = 0.045f;
        private const int AmbientTorchMinAdults = 4;
        private const int AmbientTorchMaxAdults = 12;

        private readonly List<StrategyResidentAgent> ambientTorchCandidates = new();
        private readonly List<StrategyResidentAgent> ambientTorchResidents = new();
        private readonly Dictionary<StrategyResidentAgent, float> ambientTorchStartProgress = new();
        private int ambientTorchDayIndex = -1;

        private void UpdateAmbientTorchCarriers(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.Phase != StrategyTimeOfDayPhase.Dusk
                && snapshot.Phase != StrategyTimeOfDayPhase.Night)
            {
                return;
            }

            if (snapshot.Phase == StrategyTimeOfDayPhase.Dusk
                && snapshot.PhaseProgress < DuskTorchStartProgress)
            {
                return;
            }

            EnsureAmbientTorchPlan(snapshot);
            for (int i = 0; i < ambientTorchResidents.Count; i++)
            {
                StrategyResidentAgent resident = ambientTorchResidents[i];
                if (resident == null)
                {
                    continue;
                }

                if (!resident.CanReceiveEveningNightTorch)
                {
                    resident.SetEveningNightTorchActive(false);
                    continue;
                }

                bool ready = snapshot.Phase == StrategyTimeOfDayPhase.Night
                    || !ambientTorchStartProgress.TryGetValue(resident, out float startProgress)
                    || snapshot.PhaseProgress >= startProgress;
                if (ready)
                {
                    resident.SetEveningNightTorchActive(true);
                }
            }
        }

        private void EnsureAmbientTorchPlan(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.DayIndex == ambientTorchDayIndex)
            {
                return;
            }

            ClearAmbientTorchCarriers();
            ambientTorchDayIndex = snapshot.DayIndex;
            CollectAmbientTorchCandidates();
            if (ambientTorchCandidates.Count <= 0)
            {
                return;
            }

            Shuffle(ambientTorchCandidates);
            int desired = Mathf.CeilToInt(ambientTorchCandidates.Count * AmbientTorchAdultShare);
            desired = Mathf.Max(AmbientTorchMinAdults, desired);
            desired = Mathf.Clamp(desired, 1, Mathf.Min(AmbientTorchMaxAdults, ambientTorchCandidates.Count));

            float start = snapshot.Phase == StrategyTimeOfDayPhase.Dusk
                ? Mathf.Max(DuskTorchStartProgress, snapshot.PhaseProgress)
                : DuskTorchStartProgress;
            float end = Mathf.Max(start, AmbientTorchEndProgress);
            for (int i = 0; i < desired; i++)
            {
                StrategyResidentAgent resident = ambientTorchCandidates[i];
                ambientTorchResidents.Add(resident);

                float t = desired <= 1 ? 0f : i / (float)(desired - 1);
                float jitter = desired <= 1 ? 0f : Random.Range(-AmbientTorchStartJitter, AmbientTorchStartJitter);
                ambientTorchStartProgress[resident] = Mathf.Clamp(Mathf.Lerp(start, end, t) + jitter, start, 0.995f);
            }

            StrategyDebugLogger.Info(
                "NightLights",
                "AmbientTorchPlanCreated",
                StrategyDebugLogger.F("day", snapshot.DisplayDay),
                StrategyDebugLogger.F("phase", snapshot.PhaseLabel),
                StrategyDebugLogger.F("candidates", ambientTorchCandidates.Count),
                StrategyDebugLogger.F("carriers", ambientTorchResidents.Count));
        }

        private void CollectAmbientTorchCandidates()
        {
            ambientTorchCandidates.Clear();
            if (population == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.CanReceiveEveningNightTorch)
                {
                    ambientTorchCandidates.Add(resident);
                }
            }
        }

        private void ClearAmbientTorchCarriers()
        {
            for (int i = 0; i < ambientTorchResidents.Count; i++)
            {
                ambientTorchResidents[i]?.SetEveningNightTorchActive(false);
            }

            ambientTorchCandidates.Clear();
            ambientTorchResidents.Clear();
            ambientTorchStartProgress.Clear();
            ambientTorchDayIndex = -1;
        }

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
