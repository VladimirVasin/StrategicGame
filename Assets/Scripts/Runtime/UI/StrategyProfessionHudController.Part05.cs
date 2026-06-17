namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {
        private void EnsureProfessionRows()
        {
            if (contentRoot == null)
            {
                return;
            }

            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (rows[i] == null || rows[i].Root == null)
                {
                    rows[i] = CreateRow(DisplayOrder[i], contentRoot);
                }

                rows[i].Root.SetSiblingIndex(i);
            }
        }
    }
}
