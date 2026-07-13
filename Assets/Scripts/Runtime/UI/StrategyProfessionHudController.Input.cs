using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
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

        private void HandleInput()
        {
            if (!isOpen || inputRouter == null || !inputRouter.IsAvailable)
            {
                return;
            }

            if (inputRouter.TryConsumeCancel(this))
            {
                Close();
                return;
            }

            if (inputRouter.GameplayPrimaryClickPressed
                && EventSystem.current != null
                && !EventSystem.current.IsPointerOverGameObject())
            {
                Close();
            }
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
        }
    }
}
