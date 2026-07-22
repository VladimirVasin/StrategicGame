namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private StrategyBattleLifecycleController battleLifecycle;

        public void ConfigureBattleLifecycle(
            StrategyBattleLifecycleController lifecycleController)
        {
            battleLifecycle = lifecycleController;
            EnsureFuneralController();
        }
    }
}
