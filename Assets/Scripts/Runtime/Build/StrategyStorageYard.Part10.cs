using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        public bool TryReserveToolsSource(object owner, out IStrategyProductionLogisticsNode source)
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
                    || resource != StrategyResourceType.Tools
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

            if (best == null || !best.TryReserveOutputPickup(StrategyResourceType.Tools, owner, out _))
            {
                return false;
            }

            source = best;
            return true;
        }

        public void AddTools(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            toolsStored += amount;
            UpdateStockVisual();
            PlayResourceStoredEffect(StrategyResourceType.Tools, amount);
            StrategyDebugLogger.Info(
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Tools),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", toolsStored));
        }

        private static int CountAvailableToolsSources()
        {
            int count = 0;
            List<IStrategyProductionLogisticsNode> nodes = GetProductionNodesSortedByDistance(Vector3.zero);
            for (int i = 0; i < nodes.Count; i++)
            {
                IStrategyProductionLogisticsNode node = nodes[i];
                if (node != null
                    && node.TryGetOutputPickupRequest(out StrategyResourceType resource, out int available)
                    && resource == StrategyResourceType.Tools
                    && available > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsureToolsStockRenderer()
        {
            if (toolsStockRenderer != null)
            {
                return;
            }

            GameObject toolsObject = new GameObject("Storage Tools Stock");
            toolsObject.transform.SetParent(transform, false);
            toolsStockRenderer = toolsObject.AddComponent<SpriteRenderer>();
            toolsStockRenderer.color = Color.white;
        }

        private void UpdateToolsStockVisual()
        {
            EnsureToolsStockRenderer();
            if (toolsStockRenderer == null)
            {
                return;
            }

            toolsStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardToolsStockSprite(toolsStored);
            toolsStockRenderer.gameObject.SetActive(toolsStored > 0 && toolsStockRenderer.sprite != null);
        }

        private void UpdateToolsStockPosition(Bounds bounds)
        {
            if (toolsStockRenderer == null)
            {
                return;
            }

            Vector3 toolsWorld = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                StrategyBuildTool.StorageYard,
                StrategyResourceType.Tools,
                bounds);
            toolsStockRenderer.transform.localPosition = transform.InverseTransformPoint(toolsWorld);
            toolsStockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(toolsStockRenderer, toolsWorld, 1);
        }
    }
}
