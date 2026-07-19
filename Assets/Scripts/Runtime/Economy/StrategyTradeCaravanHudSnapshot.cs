using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal readonly struct StrategyTradeCaravanHudSnapshot
    {
        public StrategyTradeCaravanHudSnapshot(
            string state,
            string timingLabel,
            string timingValue,
            string detail,
            bool isTrading,
            bool isWarning)
        {
            State = state ?? string.Empty;
            TimingLabel = timingLabel ?? string.Empty;
            TimingValue = timingValue ?? string.Empty;
            Detail = detail ?? string.Empty;
            IsTrading = isTrading;
            IsWarning = isWarning;
        }

        public string State { get; }
        public string TimingLabel { get; }
        public string TimingValue { get; }
        public string Detail { get; }
        public bool IsTrading { get; }
        public bool IsWarning { get; }
    }

    public sealed partial class StrategyTradeCaravanController
    {
        internal StrategyTradeCaravanHudSnapshot GetHudSnapshot(
            StrategyTradingPost post)
        {
            if (post == null)
            {
                return new StrategyTradeCaravanHudSnapshot(
                    "Unavailable",
                    "Caravan",
                    "--",
                    "Trading post data is unavailable.",
                    false,
                    true);
            }

            if (state == TradeState.Arriving && post == activePost)
            {
                int seconds = Mathf.CeilToInt(
                    activeAgent != null ? activeAgent.EstimatedRemainingSeconds : 0f);
                return new StrategyTradeCaravanHudSnapshot(
                    "Arriving",
                    "ETA",
                    seconds + "s",
                    "A caravan is travelling to this post.",
                    false,
                    false);
            }

            if (state == TradeState.Trading && post == activePost)
            {
                return new StrategyTradeCaravanHudSnapshot(
                    "Trading",
                    "Leaves in",
                    Mathf.CeilToInt(dwellTimer) + "s",
                    string.IsNullOrEmpty(lastMessage)
                        ? "Trade goods while the caravan waits."
                        : lastMessage,
                    true,
                    false);
            }

            if (state == TradeState.Departing && post == activePost)
            {
                return new StrategyTradeCaravanHudSnapshot(
                    "Departing",
                    "Next visit",
                    "after departure",
                    "The current caravan is leaving the settlement.",
                    false,
                    false);
            }

            return new StrategyTradeCaravanHudSnapshot(
                "Waiting",
                "Arrives in",
                Mathf.CeilToInt(arrivalTimer) + "s",
                "The post is ready for the next caravan.",
                false,
                false);
        }
    }
}
