namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStarterCaravanCart
    {
        private readonly StrategyResourceStore resourceStore = new();

        private ref int logsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Logs);
        private ref int stoneStored => ref resourceStore.GetAmountRef(StrategyResourceType.Stone);
        private ref int planksStored => ref resourceStore.GetAmountRef(StrategyResourceType.Planks);
        private ref int gameStored => ref resourceStore.GetAmountRef(StrategyResourceType.Game);
        private ref int fishStored => ref resourceStore.GetAmountRef(StrategyResourceType.Fish);
        private ref int eggsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Eggs);
        private ref int berriesStored => ref resourceStore.GetAmountRef(StrategyResourceType.Berries);
        private ref int rootsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Roots);
        private ref int mushroomsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Mushrooms);

        private void ConfigureResourceStore()
        {
            resourceStore.Bind(
                this,
                StrategyResourceStoreScope.TemporarySettlement,
                0,
                false);
        }
    }
}
