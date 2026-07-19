using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHudChromeController : MonoBehaviour
    {
        private bool initialized;

        public void Configure()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            BuildUi();
        }

        private void Awake()
        {
            Configure();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new(
                "StrategyHudChromeCanvas",
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8;
            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            RectTransform topRail = CreateRect("HearthLedgerTopRail", canvasObject.transform);
            topRail.anchorMin = new Vector2(0f, 1f);
            topRail.anchorMax = new Vector2(1f, 1f);
            topRail.pivot = new Vector2(0.5f, 1f);
            topRail.offsetMin = new Vector2(0f, -StrategyHudStyle.TopRailHeight);
            topRail.offsetMax = Vector2.zero;
            Image topRailImage = topRail.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(topRailImage, new Color(
                StrategyHudStyle.Background.r,
                StrategyHudStyle.Background.g,
                StrategyHudStyle.Background.b,
                0.97f));
            StrategyHudStyle.AddShadow(topRail.gameObject, 0.58f);

            AddVerticalSeparator(topRail, 292f);
            AddVerticalSeparator(topRail, 450f);
            AddVerticalSeparator(topRail, 604f);
            AddVerticalSeparator(topRail, 808f);
            AddVerticalSeparator(topRail, 968f);

            RectTransform bottomLine = CreateRect("TopRailBrassLine", topRail);
            bottomLine.anchorMin = Vector2.zero;
            bottomLine.anchorMax = new Vector2(1f, 0f);
            bottomLine.pivot = new Vector2(0.5f, 0f);
            bottomLine.offsetMin = Vector2.zero;
            bottomLine.offsetMax = new Vector2(0f, 2f);
            Image bottomLineImage = bottomLine.gameObject.AddComponent<Image>();
            bottomLineImage.color = new Color(
                StrategyHudStyle.Primary.r,
                StrategyHudStyle.Primary.g,
                StrategyHudStyle.Primary.b,
                0.72f);
            bottomLineImage.raycastTarget = false;
        }

        private static void AddVerticalSeparator(RectTransform parent, float x)
        {
            RectTransform line = CreateRect("RailDivider", parent);
            line.anchorMin = new Vector2(0f, 0.5f);
            line.anchorMax = new Vector2(0f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = new Vector2(x, 0f);
            line.sizeDelta = new Vector2(1f, 42f);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = new Color(StrategyHudStyle.Primary.r, StrategyHudStyle.Primary.g, StrategyHudStyle.Primary.b, 0.42f);
            image.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }
    }
}
