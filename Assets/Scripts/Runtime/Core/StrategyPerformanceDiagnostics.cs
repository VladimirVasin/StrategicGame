using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyPerformanceDiagnostics : MonoBehaviour
    {
        private const float WindowSeconds = 10f;
        private const float MinimumWindowSeconds = 4f;
        private const int MaximumFrameSamples = 4096;
        private const float LongFrameThresholdMs = 33.333f;
        private const float SevereFrameThresholdMs = 50f;

        private readonly float[] frameTimesMs = new float[MaximumFrameSamples];
        private readonly float[] sortedFrameTimesMs = new float[MaximumFrameSamples];
        private readonly List<StrategyNightLightSource> nightLightSources = new();

        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyWildlifeController wildlife;
        private StrategyWeatherController weather;
        private StrategyTimeScaleController timeScale;
        private BenchmarkContext context;
        private StrategyResidentPerformanceSnapshot residentCountersAtWindowStart;
        private float windowElapsed;
        private float frameTimeSumMs;
        private float maximumFrameTimeMs;
        private int frameSampleCount;
        private int longFrameCount;
        private int severeFrameCount;
        private int gen0CollectionsAtWindowStart;
        private bool configured;
        private bool hasApplicationFocus = true;
        private bool applicationPaused;
        private bool skipNextFrameSample;

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyWildlifeController wildlifeController,
            StrategyWeatherController weatherController,
            StrategyTimeScaleController timeScaleController)
        {
            map = mapController;
            population = populationController;
            wildlife = wildlifeController;
            weather = weatherController;
            timeScale = timeScaleController;
            configured = map != null && population != null;
            context = CaptureContext();
            ResetWindow();
            skipNextFrameSample = true;

            StrategyDebugLogger.Info(
                "Performance",
                "DiagnosticsConfigured",
                StrategyDebugLogger.F("windowSeconds", WindowSeconds),
                StrategyDebugLogger.F("seed", map != null ? map.ActiveSeed : 0),
                StrategyDebugLogger.F("forcedSeed", StrategyPerformanceBenchmarkOptions.HasForcedSeed),
                StrategyDebugLogger.F("populationTargets", "15,30,50"));
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            if (!hasApplicationFocus || applicationPaused || Time.timeScale <= 0.001f)
            {
                ResetWindow();
                return;
            }

            if (skipNextFrameSample)
            {
                skipNextFrameSample = false;
                ResetWindow();
                return;
            }

            BenchmarkContext currentContext = CaptureContext();
            if (!currentContext.Equals(context))
            {
                if (windowElapsed >= MinimumWindowSeconds)
                {
                    LogWindow("context_changed");
                }

                context = currentContext;
                ResetWindow();
            }

            float frameMs = Mathf.Max(0f, Time.unscaledDeltaTime) * 1000f;
            frameTimesMs[frameSampleCount] = frameMs;
            frameSampleCount++;
            frameTimeSumMs += frameMs;
            maximumFrameTimeMs = Mathf.Max(maximumFrameTimeMs, frameMs);
            if (frameMs >= LongFrameThresholdMs)
            {
                longFrameCount++;
            }

            if (frameMs >= SevereFrameThresholdMs)
            {
                severeFrameCount++;
            }

            windowElapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
            if (windowElapsed >= WindowSeconds || frameSampleCount >= MaximumFrameSamples)
            {
                LogWindow("complete");
                ResetWindow();
            }
        }

        private void LogWindow(string reason)
        {
            if (frameSampleCount <= 0 || windowElapsed <= 0f)
            {
                return;
            }

            Array.Copy(frameTimesMs, sortedFrameTimesMs, frameSampleCount);
            Array.Sort(sortedFrameTimesMs, 0, frameSampleCount);
            float averageFrameMs = frameTimeSumMs / frameSampleCount;
            float p95FrameMs = GetPercentile(0.95f);
            float p99FrameMs = GetPercentile(0.99f);
            float averageFps = frameSampleCount / windowElapsed;
            float onePercentLowFps = p99FrameMs > 0.001f ? 1000f / p99FrameMs : 0f;
            StrategyResidentPerformanceSnapshot currentCounters = StrategyResidentPerformanceCounters.Capture();

            CountNightLights(out int nightLights, out int litNightLights);
            int residentCount = population != null ? population.TotalResidentCount : 0;
            int adultCount = population != null ? population.AdultResidentCount : 0;
            int childCount = population != null ? population.ChildResidentCount : 0;

            StrategyDebugLogger.Info(
                "Performance",
                "BaselineWindow",
                StrategyDebugLogger.F("scenario", context.GetScenarioId(map != null ? map.ActiveSeed : 0)),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("seconds", windowElapsed),
                StrategyDebugLogger.F("frames", frameSampleCount),
                StrategyDebugLogger.F("averageFps", averageFps),
                StrategyDebugLogger.F("onePercentLowFps", onePercentLowFps),
                StrategyDebugLogger.F("averageFrameMs", averageFrameMs),
                StrategyDebugLogger.F("p95FrameMs", p95FrameMs),
                StrategyDebugLogger.F("p99FrameMs", p99FrameMs),
                StrategyDebugLogger.F("maximumFrameMs", maximumFrameTimeMs),
                StrategyDebugLogger.F("framesOver33Ms", longFrameCount),
                StrategyDebugLogger.F("framesOver50Ms", severeFrameCount),
                StrategyDebugLogger.F("populationTarget", context.PopulationTarget),
                StrategyDebugLogger.F("residents", residentCount),
                StrategyDebugLogger.F("adults", adultCount),
                StrategyDebugLogger.F("children", childCount),
                StrategyDebugLogger.F("buildings", StrategyPlacedBuilding.ActiveBuildings.Count),
                StrategyDebugLogger.F("constructionSites", StrategyConstructionSite.ActiveSites.Count),
                StrategyDebugLogger.F("haulers", StrategyPopulationController.CountActiveSettlementHaulers()),
                StrategyDebugLogger.F("builders", StrategyPopulationController.CountActiveSettlementBuilders()),
                StrategyDebugLogger.F("deer", wildlife != null ? wildlife.Deer.Count : 0),
                StrategyDebugLogger.F("rabbits", wildlife != null ? wildlife.Rabbits.Count : 0),
                StrategyDebugLogger.F("fish", wildlife != null ? wildlife.Fish.Count : 0),
                StrategyDebugLogger.F("birds", wildlife != null ? wildlife.Birds.Count : 0),
                StrategyDebugLogger.F("wolves", wildlife != null ? wildlife.Wolves.Count : 0),
                StrategyDebugLogger.F("nightLights", nightLights),
                StrategyDebugLogger.F("litNightLights", litNightLights),
                StrategyDebugLogger.F("handTorches", StrategyResidentAgent.ActiveNightTorchLightCount),
                StrategyDebugLogger.F("pathRequests", currentCounters.PathRequests - residentCountersAtWindowStart.PathRequests),
                StrategyDebugLogger.F("pathSuccesses", currentCounters.PathSuccesses - residentCountersAtWindowStart.PathSuccesses),
                StrategyDebugLogger.F("pathFailures", currentCounters.PathFailures - residentCountersAtWindowStart.PathFailures),
                StrategyDebugLogger.F("pathDeferrals", currentCounters.PathBudgetDeferrals - residentCountersAtWindowStart.PathBudgetDeferrals),
                StrategyDebugLogger.F("navigationPending", StrategyNavigationService.Active != null ? StrategyNavigationService.Active.PendingCount : 0),
                StrategyDebugLogger.F("navigationCachedPaths", StrategyNavigationService.Active != null ? StrategyNavigationService.Active.CachedPathCount : 0),
                StrategyDebugLogger.F("decisionRuns", currentCounters.ScheduledDecisionRuns - residentCountersAtWindowStart.ScheduledDecisionRuns),
                StrategyDebugLogger.F("decisionDeferrals", currentCounters.ScheduledDecisionDeferrals - residentCountersAtWindowStart.ScheduledDecisionDeferrals),
                StrategyDebugLogger.F("monoMemoryMb", BytesToMegabytes(Profiler.GetMonoUsedSizeLong())),
                StrategyDebugLogger.F("allocatedMemoryMb", BytesToMegabytes(Profiler.GetTotalAllocatedMemoryLong())),
                StrategyDebugLogger.F("gen0Collections", GC.CollectionCount(0) - gen0CollectionsAtWindowStart));
        }

        private BenchmarkContext CaptureContext()
        {
            int residents = population != null ? population.TotalResidentCount : 0;
            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            StrategyWeatherKind weatherKind = weather != null ? weather.CurrentWeather : StrategyWeatherKind.Clear;
            int scale = Mathf.RoundToInt(timeScale != null ? timeScale.CurrentScale : Time.timeScale);
            return new BenchmarkContext(GetPopulationTarget(residents), calendar.Phase, weatherKind, Mathf.Max(0, scale));
        }

        private void ResetWindow()
        {
            windowElapsed = 0f;
            frameTimeSumMs = 0f;
            maximumFrameTimeMs = 0f;
            frameSampleCount = 0;
            longFrameCount = 0;
            severeFrameCount = 0;
            gen0CollectionsAtWindowStart = GC.CollectionCount(0);
            residentCountersAtWindowStart = StrategyResidentPerformanceCounters.Capture();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            hasApplicationFocus = hasFocus;
            skipNextFrameSample = true;
            ResetWindow();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            skipNextFrameSample = true;
            ResetWindow();
        }

        private float GetPercentile(float percentile)
        {
            int index = Mathf.Clamp(Mathf.CeilToInt(frameSampleCount * percentile) - 1, 0, frameSampleCount - 1);
            return sortedFrameTimesMs[index];
        }

        private void CountNightLights(out int total, out int lit)
        {
            StrategyNightLightSource.CopyActiveSources(nightLightSources);
            total = nightLightSources.Count;
            lit = 0;
            for (int i = 0; i < nightLightSources.Count; i++)
            {
                if (nightLightSources[i] != null && nightLightSources[i].IsLit)
                {
                    lit++;
                }
            }
        }

        private static int GetPopulationTarget(int residentCount)
        {
            if (residentCount >= 50)
            {
                return 50;
            }

            if (residentCount >= 30)
            {
                return 30;
            }

            return residentCount >= 15 ? 15 : 0;
        }

        private static float BytesToMegabytes(long bytes)
        {
            return bytes / (1024f * 1024f);
        }

        private readonly struct BenchmarkContext : IEquatable<BenchmarkContext>
        {
            public BenchmarkContext(
                int populationTarget,
                StrategyTimeOfDayPhase phase,
                StrategyWeatherKind weather,
                int timeScale)
            {
                PopulationTarget = populationTarget;
                Phase = phase;
                Weather = weather;
                TimeScale = timeScale;
            }

            public int PopulationTarget { get; }
            public StrategyTimeOfDayPhase Phase { get; }
            public StrategyWeatherKind Weather { get; }
            public int TimeScale { get; }

            public string GetScenarioId(int seed)
            {
                string population = PopulationTarget > 0 ? "p" + PopulationTarget : "startup";
                return "seed" + seed + "_" + population + "_" + Phase + "_" + Weather + "_x" + TimeScale;
            }

            public bool Equals(BenchmarkContext other)
            {
                return PopulationTarget == other.PopulationTarget
                    && Phase == other.Phase
                    && Weather == other.Weather
                    && TimeScale == other.TimeScale;
            }
        }
    }

    internal static class StrategyPerformanceBenchmarkOptions
    {
        private const string SeedArgument = "-strategyBenchmarkSeed";
        private static bool parsed;
        private static bool hasForcedSeed;
        private static int forcedSeed;

        public static bool HasForcedSeed
        {
            get
            {
                EnsureParsed();
                return hasForcedSeed;
            }
        }

        public static bool TryGetForcedSeed(out int seed)
        {
            EnsureParsed();
            seed = forcedSeed;
            return hasForcedSeed;
        }

        private static void EnsureParsed()
        {
            if (parsed)
            {
                return;
            }

            parsed = true;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                string argument = arguments[i];
                if (argument.StartsWith(SeedArgument + "=", StringComparison.OrdinalIgnoreCase))
                {
                    TrySetSeed(argument.Substring(SeedArgument.Length + 1));
                    return;
                }

                if (string.Equals(argument, SeedArgument, StringComparison.OrdinalIgnoreCase)
                    && i + 1 < arguments.Length)
                {
                    TrySetSeed(arguments[i + 1]);
                    return;
                }
            }
        }

        private static void TrySetSeed(string value)
        {
            if (int.TryParse(value, out int parsedSeed) && parsedSeed > 0)
            {
                forcedSeed = parsedSeed;
                hasForcedSeed = true;
            }
        }
    }
}
