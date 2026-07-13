namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;

        public void SetInputRouter(StrategyInputRouter router)
        {
            ReleaseInputContext();
            inputRouter = router;
            RefreshInputContext();
        }

        public void ReleaseInputContext()
        {
            inputContext?.Dispose();
            inputContext = null;
        }

        private void HandlePointerDismissal()
        {
            if (!isOpen
                || inputRouter == null
                || !inputRouter.BuildPlacePressed
                || IsPointerOverBuildUi()
                || ActiveTool != StrategyBuildTool.None)
            {
                return;
            }

            CloseAll();
        }

        private void HandleHotkeys()
        {
            if (inputRouter == null)
            {
                return;
            }

            if (inputRouter.BuildTogglePressed)
            {
                ToggleOpen();
                return;
            }

            if (!isOpen)
            {
                return;
            }

            if (inputRouter.TryConsumeCancel(ownerTransform))
            {
                CancelOneLayer();
                return;
            }

            if (inputRouter.BuildCancelPointerPressed && IsPointerOverBuildUi())
            {
                CancelOneLayer();
                return;
            }

            int number = inputRouter.BuildSlotPressed;
            if (number <= 0)
            {
                return;
            }

            if (selectedCategoryIndex >= 0 && TryActivateItemHotkey(number))
            {
                return;
            }

            if (selectedCategoryIndex >= 0 && TrySelectSubcategoryHotkey(number))
            {
                return;
            }

            TrySelectCategoryHotkey(number);
        }

        private void RefreshInputContext()
        {
            bool ownsInput = inputRouter != null
                && inputRouter.IsAvailable
                && (isOpen || ActiveTool != StrategyBuildTool.None);
            if (!ownsInput)
            {
                ReleaseInputContext();
                return;
            }

            if (inputContext == null || inputContext.IsDisposed)
            {
                inputContext = inputRouter.PushContext(
                    ownerTransform,
                    StrategyInputChannel.Gameplay,
                    StrategyCancelMode.Close,
                    true);
            }
        }
    }
}
