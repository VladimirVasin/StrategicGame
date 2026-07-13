using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        public bool TryReservePotterySource(object owner, out IStrategyProductionLogisticsNode source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            List<IStrategyProductionLogisticsNode> nodes = GetProductionNodesSortedByDistance(FootprintBounds.center);
            IStrategyProductionLogisticsNode best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                IStrategyProductionLogisticsNode node = nodes[i];
                if (node == null
                    || !node.TryGetOutputPickupRequest(out StrategyResourceType resource, out int available)
                    || resource != StrategyResourceType.Pottery
                    || available <= 0
                    || !CanOwnerReachReservationNode(owner, node))
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

            if (best == null || !best.TryReserveOutputPickup(StrategyResourceType.Pottery, owner, out _))
            {
                return false;
            }

            source = best;
            return true;
        }

        public void AddPottery(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            potteryStored += amount;
            UpdateStockVisual();
            PlayResourceStoredEffect(StrategyResourceType.Pottery, amount);
            StrategyDebugLogger.Info(
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Pottery),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", potteryStored));
        }

        private static int CountAvailablePotterySources()
        {
            int count = 0;
            List<IStrategyProductionLogisticsNode> nodes = GetProductionNodesSortedByDistance(Vector3.zero);
            for (int i = 0; i < nodes.Count; i++)
            {
                IStrategyProductionLogisticsNode node = nodes[i];
                if (node != null
                    && node.TryGetOutputPickupRequest(out StrategyResourceType resource, out int available)
                    && resource == StrategyResourceType.Pottery
                    && available > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsurePotteryStockRenderer()
        {
            if (potteryStockRenderer != null)
            {
                return;
            }

            GameObject potteryObject = new GameObject("Storage Pottery Stock");
            potteryObject.transform.SetParent(transform, false);
            potteryStockRenderer = potteryObject.AddComponent<SpriteRenderer>();
            potteryStockRenderer.color = Color.white;
        }

        private void UpdatePotteryStockVisual()
        {
            EnsurePotteryStockRenderer();
            if (potteryStockRenderer == null)
            {
                return;
            }

            potteryStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardPotteryStockSprite(potteryStored);
            potteryStockRenderer.gameObject.SetActive(potteryStored > 0 && potteryStockRenderer.sprite != null);
        }

        private void UpdatePotteryStockPosition(Bounds bounds)
        {
            if (potteryStockRenderer == null)
            {
                return;
            }

            Vector3 potteryWorld = new Vector3(bounds.max.x - 0.34f, bounds.min.y + 0.58f, -0.146f);
            potteryStockRenderer.transform.localPosition = transform.InverseTransformPoint(potteryWorld);
            potteryStockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(potteryStockRenderer, potteryWorld, 1);
        }
    }
}
