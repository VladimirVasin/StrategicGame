using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private bool TryGetCellForWorld(Vector3 world, out Vector2Int cell)
        {
            if (map == null)
            {
                map = Object.FindAnyObjectByType<CityMapController>();
            }

            cell = default;
            return map != null && map.TryWorldToCell(world, out cell);
        }

        private void EnsureInspectHud()
        {
            if (inspectHud != null)
            {
                return;
            }

            GameObject obj = new GameObject("Strategy World Inspect HUD");
            obj.transform.SetParent(transform, false);
            inspectHud = obj.AddComponent<StrategyWorldInspectHudController>();
            inspectHud.Configure(selectedTransform != null ? HudWidth + 18f : 18f);
        }

        private void Select(Transform target, Bounds bounds)
        {
            bool changedSelection = selectedTransform != target;
            selectedTransform = target;
            EnsureMarker();
            if (changedSelection)
            {
                upgradeStatusMessage = string.Empty;
                StrategyDebugLogger.Info(
                    "Selection",
                    "Selected",
                    StrategyDebugLogger.F("target", DescribeSelection(target)));
            }

            RefreshHud();
            if (changedSelection)
            {
                ResetHudScroll();
            }

            UpdateSelectionMarker(bounds);
            UpdateSelectionLinks(target);
        }

        public void SelectBuilding(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return;
            }

            inspectHud?.Hide();
            Select(building.transform, building.SelectionBounds);
        }

        public void SelectResident(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            inspectHud?.Hide();
            Select(resident.transform, resident.SelectionBounds);
        }

        private void UpdateSelectionMarker(Bounds bounds)
        {
            EnsureMarker();
            markerRenderer.gameObject.SetActive(true);
            markerRenderer.transform.position = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.08f, -0.05f);
            markerRenderer.transform.localScale = new Vector3(
                Mathf.Max(0.45f, bounds.size.x + 0.22f),
                Mathf.Max(0.18f, Mathf.Min(bounds.size.y * 0.25f, 0.65f)),
                1f);
            StrategyWorldSorting.Apply(markerRenderer, markerRenderer.transform.position, -3);
        }

        private void ClearSelection()
        {
            if (selectedTransform != null)
            {
                StrategyDebugLogger.Info(
                    "Selection",
                    "Cleared",
                    StrategyDebugLogger.F("target", DescribeSelection(selectedTransform)));
            }

            selectedTransform = null;
            upgradeStatusMessage = string.Empty;
            if (markerRenderer != null)
            {
                markerRenderer.gameObject.SetActive(false);
            }

            ClearSelectionLinks();
            RefreshHud();
        }

        public void ClearSelectionIfTarget(Component target)
        {
            if (target != null && selectedTransform == target.transform)
            {
                ClearSelection();
            }
        }

        public void DismissForBuildMode()
        {
            ClearSelection();
            inspectHud?.Hide();
        }

        private void LateUpdate()
        {
            if (selectedTransform == null || markerRenderer == null || !markerRenderer.gameObject.activeSelf)
            {
                return;
            }

            bool refreshHud = Time.unscaledTime >= nextHudRefreshTime;
            if (refreshHud)
            {
                nextHudRefreshTime = Time.unscaledTime + HudRefreshInterval;
            }

            StrategyResidentAgent resident = selectedTransform.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                UpdateSelectionMarker(resident.SelectionBounds);
                ClearSelectionLinks();
                if (refreshHud)
                {
                    RefreshHud();
                }
                return;
            }

            StrategyPlacedBuilding building = selectedTransform.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                UpdateSelectionMarker(building.SelectionBounds);
                UpdateSelectionLinks(building);
                if (refreshHud)
                {
                    RefreshHud();
                }
                return;
            }

            StrategyConstructionSite constructionSite = selectedTransform.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                UpdateSelectionMarker(constructionSite.SelectionBounds);
                UpdateSelectionLinks(constructionSite);
                if (refreshHud)
                {
                    RefreshHud();
                }
                return;
            }

            StrategyGraveMarker grave = selectedTransform.GetComponent<StrategyGraveMarker>();
            if (grave != null)
            {
                UpdateSelectionMarker(grave.SelectionBounds);
                ClearSelectionLinks();
                if (refreshHud)
                {
                    RefreshHud();
                }
            }
        }

        private void UpdateHudAnimation()
        {
            inspectHud?.SetRightInset(selectedTransform != null ? HudWidth + 18f : 18f);

            if (hudPanel == null || hudGroup == null)
            {
                return;
            }

            float target = selectedTransform != null ? 1f : 0f;
            hudT = StrategyHudStyle.ReducedMotion
                ? target
                : Mathf.MoveTowards(hudT, target, Time.unscaledDeltaTime * HudAnimationSpeed);
            float eased = Smooth01(hudT);
            hudPanel.anchoredPosition = new Vector2(
                Mathf.Lerp(HudWidth, 0f, eased),
                -StrategyHudStyle.TopRailHeight * 0.5f);
            hudGroup.alpha = eased;
            hudGroup.blocksRaycasts = eased > 0.9f;
            hudGroup.interactable = eased > 0.9f;
        }

        private void RefreshHud()
        {
            EnsureHud();
            HideBuildingSelectionHud();

            if (selectedTransform == null)
            {
                hudTitleText.text = string.Empty;
                hudSubtitleText.text = string.Empty;
                hudSummaryTitleText.text = string.Empty;
                hudBodyText.text = string.Empty;
                hudStatusTitleText.text = string.Empty;
                hudStatusBodyText.text = string.Empty;
                hudContextTitleText.text = string.Empty;
                hudContextBodyText.text = string.Empty;
                SetPreviewSprite(null);
                SetProfileSectionVisible(false);
                SetStatusSectionVisible(false);
                SetContextSectionVisible(false);
                SetResidentHudVisible(false);
                SetStorageYardHudVisible(false);
                SetResidentsSectionVisible(false);
                SetWorkersSectionVisible(false);
                SetResourcesVisible(false);
                SetUpgradeActionsVisible(false);
                SetProductionUpgradeHudVisible(false);
                SetTradingPostHudVisible(false);
                return;
            }

            LayoutStatusSection(272f, 76f);
            LayoutContextSection(366f, 118f);
            SetStorageYardHudVisible(false);
            SetTradingPostHudVisible(false);
            StrategyResidentAgent resident = selectedTransform.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                RefreshResidentHud(resident);
                return;
            }

            SetResidentHudVisible(false);
            StrategyPlacedBuilding building = selectedTransform.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                RefreshPlacedBuildingHud(building);

                return;
            }

            StrategyGraveMarker grave = selectedTransform.GetComponent<StrategyGraveMarker>();
            if (grave != null)
            {
                LayoutStatusSection(272f, 102f);
                LayoutContextSection(392f, 122f);
                hudTitleText.text = grave.DeceasedName;
                hudSubtitleText.text = "Grave";
                SetPreviewSprite(grave.PreviewSprite);
                hudSummaryTitleText.text = "Epitaph";
                hudBodyText.text = grave.Epitaph;
                hudStatusTitleText.text = "Life";
                hudStatusBodyText.text = grave.GetLifeText();
                hudContextTitleText.text = "Memory";
                hudContextBodyText.text = grave.GetMemoryText();
                SetProfileSectionVisible(true);
                SetStatusSectionVisible(true);
                SetContextSectionVisible(true);
                SetResidentsSectionVisible(false);
                SetWorkersSectionVisible(false);
                SetResourcesVisible(false);
                SetUpgradeActionsVisible(false);
                SetProductionUpgradeHudVisible(false);
                SetTradingPostHudVisible(false);
                return;
            }

            StrategyConstructionSite constructionSite = selectedTransform.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                RefreshConstructionSiteHud(constructionSite);
            }
        }

        private void SetBuildingPreviewSprite(StrategyPlacedBuilding building)
        {
            SetPreviewSprite(GetBuildingPreviewSprite(building));
        }
    }
}
