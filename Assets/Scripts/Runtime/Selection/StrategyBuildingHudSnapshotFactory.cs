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
                L("label.builders"),
                site.BuilderCount.ToString(),
                StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Builder),
                site.BuilderCount > 0
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "materials",
                L("label.materials"),
                delivered + "/" + total,
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs),
                site.ResourcesComplete
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "progress",
                L("label.progress"),
                Mathf.RoundToInt(site.Progress * 100f) + "%",
                GetBuildingIcon(site.Tool),
                site.Progress >= 1f
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Info);

            StrategyBuildingHudSection materials = snapshot.AddSection(
                "materials",
                L("section.delivered_materials"));
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
                    L("section.assigned_builders"));
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
                    ? L("construction.ready_with_builders")
                    : L("construction.ready_needs_builders")
                : BuildMissingMaterialsText(site);
            snapshot.SetStatus(
                site.ResourcesComplete
                    ? L("construction.ready_title")
                    : L("construction.waiting_title"),
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
                L("label.residents"),
                building.ResidentCount + "/" + building.ResidentCapacity,
                homeIcon,
                building.ResidentCount > 0
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "home",
                L("label.home"),
                LocalizedValue(building.ResidentCount > 0 ? "Occupied" : "Available"),
                homeIcon,
                StrategyBuildingHudTone.Neutral);

            StrategyHouseWarmthState warmth = building.Warmth;
            string warmthValue = warmth != null
                ? L(
                    "warmth.status",
                    LocalizedValue(warmth.WarmthLevel.ToString()),
                    StrategyTemperatureModel.FormatCelsius(warmth.IndoorCelsius))
                : LocalizedValue("Sheltered");
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
                L("label.warmth"),
                warmthValue,
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs),
                warmthTone);

            if (building.IsDemolishing)
            {
                snapshot.SetStatus(
                    L("demolition.queued_title"),
                    L("demolition.house_body"),
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
                L("label.scouts"),
                lodge.WorkerCount + "/" + StrategyScoutLodge.MaxWorkers,
                StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Scout),
                hasScout ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "mission",
                L("label.mission"),
                LocalizedValue(hasScout ? lodge.ExpeditionState.ToString() : "Unassigned"),
                GetBuildingIcon(StrategyBuildTool.ScoutLodge),
                lodge.IsExploring
                    ? StrategyBuildingHudTone.Positive
                    : hasScout ? StrategyBuildingHudTone.Info : StrategyBuildingHudTone.Warning);
            snapshot.AddChip(
                "provisions",
                L("label.provisions"),
                StrategySelectionLocalization.Rations(lodge.GetAvailableExpeditionRations()),
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game),
                lodge.GetAvailableExpeditionRations() > 0f
                    ? StrategyBuildingHudTone.Neutral
                    : StrategyBuildingHudTone.Warning);

            StrategyBuildingHudSection mission = snapshot.AddSection("mission", L("section.expedition"));
            mission.AddRow(
                "state",
                L("label.current_state"),
                LocalizedValue(lodge.HudMissionStatus),
                GetBuildingIcon(StrategyBuildTool.ScoutLodge),
                StrategyBuildingHudTone.Info);
            if (lodge.IsExploring)
            {
                mission.AddRow(
                    "duration",
                    L("label.planned_duration"),
                    L("format.days_short", lodge.PlannedExpeditionDays),
                    null);
                mission.AddRow(
                    "return",
                    L("label.returns_in"),
                    FormatExpeditionTime(lodge.RemainingExpeditionSeconds),
                    null,
                    StrategyBuildingHudTone.Info);
                mission.AddRow(
                    "field_rations",
                    L("label.field_rations"),
                    StrategySelectionLocalization.Rations(lodge.RemainingFieldRations),
                    StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game));
            }

            snapshot.SetStatus(
                hasScout ? L("scout.ready_title") : L("scout.needed_title"),
                hasScout
                    ? L("scout.ready_body")
                    : L("scout.needed_body"),
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
                L("label.coins"),
                coins.ToString(),
                GetBuildingIcon(StrategyBuildTool.TradingPost),
                StrategyBuildingHudTone.Neutral);
            snapshot.AddChip(
                "caravan",
                L("label.caravan"),
                LocalizedValue(trade.State),
                GetBuildingIcon(StrategyBuildTool.StarterCaravanCart),
                trade.IsTrading
                    ? StrategyBuildingHudTone.Positive
                    : trade.IsWarning
                        ? StrategyBuildingHudTone.Warning
                        : StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "timing",
                LocalizedValue(trade.TimingLabel),
                StrategySelectionLocalization.LocalizeTradeTiming(trade.TimingValue),
                GetBuildingIcon(StrategyBuildTool.TradingPost),
                StrategyBuildingHudTone.Info);

            StrategyBuildingHudSection market = snapshot.AddSection("market", L("section.market"));
            market.AddRow(
                "offers",
                L("label.active_offers"),
                validOffers.ToString(),
                GetBuildingIcon(StrategyBuildTool.TradingPost),
                validOffers > 0
                    ? StrategyBuildingHudTone.Positive
                    : StrategyBuildingHudTone.Neutral,
                trade.IsTrading
                    ? L("trade.available_now")
                    : L("trade.available_when_trading"));
            snapshot.SetStatus(
                LocalizedValue(trade.State),
                StrategySelectionLocalization.LocalizeTradeDetail(trade.Detail),
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
                required > delivered
                    ? L("format.still_needed", required - delivered)
                    : LocalizedValue("Delivered"),
                Mathf.Clamp01(progress));
        }

        private static string BuildMissingMaterialsText(StrategyConstructionSite site)
        {
            string text = string.Empty;
            AppendNeed(ref text, GetResourceTitle(StrategyResourceType.Logs), site.NeededLogs);
            AppendNeed(ref text, GetResourceTitle(StrategyResourceType.Stone), site.NeededStone);
            AppendNeed(ref text, GetResourceTitle(StrategyResourceType.Planks), site.NeededPlanks);
            return string.IsNullOrEmpty(text)
                ? L("construction.builders_continue")
                : L("construction.still_needed", text);
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

            text += L("format.resource_amount", label, amount);
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

            return days > 0
                ? L("format.days_hours_short", days, hours)
                : L("format.hours_short", Mathf.Max(1, hours));
        }

        private static string FormatCell(Vector2Int cell) => cell.x + ", " + cell.y;

        private static Sprite GetBuildingIcon(StrategyBuildTool tool) =>
            StrategyBuildingSpriteFactory.TryGetBuildSprite(tool, out Sprite sprite)
                ? sprite
                : null;

        private static string GetResourceTitle(StrategyResourceType type) =>
            StrategySelectionLocalization.Resource(type);
    }
}
