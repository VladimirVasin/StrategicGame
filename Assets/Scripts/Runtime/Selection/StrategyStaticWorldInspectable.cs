using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStaticWorldInspectable : MonoBehaviour, IStrategyWorldInspectable
    {
        private string title = "Object";
        private string subtitle = string.Empty;
        private string body = string.Empty;
        private string titleKey = string.Empty;
        private string subtitleKey = string.Empty;
        private string bodyKey = string.Empty;
        private string[] bodyValueArguments = System.Array.Empty<string>();
        private Sprite icon;
        private Vector2Int cell;
        private bool hasCell;

        public void Configure(string inspectTitle, string inspectSubtitle, string inspectBody, Sprite inspectIcon, Vector2Int inspectCell, bool inspectHasCell)
        {
            titleKey = string.Empty;
            subtitleKey = string.Empty;
            bodyKey = string.Empty;
            bodyValueArguments = System.Array.Empty<string>();
            title = string.IsNullOrWhiteSpace(inspectTitle) ? "Object" : inspectTitle;
            subtitle = inspectSubtitle ?? string.Empty;
            body = inspectBody ?? string.Empty;
            icon = inspectIcon;
            cell = inspectCell;
            hasCell = inspectHasCell;
        }

        public void ConfigureLocalized(
            string inspectTitleKey,
            string inspectSubtitleKey,
            string inspectBodyKey,
            string[] inspectBodyValueArguments,
            Sprite inspectIcon,
            Vector2Int inspectCell,
            bool inspectHasCell)
        {
            titleKey = inspectTitleKey ?? string.Empty;
            subtitleKey = inspectSubtitleKey ?? string.Empty;
            bodyKey = inspectBodyKey ?? string.Empty;
            bodyValueArguments = inspectBodyValueArguments ?? System.Array.Empty<string>();
            icon = inspectIcon;
            cell = inspectCell;
            hasCell = inspectHasCell;
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            string resolvedTitle = string.IsNullOrEmpty(titleKey)
                ? StrategyLocalization.TranslateLiteral(title)
                : StrategySelectionLocalization.Text(titleKey);
            string resolvedSubtitle = string.IsNullOrEmpty(subtitleKey)
                ? StrategyLocalization.TranslateLiteral(subtitle)
                : StrategySelectionLocalization.Text(subtitleKey);
            string resolvedBody = string.IsNullOrEmpty(bodyKey)
                ? StrategyLocalization.TranslateLiteral(body)
                : ResolveLocalizedBody();
            info = new StrategyWorldInspectInfo(resolvedTitle, resolvedSubtitle, resolvedBody, icon, cell, hasCell);
            return true;
        }

        private string ResolveLocalizedBody()
        {
            object[] arguments = new object[bodyValueArguments.Length];
            for (int i = 0; i < bodyValueArguments.Length; i++)
            {
                arguments[i] = StrategySelectionLocalization.Value(bodyValueArguments[i]);
            }

            return StrategySelectionLocalization.Text(bodyKey, arguments);
        }
    }
}
