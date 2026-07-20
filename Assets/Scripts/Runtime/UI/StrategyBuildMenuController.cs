using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBuildMenuController : MonoBehaviour
    {
        private StrategyBuildMenuControllerDriver driver;

        public StrategyBuildTool ActiveTool => driver != null ? driver.ActiveTool : StrategyBuildTool.None;
        public bool IsMenuOpen => driver != null && driver.IsOpen;
        public int LastPlacementFrame => driver != null ? driver.LastPlacementFrame : -1;
        public StrategyConstructionResourceCost AvailableConstructionResources => StrategyStorageYard.GetTotalConstructionResources();

        public bool TryGetActiveToolInfo(out StrategyBuildToolInfo info) => Driver.TryGetActiveToolInfo(out info);
        public bool CanAffordActiveTool() => Driver.CanAffordActiveTool();
        public bool TrySpendForActiveTool() => Driver.TrySpendForActiveTool();
        public void ClearActiveTool() => Driver.ClearActiveTool();
        public void CloseAll() => Driver.CloseAll();
        public void CloseAfterPlacement() => Driver.CloseAfterPlacement();
        public void SetAllowedTools(IEnumerable<StrategyBuildTool> tools) => Driver.SetAllowedTools(tools);
        public void ClearAllowedTools() => Driver.ClearAllowedTools();
        public bool IsToolAllowed(StrategyBuildTool tool) => Driver.IsToolAllowedForBuild(tool);
        public void SetInputRouter(StrategyInputRouter router) => Driver.SetInputRouter(router);
        public void SetPlacementFeedback(bool valid, string message) => Driver.SetPlacementFeedback(valid, message);
        public void ToggleMenu() => Driver.ToggleOpen();

        private StrategyBuildMenuControllerDriver Driver
        {
            get
            {
                if (driver == null)
                {
                    driver = new StrategyBuildMenuControllerDriver(transform);
                    driver.Initialize();
                }

                return driver;
            }
        }

        private void Awake()
        {
            Driver.Initialize();
        }

        private void Update()
        {
            driver?.Tick();
        }

        private void OnDisable()
        {
            driver?.ReleaseInputContext();
        }

        private void OnDestroy()
        {
            driver?.Dispose();
        }
    }
}
