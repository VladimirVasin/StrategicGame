using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private const int ProductionInputDeliveryStackLimit = StrategyProductionStorage.HaulerCarryLimit;
        private static readonly List<IStrategyProductionLogisticsNode> productionNodeQuery = new();
        private static Vector3 productionNodeSortWorld;

        public bool TryReserveProductionInputDelivery(
            object owner,
            out IStrategyProductionLogisticsNode target,
            out StrategyResourceType resource,
            out int amount)
        {
            target = null;
            resource = StrategyResourceType.None;
            amount = 0;
            if (owner == null)
            {
                return false;
            }

            List<IStrategyProductionLogisticsNode> nodes = GetProductionNodesSortedByDistance(FootprintBounds.center);
            for (int i = 0; i < nodes.Count; i++)
            {
                IStrategyProductionLogisticsNode node = nodes[i];
                if (node == null
                    || !node.TryGetInputDeliveryRequest(out StrategyResourceType requestedResource, out int maxAmount)
                    || requestedResource == StrategyResourceType.None
                    || StrategyFoodNutrition.IsFood(requestedResource))
                {
                    continue;
                }

                int available = GetAvailableLogisticsAmount(requestedResource);
                int requestedAmount = Mathf.Min(maxAmount, available, ProductionInputDeliveryStackLimit);
                if (requestedAmount <= 0
                    || !node.TryReserveInputDelivery(requestedResource, owner, requestedAmount, out int reservedByNode))
                {
                    continue;
                }

                int reservedByYard = ReserveProductionInputResource(requestedResource, owner, reservedByNode);
                if (reservedByYard != reservedByNode)
                {
                    node.ReleaseInputDeliveryReservation(requestedResource, owner);
                    ReleaseProductionInputReservation(owner, requestedResource);
                    continue;
                }

                target = node;
                resource = requestedResource;
                amount = reservedByYard;
                StrategyDebugLogger.Info(
                    "Logistics",
                    "ProductionInputPickupReserved",
                    StrategyDebugLogger.F("yardOrigin", Origin),
                    StrategyDebugLogger.F("targetOrigin", node.Origin),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount));
                return true;
            }

            return false;
        }

        public bool TryTakeReservedProductionInput(object owner, StrategyResourceType resource, out int amount)
        {
            amount = 0;
            Dictionary<object, int> reservations = GetProductionInputReservations(resource, false);
            if (owner == null
                || reservations == null
                || !reservations.TryGetValue(owner, out int reserved)
                || reserved <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reserved, GetStoredAmount(resource));
            reservations.Remove(owner);
            if (amount <= 0)
            {
                return false;
            }

            SpendLogisticsAmount(resource, amount);
            StrategyDebugLogger.Info(
                "Logistics",
                "ProductionInputTaken",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount));
            return true;
        }

        public void ReleaseProductionInputReservation(object owner, StrategyResourceType resource)
        {
            Dictionary<object, int> reservations = GetProductionInputReservations(resource, false);
            if (owner != null && reservations != null)
            {
                reservations.Remove(owner);
            }
        }

        public void ReleaseProductionInputReservation(object owner)
        {
            if (owner == null)
            {
                return;
            }

            foreach (KeyValuePair<StrategyResourceType, Dictionary<object, int>> pair in productionInputReservations)
            {
                pair.Value?.Remove(owner);
            }
        }

        public static int CountProductionInputBacklog(out Vector3 focus)
        {
            int backlog = 0;
            Vector3 weighted = Vector3.zero;
            List<StrategyStorageYard> yards = GetActiveYards();
            List<IStrategyProductionLogisticsNode> nodes = GetProductionNodesSortedByDistance(Vector3.zero);
            for (int i = 0; i < nodes.Count; i++)
            {
                IStrategyProductionLogisticsNode node = nodes[i];
                if (node == null
                    || !node.TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount)
                    || resource == StrategyResourceType.None
                    || StrategyFoodNutrition.IsFood(resource)
                    || maxAmount <= 0)
                {
                    continue;
                }

                int available = 0;
                for (int j = 0; j < yards.Count; j++)
                {
                    available += yards[j] != null ? yards[j].GetAvailableLogisticsAmount(resource) : 0;
                }

                int amount = Mathf.Min(maxAmount, available);
                if (amount <= 0)
                {
                    continue;
                }

                backlog += amount;
                weighted += node.FootprintBounds.center * amount;
            }

            focus = backlog > 0 ? weighted / backlog : Vector3.zero;
            return backlog;
        }

        private int ReserveProductionInputResource(StrategyResourceType resource, object owner, int requested)
        {
            if (owner == null || requested <= 0)
            {
                return 0;
            }

            Dictionary<object, int> reservations = GetProductionInputReservations(resource, true);
            if (reservations == null)
            {
                return 0;
            }

            if (reservations.TryGetValue(owner, out int existing) && existing > 0)
            {
                return existing;
            }

            int amount = Mathf.Min(requested, GetAvailableLogisticsAmount(resource));
            if (amount <= 0)
            {
                return 0;
            }

            reservations[owner] = amount;
            return amount;
        }

        private int CountProductionInputReservations(StrategyResourceType resource)
        {
            Dictionary<object, int> reservations = GetProductionInputReservations(resource, false);
            return reservations == null ? 0 : CountReservations(reservations);
        }

        private Dictionary<object, int> GetProductionInputReservations(StrategyResourceType resource, bool create)
        {
            if (resource == StrategyResourceType.None || StrategyFoodNutrition.IsFood(resource))
            {
                return null;
            }

            if (!productionInputReservations.TryGetValue(resource, out Dictionary<object, int> reservations) && create)
            {
                reservations = new Dictionary<object, int>();
                productionInputReservations[resource] = reservations;
            }

            return reservations;
        }

        public int GetAvailableLogisticsAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Logs => AvailableLogisticsLogs,
                StrategyResourceType.Stone => AvailableLogisticsStone,
                StrategyResourceType.Iron => Mathf.Max(0, ironStored - CountProductionInputReservations(resource)),
                StrategyResourceType.Coal => Mathf.Max(0, coalStored - CountProductionInputReservations(resource)),
                StrategyResourceType.Clay => Mathf.Max(0, clayStored - CountProductionInputReservations(resource)),
                StrategyResourceType.Pottery => Mathf.Max(0, potteryStored - CountProductionInputReservations(resource) - CountHouseholdPotteryReservations()),
                StrategyResourceType.Planks => AvailableLogisticsPlanks,
                StrategyResourceType.Tools => Mathf.Max(0, toolsStored - CountProductionInputReservations(resource)),
                _ => 0
            };
        }

        public static int GetTotalAvailableLogisticsAmount(StrategyResourceType resource)
        {
            int total = 0;
            List<StrategyStorageYard> yards = GetActiveYards();
            for (int i = 0; i < yards.Count; i++)
            {
                total += yards[i] != null ? yards[i].GetAvailableLogisticsAmount(resource) : 0;
            }

            return total;
        }

        public static bool CanAffordProductionUpgrade(StrategyProductionUpgradeCost cost)
        {
            return cost.CanAfford(
                GetTotalAvailableLogisticsAmount(StrategyResourceType.Tools),
                GetTotalAvailableLogisticsAmount(StrategyResourceType.Planks),
                GetTotalAvailableLogisticsAmount(StrategyResourceType.Stone));
        }

        public static bool TrySpendProductionUpgradeResources(
            StrategyProductionUpgradeCost cost,
            Vector3 nearWorld,
            string reason)
        {
            if (cost.IsFree)
            {
                return true;
            }

            if (!CanAffordProductionUpgrade(cost))
            {
                StrategyDebugLogger.Warn(
                    "StorageYard",
                    "ProductionUpgradeSpendRejected",
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("costTools", cost.Tools),
                    StrategyDebugLogger.F("costPlanks", cost.Planks),
                    StrategyDebugLogger.F("costStone", cost.Stone),
                    StrategyDebugLogger.F("availableTools", GetTotalAvailableLogisticsAmount(StrategyResourceType.Tools)),
                    StrategyDebugLogger.F("availablePlanks", GetTotalAvailableLogisticsAmount(StrategyResourceType.Planks)),
                    StrategyDebugLogger.F("availableStone", GetTotalAvailableLogisticsAmount(StrategyResourceType.Stone)));
                return false;
            }

            List<StrategyStorageYard> yards = GetYardsSortedByDistance(nearWorld);
            int remainingTools = SpendLogisticsFromYards(yards, StrategyResourceType.Tools, cost.Tools);
            int remainingPlanks = SpendLogisticsFromYards(yards, StrategyResourceType.Planks, cost.Planks);
            int remainingStone = SpendLogisticsFromYards(yards, StrategyResourceType.Stone, cost.Stone);
            bool spent = remainingTools <= 0 && remainingPlanks <= 0 && remainingStone <= 0;
            StrategyDebugLogger.Info(
                "StorageYard",
                spent ? "ProductionUpgradeResourcesSpent" : "ProductionUpgradeSpendShort",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("tools", cost.Tools),
                StrategyDebugLogger.F("planks", cost.Planks),
                StrategyDebugLogger.F("stone", cost.Stone),
                StrategyDebugLogger.F("remainingTools", remainingTools),
                StrategyDebugLogger.F("remainingPlanks", remainingPlanks),
                StrategyDebugLogger.F("remainingStone", remainingStone));
            return spent;
        }

        private static int SpendLogisticsFromYards(
            IReadOnlyList<StrategyStorageYard> yards,
            StrategyResourceType resource,
            int requested)
        {
            int remaining = requested;
            for (int i = 0; i < yards.Count && remaining > 0; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard == null)
                {
                    continue;
                }

                int amount = Mathf.Min(remaining, yard.GetAvailableLogisticsAmount(resource));
                if (amount <= 0)
                {
                    continue;
                }

                yard.SpendLogisticsAmount(resource, amount);
                remaining -= amount;
            }

            return remaining;
        }

        private int GetStoredAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Logs => logsStored,
                StrategyResourceType.Stone => stoneStored,
                StrategyResourceType.Iron => ironStored,
                StrategyResourceType.Coal => coalStored,
                StrategyResourceType.Clay => clayStored,
                StrategyResourceType.Pottery => potteryStored,
                StrategyResourceType.Planks => planksStored,
                StrategyResourceType.Tools => toolsStored,
                _ => 0
            };
        }

        private void SpendLogisticsAmount(StrategyResourceType resource, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (resource == StrategyResourceType.Logs)
            {
                logsStored = Mathf.Max(0, logsStored - amount);
            }
            else if (resource == StrategyResourceType.Stone)
            {
                stoneStored = Mathf.Max(0, stoneStored - amount);
            }
            else if (resource == StrategyResourceType.Iron)
            {
                ironStored = Mathf.Max(0, ironStored - amount);
            }
            else if (resource == StrategyResourceType.Coal)
            {
                coalStored = Mathf.Max(0, coalStored - amount);
            }
            else if (resource == StrategyResourceType.Clay)
            {
                clayStored = Mathf.Max(0, clayStored - amount);
            }
            else if (resource == StrategyResourceType.Pottery)
            {
                potteryStored = Mathf.Max(0, potteryStored - amount);
            }
            else if (resource == StrategyResourceType.Planks)
            {
                planksStored = Mathf.Max(0, planksStored - amount);
            }
            else if (resource == StrategyResourceType.Tools)
            {
                toolsStored = Mathf.Max(0, toolsStored - amount);
            }

            UpdateStockVisual();
        }

        private static List<IStrategyProductionLogisticsNode> GetProductionNodesSortedByDistance(Vector3 nearWorld)
        {
            productionNodeQuery.Clear();
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null)
                {
                    continue;
                }

                if (building.TryGetComponent(out StrategySawmill sawmill) && sawmill != null)
                {
                    productionNodeQuery.Add(sawmill);
                }

                if (building.TryGetComponent(out StrategyKiln kiln) && kiln != null)
                {
                    productionNodeQuery.Add(kiln);
                }

                if (building.TryGetComponent(out StrategyForge forge) && forge != null)
                {
                    productionNodeQuery.Add(forge);
                }
            }

            productionNodeSortWorld = nearWorld;
            productionNodeQuery.Sort(CompareProductionNodesByDistance);
            return productionNodeQuery;
        }

        private static int CompareProductionNodesByDistance(
            IStrategyProductionLogisticsNode left,
            IStrategyProductionLogisticsNode right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            float leftDistance = (left.FootprintBounds.center - productionNodeSortWorld).sqrMagnitude;
            float rightDistance = (right.FootprintBounds.center - productionNodeSortWorld).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        }
    }
}
