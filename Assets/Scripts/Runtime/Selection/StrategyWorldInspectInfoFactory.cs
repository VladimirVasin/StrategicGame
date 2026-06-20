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
            return Create(
                carcass ? "Rabbit Carcass" : "Rabbit",
                "Wildlife",
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[]
                {
                    Chip(rabbit.LifeStage.ToString(), null, Neutral),
                    Chip(rabbit.Sex.ToString(), null, Neutral),
                    Chip(rabbit.CanBeHunted ? "Huntable" : "Protected", null, rabbit.CanBeHunted ? Good : Warn)
                },
                new[]
                {
                    Row("State", rabbit.State.ToString(), null, StateColor(rabbit.State.ToString())),
                    Row("Group", rabbit.GroupId.ToString(), null, Neutral),
                    Row("Home", FormatCell(rabbit.HomeCell), null, Neutral),
                    Row("Radius", rabbit.HomeRadius.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateDeer(StrategyDeerAgent deer, Sprite icon, Vector2Int cell, bool hasCell)
        {
            return Create(
                deer.Sex == StrategyDeerSex.Male ? "Stag" : "Doe",
                "Wildlife",
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[]
                {
                    Chip(deer.LifeStage.ToString(), null, Neutral),
                    Chip(deer.Sex.ToString(), null, Neutral),
                    Chip(deer.CanBeWolfPrey ? "Wolf prey" : "Safe", null, deer.CanBeWolfPrey ? Warn : Good)
                },
                new[]
                {
                    Row("State", deer.State.ToString(), null, StateColor(deer.State.ToString())),
                    Row("Herd", deer.HerdId.ToString(), null, Neutral),
                    Row("Home", FormatCell(deer.HomeCell), null, Neutral),
                    Row("Radius", deer.HomeRadius.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateFish(StrategyFishAgent fish, Sprite icon, Vector2Int cell, bool hasCell)
        {
            return Create(
                "Fish",
                fish.HabitatKind == StrategyFishHabitatKind.River ? "River wildlife" : "Lake wildlife",
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                new Color(0.38f, 0.62f, 0.70f, 1f),
                new[]
                {
                    Chip(fish.Species.ToString(), null, Neutral),
                    Chip(fish.HabitatKind.ToString(), null, Neutral),
                    Chip(fish.CanBeFished ? "Fishable" : "Unavailable", null, fish.CanBeFished ? Good : Warn)
                },
                new[]
                {
                    Row("Stage", fish.LifeStage.ToString(), null, Neutral),
                    Row("State", fish.State.ToString(), null, StateColor(fish.State.ToString())),
                    Row("Shoal", fish.ShoalId.ToString(), null, Neutral),
                    Row("Region", fish.WaterRegionId >= 0 ? fish.WaterRegionId.ToString() : "none", null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateBird(StrategyBirdAgent bird, Sprite icon, Vector2Int cell, bool hasCell)
        {
            return Create(
                bird.Species.ToString(),
                "Decorative wildlife",
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[]
                {
                    Chip(bird.Species.ToString(), null, Neutral),
                    Chip(bird.State.ToString(), null, StateColor(bird.State.ToString())),
                    Chip("Home " + bird.HomeRadius, null, Neutral)
                },
                new[]
                {
                    Row("State", bird.State.ToString(), null, StateColor(bird.State.ToString())),
                    Row("Home", FormatCell(bird.HomeCell), null, Neutral),
                    Row("Radius", bird.HomeRadius.ToString(), null, Neutral),
                    Row("Id", bird.BirdId.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateWolf(StrategyWolfAgent wolf, Sprite icon, Vector2Int cell, bool hasCell)
        {
            return Create(
                "Wolf",
                "Predator wildlife",
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                PredatorAccent,
                new[]
                {
                    Chip("Pack " + wolf.PackId, null, Neutral),
                    Chip("Size " + wolf.PackMemberCount, null, Neutral),
                    Chip(wolf.State.ToString(), null, StateColor(wolf.State.ToString()))
                },
                new[]
                {
                    Row("State", wolf.State.ToString(), null, StateColor(wolf.State.ToString())),
                    Row("Pack", wolf.PackId.ToString(), null, Neutral),
                    Row("Home", FormatCell(wolf.HomeCell), null, Neutral),
                    Row("Radius", wolf.HomeRadius.ToString(), null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateChicken(string state, string coop, Sprite icon, Vector2Int cell, bool hasCell)
        {
            return Create(
                "Chicken",
                "Household animal",
                icon,
                cell,
                hasCell,
                StrategyWorldInspectKind.Wildlife,
                WildlifeAccent,
                new[] { Chip(state, null, StateColor(state)), Chip("Coop", null, Neutral) },
                new[] { Row("State", state, null, StateColor(state)), Row("Coop", coop, null, Neutral) });
        }

        public static StrategyWorldInspectInfo CreateStoneDeposit(StrategyStoneDeposit deposit, Sprite icon)
        {
            return CreateDeposit(
                GetStoneTitle(deposit.Kind),
                "Stone deposit",
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
                deposit.Kind == StrategyIronDepositKind.IronVein ? "Iron Vein" : "Iron-stained Ground",
                "Iron deposit",
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
                deposit.Kind == StrategyCoalDepositKind.CoalSeam ? "Coal Seam" : "Coal Dust Ground",
                "Coal deposit",
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
                deposit.Kind == StrategyClayDepositKind.ClayBank ? "Clay Bank" : "Clay Patch",
                "Clay deposit",
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
                tree.IsMature ? "Tree" : "Young Tree",
                "Forest resource",
                icon,
                tree.Cell,
                true,
                StrategyWorldInspectKind.Tree,
                ResourceAccent,
                new[]
                {
                    Chip(GetTreeStage(tree.Stage), null, Neutral),
                    Chip(state, null, StateColor(state)),
                    Chip(tree.IsReserved ? "Reserved" : "Available", null, tree.IsReserved ? Warn : Good)
                },
                new[]
                {
                    Row("Stage", GetTreeStage(tree.Stage), null, Neutral),
                    Row("State", state, null, StateColor(state)),
                    Row("Logs", tree.HasLogsReady ? tree.LogYield.ToString() : "not ready", StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs), Neutral),
                    Row("Reserved", YesNo(tree.IsReserved), null, tree.IsReserved ? Warn : Good)
                });
        }

        public static StrategyWorldInspectInfo CreateForage(string title, StrategyResourceType type, int yield, bool depleted, bool reserved, Sprite icon, Vector2Int cell)
        {
            string state = depleted ? "regrowing" : reserved ? "reserved" : "ready";
            return Create(
                title,
                "Forage node",
                icon,
                cell,
                true,
                StrategyWorldInspectKind.Resource,
                ResourceAccent,
                new[] { Chip(state, null, StateColor(state)), Chip("Yield " + yield, StrategyResourceIconFactory.GetSprite(type), Neutral) },
                new[]
                {
                    Row("Resource", title, StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row("Yield", yield.ToString(), StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row("State", state, null, StateColor(state)),
                    Row("Use", "household food", null, Neutral)
                });
        }

        public static StrategyWorldInspectInfo CreateLooseResource(StrategyResourceType type, int amount, bool reserved, Sprite icon, Vector2Int cell)
        {
            string title = GetResourceTitle(type);
            return Create(
                "Dropped " + title,
                "Loose resource",
                icon != null ? icon : StrategyResourceIconFactory.GetSprite(type),
                cell,
                true,
                StrategyWorldInspectKind.LoosePile,
                ResourceAccent,
                new[] { Chip(title, StrategyResourceIconFactory.GetSprite(type), Neutral), Chip(reserved ? "Reserved" : "Available", null, reserved ? Warn : Good) },
                new[]
                {
                    Row("Resource", title, StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row("Amount", amount.ToString(), StrategyResourceIconFactory.GetSprite(type), Neutral),
                    Row("State", reserved ? "reserved" : "available", null, reserved ? Warn : Good)
                });
        }

        public static StrategyWorldInspectInfo CreateLooseConstructionPile(int logs, int stone, int planks, int availableLogs, int availableStone, int availablePlanks, Sprite icon, Vector2Int cell)
        {
            return Create(
                "Loose Building Materials",
                "Construction resource pile",
                icon,
                cell,
                true,
                StrategyWorldInspectKind.LoosePile,
                ResourceAccent,
                new[] { Chip("Logs " + logs, StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs), Neutral), Chip("Stone " + stone, StrategyResourceIconFactory.GetSprite(StrategyResourceType.Stone), Neutral), Chip("Planks " + planks, StrategyResourceIconFactory.GetSprite(StrategyResourceType.Planks), Neutral) },
                new[]
                {
                    Row("Logs", logs + " / " + availableLogs + " free", StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs), logs > 0 ? Good : Neutral),
                    Row("Stone", stone + " / " + availableStone + " free", StrategyResourceIconFactory.GetSprite(StrategyResourceType.Stone), stone > 0 ? Good : Neutral),
                    Row("Planks", planks + " / " + availablePlanks + " free", StrategyResourceIconFactory.GetSprite(StrategyResourceType.Planks), planks > 0 ? Good : Neutral)
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
                new[] { Chip(GetResourceTitle(resource) + " " + amount, resourceIcon, Neutral), Chip(state, null, StateColor(state)), Chip("Build " + build, null, Warn) },
                new[]
                {
                    Row("Amount", amount.ToString(), resourceIcon, amount > 0 ? Good : Neutral),
                    Row("Footprint", footprint.x + "x" + footprint.y, null, Neutral),
                    Row("State", state, null, StateColor(state)),
                    Row("Walk/Build", walk + " / " + build, null, build == "blocked" ? Danger : Warn)
                });
        }

        private static StrategyWorldInspectInfo Create(string title, string subtitle, Sprite icon, Vector2Int cell, bool hasCell, StrategyWorldInspectKind kind, Color accent, StrategyWorldInspectChip[] chips, StrategyWorldInspectRow[] rows)
        {
            return new StrategyWorldInspectInfo(title, subtitle, BuildBody(rows), icon, cell, hasCell, kind, accent, chips, rows);
        }

        private static StrategyWorldInspectChip Chip(string label, Sprite icon, Color color) => new(label, icon, color);
        private static StrategyWorldInspectRow Row(string label, string value, Sprite icon, Color color) => new(label, value, icon, color);
        private static string FormatCell(Vector2Int cell) => cell.x + ", " + cell.y;
        private static string YesNo(bool value) => value ? "yes" : "no";

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

        private static string GetTreeStage(int stage) => stage <= 0 ? "Sapling" : stage == 1 ? "Young" : "Mature";

        private static string GetStoneTitle(StrategyStoneDepositKind kind)
        {
            return kind switch
            {
                StrategyStoneDepositKind.RockCluster => "Rock Cluster",
                StrategyStoneDepositKind.Cliff => "Stone Cliff",
                _ => "Boulder"
            };
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Eggs => "Eggs",
                StrategyResourceType.Turnip => "Turnip",
                StrategyResourceType.Cabbage => "Cabbage",
                StrategyResourceType.Onion => "Onion",
                StrategyResourceType.Carrot => "Carrot",
                StrategyResourceType.Potato => "Potato",
                StrategyResourceType.Berries => "Berries",
                StrategyResourceType.Roots => "Roots",
                StrategyResourceType.Mushrooms => "Mushrooms",
                StrategyResourceType.Game => "Game",
                StrategyResourceType.Fish => "Fish",
                StrategyResourceType.Logs => "Logs",
                StrategyResourceType.Stone => "Stone",
                StrategyResourceType.Iron => "Iron",
                StrategyResourceType.Coal => "Coal",
                StrategyResourceType.Clay => "Clay",
                StrategyResourceType.Pottery => "Pottery",
                StrategyResourceType.Planks => "Planks",
                _ => type.ToString()
            };
        }
    }
}
