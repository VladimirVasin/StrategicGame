using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFisherHut
    {
        private void LogReservationFailure(string reason)
        {
            if (Time.time < nextReservationFailureLogTime)
            {
                return;
            }

            nextReservationFailureLogTime = Time.time + 10f;
            StrategyDebugLogger.Warn(
                "Fishing",
                "FishTargetUnavailable",
                StrategyDebugLogger.F("hutOrigin", Origin),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("fishNearby", wildlife != null ? wildlife.CountCatchableFish(Origin, WorkRadius) : 0),
                StrategyDebugLogger.F("storageSpace", HasStorageSpace),
                StrategyDebugLogger.F("waterFrozen", StrategySeasonalSurfaceController.IsWaterFrozenForGameplay));
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Fish Stock");
            stockObject.transform.SetParent(transform, false);
            stockRenderer = stockObject.AddComponent<SpriteRenderer>();
            stockRenderer.color = Color.white;
            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (stockRenderer == null)
            {
                return;
            }

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetFisherHutStockSprite(fishStored);
            stockRenderer.gameObject.SetActive(fishStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.25f, bounds.min.y + 0.38f, -0.13f);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearFisherWorkplace(this);
                }
            }

            workers.Clear();
        }
    }
}
