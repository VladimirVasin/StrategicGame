using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void SyncReadabilityRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.sprite = spriteRenderer.sprite;
                outlineRenderer.flipX = spriteRenderer.flipX;
                outlineRenderer.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 1);
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 2);
            }

            SyncCarriedLogsRenderer();
            SyncCarriedStoneRenderer();
            SyncCarriedIronRenderer();
            SyncCarriedCoalRenderer();
            SyncCarriedClayRenderer();
            SyncCarriedPlanksRenderer();
            SyncCarriedPotteryRenderer();
            SyncCarriedGameRenderer();
            SyncCarriedFishRenderer();
            SyncCarriedForageRenderer();
            SyncFishingLineRenderer();
        }
    }
}
