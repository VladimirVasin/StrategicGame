using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFamilyTreeHudController
    {
        private StrategyUiPanelTransition panelTransition;

        private void ConfigurePanelTransition()
        {
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            panelTransition = panel.gameObject.AddComponent<StrategyUiPanelTransition>();
            panelTransition.Configure(
                group,
                panel,
                new Vector2(0f, 20f),
                0.995f,
                0.20f,
                0.14f);
            panelTransition.SetVisible(false, true);
        }

        private void SetPanelTransitionOpen(bool open)
        {
            panelTransition?.SetVisible(open);
        }

        private void ResetPanelTransition()
        {
            isOpen = false;
            panelTransition?.SetVisible(false, true);
        }
    }
}
