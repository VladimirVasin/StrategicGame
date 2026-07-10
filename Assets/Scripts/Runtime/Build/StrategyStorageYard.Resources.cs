namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard : IStrategyResourceStoreOwner
    {
        private readonly StrategyResourceStore resourceStore = new();

        public StrategyResourceStore ResourceStore => resourceStore;
        private ref int logsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Logs);
        private ref int stoneStored => ref resourceStore.GetAmountRef(StrategyResourceType.Stone);
        private ref int ironStored => ref resourceStore.GetAmountRef(StrategyResourceType.Iron);
        private ref int coalStored => ref resourceStore.GetAmountRef(StrategyResourceType.Coal);
        private ref int clayStored => ref resourceStore.GetAmountRef(StrategyResourceType.Clay);
        private ref int potteryStored => ref resourceStore.GetAmountRef(StrategyResourceType.Pottery);
        private ref int planksStored => ref resourceStore.GetAmountRef(StrategyResourceType.Planks);
        private ref int toolsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Tools);

        private void ConfigureResourceStore()
        {
            resourceStore.Bind(this, StrategyResourceStoreScope.Settlement);
        }
    }
}
