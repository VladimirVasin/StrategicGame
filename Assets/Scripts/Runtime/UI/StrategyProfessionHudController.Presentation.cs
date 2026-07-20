using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {
        private void ApplySnapshot(ProfessionRow row, ProfessionSnapshot snapshot)
        {
            row.Title.text = snapshot.Title;
            row.Subtitle.text = snapshot.Subtitle;
            row.Count.text = snapshot.IsUnlimited
                ? snapshot.Assigned + "/\u221e"
                : snapshot.Assigned + "/" + snapshot.Capacity;
            row.Count.color = !snapshot.IsUnlimited && snapshot.Assigned >= snapshot.Capacity
                ? new Color(0.95f, 0.72f, 0.32f)
                : new Color(0.95f, 0.88f, 0.62f);
            row.Background.color = snapshot.Assigned > 0
                ? new Color(snapshot.Accent.r, snapshot.Accent.g, snapshot.Accent.b, 0.18f)
                : new Color(1f, 1f, 1f, 0.055f);
            row.MinusButton.interactable = snapshot.Assigned > 0
                && (snapshot.Type != StrategyProfessionType.Scout || HasReadyScout());
            row.PlusButton.interactable = (snapshot.IsUnlimited || snapshot.Assigned < snapshot.Capacity)
                && snapshot.FreeWorkers > 0;
        }

        private static bool HasReadyScout()
        {
            StrategyScoutLodge[] lodges = FindSorted<StrategyScoutLodge>();
            for (int i = 0; i < lodges.Length; i++)
            {
                if (lodges[i] != null
                    && lodges[i].WorkerCount > 0
                    && lodges[i].ExpeditionState == StrategyScoutExpeditionState.Ready)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
