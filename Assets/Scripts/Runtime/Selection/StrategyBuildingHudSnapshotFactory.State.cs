namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingHudSnapshotFactory
    {
        private static void FillBridge(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            int span = building.BridgeCells != null ? building.BridgeCells.Count : 0;
            snapshot.AddChip(
                "state",
                L("label.state"),
                LocalizedValue("Operational"),
                GetBuildingIcon(StrategyBuildTool.Bridge),
                StrategyBuildingHudTone.Positive);
            snapshot.AddChip(
                "span",
                L("label.span"),
                L("format.cells", span),
                GetBuildingIcon(StrategyBuildTool.Bridge),
                StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "footprint",
                L("label.footprint"),
                building.Footprint.x + "x" + building.Footprint.y,
                GetBuildingIcon(StrategyBuildTool.Bridge));

            StrategyBuildingHudSection infrastructure = snapshot.AddSection(
                "infrastructure",
                L("section.infrastructure"));
            infrastructure.AddRow(
                "start",
                L("label.start_cell"),
                FormatCell(building.BridgeStartCell),
                null);
            infrastructure.AddRow(
                "end",
                L("label.end_cell"),
                FormatCell(building.BridgeEndCell),
                null);
            snapshot.SetStatus(
                L("bridge.open_title"),
                L("bridge.open_body"),
                StrategyBuildingHudTone.Positive);
        }

        private static void FillOperationalBuilding(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.AddChip(
                "state",
                L("label.state"),
                LocalizedValue("Operational"),
                GetBuildingIcon(building.Tool),
                StrategyBuildingHudTone.Positive);
            StrategyBuildingHudSection overview = snapshot.AddSection(
                "overview",
                L("section.overview"));
            overview.AddRow(
                "footprint",
                L("label.footprint"),
                building.Footprint.x + "x" + building.Footprint.y,
                GetBuildingIcon(building.Tool));
        }

        private static void FillDemolition(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.AddChip(
                "state",
                L("label.state"),
                L("demolition.queued_title"),
                GetBuildingIcon(building.Tool),
                StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "footprint",
                L("label.footprint"),
                building.Footprint.x + "x" + building.Footprint.y,
                GetBuildingIcon(building.Tool));

            StrategyBuildingHudSection overview = snapshot.AddSection(
                "demolition",
                L("section.demolition"));
            overview.AddRow(
                "status",
                L("label.current_state"),
                LocalizedValue("Queued"),
                GetBuildingIcon(building.Tool),
                StrategyBuildingHudTone.Warning,
                L("demolition.visible_until_complete"));
            snapshot.SetStatus(
                L("demolition.queued_title"),
                L("demolition.actions_unavailable"),
                StrategyBuildingHudTone.Warning);
        }
    }
}
