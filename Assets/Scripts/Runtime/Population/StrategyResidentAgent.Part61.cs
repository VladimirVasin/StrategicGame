using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const int HouseholdFoodChildHelperMinDisplayAgeYears = 6;

        private bool CanStartHouseholdFoodPickupAsHomeCarrier()
        {
            if (IsHouseholder)
            {
                return constructionSite == null
                    && CanWork
                    && !HasExternalWorkplace;
            }

            return CanStartOlderChildHouseholdFoodHelp();
        }

        private bool CanStartOlderChildHouseholdFoodHelp()
        {
            return lifeStage == StrategyResidentLifeStage.Child
                && DisplayAgeYears >= HouseholdFoodChildHelperMinDisplayAgeYears
                && home != null
                && !deathRequested
                && !IsPendingRefugee
                && !hiddenInsideHome
                && !hiddenUnderground
                && !sleepingInsideHome
                && !returningHomeToSleep
                && !IsFuneralDutyActive
                && constructionSite == null
                && !HasExternalWorkplace
                && !HasAnyCarriedResource()
                && childPlayKind == ChildPlayKind.None
                && childPlayPartner == null;
        }

        private bool ShouldPrioritizeHouseholdFoodHelpOverChildPlay()
        {
            if (!CanStartOlderChildHouseholdFoodHelp()
                || householdFoodWorkCooldown > 0f
                || !StrategyDayNightCycleController.IsHouseholdOutdoorWorkTime
                || home == null
                || home.Resources == null)
            {
                return false;
            }

            float dailyNeed = CalculateHomeDailyRationNeed();
            if (dailyNeed <= 0f)
            {
                return false;
            }

            float homeRations = home.Resources.GetPreparedDishRations() + home.Resources.GetTotalIngredientRationValue();
            float desiredReserve = Mathf.Max(1f, dailyNeed * HouseholdFoodReserveDays);
            return homeRations < desiredReserve;
        }
    }
}
