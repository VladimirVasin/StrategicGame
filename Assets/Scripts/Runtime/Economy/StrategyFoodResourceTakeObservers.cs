namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyGranary : IStrategyExternalResourceTakeObserver
    {
        public void OnExternalResourceTaken()
        {
            UpdateStockVisual();
        }
    }

    public sealed partial class StrategyStarterCaravanCart : IStrategyExternalResourceTakeObserver
    {
        public void OnExternalResourceTaken()
        {
            TryDespawnIfEmpty();
        }
    }

    public sealed partial class StrategyForagerCamp : IStrategyExternalResourceTakeObserver
    {
        public void OnExternalResourceTaken()
        {
            UpdateStockVisual();
        }
    }

    public sealed partial class StrategyHunterCamp : IStrategyExternalResourceTakeObserver
    {
        public void OnExternalResourceTaken()
        {
            UpdateStockVisual();
        }
    }

    public sealed partial class StrategyFisherHut : IStrategyExternalResourceTakeObserver
    {
        public void OnExternalResourceTaken()
        {
            UpdateStockVisual();
        }
    }

    public sealed partial class StrategyChickenCoop : IStrategyExternalResourceTakeObserver
    {
        public void OnExternalResourceTaken()
        {
            UpdateStockVisual();
        }
    }
}
