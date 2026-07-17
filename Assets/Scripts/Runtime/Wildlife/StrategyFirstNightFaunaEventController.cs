using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFirstNightFaunaEventController : MonoBehaviour
    {
        private StrategySettlementFaunaController fauna;
        private StrategyFirstNightFaunaStoryController story;
        private StrategyFirstNightFaunaStage stage = StrategyFirstNightFaunaStage.Dormant;
        private bool configured;

        public static StrategyFirstNightFaunaEventController Active { get; private set; }
        public StrategyFirstNightFaunaStage Stage => stage;
        public bool IsStoryPending => configured
            && stage == StrategyFirstNightFaunaStage.MiceVisible
            && HasReachedFirstNight(StrategyDayNightCycleController.CurrentCalendarSnapshot);

        public void Configure(
            StrategySettlementFaunaController faunaController,
            StrategyFirstNightFaunaStoryController storyController)
        {
            fauna = faunaController;
            story = storyController;
            configured = fauna != null && story != null;
            stage = fauna != null ? fauna.Stage : StrategyFirstNightFaunaStage.Dormant;
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
            stage = IsValidStage(restoredStage)
                ? restoredStage
                : StrategyFirstNightFaunaStage.Dormant;
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

        private void Awake()
        {
            Active = this;
        }

        private void Update()
        {
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
            if (story.CanOpenWithoutStacking)
            {
                story.TryShow(HandleStoryCompleted);
            }
        }

        private void HandleStoryCompleted()
        {
            if (stage == StrategyFirstNightFaunaStage.StoryCompleted)
            {
                return;
            }

            SetStage(StrategyFirstNightFaunaStage.StoryCompleted);
            StrategyEventLogHudController.Notify(
                "The settlement has gained its first quiet hunter.",
                new Color(0.90f, 0.70f, 0.35f));
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

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
