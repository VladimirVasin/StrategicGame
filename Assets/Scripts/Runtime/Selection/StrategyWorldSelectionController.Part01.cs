using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
            UpdateSelectionMarker(bounds);
            UpdateSelectionLinks(target);
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

        private void LateUpdate()
        {
            if (selectedTransform == null || markerRenderer == null || !markerRenderer.gameObject.activeSelf)
            {
                return;
            }

            StrategyResidentAgent resident = selectedTransform.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                UpdateSelectionMarker(resident.SelectionBounds);
                ClearSelectionLinks();
                RefreshHud();
                return;
            }

            StrategyPlacedBuilding building = selectedTransform.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                UpdateSelectionMarker(building.SelectionBounds);
                UpdateSelectionLinks(building);
                RefreshHud();
                return;
            }

            StrategyConstructionSite constructionSite = selectedTransform.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                UpdateSelectionMarker(constructionSite.SelectionBounds);
                UpdateSelectionLinks(constructionSite);
                RefreshHud();
                return;
            }

            StrategyGraveMarker grave = selectedTransform.GetComponent<StrategyGraveMarker>();
            if (grave != null)
            {
                UpdateSelectionMarker(grave.SelectionBounds);
                ClearSelectionLinks();
                RefreshHud();
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
            hudT = Mathf.MoveTowards(hudT, target, Time.unscaledDeltaTime * HudAnimationSpeed);
            float eased = Smooth01(hudT);
            hudPanel.anchoredPosition = new Vector2(Mathf.Lerp(HudWidth, 0f, eased), 0f);
            hudGroup.alpha = eased;
            hudGroup.blocksRaycasts = eased > 0.9f;
            hudGroup.interactable = eased > 0.9f;
        }

        private void RefreshHud()
        {
            EnsureHud();

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
                hudTitleText.text = GetBuildingTitle(building.Tool);
                hudSubtitleText.text = GetBuildingSubtitle(building);
                SetBuildingPreviewSprite(building);
                SetProfileSectionVisible(false);
                SetStatusSectionVisible(false);
                SetContextSectionVisible(false);

                bool isHouse = building.Tool == StrategyBuildTool.House;
                StrategyLumberjackCamp camp = building.GetComponent<StrategyLumberjackCamp>();
                StrategyStonecutterCamp stoneCamp = building.GetComponent<StrategyStonecutterCamp>();
                StrategySawmill sawmill = building.GetComponent<StrategySawmill>();
                StrategyMine mine = building.GetComponent<StrategyMine>();
                StrategyCoalPit coalPit = building.GetComponent<StrategyCoalPit>();
                StrategyClayPit clayPit = building.GetComponent<StrategyClayPit>();
                StrategyKiln kiln = building.GetComponent<StrategyKiln>();
                StrategyForge forge = building.GetComponent<StrategyForge>();
                StrategyHunterCamp hunterCamp = building.GetComponent<StrategyHunterCamp>();
                StrategyFisherHut fisherHut = building.GetComponent<StrategyFisherHut>();
                StrategyForagerCamp foragerCamp = building.GetComponent<StrategyForagerCamp>();
                StrategyChickenCoop chickenCoop = building.GetComponent<StrategyChickenCoop>();
                StrategyTradingPost tradingPost = building.GetComponent<StrategyTradingPost>();
                StrategyStorageYard yard = building.GetComponent<StrategyStorageYard>();
                StrategyGranary granary = building.GetComponent<StrategyGranary>();
                bool isLumberjackCamp = camp != null;
                bool isStonecutterCamp = stoneCamp != null;
                bool isSawmill = sawmill != null;
                bool isMine = mine != null;
                bool isCoalPit = coalPit != null;
                bool isClayPit = clayPit != null;
                bool isKiln = kiln != null;
                bool isForge = forge != null;
                bool isHunterCamp = hunterCamp != null;
                bool isFisherHut = fisherHut != null;
                bool isForagerCamp = foragerCamp != null;
                bool isChickenCoop = chickenCoop != null;
                bool isTradingPost = tradingPost != null;
                bool isStorageYard = yard != null;
                bool isGranary = granary != null;
                SetResidentsSectionVisible(isHouse);
                if (isHouse)
                {
                    RefreshResidents(building);
                }

                SetWorkersSectionVisible(false);
                if (isLumberjackCamp || isStonecutterCamp || isSawmill || isMine || isCoalPit || isClayPit || isKiln || isForge || isHunterCamp || isFisherHut || isForagerCamp || isChickenCoop || isGranary)
                {
                    LayoutContextSection(128f, 214f);
                }

                if (isLumberjackCamp)
                {
                    hudContextTitleText.text = "Forest and Stock";
                    hudContextBodyText.text = camp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isStonecutterCamp)
                {
                    hudContextTitleText.text = "Stone and Stock";
                    hudContextBodyText.text = stoneCamp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isSawmill)
                {
                    hudContextTitleText.text = "Logs and Planks";
                    hudContextBodyText.text = sawmill.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isMine)
                {
                    hudContextTitleText.text = "Iron and Stock";
                    hudContextBodyText.text = mine.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isCoalPit)
                {
                    hudContextTitleText.text = "Coal and Stock";
                    hudContextBodyText.text = coalPit.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isClayPit)
                {
                    hudContextTitleText.text = "Clay and Stock";
                    hudContextBodyText.text = clayPit.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isKiln)
                {
                    hudContextTitleText.text = "Clay, Coal and Pottery";
                    hudContextBodyText.text = kiln.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isForge)
                {
                    hudContextTitleText.text = "Iron, Coal, Logs and Tools";
                    hudContextBodyText.text = forge.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isHunterCamp)
                {
                    hudContextTitleText.text = "Hunting and Stock";
                    hudContextBodyText.text = hunterCamp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isFisherHut)
                {
                    hudContextTitleText.text = "Fishing and Stock";
                    hudContextBodyText.text = fisherHut.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isForagerCamp)
                {
                    hudContextTitleText.text = "Forage and Stock";
                    hudContextBodyText.text = foragerCamp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isChickenCoop)
                {
                    hudContextTitleText.text = "Eggs and Stock";
                    hudContextBodyText.text = chickenCoop.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isTradingPost)
                {
                    RefreshTradingPostHud(tradingPost);
                }
                else if (isStorageYard)
                {
                    RefreshStorageYardHud(yard);
                }
                else if (isGranary)
                {
                    hudContextTitleText.text = "Food and Stock";
                    hudContextBodyText.text = granary.GetHudStatusText();
                    SetContextSectionVisible(true);
                }

                SetResourcesVisible(isHouse);
                if (isHouse)
                {
                    RefreshResources(building);
                }

                SetUpgradeActionsVisible(false);

                RefreshProductionUpgradeHud(building);

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
                hudTitleText.text = "Construction";
                hudSubtitleText.text = constructionSite.Title;
                int stage = constructionSite.ResourcesComplete
                    ? Mathf.Clamp(1 + Mathf.FloorToInt(constructionSite.Progress * (StrategyConstructionSpriteFactory.StageCount - 1)), 1, StrategyConstructionSpriteFactory.StageCount - 1)
                    : 0;
                SetPreviewSprite(constructionSite.Tool == StrategyBuildTool.Bridge
                    ? StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(constructionSite.Footprint, stage)
                    : StrategyConstructionSpriteFactory.GetConstructionSprite(constructionSite.Tool, constructionSite.VisualVariant, stage));
                hudSummaryTitleText.text = "Plan";
                hudBodyText.text = GetBuildingTitle(constructionSite.Tool)
                    + "\n"
                    + constructionSite.Cost.ToDisplayText();
                hudStatusTitleText.text = "Construction Progress";
                hudStatusBodyText.text = constructionSite.GetHudStatusText();
                hudContextTitleText.text = "Builders";
                hudContextBodyText.text = GetConstructionBuildersText(constructionSite);
                SetProfileSectionVisible(true);
                SetStatusSectionVisible(true);
                SetContextSectionVisible(true);
                SetResidentsSectionVisible(false);
                SetWorkersSectionVisible(false);
                SetResourcesVisible(false);
                SetUpgradeActionsVisible(false);
                SetProductionUpgradeHudVisible(false);
                SetTradingPostHudVisible(false);
            }
        }

        private void SetBuildingPreviewSprite(StrategyPlacedBuilding building)
        {
            SetPreviewSprite(GetBuildingPreviewSprite(building));
        }
    }
}
