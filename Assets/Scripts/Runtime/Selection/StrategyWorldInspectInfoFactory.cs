using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyWorldInspectInfoFactory
    {
        private static readonly Color Neutral = new Color(0.11f, 0.15f, 0.15f, 0.94f);
        private static readonly Color Good = new Color(0.12f, 0.22f, 0.16f, 0.94f);
        private static readonly Color Warn = new Color(0.26f, 0.17f, 0.08f, 0.94f);
        private static readonly Color Danger = new Color(0.30f, 0.09f, 0.08f, 0.94f);
        private static readonly Color WildlifeAccent = new Color(0.72f, 0.57f, 0.32f, 1f);
        private static readonly Color ResourceAccent = new Color(0.54f, 0.70f, 0.46f, 1f);
        private static readonly Color DepositAccent = new Color(0.72f, 0.63f, 0.48f, 1f);
        private static readonly Color PredatorAccent = new Color(0.66f, 0.36f, 0.31f, 1f);

        public static StrategyWorldInspectInfo CreateRabbit(StrategyRabbitAgent rabbit, Sprite icon, Vector2Int cell, bool hasCell)
        {
            bool carcass = rabbit.IsCarcass;
            string state = rabbit.State.ToString();
            return Create(
                L(carcass ? "inspect.rabbit_carcass" : "inspect.rabbit"),
                L("inspect.wildlife"),
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[]
                {
                    Chip(V(rabbit.LifeStage), null, Neutral),
                    Chip(V(rabbit.Sex), null, Neutral),
                    Chip(V(rabbit.CanBeHunted ? "Huntable" : "Protected"), null, rabbit.CanBeHunted ? Good : Warn)
                },
                new[]
                {
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(L("label.group"), rabbit.GroupId.ToString(), null, Neutral),
                    Row(L("label.home"), FormatCell(rabbit.HomeCell), null, Neutral),
                    Row(L("label.radius"), rabbit.HomeRadius.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateDeer(StrategyDeerAgent deer, Sprite icon, Vector2Int cell, bool hasCell)
        {
            string state = deer.State.ToString();
            return Create(
                L(deer.Sex == StrategyDeerSex.Male ? "inspect.stag" : "inspect.doe"),
                L("inspect.wildlife"),
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[]
                {
                    Chip(V(deer.LifeStage), null, Neutral),
                    Chip(V(deer.Sex), null, Neutral),
                    Chip(V(deer.CanBeWolfPrey ? "Wolf prey" : "Safe"), null, deer.CanBeWolfPrey ? Warn : Good)
                },
                new[]
                {
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(L("label.herd"), deer.HerdId.ToString(), null, Neutral),
                    Row(L("label.home"), FormatCell(deer.HomeCell), null, Neutral),
                    Row(L("label.radius"), deer.HomeRadius.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateFish(StrategyFishAgent fish, Sprite icon, Vector2Int cell, bool hasCell)
        {
            string state = fish.State.ToString();
            return Create(
                StrategySelectionLocalization.Resource(StrategyResourceType.Fish),
                L(fish.HabitatKind == StrategyFishHabitatKind.River ? "inspect.river_wildlife" : "inspect.lake_wildlife"),
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                new Color(0.38f, 0.62f, 0.70f, 1f),
                new[]
                {
                    Chip(V(fish.Species), null, Neutral),
                    Chip(V(fish.HabitatKind), null, Neutral),
                    Chip(V(fish.CanBeFished ? "Fishable" : "Unavailable"), null, fish.CanBeFished ? Good : Warn)
                },
                new[]
                {
                    Row(L("label.stage"), V(fish.LifeStage), null, Neutral),
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(L("label.shoal"), fish.ShoalId.ToString(), null, Neutral),
                    Row(L("label.region"), fish.WaterRegionId >= 0 ? fish.WaterRegionId.ToString() : V("none"), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateBird(StrategyBirdAgent bird, Sprite icon, Vector2Int cell, bool hasCell)
        {
            string state = bird.State.ToString();
            return Create(
                V(bird.Species),
                L("inspect.decorative_wildlife"),
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[]
                {
                    Chip(V(bird.Species), null, Neutral),
                    Chip(V(state), null, StateColor(state)),
                    Chip(L("format.home_radius", bird.HomeRadius), null, Neutral)
                },
                new[]
                {
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(L("label.home"), FormatCell(bird.HomeCell), null, Neutral),
                    Row(L("label.radius"), bird.HomeRadius.ToString(), null, Neutral),
                    Row(L("label.id"), bird.BirdId.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateWolf(StrategyWolfAgent wolf, Sprite icon, Vector2Int cell, bool hasCell)
        {
            string state = wolf.State.ToString();
            return Create(
                L("inspect.wolf"),
                L("inspect.predator_wildlife"),
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                PredatorAccent,
                new[]
                {
                    Chip(L("format.pack", wolf.PackId), null, Neutral),
                    Chip(L("format.size", wolf.PackMemberCount), null, Neutral),
                    Chip(V(state), null, StateColor(state))
                },
                new[]
                {
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(L("label.pack"), wolf.PackId.ToString(), null, Neutral),
                    Row(L("label.home"), FormatCell(wolf.HomeCell), null, Neutral),
                    Row(L("label.radius"), wolf.HomeRadius.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateChicken(string state, string coop, Sprite icon, Vector2Int cell, bool hasCell)
        {
            return Create(
                L("inspect.chicken"),
                L("inspect.household_animal"),
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[] { Chip(V(state), null, StateColor(state)), Chip(L("label.coop"), null, Neutral) },
                new[] { Row(L("label.state"), V(state), null, StateColor(state)), Row(L("label.coop"), coop == "none" ? V(coop) : coop, null, Neutral) });
        }

        public static StrategyWorldInspectInfo CreateStoneDeposit(StrategyStoneDeposit deposit, Sprite icon)
        {
            return CreateDeposit(
                GetStoneTitle(deposit.Kind),
                L("inspect.stone_deposit"),
                icon,
                StrategyResourceType.Stone,
                deposit.StoneAmount,
                deposit.Footprint,
                deposit.IsDepleted ? "depleted" : deposit.IsReserved ? "reserved" : "available",
                "blocked",
                "blocked",
                deposit.Cell);
        }

        public static StrategyWorldInspectInfo CreateIronDeposit(StrategyIronDeposit deposit, Sprite icon)
        {
            return CreateDeposit(
                L(deposit.Kind == StrategyIronDepositKind.IronVein ? "inspect.iron_vein" : "inspect.iron_stained_ground"),
                L("inspect.iron_deposit"),
                icon,
                StrategyResourceType.Iron,
                deposit.IronAmount,
                deposit.Footprint,
                deposit.IsDepleted ? "depleted" : deposit.IsReserved ? "reserved" : "mineable",
                "free",
                "Mine only",
                deposit.Cell);
        }

        public static StrategyWorldInspectInfo CreateCoalDeposit(StrategyCoalDeposit deposit, Sprite icon)
        {
            return CreateDeposit(
                L(deposit.Kind == StrategyCoalDepositKind.CoalSeam ? "inspect.coal_seam" : "inspect.coal_dust_ground"),
                L("inspect.coal_deposit"),
                icon,
                StrategyResourceType.Coal,
                deposit.CoalAmount,
                deposit.Footprint,
                deposit.IsDepleted ? "depleted" : deposit.IsReserved ? "reserved" : "mineable",
                "free",
                "Coal Pit only",
                deposit.Cell);
        }

        public static StrategyWorldInspectInfo CreateClayDeposit(StrategyClayDeposit deposit, Sprite icon)
        {
            return CreateDeposit(
                L(deposit.Kind == StrategyClayDepositKind.ClayBank ? "inspect.clay_bank" : "inspect.clay_patch"),
                L("inspect.clay_deposit"),
                icon,
                StrategyResourceType.Clay,
                deposit.ClayAmount,
                deposit.Footprint,
                deposit.IsDepleted ? "depleted" : deposit.IsReserved ? "reserved" : "available",
                "free",
                "blocked",
                deposit.Cell);
        }

        public static StrategyWorldInspectInfo CreateTree(StrategyForestryTree tree, Sprite icon)
        {
            string state = tree.HasLogsReady ? "logs ready" : tree.IsFalling ? "falling" : tree.IsFelled ? "felled" : tree.CanBeChopped ? "standing" : "growing";
            return Create(
                L(tree.IsMature ? "inspect.tree" : "inspect.young_tree"),
                L("inspect.forest_resource"),
                icon,
                tree.Cell,
                true,
                StrategyWorldInspectKind.Tree,
                ResourceAccent,
                new[]
                {
                    Chip(GetTreeStage(tree.Stage), null, Neutral),
                    Chip(V(state), null, StateColor(state)),
                    Chip(V(tree.IsReserved ? "Reserved" : "Available"), null, tree.IsReserved ? Warn : Good)
                },
                new[]
                {
                    Row(L("label.stage"), GetTreeStage(tree.Stage), null, Neutral),
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(StrategySelectionLocalization.Resource(StrategyResourceType.Logs), tree.HasLogsReady ? tree.LogYield.ToString() : V("not ready"), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs), Neutral),
                    Row(L("label.reserved"), YesNo(tree.IsReserved), null, tree.IsReserved ? Warn : Good)
                });
        }

        public static StrategyWorldInspectInfo CreateForage(string title, StrategyResourceType type, int yield, bool depleted, bool reserved, Sprite icon, Vector2Int cell)
        {
            string state = depleted ? "regrowing" : reserved ? "reserved" : "ready";
            string resourceTitle = StrategySelectionLocalization.Resource(type);
            return Create(
                resourceTitle,
                L("inspect.forage_node"),
                icon,
                cell,
                true,
                StrategyWorldInspectKind.Resource,
                ResourceAccent,
                new[] { Chip(V(state), null, StateColor(state)), Chip(L("format.yield", yield), StrategyResourceIconFactory.GetSprite(type), Neutral) },
                new[]
                {
                    Row(L("label.resource"), resourceTitle, StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row(L("label.yield"), yield.ToString(), StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(L("label.use"), V("household food"), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateLooseResource(StrategyResourceType type, int amount, bool reserved, Sprite icon, Vector2Int cell)
        {
            string title = GetResourceTitle(type);
            return Create(
                L("format.dropped_resource", title),
                L("inspect.loose_resource"),
                icon != null ? icon : StrategyResourceIconFactory.GetSprite(type),
                cell,
                true,
                StrategyWorldInspectKind.LoosePile,
                ResourceAccent,
                new[] { Chip(title, StrategyResourceIconFactory.GetSprite(type), Neutral), Chip(V(reserved ? "Reserved" : "Available"), null, reserved ? Warn : Good) },
                new[]
                {
                    Row(L("label.resource"), title, StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row(L("label.amount"), amount.ToString(), StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row(L("label.state"), V(reserved ? "reserved" : "available"), null, reserved ? Warn : Good)
                });
        }

        public static StrategyWorldInspectInfo CreateLooseConstructionPile(int logs, int stone, int planks, int availableLogs, int availableStone, int availablePlanks, Sprite icon, Vector2Int cell)
        {
            return Create(
                L("inspect.loose_building_materials"),
                L("inspect.construction_resource_pile"),
                icon,
                cell,
                true,
                StrategyWorldInspectKind.LoosePile,
                ResourceAccent,
                new[] { Chip(L("format.resource_amount", GetResourceTitle(StrategyResourceType.Logs), logs), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs), Neutral), Chip(L("format.resource_amount", GetResourceTitle(StrategyResourceType.Stone), stone), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Stone), Neutral), Chip(L("format.resource_amount", GetResourceTitle(StrategyResourceType.Planks), planks), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Planks), Neutral) },
                new[]
                {
                    Row(GetResourceTitle(StrategyResourceType.Logs), L("format.amount_free", logs, availableLogs), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs), logs > 0 ? Good : Neutral),
                    Row(GetResourceTitle(StrategyResourceType.Stone), L("format.amount_free", stone, availableStone), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Stone), stone > 0 ? Good : Neutral),
                    Row(GetResourceTitle(StrategyResourceType.Planks), L("format.amount_free", planks, availablePlanks), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Planks), planks > 0 ? Good : Neutral)
                });
        }

        private static StrategyWorldInspectInfo CreateDeposit(string title, string subtitle, Sprite icon, StrategyResourceType resource, int amount, Vector2Int footprint, string state, string walk, string build, Vector2Int cell)
        {
            Sprite resourceIcon = StrategyResourceIconFactory.GetSprite(resource);
            return Create(
                title,
                subtitle,
                icon,
                cell,
                true,
                StrategyWorldInspectKind.Deposit,
                DepositAccent,
                new[]
                {
                    Chip(L("format.resource_amount", GetResourceTitle(resource), amount), resourceIcon, Neutral),
                    Chip(V(state), null, StateColor(state)),
                    Chip(L("format.build_rule", V(build)), null, Warn)
                },
                new[]
                {
                    Row(L("label.amount"), amount.ToString(), resourceIcon, amount > 0 ? Good : Neutral),
                    Row(L("label.footprint"), footprint.x + "x" + footprint.y, null, Neutral),
                    Row(L("label.state"), V(state), null, StateColor(state)),
                    Row(L("label.walk_build"), L("format.walk_build", V(walk), V(build)), null, build == "blocked" ? Danger : Warn)
                });
        }

        private static StrategyWorldInspectInfo Create(string title, string subtitle, Sprite icon, Vector2Int cell, bool hasCell, StrategyWorldInspectKind kind, Color accent, StrategyWorldInspectChip[] chips, StrategyWorldInspectRow[] rows)
        {
            return new StrategyWorldInspectInfo(title, subtitle, BuildBody(rows), icon, cell, hasCell, kind, accent, chips, rows);
        }

        private static StrategyWorldInspectChip Chip(string label, Sprite icon, Color color) => new(label, icon, color);
        private static StrategyWorldInspectRow Row(string label, string value, Sprite icon, Color color) => new(label, value, icon, color);
        private static string FormatCell(Vector2Int cell) => cell.x + ", " + cell.y;
        private static string YesNo(bool value) => V(value ? "yes" : "no");

        private static Color StateColor(string state)
        {
            string lower = state != null ? state.ToLowerInvariant() : string.Empty;
            if (lower.Contains("flee") || lower.Contains("dead") || lower.Contains("depleted"))
            {
                return Danger;
            }

            if (lower.Contains("reserved") || lower.Contains("hunted") || lower.Contains("hooked") || lower.Contains("blocked"))
            {
                return Warn;
            }

            return lower.Contains("ready") || lower.Contains("available") || lower.Contains("standing") ? Good : Neutral;
        }

        private static string BuildBody(StrategyWorldInspectRow[] rows)
        {
            if (rows == null || rows.Length <= 0)
            {
                return string.Empty;
            }

            string body = string.Empty;
            for (int i = 0; i < rows.Length; i++)
            {
                StrategyWorldInspectRow row = rows[i];
                if (!row.IsValid)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(body))
                {
                    body += "\n";
                }

                body += row.Label + ": " + row.Value;
            }

            return body;
        }

        private static string GetTreeStage(int stage) =>
            V(stage <= 0 ? "Sapling" : stage == 1 ? "Young" : "Mature");

        private static string GetStoneTitle(StrategyStoneDepositKind kind)
        {
            return kind switch
            {
                StrategyStoneDepositKind.RockCluster => L("inspect.rock_cluster"),
                StrategyStoneDepositKind.Cliff => L("inspect.stone_cliff"),
                _ => L("inspect.boulder")
            };
        }

        private static string GetResourceTitle(StrategyResourceType type) =>
            StrategySelectionLocalization.Resource(type);

        private static string L(string key, params object[] arguments) =>
            StrategySelectionLocalization.Text(key, arguments);

        private static string V(Enum value) => StrategySelectionLocalization.Value(value);
        private static string V(string value) => StrategySelectionLocalization.Value(value);
    }
}
