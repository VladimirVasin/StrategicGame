namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDayNightCycleController
    {
        private StrategyTimeOfDayPhase announcedPhase = (StrategyTimeOfDayPhase)(-1);
        private int announcedPhaseDayIndex = -1;
        private int announcedSeasonStartDayIndex = -1;
        private int announcedWinterWarningDayIndex = -1;

        private void AnnouncePlayerFacingPhase(StrategyCalendarSnapshot snapshot)
        {
            AnnounceSeasonStart(snapshot);
            AnnounceWinterWarning(snapshot);
            AnnounceTimeOfDayPhase(snapshot);
        }

        private void AnnounceSeasonStart(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.SeasonDay != 1 || announcedSeasonStartDayIndex == snapshot.DayIndex)
            {
                return;
            }

            announcedSeasonStartDayIndex = snapshot.DayIndex;
            StrategyEventLogHudController.Notify(
                snapshot.SeasonLabel + " begins, Year " + snapshot.Year,
                StrategySeasonCalendar.GetSeasonAccentColor(snapshot.Season));
        }

        private void AnnounceWinterWarning(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.Phase != StrategyTimeOfDayPhase.Dawn
                || snapshot.Season == StrategySeason.Winter
                || announcedWinterWarningDayIndex == snapshot.DayIndex)
            {
                return;
            }

            int daysUntilWinter = StrategySeasonCalendar.GetDaysUntilSeason(snapshot.DayIndex, StrategySeason.Winter);
            if (daysUntilWinter < 1 || daysUntilWinter > 3)
            {
                return;
            }

            announcedWinterWarningDayIndex = snapshot.DayIndex;
            StrategyEventLogHudController.Notify(
                "Winter in " + daysUntilWinter + " days - check food and Logs",
                StrategySeasonCalendar.GetSeasonAccentColor(StrategySeason.Winter));
        }

        private void AnnounceTimeOfDayPhase(StrategyCalendarSnapshot snapshot)
        {
            bool keyPhase = snapshot.Phase == StrategyTimeOfDayPhase.Dawn
                || snapshot.Phase == StrategyTimeOfDayPhase.Night;
            if (!keyPhase)
            {
                return;
            }

            bool alreadyAnnounced = announcedPhase == snapshot.Phase
                && announcedPhaseDayIndex == snapshot.DayIndex;
            if (alreadyAnnounced)
            {
                return;
            }

            announcedPhase = snapshot.Phase;
            announcedPhaseDayIndex = snapshot.DayIndex;
            string label = snapshot.Phase == StrategyTimeOfDayPhase.Night ? "Nightfall" : "Dawn";
            StrategyEventLogHudController.Notify(
                label + ", Day " + snapshot.DisplayDay,
                GetPhaseAccentColor(snapshot.Phase));
        }
    }
}
