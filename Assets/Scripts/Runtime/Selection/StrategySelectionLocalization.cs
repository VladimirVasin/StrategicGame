using System;
using System.Globalization;
using System.Text;

namespace ProjectUnknown.Strategy
{
    internal static class StrategySelectionLocalization
    {
        private const string Prefix = "hud.selection.";
        private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
        private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

        internal static string Text(string key, params object[] arguments)
        {
            return StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                Prefix + key,
                arguments);
        }

        internal static string TextOrLiteral(string key, string englishFallback)
        {
            return GetOrLiteral(
                StrategyLocalizationTables.Hud,
                Prefix + key,
                englishFallback);
        }

        internal static string Resource(StrategyResourceType type)
        {
            string token = type == StrategyResourceType.Dish
                ? "dish"
                : ToKeyToken(type.ToString());
            string fallback = type == StrategyResourceType.Dish
                ? "Dish"
                : Humanize(type.ToString());
            return GetOrLiteral(
                StrategyLocalizationTables.Resources,
                "resource." + token + ".name",
                fallback);
        }

        internal static string Building(StrategyBuildTool tool)
        {
            string token = tool == StrategyBuildTool.StarterCaravanCart
                ? "caravan_cart"
                : ToKeyToken(tool.ToString());
            string fallback = tool == StrategyBuildTool.StarterCaravanCart
                ? "Caravan Cart"
                : Humanize(tool.ToString());
            return GetOrLiteral(
                StrategyLocalizationTables.Buildings,
                "building." + token + ".name",
                fallback);
        }

        internal static string Value(Enum value)
        {
            return value == null ? string.Empty : Value(value.ToString());
        }

        internal static string Value(string english)
        {
            if (string.IsNullOrWhiteSpace(english))
            {
                return string.Empty;
            }

            string key = Prefix + "value." + ToKeyToken(english);
            return GetOrLiteral(StrategyLocalizationTables.Hud, key, english);
        }

        internal static string Rations(float value)
        {
            CultureInfo culture = StrategyLocalization.CurrentLanguage == StrategyGameLanguage.English
                ? EnglishCulture
                : RussianCulture;
            return Text("format.rations_short", value.ToString("0.#", culture));
        }

        internal static string ConstructionCost(StrategyConstructionResourceCost cost)
        {
            string result = string.Empty;
            AppendCost(ref result, StrategyResourceType.Logs, cost.Logs);
            AppendCost(ref result, StrategyResourceType.Stone, cost.Stone);
            AppendCost(ref result, StrategyResourceType.Planks, cost.Planks);
            return string.IsNullOrEmpty(result) ? Text("cost.free") : result;
        }

        internal static string ProductionCost(StrategyProductionUpgradeCost cost)
        {
            string result = string.Empty;
            AppendCost(ref result, StrategyResourceType.Tools, cost.Tools);
            AppendCost(ref result, StrategyResourceType.Planks, cost.Planks);
            AppendCost(ref result, StrategyResourceType.Stone, cost.Stone);
            return string.IsNullOrEmpty(result) ? Text("cost.free") : result;
        }

        internal static string LocalizeTradeTiming(string value)
        {
            if (!string.IsNullOrEmpty(value)
                && value.EndsWith("s", StringComparison.Ordinal)
                && int.TryParse(
                    value.Substring(0, value.Length - 1),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int seconds))
            {
                return Text("format.seconds_short", seconds);
            }

            return value == "--" ? value : Value(value);
        }

        internal static string LocalizeTradeDetail(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return string.Empty;
            }

            if (TryParseTradeResult(detail, "Sold ", out int sold, out StrategyResourceType soldType))
            {
                return Text("trade.sold", sold, Resource(soldType));
            }

            if (TryParseTradeResult(detail, "Bought ", out int bought, out StrategyResourceType boughtType))
            {
                return Text("trade.bought", bought, Resource(boughtType));
            }

            return Value(detail);
        }

        internal static string ToKeyToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            StringBuilder result = new StringBuilder(value.Length + 8);
            bool lastWasSeparator = true;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (!char.IsLetterOrDigit(current))
                {
                    if (!lastWasSeparator && result.Length > 0)
                    {
                        result.Append('_');
                    }

                    lastWasSeparator = true;
                    continue;
                }

                if (char.IsUpper(current)
                    && !lastWasSeparator
                    && result.Length > 0
                    && i > 0
                    && char.IsLower(value[i - 1]))
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(current));
                lastWasSeparator = false;
            }

            while (result.Length > 0 && result[result.Length - 1] == '_')
            {
                result.Length--;
            }

            return result.Length > 0 ? result.ToString() : "unknown";
        }

        private static string GetOrLiteral(string table, string key, string englishFallback)
        {
            string localized = StrategyLocalization.Get(table, key);
            return string.Equals(localized, key, StringComparison.Ordinal)
                ? StrategyLocalization.TranslateLiteral(englishFallback)
                : localized;
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i]) && char.IsLower(value[i - 1]))
                {
                    result.Append(' ');
                }

                result.Append(value[i]);
            }

            return result.ToString();
        }

        private static bool TryParseTradeResult(
            string detail,
            string prefix,
            out int amount,
            out StrategyResourceType type)
        {
            amount = 0;
            type = StrategyResourceType.None;
            if (!detail.StartsWith(prefix, StringComparison.Ordinal)
                || !detail.EndsWith(".", StringComparison.Ordinal))
            {
                return false;
            }

            string payload = detail.Substring(prefix.Length, detail.Length - prefix.Length - 1);
            int separator = payload.IndexOf(' ');
            return separator > 0
                && int.TryParse(
                    payload.Substring(0, separator),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out amount)
                && Enum.TryParse(
                    payload.Substring(separator + 1),
                    true,
                    out type);
        }

        private static void AppendCost(
            ref string result,
            StrategyResourceType type,
            int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            string part = Text("format.resource_amount", Resource(type), amount);
            result = string.IsNullOrEmpty(result)
                ? part
                : Text("format.cost_join", result, part);
        }
    }

    internal static partial class StrategyBuildingHudSnapshotFactory
    {
        private static string L(string key, params object[] arguments) =>
            StrategySelectionLocalization.Text(key, arguments);

        private static string LocalizedValue(string english) =>
            StrategySelectionLocalization.Value(english);
    }

    public sealed partial class StrategyWorldSelectionController
    {
        private static string L(string key, params object[] arguments) =>
            StrategySelectionLocalization.Text(key, arguments);

        private static string LocalizedValue(string english) =>
            StrategySelectionLocalization.Value(english);
    }
}
