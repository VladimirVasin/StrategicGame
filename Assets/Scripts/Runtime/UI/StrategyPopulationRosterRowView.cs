using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyPopulationRosterRowView
    {
        public RectTransform Root;
        public Image Background;
        public Text Name;
        public Text Age;
        public Text Home;
        public Text Role;
        public Text Status;
        public Text Food;

        public void Set(StrategyResidentAgent resident, int index)
        {
            Name.text = resident.FullName;
            Age.text = StrategyLocalization.Get(
                StrategyLocalizationTables.Residents,
                "resident.age.short",
                resident.DisplayAgeYears);
            Home.text = StrategyResidentHudText.GetHomeTitle(resident);
            Role.text = StrategyResidentHudText.GetRoleTitle(resident);
            Status.text = StrategyResidentHudText.GetStatusText(resident);
            Food.text = StrategyResidentHudText.GetFoodTitle(resident);

            Background.color = resident.IsStarving
                ? new Color(0.28f, 0.08f, 0.07f, 0.92f)
                : resident.IsHungry
                    ? new Color(0.25f, 0.18f, 0.07f, 0.90f)
                    : index % 2 == 0
                        ? new Color(0.08f, 0.105f, 0.11f, 0.88f)
                        : new Color(0.10f, 0.125f, 0.13f, 0.88f);
        }
    }
}
