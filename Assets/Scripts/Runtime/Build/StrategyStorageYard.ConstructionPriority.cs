using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private void ClearNonConstructionReservationsForConstruction(StrategyConstructionResourceKind kind)
        {
            if (kind == StrategyConstructionResourceKind.Logs)
            {
                logisticsLogsReservationOwner = null;
                reservedLogisticsLogs = 0;
                ClearProductionInputReservationsForConstruction(StrategyResourceType.Logs);
            }
            else if (kind == StrategyConstructionResourceKind.Stone)
            {
                ClearProductionInputReservationsForConstruction(StrategyResourceType.Stone);
            }
            else if (kind == StrategyConstructionResourceKind.Planks)
            {
                ClearProductionInputReservationsForConstruction(StrategyResourceType.Planks);
            }
        }

        private void ClearProductionInputReservationsForConstruction(StrategyResourceType resource)
        {
            Dictionary<object, int> reservations = GetProductionInputReservations(resource, false);
            reservations?.Clear();
        }
    }
}
