namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void StartBuildingConstruction()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (constructionSite == null || !constructionSite.CanBuildWithDeliveredResources)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            activity = ResidentActivity.BuildingConstruction;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            FaceWorldPoint(constructionSite.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderWorkStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin));
        }

        private void UpdateBuildingConstruction()
        {
            if (constructionSite == null || constructionSite.IsCompleted)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            if (!constructionSite.CanBuildWithDeliveredResources)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            AnimateConstructionWork();
        }
    }
}
