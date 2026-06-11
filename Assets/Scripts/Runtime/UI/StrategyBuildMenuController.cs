using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBuildMenuController : MonoBehaviour
    {
        private StrategyBuildMenuControllerDriver driver;

        public StrategyBuildTool ActiveTool => driver != null ? driver.ActiveTool : StrategyBuildTool.None;
        public int LastPlacementFrame => driver != null ? driver.LastPlacementFrame : -1;
        public StrategyConstructionResourceCost AvailableConstructionResources => StrategyStorageYard.GetTotalConstructionResources();

        public bool TryGetActiveToolInfo(out StrategyBuildToolInfo info) => Driver.TryGetActiveToolInfo(out info);
        public bool CanAffordActiveTool() => Driver.CanAffordActiveTool();
        public bool TrySpendForActiveTool() => Driver.TrySpendForActiveTool();
        public void ClearActiveTool() => Driver.ClearActiveTool();
        public void CloseAll() => Driver.CloseAll();
        public void CloseAfterPlacement() => Driver.CloseAfterPlacement();

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
    }
}
