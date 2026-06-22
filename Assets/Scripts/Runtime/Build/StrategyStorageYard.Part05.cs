using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        public void ReleaseConstructionPickupReservation(StrategyResidentAgent builder)
        {
            if (builder == null || !constructionPickupReservations.Remove(builder))
            {
                return;
            }

            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionPickupReleased",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("builder", builder.FullName));
        }

        public bool TryTakeReservedConstructionResource(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int maxAmount,
            out int amount)
        {
            amount = 0;
            if (owner == null
                || builder == null
                || maxAmount <= 0
                || !constructionPickupReservations.TryGetValue(builder, out ConstructionPickupReservation pickup)
                || pickup == null
                || !ReferenceEquals(pickup.Owner, owner)
                || pickup.Kind != kind
                || pickup.Amount <= 0)
            {
                return false;
            }

            int requested = Mathf.Min(maxAmount, pickup.Amount);
            if (kind == StrategyConstructionResourceKind.Logs)
            {
                amount = TakeReservedConstruction(owner, constructionLogReservations, ref logsStored, requested);
            }
            else if (kind == StrategyConstructionResourceKind.Stone)
            {
                amount = TakeReservedConstruction(owner, constructionStoneReservations, ref stoneStored, requested);
            }
            else if (kind == StrategyConstructionResourceKind.Planks)
            {
                amount = TakeReservedConstruction(owner, constructionPlankReservations, ref planksStored, requested);
            }

            if (amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
                return false;
            }

            pickup.Amount -= amount;
            if (pickup.Amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionResourceTaken",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("logsStock", logsStored),
                StrategyDebugLogger.F("stoneStock", stoneStored),
                StrategyDebugLogger.F("planksStock", planksStored));
            return true;
        }

        private void EnsureStockRenderer()
        {
            if (logsStockRenderer != null
                && stoneStockRenderer != null
                && ironStockRenderer != null
                && coalStockRenderer != null
                && clayStockRenderer != null
                && potteryStockRenderer != null
                && planksStockRenderer != null
                && toolsStockRenderer != null)
            {
                return;
            }

            if (logsStockRenderer == null)
            {
                GameObject stockObject = new GameObject("Storage Logs Stock");
                stockObject.transform.SetParent(transform, false);
                logsStockRenderer = stockObject.AddComponent<SpriteRenderer>();
                logsStockRenderer.color = Color.white;
            }

            if (stoneStockRenderer == null)
            {
                GameObject stoneObject = new GameObject("Storage Stone Stock");
                stoneObject.transform.SetParent(transform, false);
                stoneStockRenderer = stoneObject.AddComponent<SpriteRenderer>();
                stoneStockRenderer.color = Color.white;
            }

            EnsureIronStockRenderer();
            EnsureCoalStockRenderer();
            EnsureClayStockRenderer();
            EnsurePotteryStockRenderer();
            EnsurePlanksStockRenderer();
            EnsureToolsStockRenderer();
            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (logsStockRenderer != null)
            {
                logsStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardStockSprite(logsStored);
                logsStockRenderer.gameObject.SetActive(logsStored > 0 && logsStockRenderer.sprite != null);
            }

            if (stoneStockRenderer != null)
            {
                stoneStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardStoneStockSprite(stoneStored);
                stoneStockRenderer.gameObject.SetActive(stoneStored > 0 && stoneStockRenderer.sprite != null);
            }

            UpdateIronStockVisual();
            UpdateCoalStockVisual();
            UpdateClayStockVisual();
            UpdatePotteryStockVisual();
            UpdatePlanksStockVisual();
            UpdateToolsStockVisual();
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            if (logsStockRenderer != null)
            {
                Vector3 logsWorld = new Vector3(bounds.center.x + 0.28f, bounds.min.y + 0.45f, -0.16f);
                logsStockRenderer.transform.localPosition = transform.InverseTransformPoint(logsWorld);
                logsStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(logsStockRenderer, logsWorld, 1);
            }

            if (stoneStockRenderer != null)
            {
                Vector3 stoneWorld = new Vector3(bounds.center.x - 0.86f, bounds.min.y + 0.37f, -0.155f);
                stoneStockRenderer.transform.localPosition = transform.InverseTransformPoint(stoneWorld);
                stoneStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(stoneStockRenderer, stoneWorld, 1);
            }

            UpdateIronStockPosition(bounds);
            UpdateCoalStockPosition(bounds);
            UpdateClayStockPosition(bounds);
            UpdatePotteryStockPosition(bounds);
            UpdatePlanksStockPosition(bounds);
            UpdateToolsStockPosition(bounds);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearStorageWorkplace(this);
                }
            }

            workers.Clear();

            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.ClearBuilderWorkplace(this);
                }
            }

            builders.Clear();
        }
    }
}
