using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStaticWorldInspectable : MonoBehaviour, IStrategyWorldInspectable
    {
        private string title = "Object";
        private string subtitle = string.Empty;
        private string body = string.Empty;
        private Sprite icon;
        private Vector2Int cell;
        private bool hasCell;

        public void Configure(string inspectTitle, string inspectSubtitle, string inspectBody, Sprite inspectIcon, Vector2Int inspectCell, bool inspectHasCell)
        {
            title = string.IsNullOrWhiteSpace(inspectTitle) ? "Object" : inspectTitle;
            subtitle = inspectSubtitle ?? string.Empty;
            body = inspectBody ?? string.Empty;
            icon = inspectIcon;
            cell = inspectCell;
            hasCell = inspectHasCell;
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            info = new StrategyWorldInspectInfo(title, subtitle, body, icon, cell, hasCell);
            return true;
        }
    }
}
