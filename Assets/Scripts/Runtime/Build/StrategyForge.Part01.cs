using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForge
    {
        public bool CanStartWorkCycle()
        {
            return ironStored >= IronPerWorkCycle
                && coalStored >= CoalPerWorkCycle
                && logsStored >= LogsPerWorkCycle
                && ReservedStorageUsed - IronPerWorkCycle - CoalPerWorkCycle - LogsPerWorkCycle + ToolsPerWorkCycle <= StrategyProductionStorage.LocalCapacity;
        }

        public bool TryConsumeInputsForWork(out int toolsExpected)
        {
            toolsExpected = 0;
            if (!CanStartWorkCycle())
            {
                return false;
            }

            ironStored -= IronPerWorkCycle;
            coalStored -= CoalPerWorkCycle;
            logsStored -= LogsPerWorkCycle;
            toolsExpected = ToolsPerWorkCycle;
            pendingTools += toolsExpected;
            UpdateStockVisual();
            return true;
        }

        public void AddTools(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            pendingTools = Mathf.Max(0, pendingTools - amount);
            int capacity = Mathf.Max(0, StrategyProductionStorage.LocalCapacity - StorageUsed - pendingTools);
            int accepted = Mathf.Min(amount, capacity);
            if (accepted <= 0)
            {
                return;
            }

            toolsStored += accepted;
            UpdateStockVisual();
            PlayToolsProducedEffect(accepted);
            StrategyDebugLogger.Info(
                "Forge",
                "ToolsStoredAtForge",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", toolsStored));
        }

        public void ReleasePendingTools(int amount)
        {
            pendingTools = Mathf.Max(0, pendingTools - Mathf.Max(0, amount));
        }

        public void BeginForging(StrategyResidentAgent worker)
        {
            if (worker != null && !activeBlacksmiths.Contains(worker))
            {
                activeBlacksmiths.Add(worker);
            }
        }

        public void EndForging(StrategyResidentAgent worker)
        {
            activeBlacksmiths.Remove(worker);
        }

        public string GetHudStatusText()
        {
            return "Blacksmiths: " + workers.Count + "/" + MaxWorkers
                + "\nStorage: " + StorageUsed + "/" + StrategyProductionStorage.LocalCapacity
                + (pendingTools > 0 ? " (" + pendingTools + " pending)" : string.Empty)
                + "\nIron: " + ironStored
                + "\nCoal: " + coalStored
                + "\nLogs: " + logsStored
                + "\nTools: " + toolsStored
                + (reservedTools > 0 ? " (" + reservedTools + " reserved)" : string.Empty);
        }

        private void Update()
        {
            UpdateWorkAnimation();
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                workers[i]?.ClearForgeWorkplace(this);
            }

            workers.Clear();
            activeBlacksmiths.Clear();
            inputIronReservationOwner = null;
            inputCoalReservationOwner = null;
            inputLogsReservationOwner = null;
            toolsReservationOwner = null;
            reservedInputIron = 0;
            reservedInputCoal = 0;
            reservedInputLogs = 0;
            reservedTools = 0;
            pendingTools = 0;
        }

        private void EnsureStockRenderers()
        {
            ironStockRenderer ??= CreateChildRenderer("Forge Iron Stock");
            coalStockRenderer ??= CreateChildRenderer("Forge Coal Stock");
            logStockRenderer ??= CreateChildRenderer("Forge Log Stock");
            toolsStockRenderer ??= CreateChildRenderer("Forge Tools Stock");
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
            ironStockRenderer.sprite = StrategyBuildingSpriteFactory.GetForgeIronStockSprite(ironStored);
            ironStockRenderer.gameObject.SetActive(ironStockRenderer.sprite != null);
            coalStockRenderer.sprite = StrategyBuildingSpriteFactory.GetForgeCoalStockSprite(coalStored);
            coalStockRenderer.gameObject.SetActive(coalStockRenderer.sprite != null);
            logStockRenderer.sprite = StrategyBuildingSpriteFactory.GetForgeLogStockSprite(logsStored);
            logStockRenderer.gameObject.SetActive(logStockRenderer.sprite != null);
            toolsStockRenderer.sprite = StrategyBuildingSpriteFactory.GetForgeToolsStockSprite(toolsStored);
            toolsStockRenderer.gameObject.SetActive(toolsStockRenderer.sprite != null);
            UpdateStockPositions();
        }

        private void UpdateStockPositions()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            PositionStock(ironStockRenderer, new Vector3(bounds.min.x + 0.32f, bounds.min.y + 0.32f, -0.14f));
            PositionStock(coalStockRenderer, new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.31f, -0.14f));
            PositionStock(logStockRenderer, new Vector3(bounds.min.x + 0.24f, bounds.min.y + 0.52f, -0.13f));
            PositionStock(toolsStockRenderer, new Vector3(bounds.max.x - 0.56f, bounds.min.y + 0.54f, -0.13f));
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
            if (activeBlacksmiths.Count <= 0 || building == null)
            {
                if (workRenderer != null)
                {
                    workRenderer.gameObject.SetActive(false);
                }

                return;
            }

            workRenderer ??= CreateChildRenderer("Forge Hammer Work");
            workFrameTimer += Time.deltaTime * 9f;
            int steps = Mathf.FloorToInt(workFrameTimer);
            if (steps > 0)
            {
                workFrame = (workFrame + steps) % StrategyBuildingSpriteFactory.ForgeWorkFrameCount;
                workFrameTimer -= steps;
                PlayForgingWorkEffect(workFrame + activeBlacksmiths.Count * 41);
            }

            Vector3 world = GetForgeFocusWorld() + new Vector3(0.03f, -0.02f, -0.02f);
            workRenderer.sprite = StrategyBuildingSpriteFactory.GetForgeWorkSprite(workFrame);
            workRenderer.gameObject.SetActive(workRenderer.sprite != null);
            workRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            workRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(workRenderer, world, 3);
        }

        public void PlayForgingWorkEffect(int seed)
        {
            if (building == null || seed % 2 != 0)
            {
                return;
            }

            Vector3 world = GetForgeFocusWorld() + new Vector3(0.04f, 0.06f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(StrategyWorldEffectKind.IronSparks, world, StrategyWorldSorting.ForPosition(world, 4), seed, 0.72f);
        }

        private void PlayInputDeliveredEffect(StrategyResourceType resource, int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            Vector3 world = resource == StrategyResourceType.Iron
                ? new Vector3(bounds.min.x + 0.32f, bounds.min.y + 0.40f, -0.16f)
                : resource == StrategyResourceType.Coal
                    ? new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.39f, -0.16f)
                    : new Vector3(bounds.min.x + 0.24f, bounds.min.y + 0.60f, -0.16f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(resource, world, StrategyWorldSorting.ForPosition(world, 4), amount, StorageUsed + amount * 23);
        }

        private void PlayToolsProducedEffect(int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.56f, bounds.min.y + 0.64f, -0.16f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(StrategyResourceType.Tools, world, StrategyWorldSorting.ForPosition(world, 4), amount, toolsStored + amount * 43);
        }
    }
}
