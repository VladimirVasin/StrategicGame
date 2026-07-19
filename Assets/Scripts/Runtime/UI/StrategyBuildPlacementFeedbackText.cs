using System;

namespace ProjectUnknown.Strategy
{
    public static class StrategyBuildPlacementFeedbackText
    {
        public static string FormatFailureReason(string rawReason)
        {
            if (string.IsNullOrWhiteSpace(rawReason))
            {
                return "Choose a buildable location.";
            }

            string reason = rawReason;
            int coordinateIndex = reason.IndexOf('@');
            if (coordinateIndex >= 0)
            {
                reason = reason.Substring(0, coordinateIndex);
            }

            if (reason.Contains("unexplored", StringComparison.Ordinal))
            {
                return "Explore this area before building here.";
            }

            if (reason.Contains("occupied", StringComparison.Ordinal))
            {
                return "Another structure already occupies this space.";
            }

            if (reason.Contains("out_of_bounds", StringComparison.Ordinal))
            {
                return "Move the whole footprint inside the map.";
            }

            if (reason.Contains("terrain_Water", StringComparison.Ordinal)
                || reason is "bank_is_water" or "span_not_river")
            {
                return "This building needs suitable land or water access.";
            }

            return reason switch
            {
                "not_affordable" => "Not enough construction materials.",
                "no_builder_access" => "Builders cannot reach this site.",
                "resident_in_footprint" => "A resident is standing in the footprint.",
                "no_water_access" => "The Fisher Hut needs accessible fishing water.",
                "no_iron_deposit_under_mine" => "Place the Mine over a discovered Iron deposit.",
                "no_coal_deposit_under_pit" => "Place the Coal Pit over a discovered Coal deposit.",
                "no_clay_deposit_under_pit" => "Place the Clay Pit over a Clay field.",
                "bank_not_walkable" => "The bridge bank must be walkable.",
                "bank_not_buildable" => "The bridge bank is not buildable.",
                "bank_unexplored" => "Explore both bridge banks first.",
                "no_adjacent_river" => "Start the bridge beside a river.",
                "span_occupied" => "The bridge span is blocked.",
                "span_unexplored" => "Explore the whole bridge span first.",
                "no_river_water_span" => "The bridge must cross at least one river cell.",
                "no_opposite_river_bank" => "No valid opposite bank is in range.",
                "not_a_suggested_bank" => "Choose one of the highlighted opposite banks.",
                _ when reason.Contains("not_buildable", StringComparison.Ordinal)
                    || reason.Contains("not_walkable", StringComparison.Ordinal)
                    => "Terrain blocks construction here.",
                _ => "Choose a valid, reachable location."
            };
        }
    }
}
