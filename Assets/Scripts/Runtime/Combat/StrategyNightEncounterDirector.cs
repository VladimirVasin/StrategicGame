using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyNightEncounterDirector : MonoBehaviour
    {
        private readonly StrategyNightEncounterSchedule schedule = new();

        private StrategyCombatEncounterController encounter;
        private StrategyBattleLifecycleController battleLifecycle;
        private StrategyDayNightCycleController dayNight;
        private StrategyTimeScaleController timeScale;
        private StrategyInputRouter inputRouter;

        public bool IsConfigured { get; private set; }
        public bool HasGuaranteedNightEncounterPending =>
            schedule.HasGuaranteedPending;
        public bool HasPendingNightEncounter => schedule.HasPending;

        public void Configure(
            StrategyCombatEncounterController encounterController,
            StrategyBattleLifecycleController lifecycleController,
            StrategyDayNightCycleController dayNightController,
            StrategyTimeScaleController timeScaleController,
            StrategyInputRouter router)
        {
            encounter = encounterController;
            battleLifecycle = lifecycleController;
            dayNight = dayNightController;
            timeScale = timeScaleController;
            inputRouter = router;
            IsConfigured = encounter != null
                && battleLifecycle != null
                && dayNight != null
                && timeScale != null
                && inputRouter != null;
            schedule.Initialize(
                StrategyDayNightCycleController.CurrentCalendarSnapshot);
        }

        public bool ArmGuaranteedNightEncounter(bool moveToNightApproach = true)
        {
            if (!IsConfigured
                || encounter.IsRunning
                || battleLifecycle.IsBattleInProgress)
            {
                return false;
            }

            schedule.ArmGuaranteed();
            StrategyCalendarSnapshot snapshot =
                StrategyDayNightCycleController.CurrentCalendarSnapshot;
            bool movedClock = moveToNightApproach
                && !snapshot.IsNight
                && dayNight.DebugMoveToNightApproach();
            StrategyDebugLogger.Info(
                "Combat",
                "NightEncounterArmed",
                StrategyDebugLogger.F("day", snapshot.DisplayDay),
                StrategyDebugLogger.F("clock", snapshot.ClockText),
                StrategyDebugLogger.F("movedClock", movedClock));
            return true;
        }

        public void CancelPendingEncounter()
        {
            schedule.CancelPending();
        }

        public void ResetEncounter()
        {
            schedule.CancelPending();
            encounter?.ResetEncounter();
        }

        private void Update()
        {
            if (!IsConfigured)
            {
                return;
            }

            StrategyCalendarSnapshot snapshot =
                StrategyDayNightCycleController.CurrentCalendarSnapshot;
            bool battleBusy = encounter.IsRunning
                || battleLifecycle.IsBattleInProgress;
            schedule.Observe(snapshot, battleBusy);
            bool blocked = IsSimulationBlocked(snapshot);
            if (!schedule.CanAttempt(snapshot, blocked, battleBusy, Time.time))
            {
                return;
            }

            bool started = encounter.TryStartEncounter();
            schedule.RecordAttempt(started, Time.time);
            if (started)
            {
                StrategyEventLogHudController.Notify(
                    StrategyLocalization.Get(
                        StrategyLocalizationTables.Hud,
                        "hud.event.combat.night_attack"),
                    new Color(0.92f, 0.36f, 0.25f));
            }

            StrategyDebugLogger.Info(
                "Combat",
                started ? "NightEncounterStarted" : "NightEncounterStartDeferred",
                StrategyDebugLogger.F("day", snapshot.DisplayDay),
                StrategyDebugLogger.F("clock", snapshot.ClockText),
                StrategyDebugLogger.F("status", encounter.Status.Kind));
        }

        private bool IsSimulationBlocked(StrategyCalendarSnapshot snapshot)
        {
            if (timeScale.IsPausedByLock
                || Time.timeScale <= 0f
                || !inputRouter.IsAvailable
                || (inputRouter.BlockedChannels & StrategyInputChannel.Gameplay) != 0)
            {
                return true;
            }

            StrategyFirstNightFaunaEventController firstNight =
                StrategyFirstNightFaunaEventController.Active;
            if (firstNight == null)
            {
                return false;
            }

            return IsFirstNightSequenceBlocking(
                snapshot,
                firstNight.Stage,
                firstNight.IsRatCinematicPlaying,
                firstNight.IsCatHuntCinematicPlaying);
        }

        internal static bool IsFirstNightSequenceBlocking(
            StrategyCalendarSnapshot snapshot,
            StrategyFirstNightFaunaStage stage,
            bool ratCinematicPlaying,
            bool catHuntCinematicPlaying)
        {
            bool unresolvedFirstNight =
                stage != StrategyFirstNightFaunaStage.StoryCompleted
                && StrategyFirstNightFaunaEventController.HasReachedFirstNight(snapshot);
            return unresolvedFirstNight
                || ratCinematicPlaying
                || catHuntCinematicPlaying;
        }
    }

    internal sealed class StrategyNightEncounterSchedule
    {
        internal const int FirstAutomaticDayIndex = 1;
        internal const float RetryDelaySeconds = 1f;

        private bool initialized;
        private bool automaticPending;
        private bool guaranteedPending;
        private int observedDayIndex;
        private int lastAutomaticNightDayIndex = -1;
        private StrategyTimeOfDayPhase observedPhase;
        private float nextAttemptTime;

        internal bool HasAutomaticPending => automaticPending;
        internal bool HasGuaranteedPending => guaranteedPending;
        internal bool HasPending => automaticPending || guaranteedPending;

        internal void Initialize(StrategyCalendarSnapshot snapshot)
        {
            initialized = true;
            automaticPending = false;
            guaranteedPending = false;
            observedDayIndex = snapshot.DayIndex;
            observedPhase = snapshot.Phase;
            lastAutomaticNightDayIndex = snapshot.IsNight
                ? snapshot.DayIndex
                : -1;
            nextAttemptTime = 0f;
        }

        internal void ArmGuaranteed()
        {
            guaranteedPending = true;
            nextAttemptTime = 0f;
        }

        internal void CancelPending()
        {
            automaticPending = false;
            guaranteedPending = false;
            nextAttemptTime = 0f;
        }

        internal void Observe(
            StrategyCalendarSnapshot snapshot,
            bool battleBusyAtTransition)
        {
            if (!initialized)
            {
                Initialize(snapshot);
                return;
            }

            if (!snapshot.IsNight)
            {
                automaticPending = false;
            }

            bool enteredNewNight = snapshot.IsNight
                && (observedPhase != StrategyTimeOfDayPhase.Night
                    || observedDayIndex != snapshot.DayIndex);
            if (enteredNewNight
                && lastAutomaticNightDayIndex != snapshot.DayIndex)
            {
                lastAutomaticNightDayIndex = snapshot.DayIndex;
                automaticPending = snapshot.DayIndex >= FirstAutomaticDayIndex
                    && !battleBusyAtTransition;
                nextAttemptTime = 0f;
            }

            observedDayIndex = snapshot.DayIndex;
            observedPhase = snapshot.Phase;
        }

        internal bool CanAttempt(
            StrategyCalendarSnapshot snapshot,
            bool blocked,
            bool battleBusy,
            float currentTime)
        {
            return snapshot.IsNight
                && HasPending
                && !blocked
                && !battleBusy
                && !float.IsNaN(currentTime)
                && currentTime >= nextAttemptTime;
        }

        internal void RecordAttempt(bool succeeded, float currentTime)
        {
            if (!HasPending)
            {
                return;
            }

            if (succeeded)
            {
                automaticPending = false;
                guaranteedPending = false;
                nextAttemptTime = 0f;
                return;
            }

            float normalizedTime = float.IsNaN(currentTime)
                ? 0f
                : Mathf.Max(0f, currentTime);
            nextAttemptTime = normalizedTime + RetryDelaySeconds;
        }
    }
}
