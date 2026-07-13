using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private StrategyInputRouter inputRouter;

        public void SetInputRouter(StrategyInputRouter router)
        {
            inputRouter = router;
        }

        private void Update()
        {
            if (strategyCamera == null)
            {
                return;
            }

            HandleDeleteInput();
            HandleSelectionInput();
            UpdateHudAnimation();
        }

        private void HandleDeleteInput()
        {
            if (inputRouter == null
                || !inputRouter.GameplayDeleteSelectionPressed
                || selectedTransform == null
                || placementController == null
                || confirmationDialog == null
                || confirmationDialog.IsOpen)
            {
                return;
            }

            StrategyConstructionSite constructionSite = selectedTransform.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                RequestConstructionCancel(constructionSite);
                return;
            }

            StrategyPlacedBuilding building = selectedTransform.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                RequestBuildingDemolition(building);
            }
        }

        private void HandleSelectionInput()
        {
            if (inputRouter == null
                || !inputRouter.GameplayPrimaryClickPressed
                || !inputRouter.CameraHasPointer
                || IsPointerOverUi())
            {
                return;
            }

            if (buildMenu != null && buildMenu.LastPlacementFrame == Time.frameCount)
            {
                return;
            }

            if (buildMenu != null && buildMenu.ActiveTool != StrategyBuildTool.None)
            {
                return;
            }

            Vector2 screen = inputRouter.CameraPointerPosition;
            Vector3 world = strategyCamera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, Mathf.Abs(strategyCamera.transform.position.z)));
            if (fog != null && !fog.IsWorldExplored(world))
            {
                ClearSelection();
                inspectHud?.Hide();
                return;
            }

            Physics2D.SyncTransforms();
            Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(world.x, world.y));
            UpdateInspectHud(world, hits);
            SelectBestHit(hits);
        }
    }
}
