using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingHudSnapshotFactory
    {
        public static bool TryFill(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            if (building == null || snapshot == null)
            {
                return false;
            }

            snapshot.Reset(building.Tool);
            if (building.IsDemolishing)
            {
                FillDemolition(building, snapshot);
                return true;
            }

            if (building.Tool == StrategyBuildTool.House)
            {
                FillHouse(building, snapshot);
                return true;
            }

            if (TryFillResourceBuilding(building, snapshot))
            {
                return true;
            }

            StrategyScoutLodge lodge = building.GetComponent<StrategyScoutLodge>();
            if (lodge != null)
            {
                FillScoutLodge(lodge, snapshot);
                return true;
            }

            StrategyTradingPost tradingPost = building.GetComponent<StrategyTradingPost>();
            if (tradingPost != null)
            {
                FillTradingPost(tradingPost, snapshot);
                return true;
            }

            if (building.Tool == StrategyBuildTool.Bridge)
            {
                FillBridge(building, snapshot);
                return true;
            }

            FillOperationalBuilding(building, snapshot);
            return true;
        }

        public static bool TryFill(
            StrategyConstructionSite site,
            StrategyBuildingHudSnapshot snapshot)
        {
            if (site == null || snapshot == null)
            {
                return false;
            }

            snapshot.Reset(site.Tool, true);
            int delivered = site.DeliveredResourceTotal;
            int total = site.Cost.Total;
            snapshot.AddChip(
                "builders",
                "Builders",
                site.BuilderCount.ToString(),
                StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Builder),
                site.BuilderCount > 0
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "materials",
                "Materials",
                delivered + "/" + total,
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs),
                site.ResourcesComplete
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "progress",
                "Progress",
                Mathf.RoundToInt(site.Progress * 100f) + "%",
                GetBuildingIcon(site.Tool),
                site.Progress >= 1f
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Info);

            StrategyBuildingHudSection materials = snapshot.AddSection(
                "materials",
                "Delivered Materials");
            AddConstructionResourceRow(
                materials,
                StrategyResourceType.Logs,
                site.DeliveredLogs,
                site.Cost.Logs);
            AddConstructionResourceRow(
                materials,
                StrategyResourceType.Stone,
                site.DeliveredStone,
                site.Cost.Stone);
            AddConstructionResourceRow(
                materials,
                StrategyResourceType.Planks,
                site.DeliveredPlanks,
                site.Cost.Planks);

            if (site.BuilderCount > 0)
            {
                StrategyBuildingHudSection builders = snapshot.AddSection(
                    "builders",
                    "Assigned Builders");
                int count = Mathf.Min(site.BuilderCount, StrategyBuildingHudSection.MaxRows);
                for (int i = 0; i < count; i++)
                {
                    if (!site.TryGetBuilder(i, out StrategyResidentAgent builder)
                        || builder == null)
                    {
                        continue;
                    }

                    builders.AddRow(
                        "builder_" + i,
                        builder.FullName,
                        StrategyResidentHudText.GetStatusText(builder),
                        StrategyResidentSpriteFactory.GetPortraitSprite(
                            builder.Gender,
                            builder.VisualVariant,
                            builder.LifeStage),
                        StrategyBuildingHudTone.Neutral);
                }
            }

            string statusBody = site.ResourcesComplete
                ? site.BuilderCount > 0
                    ? "Materials are ready and builders can finish the structure."
                    : "Materials are ready. Assign Builders in the Professions HUD."
                : BuildMissingMaterialsText(site);
            snapshot.SetStatus(
                site.ResourcesComplete ? "Ready to build" : "Waiting for materials",
                statusBody,
                site.ResourcesComplete
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Warning);
            return true;
        }

        private static void FillHouse(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            Sprite homeIcon = GetBuildingIcon(StrategyBuildTool.House);
            snapshot.AddChip(
                "residents",
                "Residents",
                building.ResidentCount + "/" + building.ResidentCapacity,
                homeIcon,
                building.ResidentCount > 0
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "home",
                "Home",
                building.ResidentCount > 0 ? "Occupied" : "Available",
                homeIcon,
                StrategyBuildingHudTone.Neutral);

            StrategyHouseWarmthState warmth = building.Warmth;
            string warmthValue = warmth != null ? warmth.StatusText : "Sheltered";
            StrategyBuildingHudTone warmthTone = warmth == null
                ? StrategyBuildingHudTone.Neutral
                : warmth.WarmthLevel switch
                {
                    StrategyHouseWarmthLevel.Warm => StrategyBuildingHudTone.Positive,
                    StrategyHouseWarmthLevel.Cooling => StrategyBuildingHudTone.Info,
                    StrategyHouseWarmthLevel.Cold => StrategyBuildingHudTone.Warning,
                    StrategyHouseWarmthLevel.Freezing => StrategyBuildingHudTone.Critical,
                    _ => StrategyBuildingHudTone.Neutral
                };
            snapshot.AddChip(
                "warmth",
                "Warmth",
                warmthValue,
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs),
                warmthTone);

            if (building.IsDemolishing)
            {
                snapshot.SetStatus(
                    "Demolition queued",
                    "Residents and household stock will be detached safely.",
                    StrategyBuildingHudTone.Warning);
            }
        }

        private static void FillScoutLodge(
            StrategyScoutLodge lodge,
            StrategyBuildingHudSnapshot snapshot)
        {
            bool hasScout = lodge.WorkerCount > 0;
            snapshot.AddChip(
                "scouts",
                "Scouts",
                lodge.WorkerCount + "/" + StrategyScoutLodge.MaxWorkers,
                StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Scout),
                hasScout ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "mission",
                "Mission",
                hasScout ? lodge.ExpeditionState.ToString() : "Unassigned",
                GetBuildingIcon(StrategyBuildTool.ScoutLodge),
                lodge.IsExploring
                    ? StrategyBuildingHudTone.Positive
                    : hasScout ? StrategyBuildingHudTone.Info : StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "provisions",
                "Provisions",
                lodge.GetAvailableExpeditionRations().ToString("0.#") + "r",
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game),
                lodge.GetAvailableExpeditionRations() > 0f
                    ? StrategyBuildingHudTone.Neutral
                    : StrategyBuildingHudTone.Warning);

            StrategyBuildingHudSection mission = snapshot.AddSection("mission", "Expedition");
            mission.AddRow(
                "state",
                "Current state",
                lodge.HudMissionStatus,
                GetBuildingIcon(StrategyBuildTool.ScoutLodge),
                StrategyBuildingHudTone.Info);
            if (lodge.IsExploring)
            {
                mission.AddRow(
                    "duration",
                    "Planned duration",
                    lodge.PlannedExpeditionDays + "d",
                    null);
                mission.AddRow(
                    "return",
                    "Returns in",
                    FormatExpeditionTime(lodge.RemainingExpeditionSeconds),
                    null,
                    StrategyBuildingHudTone.Info);
                mission.AddRow(
                    "field_rations",
                    "Field rations",
                    lodge.RemainingFieldRations.ToString("0.#") + "r",
                    StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game));
            }

            snapshot.SetStatus(
                hasScout ? "Exploration ready" : "Scout needed",
                hasScout
                    ? "Use the Scout action above to send, recall or review the expedition."
                    : "Assign a Scout here; ordinary professions remain managed separately.",
                hasScout ? StrategyBuildingHudTone.Info : StrategyBuildingHudTone.Warning);
        }

        private static void FillTradingPost(
            StrategyTradingPost post,
            StrategyBuildingHudSnapshot snapshot)
        {
            StrategyTradeCaravanController controller = StrategyTradeCaravanController.Active;
            int coins = StrategySettlementTreasury.Active != null
                ? StrategySettlementTreasury.Active.Coins
                : 0;
            int validOffers = 0;
            if (controller != null)
            {
                for (int i = 0; i < controller.CurrentOffers.Count; i++)
                {
                    if (controller.CurrentOffers[i].IsValid)
                    {
                        validOffers++;
                    }
                }
            }

            StrategyTradeCaravanHudSnapshot trade = controller != null
                ? controller.GetHudSnapshot(post)
                : new StrategyTradeCaravanHudSnapshot(
                    "Unavailable",
                    "Caravan",
                    "--",
                    "Trade controller is unavailable.",
                    false,
                    true);
            snapshot.AddChip(
                "coins",
                "Coins",
                coins.ToString(),
                GetBuildingIcon(StrategyBuildTool.TradingPost),
                StrategyBuildingHudTone.Neutral);
            snapshot.AddChip(
                "caravan",
                "Caravan",
                trade.State,
                GetBuildingIcon(StrategyBuildTool.StarterCaravanCart),
                trade.IsTrading
                    ? StrategyBuildingHudTone.Positive
                    : trade.IsWarning
                        ? StrategyBuildingHudTone.Warning
                        : StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "timing",
                trade.TimingLabel,
                trade.TimingValue,
                GetBuildingIcon(StrategyBuildTool.TradingPost),
                StrategyBuildingHudTone.Info);

            StrategyBuildingHudSection market = snapshot.AddSection("market", "Market");
            market.AddRow(
                "offers",
                "Active offers",
                validOffers.ToString(),
                GetBuildingIcon(StrategyBuildTool.TradingPost),
                validOffers > 0
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Neutral,
                trade.IsTrading ? "Available now" : "Available when a caravan is trading");
            snapshot.SetStatus(
                trade.State,
                trade.Detail,
                trade.IsTrading
                    ? StrategyBuildingHudTone.Positive
                    : trade.IsWarning
                        ? StrategyBuildingHudTone.Warning
                        : StrategyBuildingHudTone.Info);
        }

        private static void AddConstructionResourceRow(
            StrategyBuildingHudSection section,
            StrategyResourceType resource,
            int delivered,
            int required)
        {
            if (section == null || required <= 0 && delivered <= 0)
            {
                return;
            }

            float progress = required > 0 ? delivered / (float)required : 1f;
            section.AddRow(
                resource.ToString().ToLowerInvariant(),
                GetResourceTitle(resource),
                delivered + "/" + required,
                StrategyResourceIconFactory.GetSprite(resource),
                delivered >= required
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Info,
                required > delivered ? required - delivered + " still needed" : "Delivered",
                Mathf.Clamp01(progress));
        }

        private static string BuildMissingMaterialsText(StrategyConstructionSite site)
        {
            string text = string.Empty;
            AppendNeed(ref text, "Logs", site.NeededLogs);
            AppendNeed(ref text, "Stone", site.NeededStone);
            AppendNeed(ref text, "Planks", site.NeededPlanks);
            return string.IsNullOrEmpty(text)
                ? "Builders can continue with the delivered materials."
                : "Still needed: " + text + ".";
        }

        private static void AppendNeed(ref string text, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                text += " · ";
            }

            text += label + " " + amount;
        }

        private static string FormatExpeditionTime(float seconds)
        {
            float dayLength = Mathf.Max(1f, StrategyDayNightCycleController.DayLengthSeconds);
            int days = Mathf.FloorToInt(Mathf.Max(0f, seconds) / dayLength);
            int hours = Mathf.CeilToInt(
                Mathf.Repeat(Mathf.Max(0f, seconds), dayLength) / dayLength * 24f);
            if (hours >= 24)
            {
                days++;
                hours = 0;
            }

            return days > 0 ? days + "d " + hours + "h" : Mathf.Max(1, hours) + "h";
        }

        private static string FormatCell(Vector2Int cell) => cell.x + ", " + cell.y;

        private static Sprite GetBuildingIcon(StrategyBuildTool tool) =>
            StrategyBuildingSpriteFactory.TryGetBuildSprite(tool, out Sprite sprite)
                ? sprite
                : null;

        private static string GetResourceTitle(StrategyResourceType type) =>
            type switch
            {
                StrategyResourceType.Game => "Game",
                StrategyResourceType.Fish => "Fish",
                StrategyResourceType.Eggs => "Eggs",
                StrategyResourceType.Berries => "Berries",
                StrategyResourceType.Roots => "Roots",
                StrategyResourceType.Mushrooms => "Mushrooms",
                StrategyResourceType.Logs => "Logs",
                StrategyResourceType.Stone => "Stone",
                StrategyResourceType.Planks => "Planks",
                StrategyResourceType.Iron => "Iron",
                StrategyResourceType.Coal => "Coal",
                StrategyResourceType.Clay => "Clay",
                StrategyResourceType.Pottery => "Pottery",
                StrategyResourceType.Tools => "Tools",
                _ => type.ToString()
            };
    }
}
