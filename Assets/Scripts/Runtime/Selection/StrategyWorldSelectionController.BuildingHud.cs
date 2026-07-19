using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private const float BuildingHudTop = 128f;
        private const float ScoutBuildingHudTop = 238f;
        private const float BuildingHudSectionGap = 12f;
        private const float HouseResidentsHeight = 236f;
        private const float HouseResourcesHeight = 274f;
        private const float ProductionUpgradeHeight = 132f;
        private const float DefaultHudContentHeight = 1050f;

        private readonly StrategyBuildingHudSnapshot buildingHudSnapshot = new();
        private StrategyBuildingSelectionHudRenderer buildingHudRenderer;

        private void EnsureBuildingHudRenderer()
        {
            if (buildingHudRenderer == null && hudContent != null)
            {
                buildingHudRenderer = new StrategyBuildingSelectionHudRenderer(hudContent);
            }
        }

        private void HideBuildingSelectionHud()
        {
            buildingHudRenderer?.Hide();
        }

        private void RefreshPlacedBuildingHud(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                HideBuildingSelectionHud();
                return;
            }

            HideLegacyBuildingHudSections();
            hudTitleText.text = GetBuildingTitle(building.Tool);
            hudSubtitleText.text = GetBuildingSubtitle(building);
            SetBuildingPreviewSprite(building);

            StrategyTradingPost tradingPost = building.GetComponent<StrategyTradingPost>();
            bool isHouse = building.Tool == StrategyBuildTool.House;
            bool isDemolishing = building.IsDemolishing;
            StrategyScoutLodge scoutLodge = building.GetComponent<StrategyScoutLodge>();
            float rendererTop = scoutLodge != null && !isDemolishing
                ? ScoutBuildingHudTop
                : BuildingHudTop;
            float rendererBottom = ShowBuildingSnapshot(building, rendererTop);

            if (isHouse)
            {
                rendererBottom = LayoutAndRefreshHouseHud(building, rendererBottom);
            }
            else if (!isDemolishing && scoutLodge != null)
            {
                SetWorkersSectionVisible(true);
                RefreshWorkers(scoutLodge);
            }
            else if (!isDemolishing && tradingPost != null)
            {
                rendererBottom = RefreshTradingPostHud(
                    tradingPost,
                    rendererBottom + BuildingHudSectionGap);
            }

            float upgradeTop = rendererBottom + BuildingHudSectionGap;
            LayoutProductionUpgradeHud(upgradeTop);
            RefreshProductionUpgradeHud(building);
            if (productionUpgradeRoot != null
                && productionUpgradeRoot.gameObject.activeSelf)
            {
                rendererBottom = upgradeTop + ProductionUpgradeHeight;
            }

            EnsureHudContentExtent(rendererBottom + 24f);
        }

        private void RefreshConstructionSiteHud(StrategyConstructionSite constructionSite)
        {
            if (constructionSite == null)
            {
                HideBuildingSelectionHud();
                return;
            }

            HideLegacyBuildingHudSections();
            hudTitleText.text = "Construction";
            hudSubtitleText.text = constructionSite.Title;
            SetPreviewSprite(GetConstructionPreviewSprite(constructionSite));
            float bottom = ShowBuildingSnapshot(constructionSite, BuildingHudTop);
            EnsureHudContentExtent(bottom + 24f);
        }

        private float ShowBuildingSnapshot(StrategyPlacedBuilding building, float top)
        {
            EnsureBuildingHudRenderer();
            if (buildingHudRenderer == null
                || !StrategyBuildingHudSnapshotFactory.TryFill(building, buildingHudSnapshot))
            {
                HideBuildingSelectionHud();
                return top;
            }

            return buildingHudRenderer.Show(buildingHudSnapshot, top);
        }

        private float ShowBuildingSnapshot(StrategyConstructionSite site, float top)
        {
            EnsureBuildingHudRenderer();
            if (buildingHudRenderer == null
                || !StrategyBuildingHudSnapshotFactory.TryFill(site, buildingHudSnapshot))
            {
                HideBuildingSelectionHud();
                return top;
            }

            return buildingHudRenderer.Show(buildingHudSnapshot, top);
        }

        private float LayoutAndRefreshHouseHud(
            StrategyPlacedBuilding building,
            float rendererBottom)
        {
            float residentsTop = rendererBottom + BuildingHudSectionGap;
            float resourcesTop = residentsTop
                + HouseResidentsHeight
                + BuildingHudSectionGap;
            if (residentsRoot != null)
            {
                SetTopStretch(
                    residentsRoot,
                    18f,
                    residentsTop,
                    18f,
                    HouseResidentsHeight);
            }

            if (resourcesRoot != null)
            {
                SetTopStretch(
                    resourcesRoot,
                    24f,
                    resourcesTop,
                    24f,
                    HouseResourcesHeight);
            }

            SetResidentsSectionVisible(true);
            RefreshResidents(building);
            SetResourcesVisible(true);
            RefreshResources(building);
            return resourcesTop + HouseResourcesHeight;
        }

        private void ResetHudScroll()
        {
            if (hudScrollRect == null)
            {
                return;
            }

            hudScrollRect.StopMovement();
            hudScrollRect.verticalNormalizedPosition = 1f;
        }

        private void EnsureHudContentExtent(float bottom)
        {
            float height = Mathf.Max(DefaultHudContentHeight, bottom);
            if (hudScrollContent != null)
            {
                hudScrollContent.sizeDelta = new Vector2(0f, height);
            }

            if (hudContent != null)
            {
                hudContent.sizeDelta = new Vector2(0f, height);
            }
        }

        private void HideLegacyBuildingHudSections()
        {
            SetProfileSectionVisible(false);
            SetStatusSectionVisible(false);
            SetContextSectionVisible(false);
            SetStorageYardHudVisible(false);
            SetTradingPostHudVisible(false);
            SetResidentsSectionVisible(false);
            SetWorkersSectionVisible(false);
            SetResourcesVisible(false);
            SetUpgradeActionsVisible(false);
            SetProductionUpgradeHudVisible(false);
            HideBuildingSelectionHud();
        }

        private static Sprite GetConstructionPreviewSprite(
            StrategyConstructionSite constructionSite)
        {
            int materialStage = Mathf.Clamp(
                Mathf.FloorToInt(constructionSite.DeliveredResourceFraction * 2f),
                0,
                2);
            int progressStage = constructionSite.Progress > 0f
                ? Mathf.Clamp(
                    1 + Mathf.FloorToInt(
                        constructionSite.Progress
                        * (StrategyConstructionSpriteFactory.StageCount - 1)),
                    1,
                    StrategyConstructionSpriteFactory.StageCount - 1)
                : materialStage;
            int stage = constructionSite.ResourcesComplete
                ? progressStage
                : Mathf.Max(materialStage, progressStage);
            return constructionSite.Tool == StrategyBuildTool.Bridge
                ? StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(
                    constructionSite.Footprint,
                    stage)
                : StrategyConstructionSpriteFactory.GetConstructionSprite(
                    constructionSite.Tool,
                    constructionSite.VisualVariant,
                    stage);
        }
    }
}
