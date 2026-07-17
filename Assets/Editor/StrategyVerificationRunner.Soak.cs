using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const int DefaultSoakSeed = 74123;
        private const float FullSoakGameSeconds = 720f;
        private const float QuickSoakGameSeconds = 45f;
        private const int SoakCaptureFramerate = 60;
        private const int SoakSampleIntervalFrames = 60;
        private const int FullSoakMaximumFrames = 18000;
        private const int QuickSoakMaximumFrames = 1200;
        private const long SoakMemoryGrowthBudgetBytes = 128L * 1024L * 1024L;
        private static readonly HashSet<int> SoakResidentIds = new();
        private static bool soakInitialized;
        private static int soakSeed;
        private static int soakFrames;
        private static int soakMinimumResidents;
        private static int soakMaximumResidents;
        private static int soakMaximumBuildings;
        private static int soakMaximumNavigationPending;
        private static int soakResolvedDialogs;
        private static long soakStartMemory;
        private static long soakMaximumMemory;
        private static float soakStartElapsed;
        private static float soakTargetGameSeconds;
        private static int soakFrameLimit;

        public static void RunSoakSmoke()
        {
            StartSoakSmoke(SmokeKind.Soak, "SoakSmoke.txt");
        }

        public static void RunQuickSoakSmoke()
        {
            StartSoakSmoke(SmokeKind.QuickSoak, "QuickSoakSmoke.txt");
        }

        private static void StartSoakSmoke(SmokeKind kind, string resultFileName)
        {
            File.WriteAllText(GetResultPath(resultFileName), "RUNNING");
            ResetSoakState();
            ConfigureSoak(kind);
            soakSeed = StrategyPerformanceBenchmarkOptions.TryGetForcedSeed(out int forcedSeed)
                ? forcedSeed
                : DefaultSoakSeed;
            UnityEngine.Random.InitState(soakSeed);
            Time.captureFramerate = SoakCaptureFramerate;
            StartPlayModeSmoke(kind, GameplayScenePath);
        }

        private static void RestoreSoakAfterDomainReload()
        {
            ResetSoakState();
            ConfigureSoak(smokeKind);
            soakSeed = StrategyPerformanceBenchmarkOptions.TryGetForcedSeed(out int forcedSeed)
                ? forcedSeed
                : DefaultSoakSeed;
            UnityEngine.Random.InitState(soakSeed);
            Time.captureFramerate = SoakCaptureFramerate;
        }

        private static void UpdateSoakSmoke(
            CityMapController map,
            StrategyPopulationController population)
        {
            if (!soakInitialized)
            {
                InitializeSoak(map, population);
            }

            ResolveSoakModalInput();
            soakFrames++;
            Require(soakFrames <= soakFrameLimit, "Soak exceeded its deterministic frame budget");
            if (soakFrames % SoakSampleIntervalFrames == 0)
            {
                VerifySoakSnapshot(map, population);
            }

            float elapsed = StrategyDayNightCycleController.CurrentElapsedSeconds - soakStartElapsed;
            if (elapsed < soakTargetGameSeconds)
            {
                return;
            }

            VerifySoakSnapshot(map, population);
            int errorCount = GetSmokeErrorCount();
            Require(errorCount == 0, "Unity emitted " + errorCount + " error(s) during soak");
            long memoryDelta = Profiler.GetTotalAllocatedMemoryLong() - soakStartMemory;
            Require(
                memoryDelta <= SoakMemoryGrowthBudgetBytes,
                $"Soak memory grew by {memoryDelta} bytes; budget is {SoakMemoryGrowthBudgetBytes} bytes");
            long peakMemoryGrowth = soakMaximumMemory - soakStartMemory;
            Require(
                peakMemoryGrowth <= SoakMemoryGrowthBudgetBytes,
                $"Soak peak memory grew by {peakMemoryGrowth} bytes; budget is {SoakMemoryGrowthBudgetBytes} bytes");
            string result = string.Format(
                CultureInfo.InvariantCulture,
                "PASS: seed={0} frames={1} gameSeconds={2:0.0} residents={3}-{4} buildingsMax={5} navPendingMax={6} dialogsResolved={7} errors={8} memoryDeltaBytes={9} memoryPeakGrowthBytes={10} memoryMaxBytes={11} memoryBudgetBytes={12}",
                soakSeed,
                soakFrames,
                elapsed,
                soakMinimumResidents,
                soakMaximumResidents,
                soakMaximumBuildings,
                soakMaximumNavigationPending,
                soakResolvedDialogs,
                errorCount,
                memoryDelta,
                peakMemoryGrowth,
                soakMaximumMemory,
                SoakMemoryGrowthBudgetBytes);
            CompletePlayMode(true, result);
        }

        private static void InitializeSoak(
            CityMapController map,
            StrategyPopulationController population)
        {
            Require(map.ActiveSeed == soakSeed, "Soak map did not use the requested deterministic seed");
            StrategyTimeScaleController timeScale =
                UnityEngine.Object.FindAnyObjectByType<StrategyTimeScaleController>();
            Require(timeScale != null, "Time-scale controller is missing during soak");
            Require(!timeScale.IsPausedByLock, "Soak started with an unexpected pause lock");
            timeScale.SetRequestedScale(3f);

            soakInitialized = true;
            soakStartElapsed = StrategyDayNightCycleController.CurrentElapsedSeconds;
            soakStartMemory = Profiler.GetTotalAllocatedMemoryLong();
            soakMaximumMemory = soakStartMemory;
            soakMinimumResidents = int.MaxValue;
            VerifySoakSnapshot(map, population);
        }

        private static void VerifySoakSnapshot(
            CityMapController map,
            StrategyPopulationController population)
        {
            StrategyGameContext context = StrategyGameContext.Current;
            Require(context != null && context.IsReady, "Game context left Ready state during soak");
            Require(map != null && map.IsGenerated, "Map became unavailable during soak");
            Require(population != null && population.TotalResidentCount > 0, "Population became empty during soak");
            Require(
                StrategyAudioVoicePool.ActiveVoiceCount <= StrategyAudioVoicePool.Capacity,
                "Audio voice budget exceeded during soak");

            StrategyNavigationService navigation =
                UnityEngine.Object.FindAnyObjectByType<StrategyNavigationService>();
            Require(navigation != null, "Navigation service is missing during soak");
            Require(navigation.PendingCount <= 256, "Navigation queue exceeded its hard capacity");

            SoakResidentIds.Clear();
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                Require(resident != null, "Population contains a null resident");
                Require(resident.ResidentId > 0, "Population contains a non-positive resident ID");
                Require(SoakResidentIds.Add(resident.ResidentId), "Population contains a duplicate resident ID");
                Vector3 position = resident.transform.position;
                Require(IsFinite(position.x) && IsFinite(position.y) && IsFinite(position.z),
                    "Resident position became non-finite");
            }

            int residentCount = population.TotalResidentCount;
            soakMinimumResidents = Mathf.Min(soakMinimumResidents, residentCount);
            soakMaximumResidents = Mathf.Max(soakMaximumResidents, residentCount);
            soakMaximumBuildings = Mathf.Max(
                soakMaximumBuildings,
                StrategyPlacedBuilding.ActiveBuildings.Count);
            soakMaximumNavigationPending = Mathf.Max(
                soakMaximumNavigationPending,
                navigation.PendingCount);
            soakMaximumMemory = Math.Max(soakMaximumMemory, Profiler.GetTotalAllocatedMemoryLong());
        }

        private static void ResolveSoakModalInput()
        {
            StrategyInputRouter router = UnityEngine.Object.FindAnyObjectByType<StrategyInputRouter>();
            Require(router != null, "Input router is missing during soak");
            StrategyFirstNightFaunaStoryController firstNightStory =
                UnityEngine.Object.FindAnyObjectByType<StrategyFirstNightFaunaStoryController>();
            if (firstNightStory != null && firstNightStory.IsOpen)
            {
                StrategyTimeScaleController firstNightTimeScale =
                    UnityEngine.Object.FindAnyObjectByType<StrategyTimeScaleController>();
                Require(
                    UnityEngine.Object.FindObjectsByType<StrategyCatAgent>().Length == 0,
                    "A settlement cat appeared before the first-night story resolved");
                Require(
                    UnityEngine.Object.FindObjectsByType<StrategyMouseAgent>().Length >= 3,
                    "First-night mice were not visibly established before the story opened");
                Require(router.ActiveContextCount == 1, "First-night story created an unexpected input-context stack");
                Require(router.BlockedChannels == StrategyInputChannel.All, "First-night story did not block all input channels");
                Require(router.TopCancelMode == StrategyCancelMode.Swallow, "First-night story did not swallow cancellation");
                Require(firstNightTimeScale != null && firstNightTimeScale.IsPausedByLock,
                    "First-night story did not hold a pause lock");

                Button[] storyButtons = firstNightStory.GetComponentsInChildren<Button>(true);
                Button continueButton = null;
                int continueButtonCount = 0;
                for (int i = 0; i < storyButtons.Length; i++)
                {
                    if (storyButtons[i] != null && storyButtons[i].name == "ContinueButton")
                    {
                        continueButton = storyButtons[i];
                        continueButtonCount++;
                    }
                }

                Require(continueButtonCount == 1, "First-night story must expose exactly one continue action");
                continueButton.onClick.Invoke();
                soakResolvedDialogs++;
                if (!firstNightStory.IsOpen)
                {
                    Require(
                        StrategyFirstNightFaunaEventController.Active != null
                        && StrategyFirstNightFaunaEventController.Active.Stage
                            == StrategyFirstNightFaunaStage.StoryCompleted,
                        "First-night story closed without completing the fauna event");
                    Require(router.ActiveContextCount == 0, "First-night story leaked its input context");
                    Require(!firstNightTimeScale.IsPausedByLock, "First-night story leaked its pause lock");
                    Require(
                        UnityEngine.Object.FindObjectsByType<StrategyCatAgent>().Length >= 1,
                        "Completing the first-night story did not create the first settlement cat");
                }

                return;
            }

            StrategyPointOfInterestDialogController pointDialog =
                UnityEngine.Object.FindAnyObjectByType<StrategyPointOfInterestDialogController>();
            if (pointDialog != null && pointDialog.IsOpen)
            {
                StrategyTimeScaleController pointTimeScale =
                    UnityEngine.Object.FindAnyObjectByType<StrategyTimeScaleController>();
                Require(router.ActiveContextCount == 1, "Point-of-interest dialog created an unexpected input-context stack");
                Require(router.BlockedChannels == StrategyInputChannel.All, "Point-of-interest dialog did not block all input channels");
                Require(router.TopCancelMode == StrategyCancelMode.Swallow, "Point-of-interest dialog did not swallow cancellation");
                Require(pointTimeScale != null && pointTimeScale.IsPausedByLock, "Point-of-interest dialog did not hold a pause lock");

                Button[] pointButtons = pointDialog.GetComponentsInChildren<Button>(true);
                Button okButton = null;
                int okButtonCount = 0;
                for (int i = 0; i < pointButtons.Length; i++)
                {
                    if (pointButtons[i] != null && pointButtons[i].name == "OkButton")
                    {
                        okButton = pointButtons[i];
                        okButtonCount++;
                    }
                }

                Require(okButtonCount == 1, "Point-of-interest dialog must expose exactly one OK action");
                okButton.onClick.Invoke();
                soakResolvedDialogs++;
                return;
            }

            if (pointDialog != null && pointDialog.IsInputShieldActive)
            {
                Require(router.ActiveContextCount == 1, "Point-of-interest dialog closing shield lost its input context");
                return;
            }

            StrategyRefugeeDialogController dialog =
                UnityEngine.Object.FindAnyObjectByType<StrategyRefugeeDialogController>();
            if (dialog == null || !dialog.IsOpen)
            {
                if (dialog != null && dialog.IsInputShieldActive)
                {
                    Require(router.ActiveContextCount == 1, "Refugee dialog closing shield lost its input context");
                }
                else
                {
                    Require(router.ActiveContextCount == 0, "Input contexts leaked outside an active modal dialog");
                }

                return;
            }

            Require(router.ActiveContextCount == 1, "Refugee dialog created an unexpected input-context stack");
            Require(router.BlockedChannels == StrategyInputChannel.All, "Refugee dialog did not block all input channels");
            Require(router.TopCancelMode == StrategyCancelMode.Swallow, "Refugee dialog did not swallow cancellation");

            Button rejectButton = null;
            int rejectButtonCount = 0;
            Button[] buttons = dialog.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "Reject")
                {
                    rejectButton = buttons[i];
                    rejectButtonCount++;
                }
            }

            Require(rejectButtonCount == 1, "Refugee dialog must expose exactly one reject action");
            rejectButton.onClick.Invoke();
            soakResolvedDialogs++;
            Require(!dialog.IsOpen, "Refugee dialog remained open after its reject action");
            Require(dialog.IsInputShieldActive, "Refugee dialog dropped its closing input shield early");
            Require(router.ActiveContextCount == 1, "Refugee dialog closing shield lost its input context");
        }

        private static void CleanupSoak()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            ResetSoakState();
        }

        private static void ResetSoakState()
        {
            soakInitialized = false;
            soakFrames = 0;
            soakMinimumResidents = 0;
            soakMaximumResidents = 0;
            soakMaximumBuildings = 0;
            soakMaximumNavigationPending = 0;
            soakResolvedDialogs = 0;
            soakStartMemory = 0;
            soakMaximumMemory = 0;
            soakStartElapsed = 0f;
            SoakResidentIds.Clear();
        }

        private static void ConfigureSoak(SmokeKind kind)
        {
            bool quick = kind == SmokeKind.QuickSoak;
            soakTargetGameSeconds = quick ? QuickSoakGameSeconds : FullSoakGameSeconds;
            soakFrameLimit = quick ? QuickSoakMaximumFrames : FullSoakMaximumFrames;
        }

        private static bool IsSoakKind(SmokeKind kind)
        {
            return kind == SmokeKind.Soak || kind == SmokeKind.QuickSoak;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
