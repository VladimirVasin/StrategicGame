using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        public bool TryReserveIronSource(object owner, out StrategyMine source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            StrategyMine[] mines = Object.FindObjectsByType<StrategyMine>();
            StrategyMine bestMine = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < mines.Length; i++)
            {
                StrategyMine mine = mines[i];
                if (mine == null || mine.AvailableIron <= 0)
                {
                    continue;
                }

                float distance = (mine.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMine = mine;
                }
            }

            if (bestMine == null || !bestMine.TryReserveStoredIron(owner, out _))
            {
                return false;
            }

            source = bestMine;
            return true;
        }

        public bool TryReserveCoalSource(object owner, out StrategyCoalPit source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            StrategyCoalPit[] pits = Object.FindObjectsByType<StrategyCoalPit>();
            StrategyCoalPit bestPit = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < pits.Length; i++)
            {
                StrategyCoalPit pit = pits[i];
                if (pit == null || pit.AvailableCoal <= 0)
                {
                    continue;
                }

                float distance = (pit.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPit = pit;
                }
            }

            if (bestPit == null || !bestPit.TryReserveStoredCoal(owner, out _))
            {
                return false;
            }

            source = bestPit;
            return true;
        }

        public bool TryReserveClaySource(object owner, out StrategyClayPit source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            StrategyClayPit[] pits = Object.FindObjectsByType<StrategyClayPit>();
            StrategyClayPit bestPit = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < pits.Length; i++)
            {
                StrategyClayPit pit = pits[i];
                if (pit == null || pit.AvailableClay <= 0)
                {
                    continue;
                }

                float distance = (pit.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPit = pit;
                }
            }

            if (bestPit == null || !bestPit.TryReserveStoredClay(owner, out _))
            {
                return false;
            }

            source = bestPit;
            return true;
        }

        public void AddIron(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ironStored += amount;
            UpdateStockVisual();
            PlayResourceStoredEffect(StrategyResourceType.Iron, amount);
            StrategyDebugLogger.Info(
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Iron),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", ironStored));
        }

        public void AddCoal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coalStored += amount;
            UpdateStockVisual();
            PlayResourceStoredEffect(StrategyResourceType.Coal, amount);
            StrategyDebugLogger.Info(
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Coal),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", coalStored));
        }

        public void AddClay(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            clayStored += amount;
            UpdateStockVisual();
            PlayResourceStoredEffect(StrategyResourceType.Clay, amount);
            StrategyDebugLogger.Info(
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Clay),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", clayStored));
        }

        public int GetAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Logs
                ? logsStored
                : resource == StrategyResourceType.Stone
                    ? stoneStored
                    : resource == StrategyResourceType.Iron
                        ? ironStored
                        : resource == StrategyResourceType.Coal ? coalStored : resource == StrategyResourceType.Clay ? clayStored : resource == StrategyResourceType.Pottery ? potteryStored : resource == StrategyResourceType.Planks ? planksStored : 0;
        }

        private static int CountAvailableIronSources()
        {
            int count = 0;
            StrategyMine[] mines = Object.FindObjectsByType<StrategyMine>();
            for (int i = 0; i < mines.Length; i++)
            {
                StrategyMine mine = mines[i];
                if (mine != null && mine.AvailableIron > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAvailableCoalSources()
        {
            int count = 0;
            StrategyCoalPit[] pits = Object.FindObjectsByType<StrategyCoalPit>();
            for (int i = 0; i < pits.Length; i++)
            {
                StrategyCoalPit pit = pits[i];
                if (pit != null && pit.AvailableCoal > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAvailableClaySources()
        {
            int count = 0;
            StrategyClayPit[] pits = Object.FindObjectsByType<StrategyClayPit>();
            for (int i = 0; i < pits.Length; i++)
            {
                StrategyClayPit pit = pits[i];
                if (pit != null && pit.AvailableClay > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsureIronStockRenderer()
        {
            if (ironStockRenderer != null)
            {
                return;
            }

            GameObject ironObject = new GameObject("Storage Iron Stock");
            ironObject.transform.SetParent(transform, false);
            ironStockRenderer = ironObject.AddComponent<SpriteRenderer>();
            ironStockRenderer.color = Color.white;
        }

        private void EnsureCoalStockRenderer()
        {
            if (coalStockRenderer != null)
            {
                return;
            }

            GameObject coalObject = new GameObject("Storage Coal Stock");
            coalObject.transform.SetParent(transform, false);
            coalStockRenderer = coalObject.AddComponent<SpriteRenderer>();
            coalStockRenderer.color = Color.white;
        }

        private void EnsureClayStockRenderer()
        {
            if (clayStockRenderer != null)
            {
                return;
            }

            GameObject clayObject = new GameObject("Storage Clay Stock");
            clayObject.transform.SetParent(transform, false);
            clayStockRenderer = clayObject.AddComponent<SpriteRenderer>();
            clayStockRenderer.color = Color.white;
        }

        private void UpdateIronStockVisual()
        {
            EnsureIronStockRenderer();
            if (ironStockRenderer == null)
            {
                return;
            }

            ironStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardIronStockSprite(ironStored);
            ironStockRenderer.gameObject.SetActive(ironStored > 0 && ironStockRenderer.sprite != null);
        }

        private void UpdateCoalStockVisual()
        {
            EnsureCoalStockRenderer();
            if (coalStockRenderer == null)
            {
                return;
            }

            coalStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardCoalStockSprite(coalStored);
            coalStockRenderer.gameObject.SetActive(coalStored > 0 && coalStockRenderer.sprite != null);
        }

        private void UpdateClayStockVisual()
        {
            EnsureClayStockRenderer();
            if (clayStockRenderer == null)
            {
                return;
            }

            clayStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardClayStockSprite(clayStored);
            clayStockRenderer.gameObject.SetActive(clayStored > 0 && clayStockRenderer.sprite != null);
        }

        private void UpdateIronStockPosition(Bounds bounds)
        {
            if (ironStockRenderer == null)
            {
                return;
            }

            Vector3 ironWorld = new Vector3(bounds.center.x + 0.82f, bounds.min.y + 0.34f, -0.15f);
            ironStockRenderer.transform.localPosition = transform.InverseTransformPoint(ironWorld);
            ironStockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(ironStockRenderer, ironWorld, 1);
        }

        private void UpdateCoalStockPosition(Bounds bounds)
        {
            if (coalStockRenderer == null)
            {
                return;
            }

            Vector3 coalWorld = new Vector3(bounds.center.x + 0.18f, bounds.min.y + 0.28f, -0.145f);
            coalStockRenderer.transform.localPosition = transform.InverseTransformPoint(coalWorld);
            coalStockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(coalStockRenderer, coalWorld, 1);
        }

        private void UpdateClayStockPosition(Bounds bounds)
        {
            if (clayStockRenderer == null)
            {
                return;
            }

            Vector3 clayWorld = new Vector3(bounds.max.x - 0.16f, bounds.min.y + 0.30f, -0.145f);
            clayStockRenderer.transform.localPosition = transform.InverseTransformPoint(clayWorld);
            clayStockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(clayStockRenderer, clayWorld, 1);
        }
    }
}
