using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyWorldSelectionController : MonoBehaviour
    {
        private const float HudWidth = 352f;
        private const float HudAnimationSpeed = 9f;
        private const float ResourceCellWidth = 138f;
        private const int StorageWorkerHudSlots = 2;
        private const int StorageBuilderHudSlots = 2;
        private const int MaxWorkerHudSlots = StorageWorkerHudSlots + StorageBuilderHudSlots;
        private const int SelectionLinkSortingOrder = StrategyWorldSorting.DayNightOverlayOrder - 120;

        private Camera strategyCamera;
        private StrategyBuildMenuController buildMenu;
        private StrategyBuildingUpgradeController upgradeController;
        private StrategyBuildPlacementController placementController;
        private StrategyConfirmationDialogController confirmationDialog;
        private StrategyFogOfWarController fog;
        private CityMapController map;
        private StrategyWorldInspectHudController inspectHud;
        private Sprite markerSprite;
        private SpriteRenderer markerRenderer;
        private Transform selectionLinksRoot;
        private Sprite linkedResidentMarkerSprite;
        private Material linkLineMaterial;
        private readonly List<StrategyResidentAgent> linkedResidents = new();
        private readonly List<StrategyResidentAgent> linkedResidentsScratch = new();
        private readonly List<SpriteRenderer> linkedResidentMarkers = new();
        private readonly List<LineRenderer> linkedResidentLines = new();
        private RectTransform hudPanel;
        private CanvasGroup hudGroup;
        private Image hudPreviewImage;
        private Text hudTitleText;
        private Text hudSubtitleText;
        private RectTransform summaryBackground;
        private Text hudSummaryTitleText;
        private Text hudBodyText;
        private RectTransform statusBackground;
        private Text hudStatusTitleText;
        private Text hudStatusBodyText;
        private RectTransform contextBackground;
        private Text hudContextTitleText;
        private Text hudContextBodyText;
        private RectTransform residentsRoot;
        private Text residentsEmptyText;
        private readonly List<RectTransform> residentRows = new();
        private readonly List<Image> residentPortraitImages = new();
        private readonly List<Text> residentNameTexts = new();
        private readonly List<Text> residentStatusTexts = new();
        private RectTransform workersRoot;
        private Text workersEmptyText;
        private readonly RectTransform[] workerRows = new RectTransform[MaxWorkerHudSlots];
        private readonly Image[] workerPortraitImages = new Image[MaxWorkerHudSlots];
        private readonly Text[] workerNameTexts = new Text[MaxWorkerHudSlots];
        private readonly Text[] workerStatusTexts = new Text[MaxWorkerHudSlots];
        private readonly Button[] workerButtons = new Button[MaxWorkerHudSlots];
        private readonly Text[] workerActionTexts = new Text[MaxWorkerHudSlots];
        private RectTransform resourcesRoot;
        private Image foodStatusRowImage;
        private Image foodMealFillImage;
        private RectTransform foodMealFillRect;
        private Text foodStatusText;
        private Text foodMealText;
        private Text foodGranaryText;
        private Text resourcesEmptyText;
        private readonly RectTransform[] resourceSlots = new RectTransform[StrategyHouseResourceStore.DisplayOrder.Length];
        private readonly Image[] resourceIconImages = new Image[StrategyHouseResourceStore.DisplayOrder.Length];
        private readonly Text[] resourceAmountTexts = new Text[StrategyHouseResourceStore.DisplayOrder.Length];
        private RectTransform upgradeActionsRoot;
        private Button gardenBedsButton;
        private Text gardenBedsButtonText;
        private Text gardenBedsStateText;
        private Text gardenBedsActionText;
        private Button chickenCoopButton;
        private Text chickenCoopButtonText;
        private Text chickenCoopStateText;
        private Text chickenCoopActionText;
        private Text upgradeStatusText;
        private Transform selectedTransform;
        private string upgradeStatusMessage = string.Empty;
        private float hudT;

        public void Configure(Camera camera, StrategyBuildMenuController menu, StrategyBuildingUpgradeController upgrades)
        {
            Configure(camera, menu, upgrades, null, null, null);
        }

        public void Configure(
            Camera camera,
            StrategyBuildMenuController menu,
            StrategyBuildingUpgradeController upgrades,
            StrategyFogOfWarController fogController)
        {
            Configure(camera, menu, upgrades, fogController, null, null);
        }

        public void Configure(
            Camera camera,
            StrategyBuildMenuController menu,
            StrategyBuildingUpgradeController upgrades,
            StrategyFogOfWarController fogController,
            StrategyPopulationController populationController,
            StrategyForestryController forestryController)
        {
            Configure(camera, menu, upgrades, fogController, populationController, forestryController, null, null);
        }

        public void Configure(
            Camera camera,
            StrategyBuildMenuController menu,
            StrategyBuildingUpgradeController upgrades,
            StrategyFogOfWarController fogController,
            StrategyPopulationController populationController,
            StrategyForestryController forestryController,
            StrategyBuildPlacementController placement,
            StrategyConfirmationDialogController confirmation)
        {
            Configure(camera, menu, upgrades, fogController, populationController, forestryController, placement, confirmation, null);
        }

        public void Configure(
            Camera camera,
            StrategyBuildMenuController menu,
            StrategyBuildingUpgradeController upgrades,
            StrategyFogOfWarController fogController,
            StrategyPopulationController populationController,
            StrategyForestryController forestryController,
            StrategyBuildPlacementController placement,
            StrategyConfirmationDialogController confirmation,
            CityMapController mapController)
        {
            strategyCamera = camera;
            buildMenu = menu;
            upgradeController = upgrades;
            placementController = placement;
            confirmationDialog = confirmation;
            fog = fogController;
            map = mapController != null ? mapController : Object.FindAnyObjectByType<CityMapController>();
            EnsureMarker();
            EnsureHud();
            EnsureInspectHud();
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
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null
                || !keyboard.deleteKey.wasPressedThisFrame
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

        private void RequestConstructionCancel(StrategyConstructionSite site)
        {
            if (site == null)
            {
                return;
            }

            string body = "Cancel construction of "
                + GetBuildingTitle(site.Tool)
                + "?\nDelivered materials will be left on the ground for storage workers or other builders.";
            confirmationDialog.Show(
                "Cancel Construction",
                body,
                "Cancel Construction",
                "Keep",
                () =>
                {
                    if (placementController != null && placementController.CancelConstructionSite(site))
                    {
                        ClearSelection();
                    }
                });
        }

        private void RequestBuildingDemolition(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return;
            }

            string body = "Demolish "
                + GetBuildingTitle(building.Tool)
                + "?\nThe building will be removed from the settlement.";
            if (building.Tool == StrategyBuildTool.House && building.ResidentCount > 0)
            {
                body += "\nResidents living here will become homeless.";
            }

            confirmationDialog.Show(
                "Demolish Building",
                body,
                "Demolish",
                "Keep",
                () =>
                {
                    if (placementController != null && placementController.DemolishBuilding(building))
                    {
                        ClearSelection();
                    }
                });
        }

        private void HandleSelectionInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || IsPointerOverUi())
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

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = strategyCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(strategyCamera.transform.position.z)));
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

        private void SelectBestHit(Collider2D[] hits)
        {
            StrategyResidentAgent bestResident = null;
            StrategyPlacedBuilding bestBuilding = null;
            StrategyConstructionSite bestConstructionSite = null;
            StrategyGraveMarker bestGrave = null;

            for (int i = 0; i < hits.Length; i++)
            {
                StrategyResidentAgent resident = hits[i].GetComponentInParent<StrategyResidentAgent>();
                if (resident != null)
                {
                    bestResident = resident;
                    break;
                }

                StrategyPlacedBuilding building = hits[i].GetComponentInParent<StrategyPlacedBuilding>();
                if (building != null)
                {
                    bestBuilding = building;
                }

                StrategyConstructionSite constructionSite = hits[i].GetComponentInParent<StrategyConstructionSite>();
                if (constructionSite != null)
                {
                    bestConstructionSite = constructionSite;
                }

                StrategyGraveMarker grave = hits[i].GetComponentInParent<StrategyGraveMarker>();
                if (grave != null)
                {
                    bestGrave = grave;
                }
            }

            if (bestResident != null)
            {
                Select(bestResident.transform, bestResident.SelectionBounds);
                return;
            }

            if (bestBuilding != null)
            {
                Select(bestBuilding.transform, bestBuilding.SelectionBounds);
                return;
            }

            if (bestConstructionSite != null)
            {
                Select(bestConstructionSite.transform, bestConstructionSite.SelectionBounds);
                return;
            }

            if (bestGrave != null)
            {
                Select(bestGrave.transform, bestGrave.SelectionBounds);
                return;
            }

            ClearSelection();
        }

        private void UpdateInspectHud(Vector3 world, Collider2D[] hits)
        {
            EnsureInspectHud();
            if (inspectHud == null)
            {
                return;
            }

            if (TryBuildSelectionInspectInfo(world, hits, out StrategyWorldInspectInfo selectionInfo))
            {
                inspectHud.Show(selectionInfo);
                return;
            }

            inspectHud.Hide();
        }

        private bool TryBuildSelectionInspectInfo(Vector3 world, Collider2D[] hits, out StrategyWorldInspectInfo info)
        {
            info = default;
            if (hits == null)
            {
                return false;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                StrategyResidentAgent resident = hits[i].GetComponentInParent<StrategyResidentAgent>();
                if (resident != null)
                {
                    return false;
                }
            }

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].GetComponentInParent<StrategyPlacedBuilding>() != null
                    || hits[i].GetComponentInParent<StrategyConstructionSite>() != null)
                {
                    return false;
                }
            }

            if (TryBuildInspectableWorldInfo(world, out info))
            {
                return true;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                StrategyGraveMarker grave = hits[i].GetComponentInParent<StrategyGraveMarker>();
                if (grave != null)
                {
                    info = BuildGraveInspectInfo(grave);
                    return true;
                }
            }

            return false;
        }

        private bool TryBuildInspectableWorldInfo(Vector3 world, out StrategyWorldInspectInfo info)
        {
            info = default;
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>();
            IStrategyWorldInspectable best = null;
            int bestSortingOrder = int.MinValue;
            float bestArea = float.MaxValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null
                    || !behaviour.isActiveAndEnabled
                    || behaviour is not IStrategyWorldInspectable inspectable
                    || behaviour is StrategyResidentAgent
                    || behaviour is StrategyPlacedBuilding
                    || behaviour is StrategyConstructionSite
                    || behaviour is StrategyBuildingUpgrade
                    || behaviour is StrategyGraveMarker)
                {
                    continue;
                }

                if (!TryGetInspectableSpriteBounds(behaviour, world, out Bounds bounds, out int sortingOrder))
                {
                    continue;
                }

                float area = Mathf.Max(0.0001f, bounds.size.x * bounds.size.y);
                if (best == null
                    || sortingOrder > bestSortingOrder
                    || (sortingOrder == bestSortingOrder && area < bestArea))
                {
                    best = inspectable;
                    bestSortingOrder = sortingOrder;
                    bestArea = area;
                }
            }

            return best != null
                && best.TryGetWorldInspectInfo(out info)
                && info.IsValid;
        }

        private static bool TryGetInspectableSpriteBounds(MonoBehaviour behaviour, Vector3 world, out Bounds bounds, out int sortingOrder)
        {
            bounds = default;
            sortingOrder = int.MinValue;
            SpriteRenderer[] renderers = behaviour.GetComponentsInChildren<SpriteRenderer>(false);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null
                    || !renderer.enabled
                    || renderer.sprite == null
                    || IsAuxiliaryInspectRenderer(renderer))
                {
                    continue;
                }

                Bounds rendererBounds = renderer.bounds;
                if (!hasBounds)
                {
                    bounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }

                sortingOrder = Mathf.Max(sortingOrder, renderer.sortingOrder);
            }

            if (!hasBounds)
            {
                return false;
            }

            Vector3 testWorld = new Vector3(world.x, world.y, bounds.center.z);
            return bounds.Contains(testWorld);
        }
    }
}
