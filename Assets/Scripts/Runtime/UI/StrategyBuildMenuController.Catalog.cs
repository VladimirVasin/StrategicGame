using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildMenuCatalog
    {
        private static Color HtmlColor(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
        }

        internal static StrategyBuildMenuControllerDriver.BuildCategoryData[] CreateCatalog()
        {
            return new[]
            {
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "\u0416\u0438\u043b\u0438\u0449\u0430",
                    HtmlColor("#A97845"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.House, "HM", "\u0414\u043e\u043c", new StrategyConstructionResourceCost(2, 1), HtmlColor("#C39B69"))
                    }),
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "\u041f\u0440\u043e\u043c\u044b\u0441\u043b\u044b",
                    HtmlColor("#4F7A47"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.LumberjackCamp, "LC", "\u041b\u0430\u0433\u0435\u0440\u044c \u0434\u0440\u043e\u0432\u043e\u0441\u0435\u043a\u043e\u0432", new StrategyConstructionResourceCost(4, 2), HtmlColor("#7D8E4A")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.StonecutterCamp, "SC", "\u041b\u0430\u0433\u0435\u0440\u044c \u043a\u0430\u043c\u0435\u043d\u043e\u0442\u0451\u0441\u043e\u0432", new StrategyConstructionResourceCost(3, 4), HtmlColor("#7B8582")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.HunterCamp, "HC", "\u041b\u0430\u0433\u0435\u0440\u044c \u043e\u0445\u043e\u0442\u043d\u0438\u043a\u043e\u0432", new StrategyConstructionResourceCost(4, 2), HtmlColor("#7B6A45")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.FisherHut, "FH", "\u0425\u0438\u0436\u0438\u043d\u0430 \u0440\u044b\u0431\u0430\u043a\u0430", new StrategyConstructionResourceCost(4, 2), HtmlColor("#4F7E8A"))
                    }),
                new StrategyBuildMenuControllerDriver.BuildCategoryData(
                    "\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0430",
                    HtmlColor("#78624B"),
                    new[]
                    {
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.StorageYard, "ST", "\u0421\u043a\u043b\u0430\u0434", new StrategyConstructionResourceCost(6, 4), HtmlColor("#9B8061")),
                        new StrategyBuildMenuControllerDriver.BuildItemData(StrategyBuildTool.Granary, "GR", "\u0410\u043c\u0431\u0430\u0440", new StrategyConstructionResourceCost(5, 3), HtmlColor("#A8874D"))
                    })
            };
        }

        internal static void DrawBuildIcon(RectTransform parent, Color beam, Color metal)
        {
            DrawRect(parent, "Beam", new Vector2(0.50f, 0.50f), new Vector2(8f, 30f), beam, -42f);
            DrawRect(parent, "Head", new Vector2(0.34f, 0.68f), new Vector2(24f, 8f), metal, -42f);
            DrawRect(parent, "Tip", new Vector2(0.64f, 0.30f), new Vector2(18f, 8f), metal, -42f);
        }

        internal static void DrawItemIcon(RectTransform parent, StrategyBuildMenuControllerDriver.BuildItemData data)
        {
            if (StrategyBuildingSpriteFactory.TryGetBuildSprite(data.Tool, out Sprite sprite))
            {
                RectTransform spriteRoot = StrategyBuildMenuControllerDriver.CreateUiObject("Sprite", parent).GetComponent<RectTransform>();
                StrategyBuildMenuControllerDriver.Stretch(spriteRoot, -4f, -2f, -4f, -2f);

                Image image = spriteRoot.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            Text label = StrategyBuildMenuControllerDriver.CreateText("Abbrev", parent, data.Abbrev, 18, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            StrategyBuildMenuControllerDriver.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
        }

        private static void DrawRect(RectTransform parent, string name, Vector2 anchor, Vector2 size, Color color, float rotation)
        {
            RectTransform rect = StrategyBuildMenuControllerDriver.CreateUiObject(name, parent).GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
