using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private const int ProductionInputDeliveryStackLimit = StrategyProductionStorage.HaulerCarryLimit;

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

            IStrategyProductionLogisticsNode[] nodes = GetProductionNodesSortedByDistance(FootprintBounds.center);
            for (int i = 0; i < nodes.Length; i++)
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
            StrategyStorageYard[] yards = Object.FindObjectsByType<StrategyStorageYard>();
            IStrategyProductionLogisticsNode[] nodes = GetProductionNodesSortedByDistance(Vector3.zero);
            for (int i = 0; i < nodes.Length; i++)
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
                for (int j = 0; j < yards.Length; j++)
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

        private int GetAvailableLogisticsAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Logs => AvailableLogisticsLogs,
                StrategyResourceType.Stone => AvailableConstructionStone,
                StrategyResourceType.Iron => Mathf.Max(0, ironStored - CountProductionInputReservations(resource)),
                StrategyResourceType.Coal => Mathf.Max(0, coalStored - CountProductionInputReservations(resource)),
                StrategyResourceType.Planks => AvailableConstructionPlanks,
                _ => 0
            };
        }

        private int GetStoredAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Logs => logsStored,
                StrategyResourceType.Stone => stoneStored,
                StrategyResourceType.Iron => ironStored,
                StrategyResourceType.Coal => coalStored,
                StrategyResourceType.Planks => planksStored,
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
            else if (resource == StrategyResourceType.Planks)
            {
                planksStored = Mathf.Max(0, planksStored - amount);
            }

            UpdateStockVisual();
        }

        private static IStrategyProductionLogisticsNode[] GetProductionNodesSortedByDistance(Vector3 nearWorld)
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>();
            List<IStrategyProductionLogisticsNode> nodes = new();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IStrategyProductionLogisticsNode node)
                {
                    nodes.Add(node);
                }
            }

            nodes.Sort((left, right) =>
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

                float leftDistance = (left.FootprintBounds.center - nearWorld).sqrMagnitude;
                float rightDistance = (right.FootprintBounds.center - nearWorld).sqrMagnitude;
                return leftDistance.CompareTo(rightDistance);
            });
            return nodes.ToArray();
        }
    }
}
