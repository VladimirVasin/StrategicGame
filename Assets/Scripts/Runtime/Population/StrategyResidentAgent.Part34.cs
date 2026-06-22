using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void DropCarriedResourcesOnDeath()
        {
            CaptureCarriedConstructionReturnReservation();
            int droppedLogs = carriedLogAmount;
            int droppedStone = carriedStoneAmount;
            int droppedIron = carriedIronAmount;
            int droppedCoal = carriedCoalAmount;
            int droppedClay = carriedClayAmount;
            int droppedPlanks = carriedPlanksAmount;
            int droppedPottery = carriedPotteryAmount;
            int droppedTools = carriedToolsAmount;
            int droppedGame = carriedGameAmount;
            int droppedFish = carriedFishAmount;
            int droppedForage = carriedForageAmount;
            StrategyResourceType droppedForageResource = carriedForageResource;
            if (droppedLogs <= 0
                && droppedStone <= 0
                && droppedIron <= 0
                && droppedCoal <= 0
                && droppedClay <= 0
                && droppedPlanks <= 0
                && droppedPottery <= 0
                && droppedTools <= 0
                && droppedGame <= 0
                && droppedFish <= 0
                && droppedForage <= 0)
            {
                ClearCarriedConstructionReturnReservation();
                return;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                DropCarriedResourcesAtDeathCell(
                    cell,
                    droppedLogs,
                    droppedStone,
                    droppedIron,
                    droppedCoal,
                    droppedClay,
                    ref droppedPlanks,
                    droppedPottery,
                    droppedTools,
                    droppedGame,
                    droppedFish,
                    droppedForageResource,
                    droppedForage);
            }
            else
            {
                LogCarriedResourcesLostOnDeath(
                    droppedLogs,
                    droppedStone,
                    droppedIron,
                    droppedCoal,
                    droppedClay,
                    droppedPlanks,
                    droppedPottery,
                    droppedTools,
                    droppedGame,
                    droppedFish,
                    droppedForageResource,
                    droppedForage);
            }

            ClearCarriedResourcesAfterDeathDrop();
        }

        private void DropCarriedResourcesAtDeathCell(
            Vector2Int cell,
            int droppedLogs,
            int droppedStone,
            int droppedIron,
            int droppedCoal,
            int droppedClay,
            ref int droppedPlanks,
            int droppedPottery,
            int droppedTools,
            int droppedGame,
            int droppedFish,
            StrategyResourceType droppedForageResource,
            int droppedForage)
        {
            bool droppedConstructionPlanks = droppedPlanks > 0
                && (carriedConstructionReturnResource == StrategyConstructionResourceKind.Planks
                    || activeConstructionResource == StrategyConstructionResourceKind.Planks
                    || IsConstructionActivity(activity));
            if (droppedLogs > 0 || droppedStone > 0 || droppedConstructionPlanks)
            {
                StrategyLooseConstructionResourcePile pile = StrategyLooseConstructionResourcePile.Create(
                    map,
                    cell,
                    transform.position,
                    droppedLogs,
                    droppedStone,
                    droppedConstructionPlanks ? droppedPlanks : 0);
                string reservationState = RestoreDeathDroppedConstructionReservation(
                    pile,
                    droppedLogs,
                    droppedStone,
                    droppedConstructionPlanks ? droppedPlanks : 0);
                StrategyDebugLogger.Warn(
                    "Construction",
                    "CarriedConstructionResourcesDroppedOnDeath",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("origin", cell),
                    StrategyDebugLogger.F("logs", droppedLogs),
                    StrategyDebugLogger.F("stone", droppedStone),
                    StrategyDebugLogger.F("planks", droppedConstructionPlanks ? droppedPlanks : 0),
                    StrategyDebugLogger.F("reservation", reservationState));
                if (droppedConstructionPlanks)
                {
                    droppedPlanks = 0;
                }
            }

            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Game, droppedGame);
            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Fish, droppedFish);
            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Iron, droppedIron);
            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Coal, droppedCoal);
            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Clay, droppedClay);
            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Planks, droppedPlanks);
            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Pottery, droppedPottery);
            DropLooseCarriedResourceOnDeath(cell, StrategyResourceType.Tools, droppedTools);
            DropLooseCarriedResourceOnDeath(cell, droppedForageResource, droppedForage);
        }

        private void LogCarriedResourcesLostOnDeath(
            int droppedLogs,
            int droppedStone,
            int droppedIron,
            int droppedCoal,
            int droppedClay,
            int droppedPlanks,
            int droppedPottery,
            int droppedTools,
            int droppedGame,
            int droppedFish,
            StrategyResourceType droppedForageResource,
            int droppedForage)
        {
            StrategyDebugLogger.Warn(
                "Logistics",
                "CarriedResourcesLostOnDeath",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("logs", droppedLogs),
                StrategyDebugLogger.F("stone", droppedStone),
                StrategyDebugLogger.F("iron", droppedIron),
                StrategyDebugLogger.F("coal", droppedCoal),
                StrategyDebugLogger.F("clay", droppedClay),
                StrategyDebugLogger.F("planks", droppedPlanks),
                StrategyDebugLogger.F("pottery", droppedPottery),
                StrategyDebugLogger.F("tools", droppedTools),
                StrategyDebugLogger.F("game", droppedGame),
                StrategyDebugLogger.F("fish", droppedFish),
                StrategyDebugLogger.F("forageResource", droppedForageResource),
                StrategyDebugLogger.F("forage", droppedForage),
                StrategyDebugLogger.F("reason", "no_map_cell"));
        }

        private void ClearCarriedResourcesAfterDeathDrop()
        {
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedIronAmount = 0;
            carriedCoalAmount = 0;
            carriedClayAmount = 0;
            carriedPlanksAmount = 0;
            carriedPotteryAmount = 0;
            carriedToolsAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            ClearCarriedConstructionReturnReservation();
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedIronVisible(false);
            SetCarriedCoalVisible(false);
            SetCarriedClayVisible(false);
            SetCarriedPlanksVisible(false);
            SetCarriedPotteryVisible(false);
            SetCarriedToolsVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
        }

        private string RestoreDeathDroppedConstructionReservation(
            StrategyLooseConstructionResourcePile pile,
            int logs,
            int stone,
            int planks)
        {
            if (pile == null || carriedConstructionReturnResource == StrategyConstructionResourceKind.None)
            {
                return "none";
            }

            int amount = carriedConstructionReturnResource switch
            {
                StrategyConstructionResourceKind.Logs => logs,
                StrategyConstructionResourceKind.Stone => stone,
                StrategyConstructionResourceKind.Planks => planks,
                _ => 0
            };
            if (amount <= 0)
            {
                return "none";
            }

            int reservedAmount = GetRestorableCarriedConstructionReservationAmount(
                carriedConstructionReturnResource,
                amount,
                out StrategyConstructionSite site);
            if (reservedAmount <= 0)
            {
                return "unrestored";
            }

            return pile.TryRestoreConstructionReservation(site, carriedConstructionReturnResource, reservedAmount)
                ? "restored"
                : "unrestored";
        }
    }
}
