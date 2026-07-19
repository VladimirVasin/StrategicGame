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
                "State",
                "Operational",
                GetBuildingIcon(StrategyBuildTool.Bridge),
                StrategyBuildingHudTone.Positive);
            snapshot.AddChip(
                "span",
                "Span",
                span + " cells",
                GetBuildingIcon(StrategyBuildTool.Bridge),
                StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "footprint",
                "Footprint",
                building.Footprint.x + "x" + building.Footprint.y,
                GetBuildingIcon(StrategyBuildTool.Bridge));

            StrategyBuildingHudSection infrastructure = snapshot.AddSection(
                "infrastructure",
                "Infrastructure");
            infrastructure.AddRow(
                "start",
                "Start cell",
                FormatCell(building.BridgeStartCell),
                null);
            infrastructure.AddRow(
                "end",
                "End cell",
                FormatCell(building.BridgeEndCell),
                null);
            snapshot.SetStatus(
                "Crossing open",
                "Residents can use this span as part of the walkable network.",
                StrategyBuildingHudTone.Positive);
        }

        private static void FillOperationalBuilding(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.AddChip(
                "state",
                "State",
                "Operational",
                GetBuildingIcon(building.Tool),
                StrategyBuildingHudTone.Positive);
            StrategyBuildingHudSection overview = snapshot.AddSection(
                "overview",
                "Overview");
            overview.AddRow(
                "footprint",
                "Footprint",
                building.Footprint.x + "x" + building.Footprint.y,
                GetBuildingIcon(building.Tool));
        }

        private static void FillDemolition(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.AddChip(
                "state",
                "State",
                "Demolition queued",
                GetBuildingIcon(building.Tool),
                StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "footprint",
                "Footprint",
                building.Footprint.x + "x" + building.Footprint.y,
                GetBuildingIcon(building.Tool));

            StrategyBuildingHudSection overview = snapshot.AddSection(
                "demolition",
                "Demolition");
            overview.AddRow(
                "status",
                "Current state",
                "Queued",
                GetBuildingIcon(building.Tool),
                StrategyBuildingHudTone.Warning,
                "The building remains visible until the demolition flow completes.");
            snapshot.SetStatus(
                "Demolition queued",
                "Trade, Scout and upgrade actions are unavailable while removal is pending.",
                StrategyBuildingHudTone.Warning);
        }
    }
}
