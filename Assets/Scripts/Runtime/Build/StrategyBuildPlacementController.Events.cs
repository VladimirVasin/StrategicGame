namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {
        public event System.Action<StrategyPlacedBuilding> BuildingCompleted;

        private void NotifyBuildingCompleted(StrategyPlacedBuilding building)
        {
            if (building != null)
            {
                BuildingCompleted?.Invoke(building);
            }
        }
    }
}
