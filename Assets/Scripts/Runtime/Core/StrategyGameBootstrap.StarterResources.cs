namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static float CalculateStarterFoodRations(StrategyPopulationController population, float reserveDays)
        {
            if (population == null || reserveDays <= 0f)
            { return 0f; }

            float dailyNeed = 0f;
            foreach (StrategyResidentAgent resident in population.Residents)
            {
                if (resident != null && !resident.IsPendingRefugee)
                { dailyNeed += resident.DailyRationNeed; }
            }

            return dailyNeed * reserveDays;
        }
    }
}
