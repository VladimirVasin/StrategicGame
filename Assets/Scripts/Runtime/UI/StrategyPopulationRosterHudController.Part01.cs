using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationRosterHudController
    {
        private void BuildFamilyTreeButton()
        {
            Button button = CreateButton("FamilyTreesButton", panel, "Family Trees", 13, new Color(0.16f, 0.18f, 0.20f, 1f));
            button.onClick.AddListener(OpenFamilyTrees);
            SetTopRight(button.GetComponent<RectTransform>(), 70f, 18f, 128f, 34f);
        }

        private void OpenFamilyTrees()
        {
            StrategyFamilyTreeHudController familyTreeHud = Object.FindAnyObjectByType<StrategyFamilyTreeHudController>();
            if (familyTreeHud == null)
            {
                GameObject familyTreeObject = new GameObject("Strategy Family Tree HUD");
                familyTreeHud = familyTreeObject.AddComponent<StrategyFamilyTreeHudController>();
            }

            familyTreeHud.Configure(population, Object.FindAnyObjectByType<StrategyTimeScaleController>());
            SetOpen(false);
            familyTreeHud.SetOpen(true);
        }
    }
}
