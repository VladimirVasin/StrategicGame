using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private const int ResidentChipRole = 0;
        private const int ResidentChipHome = 1;
        private const int ResidentChipFood = 2;
        private const int ResidentRowTask = 0;
        private const int ResidentRowHome = 1;
        private const int ResidentRowFood = 2;
        private const int ResidentRowFamily = 3;
        private const int ResidentChipCount = 3;
        private const int ResidentRowCount = 4;

        private RectTransform residentHudRoot;
        private readonly Image[] residentChipBackgrounds = new Image[ResidentChipCount];
        private readonly Image[] residentChipIconImages = new Image[ResidentChipCount];
        private readonly Text[] residentChipIconTexts = new Text[ResidentChipCount];
        private readonly Text[] residentChipTexts = new Text[ResidentChipCount];
        private readonly Image[] residentRowBackgrounds = new Image[ResidentRowCount];
        private readonly Image[] residentRowAccentImages = new Image[ResidentRowCount];
        private readonly Image[] residentRowIconImages = new Image[ResidentRowCount];
        private readonly Text[] residentRowIconTexts = new Text[ResidentRowCount];
        private readonly Text[] residentRowTitleTexts = new Text[ResidentRowCount];
        private readonly Text[] residentRowBodyTexts = new Text[ResidentRowCount];

        private void CreateResidentHud()
        {
            residentHudRoot = CreateUiObject("ResidentHud", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(residentHudRoot, 18f, 128f, 18f, 404f);
            residentHudRoot.gameObject.SetActive(false);

            CreateResidentChip(ResidentChipRole, "RoleChip", 0f, 0f, 138f, 30f);
            CreateResidentChip(ResidentChipHome, "HomeChip", 146f, 0f, 78f, 30f);
            CreateResidentChip(ResidentChipFood, "FoodChip", 232f, 0f, 84f, 30f);
            CreateResidentRow(ResidentRowTask, "TaskRow", 44f, 90f);
            CreateResidentRow(ResidentRowHome, "HomeRow", 148f, 70f);
            CreateResidentRow(ResidentRowFood, "FoodRow", 230f, 70f);
            CreateResidentRow(ResidentRowFamily, "FamilyRow", 312f, 86f);
        }

        private void RefreshResidentHud(StrategyResidentAgent resident)
        {
            SetProfileSectionVisible(false);
            SetStatusSectionVisible(false);
            SetContextSectionVisible(false);
            SetResidentsSectionVisible(false);
            SetWorkersSectionVisible(false);
            SetResourcesVisible(false);
            SetUpgradeActionsVisible(false);
            SetProductionUpgradeHudVisible(false);
            SetResidentHudVisible(true);

            hudTitleText.text = resident.FullName;
            hudSubtitleText.text = resident.DisplayAgeYears
                + "y "
                + GetResidentLifeStageTitle(resident)
                + " | "
                + GetResidentGenderTitle(resident.Gender);
            SetPreviewSprite(StrategyResidentSpriteFactory.GetPortraitSprite(
                resident.Gender,
                resident.VisualVariant,
                resident.LifeStage));

            Color roleColor = new Color(0.19f, 0.31f, 0.29f, 0.96f);
            SetResidentChip(
                ResidentChipRole,
                GetResidentRoleTitle(resident),
                GetResidentProfessionIcon(resident),
                GetResidentRoleLetter(resident),
                roleColor);
            SetResidentChip(
                ResidentChipHome,
                resident.Home != null ? "Housed" : "Camp",
                GetResidentHomeIcon(resident),
                GetResidentHomeFallback(resident),
                GetResidentHomeColor(resident));
            SetResidentChip(
                ResidentChipFood,
                resident.IsStarving ? "Starving" : resident.IsHungry ? "Hungry" : "Fed",
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game),
                "F",
                GetResidentFoodColor(resident));

            SetResidentRow(
                ResidentRowTask,
                "Current Task",
                ToSentenceCase(GetResidentStatus(resident)),
                GetResidentTaskDetail(resident),
                GetResidentProfessionIcon(resident),
                "T",
                GetResidentTaskColor(resident));
            SetResidentRow(
                ResidentRowHome,
                "Home",
                StrategyResidentHudText.GetHomeTitle(resident),
                GetResidentHomeDetail(resident),
                GetResidentHomeIcon(resident),
                GetResidentHomeFallback(resident),
                GetResidentHomeColor(resident));
            SetResidentRow(
                ResidentRowFood,
                "Food",
                resident.IsStarving ? "Starving" : resident.IsHungry ? "Needs food" : "Fed",
                GetResidentFoodDetail(resident),
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Fish),
                "F",
                GetResidentFoodColor(resident));
            SetResidentRow(
                ResidentRowFamily,
                "Family",
                string.IsNullOrWhiteSpace(resident.FamilyName) ? "Unrecorded family" : resident.FamilyName + " family",
                GetResidentFamilyDetail(resident),
                GetResidentPortraitIcon(resident),
                "R",
                new Color(0.13f, 0.18f, 0.18f, 0.94f));
        }

        private void SetResidentHudVisible(bool visible)
        {
            if (residentHudRoot != null)
            {
                residentHudRoot.gameObject.SetActive(visible);
            }
        }

        private void CreateResidentChip(int index, string name, float left, float top, float width, float height)
        {
            RectTransform rect = CreateUiObject(name, residentHudRoot).GetComponent<RectTransform>();
            SetTopLeft(rect, left, top, width, height);
            residentChipBackgrounds[index] = rect.gameObject.AddComponent<Image>();
            residentChipBackgrounds[index].color = new Color(0.10f, 0.15f, 0.14f, 0.92f);
            residentChipBackgrounds[index].raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", rect).GetComponent<RectTransform>();
            SetTopLeft(iconRect, 7f, 5f, 20f, 20f);
            residentChipIconImages[index] = iconRect.gameObject.AddComponent<Image>();
            residentChipIconImages[index].preserveAspect = true;
            residentChipIconImages[index].raycastTarget = false;
            residentChipIconTexts[index] = CreateText("IconText", iconRect, 10, TextAnchor.MiddleCenter, Color.white);
            residentChipIconTexts[index].fontStyle = FontStyle.Bold;
            SetOffsets(residentChipIconTexts[index].rectTransform, 0f, 0f, 0f, 0f);

            residentChipTexts[index] = CreateText("Text", rect, 11, TextAnchor.MiddleLeft, Color.white);
            residentChipTexts[index].fontStyle = FontStyle.Bold;
            residentChipTexts[index].resizeTextForBestFit = true;
            residentChipTexts[index].resizeTextMinSize = 8;
            residentChipTexts[index].resizeTextMaxSize = 11;
            SetOffsets(residentChipTexts[index].rectTransform, 32f, 0f, 7f, 0f);
        }

        private void CreateResidentRow(int index, string name, float top, float height)
        {
            RectTransform row = CreateUiObject(name, residentHudRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 0f, top, 0f, height);
            residentRowBackgrounds[index] = row.gameObject.AddComponent<Image>();
            residentRowBackgrounds[index].color = new Color(0.08f, 0.11f, 0.10f, 0.88f);
            residentRowBackgrounds[index].raycastTarget = false;

            RectTransform accent = CreateUiObject("Accent", row).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.offsetMin = Vector2.zero;
            accent.offsetMax = new Vector2(3f, 0f);
            residentRowAccentImages[index] = accent.gameObject.AddComponent<Image>();
            residentRowAccentImages[index].raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", row).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 12f, 15f, 36f, 36f);
            Image iconFrameImage = iconFrame.gameObject.AddComponent<Image>();
            iconFrameImage.color = new Color(1f, 1f, 1f, 0.06f);
            iconFrameImage.raycastTarget = false;
            residentRowIconImages[index] = CreateUiObject("Icon", iconFrame).AddComponent<Image>();
            residentRowIconImages[index].preserveAspect = true;
            residentRowIconImages[index].raycastTarget = false;
            SetOffsets(residentRowIconImages[index].rectTransform, 5f, 5f, 5f, 5f);
            residentRowIconTexts[index] = CreateText("IconText", iconFrame, 14, TextAnchor.MiddleCenter, new Color(0.95f, 0.78f, 0.40f));
            residentRowIconTexts[index].fontStyle = FontStyle.Bold;
            SetOffsets(residentRowIconTexts[index].rectTransform, 0f, 0f, 0f, 0f);

            residentRowTitleTexts[index] = CreateText("Title", row, 13, TextAnchor.UpperLeft, Color.white);
            residentRowTitleTexts[index].fontStyle = FontStyle.Bold;
            SetTopStretch(residentRowTitleTexts[index].rectTransform, 60f, 13f, 12f, 18f);
            residentRowBodyTexts[index] = CreateText("Body", row, 12, TextAnchor.UpperLeft, new Color(0.76f, 0.84f, 0.81f));
            residentRowBodyTexts[index].lineSpacing = 1.08f;
            SetTopStretch(residentRowBodyTexts[index].rectTransform, 60f, 34f, 12f, Mathf.Max(24f, height - 42f));
        }

        private void SetResidentChip(int index, string text, Sprite icon, string fallback, Color color)
        {
            residentChipBackgrounds[index].color = color;
            residentChipTexts[index].text = text;
            SetResidentIcon(residentChipIconImages[index], residentChipIconTexts[index], icon, fallback);
        }

        private void SetResidentRow(int index, string label, string title, string body, Sprite icon, string fallback, Color color)
        {
            residentRowBackgrounds[index].color = color;
            residentRowAccentImages[index].color = GetAccentColor(color);
            residentRowTitleTexts[index].text = label + " - " + title;
            residentRowBodyTexts[index].text = body;
            SetResidentIcon(residentRowIconImages[index], residentRowIconTexts[index], icon, fallback);
        }

        private static void SetResidentIcon(Image image, Text text, Sprite icon, string fallback)
        {
            image.sprite = icon;
            image.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            text.text = icon == null ? fallback : string.Empty;
        }

        private static Color GetAccentColor(Color color)
        {
            return new Color(
                Mathf.Min(1f, color.r + 0.36f),
                Mathf.Min(1f, color.g + 0.32f),
                Mathf.Min(1f, color.b + 0.18f),
                0.95f);
        }

        private static Sprite GetResidentProfessionIcon(StrategyResidentAgent resident)
        {
            return TryGetResidentProfession(resident, out StrategyProfessionType type)
                ? StrategyProfessionIconFactory.GetIcon(type)
                : null;
        }

        private static Sprite GetResidentHomeIcon(StrategyResidentAgent resident)
        {
            if (resident != null && (resident.Home != null || resident.ConstructionWillBecomeHome))
            {
                return StrategyBuildingSpriteFactory.TryGetBuildSprite(StrategyBuildTool.House, out Sprite sprite)
                    ? sprite
                    : null;
            }

            return StrategyCampfireSpriteFactory.GetFrame(0);
        }

        private static Sprite GetResidentPortraitIcon(StrategyResidentAgent resident)
        {
            return resident != null
                ? StrategyResidentSpriteFactory.GetPortraitSprite(resident.Gender, resident.VisualVariant, resident.LifeStage)
                : null;
        }

        private static string GetResidentHomeFallback(StrategyResidentAgent resident)
        {
            return resident != null && (resident.Home != null || resident.ConstructionWillBecomeHome) ? "H" : "C";
        }

        private static bool TryGetResidentProfession(StrategyResidentAgent resident, out StrategyProfessionType type)
        {
            type = StrategyProfessionType.Builder;
            if (resident == null || !resident.IsAdult)
            {
                return false;
            }

            if (resident.IsSettlementBuilder || resident.BuilderWorkplace != null || resident.ConstructionSite != null)
            {
                type = StrategyProfessionType.Builder;
            }
            else if (resident.Workplace != null)
            {
                type = StrategyProfessionType.Lumberjack;
            }
            else if (resident.StoneWorkplace != null)
            {
                type = StrategyProfessionType.Stonecutter;
            }
            else if (resident.MineWorkplace != null)
            {
                type = StrategyProfessionType.Miner;
            }
            else if (resident.CoalPitWorkplace != null)
            {
                type = StrategyProfessionType.CoalMiner;
            }
            else if (resident.ClayPitWorkplace != null)
            {
                type = StrategyProfessionType.ClayDigger;
            }
            else if (resident.SawmillWorkplace != null)
            {
                type = StrategyProfessionType.Sawyer;
            }
            else if (resident.KilnWorkplace != null)
            {
                type = StrategyProfessionType.Potter;
            }
            else if (resident.ForgeWorkplace != null)
            {
                type = StrategyProfessionType.Blacksmith;
            }
            else if (resident.HunterWorkplace != null)
            {
                type = StrategyProfessionType.Hunter;
            }
            else if (resident.FisherWorkplace != null)
            {
                type = StrategyProfessionType.Fisher;
            }
            else if (resident.ForagerWorkplace != null)
            {
                type = StrategyProfessionType.Forager;
            }
            else if (resident.IsSettlementHauler || resident.StorageWorkplace != null || resident.GranaryWorkplace != null)
            {
                type = StrategyProfessionType.StorageWorker;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static string GetResidentRoleLetter(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return "R";
            }

            if (!resident.IsAdult)
            {
                return "C";
            }

            return resident.IsHouseholder ? "H" : "S";
        }

        private static string GetResidentTaskDetail(StrategyResidentAgent resident)
        {
            if (resident.IsPendingRefugee)
            {
                return "Awaiting settlement decision";
            }

            if (!resident.IsAdult)
            {
                return "Too young for assigned work";
            }

            if (resident.HasWorkplace || resident.HasConstructionAssignment)
            {
                return "Assigned as " + GetResidentRoleTitle(resident);
            }

            return "Available for assignment";
        }

        private static string GetResidentHomeDetail(StrategyResidentAgent resident)
        {
            if (resident.Home != null)
            {
                return "Assigned to this home";
            }

            if (resident.ConstructionWillBecomeHome)
            {
                return "House is under construction";
            }

            return resident.IsPendingRefugee ? "Temporary refugee camp" : "Waiting for a home";
        }

        private static string GetResidentFoodDetail(StrategyResidentAgent resident)
        {
            if (!resident.IsHungry)
            {
                return "Rations are currently covered";
            }

            return resident.NutritionStatusText + " for " + resident.DaysHungry + "d";
        }

        private static string GetResidentFamilyDetail(StrategyResidentAgent resident)
        {
            int parentCount = (resident.FatherId > 0 ? 1 : 0) + (resident.MotherId > 0 ? 1 : 0);
            return "Parents recorded: "
                + parentCount
                + " | Children: "
                + resident.ChildIds.Count;
        }

        private static Color GetResidentTaskColor(StrategyResidentAgent resident)
        {
            if (resident.Activity == StrategyResidentAgent.ResidentActivity.Idle && resident.HasWorkplace)
            {
                return new Color(0.22f, 0.17f, 0.09f, 0.94f);
            }

            return new Color(0.10f, 0.15f, 0.16f, 0.94f);
        }

        private static Color GetResidentHomeColor(StrategyResidentAgent resident)
        {
            if (resident.Home != null || resident.ConstructionWillBecomeHome)
            {
                return new Color(0.12f, 0.20f, 0.17f, 0.94f);
            }

            return new Color(0.23f, 0.16f, 0.09f, 0.94f);
        }

        private static Color GetResidentFoodColor(StrategyResidentAgent resident)
        {
            if (resident.IsStarving)
            {
                return new Color(0.28f, 0.08f, 0.08f, 0.96f);
            }

            return resident.IsHungry
                ? new Color(0.27f, 0.18f, 0.08f, 0.96f)
                : new Color(0.12f, 0.20f, 0.13f, 0.94f);
        }

        private static string ToSentenceCase(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
