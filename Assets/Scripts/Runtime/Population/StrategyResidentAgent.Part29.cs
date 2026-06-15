using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        public void ClearWorkplace(StrategyLumberjackCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && workplace != camp)
            {
                return;
            }

            StrategyLumberjackCamp previousWorkplace = workplace;
            workplace = null;
            CancelLumberWork();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void ClearStoneWorkplace(StrategyStonecutterCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && stoneWorkplace != camp)
            {
                return;
            }

            StrategyStonecutterCamp previousWorkplace = stoneWorkplace;
            stoneWorkplace = null;
            CancelStoneWork();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStoneWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void ClearHunterWorkplace(StrategyHunterCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && hunterWorkplace != camp)
            {
                return;
            }

            StrategyHunterCamp previousWorkplace = hunterWorkplace;
            CancelHunterWork(true);
            hunterWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHunterWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }
    }
}
