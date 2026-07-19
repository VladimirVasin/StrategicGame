using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle placementInputContext;

        public void SetInputRouter(StrategyInputRouter router)
        {
            placementInputContext?.Dispose();
            placementInputContext = null;
            inputRouter = router;
            RefreshPlacementInputContext();
        }

        private void Update()
        {
            if (map == null || buildMenu == null || strategyCamera == null)
            {
                return;
            }

            RefreshPlacementInputContext();
            HandleCancelInput();
            if (!buildMenu.TryGetActiveToolInfo(out StrategyBuildToolInfo toolInfo))
            {
                RefreshPlacementInputContext();
                HidePreview();
                return;
            }

            if (toolInfo.Tool == StrategyBuildTool.Bridge)
            {
                UpdateBridgePreview(toolInfo);
                HandleBridgePlaceInput(toolInfo);
                return;
            }

            ResetBridgePlacement();
            UpdatePreview(toolInfo);
            HandlePlaceInput(toolInfo);
        }

        private void HandleCancelInput()
        {
            if (inputRouter == null)
            {
                return;
            }

            bool cancelPressed = inputRouter.TryConsumeCancel(this)
                || !IsPointerOverUi() && inputRouter.TryConsumeBuildCancelPointer(this);
            if (!cancelPressed)
            {
                return;
            }

            StrategyBuildTool cancelledTool = buildMenu.ActiveTool;
            buildMenu.ClearActiveTool();
            HidePreview();
            ResetBridgePlacement();
            RefreshPlacementInputContext();
            if (cancelledTool != StrategyBuildTool.None)
            {
                StrategyDebugLogger.Info("Build", "PlacementCancelled", StrategyDebugLogger.F("tool", cancelledTool));
            }
        }

        private void HandlePlaceInput(StrategyBuildToolInfo toolInfo)
        {
            if (inputRouter == null || !inputRouter.BuildPlacePressed || IsPointerOverUi())
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Build",
                "PlacementAttempt",
                StrategyDebugLogger.F("tool", toolInfo.Tool),
                StrategyDebugLogger.F("origin", hoveredOrigin),
                StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                StrategyDebugLogger.F("costPlanks", toolInfo.Cost.Planks),
                StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));

            if (!buildMenu.CanAffordActiveTool())
            {
                buildMenu.SetPlacementFeedback(
                    false,
                    StrategyBuildPlacementFeedbackText.FormatFailureReason("not_affordable"));
                StrategyDebugLogger.Warn(
                    "Build",
                    "PlacementRejected",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", hoveredOrigin),
                    StrategyDebugLogger.F("reason", "not_affordable"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("costPlanks", toolInfo.Cost.Planks),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                return;
            }

            if (!hasValidHover || !CanPlace(hoveredOrigin, toolInfo))
            {
                buildMenu.SetPlacementFeedback(
                    false,
                    StrategyBuildPlacementFeedbackText.FormatFailureReason(
                        GetPlacementFailureReason(hoveredOrigin, toolInfo)));
                StrategyDebugLogger.Warn(
                    "Build",
                    "PlacementRejected",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", hoveredOrigin),
                    StrategyDebugLogger.F("reason", GetPlacementFailureReason(hoveredOrigin, toolInfo)));
                return;
            }

            StrategyConstructionSite site = PlaceConstructionSite(toolInfo, hoveredOrigin);
            if (site == null)
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "ConstructionSiteFailed",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", hoveredOrigin),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("costPlanks", toolInfo.Cost.Planks));
                return;
            }

            buildMenu.CloseAfterPlacement();
            RefreshPlacementInputContext();
            HidePreview();
        }

        private void HandleBridgePlaceInput(StrategyBuildToolInfo toolInfo)
        {
            if (inputRouter == null || !inputRouter.BuildPlacePressed || IsPointerOverUi())
            {
                return;
            }

            if (!TryGetMouseWorld(out Vector3 world) || !map.TryWorldToCell(world, out Vector2Int clickedCell))
            {
                return;
            }

            hoveredOrigin = clickedCell;
            StrategyDebugLogger.Info(
                "Build",
                "BridgePlacementAttempt",
                StrategyDebugLogger.F("cell", clickedCell),
                StrategyDebugLogger.F("hasStart", hasBridgeStart),
                StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                StrategyDebugLogger.F("costPlanks", toolInfo.Cost.Planks),
                StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));

            if (!buildMenu.CanAffordActiveTool())
            {
                buildMenu.SetPlacementFeedback(
                    false,
                    StrategyBuildPlacementFeedbackText.FormatFailureReason("not_affordable"));
                StrategyDebugLogger.Warn(
                    "Build",
                    "BridgePlacementRejected",
                    StrategyDebugLogger.F("cell", clickedCell),
                    StrategyDebugLogger.F("reason", "not_affordable"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("costPlanks", toolInfo.Cost.Planks),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                return;
            }

            if (!hasBridgeStart)
            {
                TrySelectBridgeStart(clickedCell);
                return;
            }

            if (TryGetBridgeCandidate(clickedCell, out BridgeCandidate bridgeCandidate))
            {
                StrategyConstructionSite site = PlaceBridgeConstructionSite(toolInfo, bridgeStartCell, bridgeCandidate);
                if (site == null)
                {
                    StrategyDebugLogger.Warn(
                        "Build",
                        "BridgeConstructionSiteFailed",
                        StrategyDebugLogger.F("start", bridgeStartCell),
                        StrategyDebugLogger.F("end", bridgeCandidate.EndCell));
                    return;
                }

                buildMenu.CloseAfterPlacement();
                RefreshPlacementInputContext();
                HidePreview();
                ResetBridgePlacement();
                return;
            }

            if (TrySelectBridgeStart(clickedCell))
            {
                return;
            }

            StrategyDebugLogger.Warn(
                "Build",
                "BridgePlacementRejected",
                StrategyDebugLogger.F("cell", clickedCell),
                StrategyDebugLogger.F("reason", "not_a_suggested_bank"));
            buildMenu.SetPlacementFeedback(
                false,
                StrategyBuildPlacementFeedbackText.FormatFailureReason("not_a_suggested_bank"));
        }

        private void RefreshPlacementInputContext()
        {
            bool ownsPlacement = inputRouter != null
                && inputRouter.IsAvailable
                && buildMenu != null
                && buildMenu.ActiveTool != StrategyBuildTool.None;
            if (!ownsPlacement)
            {
                placementInputContext?.Dispose();
                placementInputContext = null;
                return;
            }

            if (placementInputContext == null || placementInputContext.IsDisposed)
            {
                placementInputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.Gameplay,
                    StrategyCancelMode.Close,
                    true);
            }
        }

        private void OnDisable()
        {
            placementInputContext?.Dispose();
            placementInputContext = null;
        }
    }
}
