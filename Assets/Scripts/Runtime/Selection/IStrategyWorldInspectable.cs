namespace ProjectUnknown.Strategy
{
    public interface IStrategyWorldInspectable
    {
        bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info);
    }
}
