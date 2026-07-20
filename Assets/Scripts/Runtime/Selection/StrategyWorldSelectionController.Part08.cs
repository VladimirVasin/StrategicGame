using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private static bool IsAuxiliaryInspectRenderer(SpriteRenderer renderer)
        {
            string objectName = renderer.gameObject.name;
            return objectName.Contains("Shadow")
                || objectName.Contains("Outline")
                || objectName.Contains("Line")
                || objectName.Contains("Damage")
                || objectName.Contains("Readability");
        }

        private StrategyWorldInspectInfo BuildGraveInspectInfo(StrategyGraveMarker grave)
        {
            bool hasCell = TryGetCellForWorld(grave.transform.position, out Vector2Int cell);
            string body = L(
                "grave.inspect_body",
                grave.DeceasedName,
                grave.GetLifeText().Replace("\n", " / "));
            return new StrategyWorldInspectInfo(
                L("grave.subtitle"),
                LocalizedValue(grave.FamilyRole),
                body,
                grave.PreviewSprite,
                cell,
                hasCell);
        }

        private static string GetBuildingTitle(StrategyBuildTool tool)
        {
            return StrategySelectionLocalization.Building(tool);
        }
    }
}
