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
                && ReservedOutputStorageUsed + ToolsPerWorkCycle <= StrategyProductionStorage.ProcessingOutputCapacity;
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
            int capacity = Mathf.Max(0, StrategyProductionStorage.ProcessingOutputCapacity - ReservedOutputStorageUsed);
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

        public void ClearPendingToolsForDemolition()
        {
            pendingTools = 0;
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
                + "\nInput Storage: " + InputStorageUsed + "/" + StrategyProductionStorage.ProcessingInputCapacity
                + (reservedInputIron + reservedInputCoal + reservedInputLogs > 0 ? " (" + (reservedInputIron + reservedInputCoal + reservedInputLogs) + " incoming)" : string.Empty)
                + "\nIron: " + ironStored
                + "\nCoal: " + coalStored
                + "\nLogs: " + logsStored
                + "\nOutput Storage: " + OutputStorageUsed + "/" + StrategyProductionStorage.ProcessingOutputCapacity
                + (pendingTools > 0 ? " (" + pendingTools + " pending)" : string.Empty)
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
            PositionStock(
                ironStockRenderer,
                StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Forge,
                    StrategyResourceType.Iron,
                    bounds));
            PositionStock(
                coalStockRenderer,
                StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Forge,
                    StrategyResourceType.Coal,
                    bounds));
            PositionStock(
                logStockRenderer,
                StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Forge,
                    StrategyResourceType.Logs,
                    bounds));
            PositionStock(
                toolsStockRenderer,
                StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Forge,
                    StrategyResourceType.Tools,
                    bounds));
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

            float zOffset = resource == StrategyResourceType.Logs ? -0.03f : -0.02f;
            Vector3 world = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Forge,
                    resource,
                    FootprintBounds)
                + new Vector3(0f, 0.08f, zOffset);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(resource, world, StrategyWorldSorting.ForPosition(world, 4), amount, StorageUsed + amount * 23);
        }

        private void PlayToolsProducedEffect(int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Vector3 world = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Forge,
                    StrategyResourceType.Tools,
                    FootprintBounds)
                + new Vector3(0f, 0.10f, -0.03f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(StrategyResourceType.Tools, world, StrategyWorldSorting.ForPosition(world, 4), amount, toolsStored + amount * 43);
        }
    }
}
