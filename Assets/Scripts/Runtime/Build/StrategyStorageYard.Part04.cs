using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        public bool TryReserveStoredLogs(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || logsStored <= 0 || logisticsLogsReservationOwner != null && logisticsLogsReservationOwner != owner)
            {
                return false;
            }

            if (logisticsLogsReservationOwner == owner && reservedLogisticsLogs > 0)
            {
                amount = reservedLogisticsLogs;
                return true;
            }

            int available = AvailableLogisticsLogs;
            if (available <= 0)
            {
                return false;
            }

            reservedLogisticsLogs = Mathf.Min(StrategyProductionStorage.HaulerCarryLimit, available);
            logisticsLogsReservationOwner = owner;
            amount = reservedLogisticsLogs;
            return true;
        }

        public bool TryTakeReservedLogs(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || logisticsLogsReservationOwner != owner || reservedLogisticsLogs <= 0 || logsStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedLogisticsLogs, logsStored);
            logsStored -= amount;
            reservedLogisticsLogs = 0;
            logisticsLogsReservationOwner = null;
            UpdateStockVisual();
            return amount > 0;
        }

        public void ReleaseStoredLogsReservation(object owner)
        {
            if (owner != null && logisticsLogsReservationOwner == owner)
            {
                logisticsLogsReservationOwner = null;
                reservedLogisticsLogs = 0;
            }
        }

        public bool TryReservePlanksSource(object owner, out IStrategyProductionLogisticsNode source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            IStrategyProductionLogisticsNode[] nodes = GetProductionNodesSortedByDistance(FootprintBounds.center);
            IStrategyProductionLogisticsNode best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < nodes.Length; i++)
            {
                IStrategyProductionLogisticsNode node = nodes[i];
                if (node == null
                    || !node.TryGetOutputPickupRequest(out StrategyResourceType resource, out int available)
                    || resource != StrategyResourceType.Planks
                    || available <= 0)
                {
                    continue;
                }

                float distance = (node.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = node;
                }
            }

            if (best == null || !best.TryReserveOutputPickup(StrategyResourceType.Planks, owner, out _))
            {
                return false;
            }

            source = best;
            return true;
        }

        public void AddResource(StrategyResourceType resource, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (resource == StrategyResourceType.Logs)
            {
                AddLogs(amount);
            }
            else if (resource == StrategyResourceType.Stone)
            {
                AddStone(amount);
            }
            else if (resource == StrategyResourceType.Iron)
            {
                AddIron(amount);
            }
            else if (resource == StrategyResourceType.Coal)
            {
                AddCoal(amount);
            }
            else if (resource == StrategyResourceType.Planks)
            {
                AddPlanks(amount);
            }
        }

        public void AddPlanks(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            planksStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Planks),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", planksStored));
        }

        private static int CountAvailablePlankSources()
        {
            int count = 0;
            IStrategyProductionLogisticsNode[] nodes = GetProductionNodesSortedByDistance(Vector3.zero);
            for (int i = 0; i < nodes.Length; i++)
            {
                IStrategyProductionLogisticsNode node = nodes[i];
                if (node != null
                    && node.TryGetOutputPickupRequest(out StrategyResourceType resource, out int available)
                    && resource == StrategyResourceType.Planks
                    && available > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsurePlanksStockRenderer()
        {
            if (planksStockRenderer != null)
            {
                return;
            }

            GameObject planksObject = new GameObject("Storage Planks Stock");
            planksObject.transform.SetParent(transform, false);
            planksStockRenderer = planksObject.AddComponent<SpriteRenderer>();
            planksStockRenderer.color = Color.white;
        }

        private void UpdatePlanksStockVisual()
        {
            EnsurePlanksStockRenderer();
            if (planksStockRenderer == null)
            {
                return;
            }

            planksStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardPlankStockSprite(planksStored);
            planksStockRenderer.gameObject.SetActive(planksStored > 0 && planksStockRenderer.sprite != null);
        }

        private void UpdatePlanksStockPosition(Bounds bounds)
        {
            if (planksStockRenderer == null)
            {
                return;
            }

            Vector3 planksWorld = new Vector3(bounds.max.x - 0.56f, bounds.min.y + 0.39f, -0.148f);
            planksStockRenderer.transform.localPosition = transform.InverseTransformPoint(planksWorld);
            planksStockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(planksStockRenderer, planksWorld, 1);
        }
    }
}
