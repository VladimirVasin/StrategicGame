using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyKiln
    {
        public bool CanStartWorkCycle()
        {
            return clayStored >= ClayPerWorkCycle
                && coalStored >= CoalPerWorkCycle
                && ReservedStorageUsed - ClayPerWorkCycle - CoalPerWorkCycle + PotteryPerWorkCycle <= StrategyProductionStorage.LocalCapacity;
        }

        public bool TryConsumeInputsForWork(out int potteryExpected)
        {
            potteryExpected = 0;
            if (!CanStartWorkCycle())
            {
                return false;
            }

            clayStored -= ClayPerWorkCycle;
            coalStored -= CoalPerWorkCycle;
            potteryExpected = PotteryPerWorkCycle;
            pendingPottery += potteryExpected;
            UpdateStockVisual();
            return true;
        }

        public void AddPottery(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            pendingPottery = Mathf.Max(0, pendingPottery - amount);
            int capacity = Mathf.Max(0, StrategyProductionStorage.LocalCapacity - clayStored - coalStored - potteryStored - pendingPottery);
            int accepted = Mathf.Min(amount, capacity);
            if (accepted <= 0)
            {
                return;
            }

            potteryStored += accepted;
            UpdateStockVisual();
            PlayPotteryProducedEffect(accepted);
            StrategyDebugLogger.Info(
                "Kiln",
                "PotteryStoredAtKiln",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", potteryStored));
        }

        public void ReleasePendingPottery(int amount)
        {
            pendingPottery = Mathf.Max(0, pendingPottery - Mathf.Max(0, amount));
        }

        public void BeginFiring(StrategyResidentAgent worker)
        {
            if (worker != null && !activePotters.Contains(worker))
            {
                activePotters.Add(worker);
            }
        }

        public void EndFiring(StrategyResidentAgent worker)
        {
            activePotters.Remove(worker);
        }

        public string GetHudStatusText()
        {
            return "Potters: " + workers.Count + "/" + MaxWorkers
                + "\nStorage: " + StorageUsed + "/" + StrategyProductionStorage.LocalCapacity
                + (pendingPottery > 0 ? " (" + pendingPottery + " pending)" : string.Empty)
                + "\nClay: " + clayStored
                + "\nCoal: " + coalStored
                + "\nPottery: " + potteryStored
                + (reservedPottery > 0 ? " (" + reservedPottery + " reserved)" : string.Empty);
        }

        private void Update()
        {
            UpdateWorkAnimation();
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                workers[i]?.ClearKilnWorkplace(this);
            }

            workers.Clear();
            activePotters.Clear();
            inputClayReservationOwner = null;
            inputCoalReservationOwner = null;
            potteryReservationOwner = null;
            reservedInputClay = 0;
            reservedInputCoal = 0;
            reservedPottery = 0;
            pendingPottery = 0;
        }

        private void EnsureStockRenderers()
        {
            clayStockRenderer ??= CreateChildRenderer("Kiln Clay Stock");
            coalStockRenderer ??= CreateChildRenderer("Kiln Coal Stock");
            potteryStockRenderer ??= CreateChildRenderer("Kiln Pottery Stock");
        }

        private SpriteRenderer CreateChildRenderer(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            return renderer;
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderers();
            clayStockRenderer.sprite = StrategyBuildingSpriteFactory.GetKilnClayStockSprite(clayStored);
            clayStockRenderer.gameObject.SetActive(clayStockRenderer.sprite != null);
            coalStockRenderer.sprite = StrategyBuildingSpriteFactory.GetKilnCoalStockSprite(coalStored);
            coalStockRenderer.gameObject.SetActive(coalStockRenderer.sprite != null);
            potteryStockRenderer.sprite = StrategyBuildingSpriteFactory.GetKilnPotteryStockSprite(potteryStored);
            potteryStockRenderer.gameObject.SetActive(potteryStockRenderer.sprite != null);
            UpdateStockPositions();
        }

        private void UpdateStockPositions()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            PositionStock(clayStockRenderer, new Vector3(bounds.min.x + 0.30f, bounds.min.y + 0.33f, -0.14f));
            PositionStock(coalStockRenderer, new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.31f, -0.14f));
            PositionStock(potteryStockRenderer, new Vector3(bounds.max.x - 0.58f, bounds.min.y + 0.52f, -0.13f));
        }

        private void PositionStock(SpriteRenderer renderer, Vector3 world)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.localPosition = transform.InverseTransformPoint(world);
            renderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(renderer, world, 1);
        }

        private void UpdateWorkAnimation()
        {
            if (activePotters.Count <= 0 || building == null)
            {
                if (workRenderer != null)
                {
                    workRenderer.gameObject.SetActive(false);
                }

                return;
            }

            workRenderer ??= CreateChildRenderer("Kiln Firing Work");
            workFrameTimer += Time.deltaTime * 7f;
            int steps = Mathf.FloorToInt(workFrameTimer);
            if (steps > 0)
            {
                workFrame = (workFrame + steps) % StrategyBuildingSpriteFactory.KilnWorkFrameCount;
                workFrameTimer -= steps;
                PlayFiringWorkEffect(workFrame + activePotters.Count * 31);
            }

            Vector3 world = GetKilnFocusWorld() + new Vector3(0.02f, -0.04f, -0.02f);
            workRenderer.sprite = StrategyBuildingSpriteFactory.GetKilnWorkSprite(workFrame, activePotters.Count);
            workRenderer.gameObject.SetActive(workRenderer.sprite != null);
            workRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            workRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(workRenderer, world, 2);
        }

        public void PlayFiringWorkEffect(int seed)
        {
            if (building == null || seed % 3 != 0)
            {
                return;
            }

            Vector3 world = GetKilnFocusWorld() + new Vector3(0.05f, 0.06f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(StrategyWorldEffectKind.IronSparks, world, StrategyWorldSorting.ForPosition(world, 4), seed, 0.62f);
        }

        private void PlayInputDeliveredEffect(StrategyResourceType resource, int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            Vector3 world = resource == StrategyResourceType.Clay
                ? new Vector3(bounds.min.x + 0.30f, bounds.min.y + 0.42f, -0.16f)
                : new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.40f, -0.16f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(resource, world, StrategyWorldSorting.ForPosition(world, 4), amount, StorageUsed + amount * 19);
        }

        private void PlayPotteryProducedEffect(int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.58f, bounds.min.y + 0.62f, -0.16f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(StrategyResourceType.Pottery, world, StrategyWorldSorting.ForPosition(world, 4), amount, potteryStored + amount * 37);
        }
    }
}
