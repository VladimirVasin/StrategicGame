namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        public float GetTotalDailyRationNeed()
        {
            float total = 0f;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && !resident.IsPendingRefugee)
                {
                    total += resident.DailyRationNeed;
                }
            }

            return total;
        }

        public float GetTotalHouseholdStoredRations()
        {
            float total = 0f;
            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house != null && house.Resources != null)
                {
                    total += house.Resources.GetTotalRationValue();
                }
            }

            return total;
        }

        public int GetTotalHouseholdStoredLogs()
        {
            int total = 0;
            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house != null && house.Resources != null)
                {
                    total += house.Resources.GetLogsAmount();
                }
            }

            return total;
        }

        public int GetOccupiedHouseCount()
        {
            int total = 0;
            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house != null && house.ResidentCount > 0)
                {
                    total++;
                }
            }

            return total;
        }
    }
}
