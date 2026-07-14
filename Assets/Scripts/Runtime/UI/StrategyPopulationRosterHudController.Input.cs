namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationRosterHudController
    {
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;

        public void SetInputRouter(StrategyInputRouter router)
        {
            inputContext?.Dispose();
            inputContext = null;
            inputRouter = router;
            RefreshInputContext();
        }

        private void RefreshInputContext()
        {
            if (!isOpen || inputRouter == null || !inputRouter.IsAvailable)
            {
                inputContext?.Dispose();
                inputContext = null;
                return;
            }

            if (inputContext == null || inputContext.IsDisposed)
            {
                inputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.None,
                    StrategyCancelMode.Close);
            }
        }

        private void OnDisable()
        {
            inputContext?.Dispose();
            inputContext = null;
            ResetPanelTransition();
        }
    }
}
