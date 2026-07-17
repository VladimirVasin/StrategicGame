using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFirstNightFaunaEventController : MonoBehaviour
    {
        private StrategySettlementFaunaController fauna;
        private StrategyFirstNightFaunaStoryController story;
        private StrategyInGameCinematicPlayer cinematicPlayer;
        private StrategyCityInventory cityInventory;
        private StrategyCityItemRewardRevealController rewardReveal;
        private StrategyFirstNightRatCinematic ratCinematic;
        private StrategyFirstNightCatHuntCinematic catHuntCinematic;
        private StrategyFirstNightFaunaStage stage = StrategyFirstNightFaunaStage.Dormant;
        private bool ratCinematicRunning;
        private bool ratCinematicCompletedThisSession;
        private bool catHuntCinematicRunning;
        private bool configured;

        public static StrategyFirstNightFaunaEventController Active { get; private set; }
        public StrategyFirstNightFaunaStage Stage => stage;
        public bool IsRatCinematicPlaying => ratCinematicRunning
            && cinematicPlayer != null
            && cinematicPlayer.IsPlaying;
        public bool IsCatHuntCinematicPlaying => catHuntCinematicRunning
            && cinematicPlayer != null
            && cinematicPlayer.IsPlaying;
        public bool HasVisibleCatHuntActors => catHuntCinematic != null
            && catHuntCinematic.AreBothActorsVisible;
        public bool IsStoryPending => configured
            && stage == StrategyFirstNightFaunaStage.MiceVisible
            && HasReachedFirstNight(StrategyDayNightCycleController.CurrentCalendarSnapshot);

        public void Configure(
            StrategySettlementFaunaController faunaController,
            StrategyFirstNightFaunaStoryController storyController,
            StrategyInGameCinematicPlayer inGameCinematicPlayer,
            StrategyPopulationController population,
            CityMapController map,
            StrategyCityInventory cityInventoryController,
            StrategyCityItemRewardRevealController rewardRevealController)
        {
            CancelCatHuntCinematic();
            CancelRatCinematic();
            fauna = faunaController;
            story = storyController;
            cinematicPlayer = inGameCinematicPlayer;
            cityInventory = cityInventoryController;
            rewardReveal = rewardRevealController;
            ratCinematic = population != null && map != null
                ? new StrategyFirstNightRatCinematic(population, map, transform)
                : null;
            catHuntCinematic = population != null && map != null
                ? new StrategyFirstNightCatHuntCinematic(population, map, transform)
                : null;
            configured = fauna != null && story != null;
            stage = fauna != null ? fauna.Stage : StrategyFirstNightFaunaStage.Dormant;
            ratCinematicCompletedThisSession = stage == StrategyFirstNightFaunaStage.StoryCompleted;
            fauna?.SetFirstNightStage(stage);
            story?.RestoreResolvedState(stage == StrategyFirstNightFaunaStage.StoryCompleted);
            StrategyDebugLogger.Info(
                "FirstNightFauna",
                "Configured",
                StrategyDebugLogger.F("stage", stage),
                StrategyDebugLogger.F("ready", configured));
        }

        public void RestoreStage(StrategyFirstNightFaunaStage restoredStage)
        {
            CancelCatHuntCinematic();
            CancelRatCinematic();
            stage = IsValidStage(restoredStage)
                ? restoredStage
                : StrategyFirstNightFaunaStage.Dormant;
            BackfillCatsEntitlementIfNeeded();
            ratCinematicCompletedThisSession = stage == StrategyFirstNightFaunaStage.StoryCompleted;
            fauna?.ResetForWorldRestore();
            fauna?.SetFirstNightStage(stage);
            story?.RestoreResolvedState(stage == StrategyFirstNightFaunaStage.StoryCompleted);
            if (stage == StrategyFirstNightFaunaStage.MiceVisible)
            {
                story?.PreloadArtwork();
            }

            StrategyDebugLogger.Info(
                "FirstNightFauna",
                "StageRestored",
                StrategyDebugLogger.F("stage", stage));
        }

        internal static bool HasReachedFirstDusk(StrategyCalendarSnapshot snapshot)
        {
            return snapshot.DayIndex > 0
                || snapshot.DayIndex == 0
                && (snapshot.Phase == StrategyTimeOfDayPhase.Dusk
                    || snapshot.Phase == StrategyTimeOfDayPhase.Night);
        }

        internal static bool HasReachedFirstNight(StrategyCalendarSnapshot snapshot)
        {
            return snapshot.DayIndex > 0
                || snapshot.DayIndex == 0
                && snapshot.Phase == StrategyTimeOfDayPhase.Night;
        }

        internal static bool ShouldRetainRatCinematicRunning(
            bool runningFlag,
            bool playerIsPlaying)
        {
            return runningFlag && playerIsPlaying;
        }

        private void Awake()
        {
            Active = this;
        }

        private void Update()
        {
            if (catHuntCinematicRunning
                && (cinematicPlayer == null || !cinematicPlayer.IsPlaying))
            {
                catHuntCinematicRunning = false;
            }

            if (!configured || stage == StrategyFirstNightFaunaStage.StoryCompleted)
            {
                return;
            }

            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            if (stage == StrategyFirstNightFaunaStage.Dormant && HasReachedFirstDusk(snapshot))
            {
                SetStage(StrategyFirstNightFaunaStage.MiceVisible);
                story.PreloadArtwork();
                StrategyEventLogHudController.Notify(
                    "A soft rustling has begun beneath the settlement's stores.",
                    StrategyDayNightCycleController.GetPhaseAccentColor(StrategyTimeOfDayPhase.Dusk));
            }

            if (stage != StrategyFirstNightFaunaStage.MiceVisible
                || !HasReachedFirstNight(snapshot))
            {
                return;
            }

            story.PreloadArtwork();
            if (fauna != null
                && fauna.LiveMouseCount < StrategySettlementFaunaPolicy.FirstNightMouseMinimum)
            {
                return;
            }

            if (!ratCinematicCompletedThisSession)
            {
                TryStartRatCinematic();
                return;
            }

            TryOpenStory();
        }

        private void TryStartRatCinematic()
        {
            if (ratCinematicRunning
                && !ShouldRetainRatCinematicRunning(
                    ratCinematicRunning,
                    cinematicPlayer != null && cinematicPlayer.IsPlaying))
            {
                ratCinematicRunning = false;
                StrategyDebugLogger.Warn("FirstNightFauna", "RatCinematicInterruptedRetrying");
            }

            if (ratCinematicRunning || cinematicPlayer != null && cinematicPlayer.IsPlaying)
            {
                return;
            }

            if (cinematicPlayer == null || ratCinematic == null)
            {
                ratCinematicCompletedThisSession = true;
                StrategyDebugLogger.Warn("FirstNightFauna", "RatCinematicUnavailable");
                TryOpenStory();
                return;
            }

            if (!cinematicPlayer.CanPlay)
            {
                return;
            }

            ratCinematicRunning = cinematicPlayer.TryPlay(
                ratCinematic,
                StrategyInGameCinematicOptions.Default,
                HandleRatCinematicCompleted);
            if (!ratCinematicRunning)
            {
                ratCinematicCompletedThisSession = true;
                StrategyDebugLogger.Warn("FirstNightFauna", "RatCinematicSkipped");
                TryOpenStory();
            }
        }

        private void HandleRatCinematicCompleted(StrategyInGameCinematicResult result)
        {
            ratCinematicRunning = false;
            if (result == StrategyInGameCinematicResult.Cancelled
                || stage != StrategyFirstNightFaunaStage.MiceVisible)
            {
                return;
            }

            ratCinematicCompletedThisSession = true;
            StrategyDebugLogger.Info(
                "FirstNightFauna",
                "RatCinematicCompleted",
                StrategyDebugLogger.F("result", result));
            TryOpenStory();
        }

        private bool TryOpenStory()
        {
            return story != null
                && story.CanOpenWithoutStacking
                && story.TryShow(HandleStoryCompleted);
        }

        private void HandleStoryCompleted()
        {
            if (stage == StrategyFirstNightFaunaStage.StoryCompleted)
            {
                return;
            }

            bool catsAdded = cityInventory != null
                && cityInventory.TryAdd(StrategyCityItemIds.Cats, 1);
            bool ownsCats = cityInventory != null
                && cityInventory.Contains(StrategyCityItemIds.Cats);
            if (!ownsCats)
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "CatsRewardGrantFailed");
            }

            SetStage(StrategyFirstNightFaunaStage.StoryCompleted);
            if (catsAdded)
            {
                TryShowCatsReward();
            }

            StrategyEventLogHudController.Notify(
                "The settlement has gained its first quiet hunter.",
                new Color(0.90f, 0.70f, 0.35f));
        }

        private void TryShowCatsReward()
        {
            if (rewardReveal == null
                || cityInventory == null
                || !cityInventory.Catalog.TryGet(
                    StrategyCityItemIds.Cats,
                    out StrategyCityItemDefinition definition))
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "CatsRewardRevealUnavailable");
                return;
            }

            Sprite artwork = string.IsNullOrWhiteSpace(definition.IconResourcePath)
                ? null
                : Resources.Load<Sprite>(definition.IconResourcePath);
            if (!rewardReveal.TryShow(
                    definition,
                    artwork,
                    HandleCatsRewardAccepted))
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "CatsRewardRevealRejected");
            }
        }

        private void HandleCatsRewardAccepted()
        {
            if (stage != StrategyFirstNightFaunaStage.StoryCompleted
                || cityInventory == null
                || !cityInventory.Contains(StrategyCityItemIds.Cats))
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "CatHuntCinematicEntitlementMissing");
                return;
            }

            if (cinematicPlayer == null
                || catHuntCinematic == null
                || !cinematicPlayer.CanPlay)
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "CatHuntCinematicUnavailable");
                return;
            }

            catHuntCinematicRunning = cinematicPlayer.TryPlay(
                catHuntCinematic,
                StrategyInGameCinematicOptions.Default,
                HandleCatHuntCinematicCompleted);
            if (!catHuntCinematicRunning)
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "CatHuntCinematicSkipped");
            }
        }

        private void HandleCatHuntCinematicCompleted(StrategyInGameCinematicResult result)
        {
            StrategyDebugLogger.Info(
                "FirstNightFauna",
                "CatHuntCinematicCompleted",
                StrategyDebugLogger.F("result", result));
        }

        private void BackfillCatsEntitlementIfNeeded()
        {
            if (stage != StrategyFirstNightFaunaStage.StoryCompleted
                || cityInventory == null
                || cityInventory.Contains(StrategyCityItemIds.Cats))
            {
                return;
            }

            if (cityInventory.TryAdd(StrategyCityItemIds.Cats, 1))
            {
                StrategyDebugLogger.Info("FirstNightFauna", "CatsEntitlementBackfilled");
            }
            else
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "CatsEntitlementBackfillFailed");
            }
        }

        private void SetStage(StrategyFirstNightFaunaStage nextStage)
        {
            if (stage == nextStage)
            {
                fauna?.SetFirstNightStage(stage);
                return;
            }

            StrategyFirstNightFaunaStage previous = stage;
            stage = nextStage;
            fauna?.SetFirstNightStage(stage);
            StrategyDebugLogger.Info(
                "FirstNightFauna",
                "StageChanged",
                StrategyDebugLogger.F("previous", previous),
                StrategyDebugLogger.F("current", stage));
        }

        private static bool IsValidStage(StrategyFirstNightFaunaStage value)
        {
            return value >= StrategyFirstNightFaunaStage.Dormant
                && value <= StrategyFirstNightFaunaStage.StoryCompleted;
        }

        private void CancelRatCinematic()
        {
            if (cinematicPlayer != null && ratCinematic != null)
            {
                cinematicPlayer.Cancel(ratCinematic, false);
            }

            ratCinematicRunning = false;
        }

        private void CancelCatHuntCinematic()
        {
            if (cinematicPlayer != null && catHuntCinematic != null)
            {
                cinematicPlayer.Cancel(catHuntCinematic, false);
            }

            catHuntCinematicRunning = false;
        }

        private void OnDisable()
        {
            CancelCatHuntCinematic();
            CancelRatCinematic();
        }

        private void OnDestroy()
        {
            CancelCatHuntCinematic();
            CancelRatCinematic();
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
