using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.Tests
{
    public sealed class StrategyBuildingHudSnapshotTests
    {
        [Test]
        public void Snapshot_EnforcesCaps_AndResetReusesSectionStorage()
        {
            StrategyBuildingHudSnapshot snapshot = new();
            snapshot.Reset(StrategyBuildTool.LumberjackCamp);

            for (int i = 0; i < StrategyBuildingHudSnapshot.MaxChips + 2; i++)
            {
                snapshot.AddChip(
                    "chip_" + i,
                    "Chip " + i,
                    i.ToString(),
                    null,
                    StrategyBuildingHudTone.Info);
            }

            StrategyBuildingHudSection firstSection = null;
            for (int i = 0; i < StrategyBuildingHudSnapshot.MaxSections + 2; i++)
            {
                StrategyBuildingHudSection section = snapshot.AddSection(
                    "section_" + i,
                    "Section " + i);
                if (i == 0)
                {
                    firstSection = section;
                }

                if (section == null)
                {
                    continue;
                }

                for (int row = 0; row < StrategyBuildingHudSection.MaxRows + 2; row++)
                {
                    section.AddRow(
                        "row_" + row,
                        "Row " + row,
                        row.ToString(),
                        null,
                        StrategyBuildingHudTone.Positive,
                        "Detail " + row,
                        row / 10f);
                }
            }

            snapshot.SetStatus(
                "Working",
                "Snapshot has content.",
                StrategyBuildingHudTone.Positive);

            Assert.That(snapshot.ChipCount, Is.EqualTo(StrategyBuildingHudSnapshot.MaxChips));
            Assert.That(snapshot.SectionCount, Is.EqualTo(StrategyBuildingHudSnapshot.MaxSections));
            Assert.That(snapshot.GetSection(0), Is.SameAs(firstSection));
            Assert.That(firstSection.RowCount, Is.EqualTo(StrategyBuildingHudSection.MaxRows));
            Assert.That(firstSection.GetRow(0).Key, Is.EqualTo("row_0"));
            Assert.That(firstSection.GetRow(0).HasProgress, Is.True);
            Assert.That(snapshot.HasStatus, Is.True);

            snapshot.Reset(StrategyBuildTool.ScoutLodge, true);

            Assert.That(snapshot.Tool, Is.EqualTo(StrategyBuildTool.ScoutLodge));
            Assert.That(snapshot.IsConstruction, Is.True);
            Assert.That(snapshot.ChipCount, Is.Zero);
            Assert.That(snapshot.SectionCount, Is.Zero);
            Assert.That(snapshot.HasStatus, Is.False);
            Assert.That(snapshot.StatusTone, Is.EqualTo(StrategyBuildingHudTone.Neutral));
            Assert.That(snapshot.GetChip(0).Key, Is.Null);
            Assert.That(snapshot.GetSection(0), Is.Null);

            StrategyBuildingHudSection reusedSection = snapshot.AddSection(
                "construction",
                "Construction");
            Assert.That(reusedSection, Is.SameAs(firstSection));
            Assert.That(reusedSection.RowCount, Is.Zero);
            Assert.That(reusedSection.Key, Is.EqualTo("construction"));
        }

        [Test]
        public void Renderer_ReusesHierarchy_ForAllBuildingsAndConstructionState()
        {
            GameObject canvasObject = new("BuildingHudRendererTest", typeof(Canvas));
            try
            {
                RectTransform parent = canvasObject.transform as RectTransform;
                StrategyBuildingSelectionHudRenderer renderer =
                    new StrategyBuildingSelectionHudRenderer(parent);
                StrategyBuildingHudSnapshot snapshot = new();
                RectTransform rendererRoot = renderer.Root;
                int initialHierarchySize = CountHierarchy(rendererRoot);
                int renderedBuildingCount = 0;

                foreach (StrategyBuildTool tool in System.Enum.GetValues(typeof(StrategyBuildTool)))
                {
                    if (tool == StrategyBuildTool.None)
                    {
                        continue;
                    }

                    renderedBuildingCount++;
                    bool showSecondSection = renderedBuildingCount % 2 == 0;
                    PopulateBuildingSnapshot(snapshot, tool, showSecondSection);

                    float bottom = renderer.Show(snapshot, 24f);

                    Assert.That(renderer.Root, Is.SameAs(rendererRoot), tool.ToString());
                    Assert.That(renderer.IsVisible, Is.True, tool.ToString());
                    Assert.That(renderer.CurrentTool, Is.EqualTo(tool));
                    Assert.That(renderer.VisibleSectionCount, Is.EqualTo(showSecondSection ? 2 : 1));
                    Assert.That(renderer.ContentHeight, Is.GreaterThan(1f));
                    Assert.That(bottom, Is.EqualTo(24f + renderer.ContentHeight).Within(0.01f));
                    Assert.That(CountHierarchy(rendererRoot), Is.EqualTo(initialHierarchySize));
                    Assert.That(FindText(rendererRoot, "Chip_0", "Value").text, Is.EqualTo(tool.ToString()));
                    Assert.That(rendererRoot.Find("Section_1").gameObject.activeSelf, Is.EqualTo(showSecondSection));
                    Assert.That(rendererRoot.Find("Status").gameObject.activeSelf, Is.EqualTo(showSecondSection));
                }

                Assert.That(renderedBuildingCount, Is.EqualTo(19));

                snapshot.Reset(StrategyBuildTool.Bridge, true);
                snapshot.SetStatus(
                    "Construction",
                    "Waiting for builders.",
                    StrategyBuildingHudTone.Warning);
                float constructionBottom = renderer.Show(snapshot, 96f);

                Assert.That(snapshot.IsConstruction, Is.True);
                Assert.That(renderer.CurrentTool, Is.EqualTo(StrategyBuildTool.Bridge));
                Assert.That(renderer.VisibleSectionCount, Is.Zero);
                Assert.That(rendererRoot.Find("Chip_0").gameObject.activeSelf, Is.False);
                Assert.That(rendererRoot.Find("Section_0").gameObject.activeSelf, Is.False);
                Assert.That(rendererRoot.Find("Status").gameObject.activeSelf, Is.True);
                Assert.That(constructionBottom, Is.EqualTo(96f + renderer.ContentHeight).Within(0.01f));
                Assert.That(CountHierarchy(rendererRoot), Is.EqualTo(initialHierarchySize));

                float unchangedTop = renderer.Show(null, 37f);

                Assert.That(unchangedTop, Is.EqualTo(37f));
                Assert.That(renderer.IsVisible, Is.False);
                Assert.That(renderer.ContentHeight, Is.Zero);
                Assert.That(renderer.VisibleSectionCount, Is.Zero);
                Assert.That(renderer.Root, Is.SameAs(rendererRoot));
                Assert.That(CountHierarchy(rendererRoot), Is.EqualTo(initialHierarchySize));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void Factory_DemolitionSnapshot_ReplacesOperationalState()
        {
            GameObject root = new("DemolitionBuilding", typeof(SpriteRenderer));
            try
            {
                StrategyPlacedBuilding building =
                    root.AddComponent<StrategyPlacedBuilding>();
                building.Configure(
                    StrategyBuildTool.Sawmill,
                    new Vector2Int(4, 7),
                    new Vector2Int(3, 2),
                    new Bounds(Vector3.zero, new Vector3(3f, 2f, 1f)),
                    root.GetComponent<SpriteRenderer>(),
                    0);
                Assert.That(building.BeginDemolition(), Is.True);

                StrategyBuildingHudSnapshot snapshot = new();
                Assert.That(
                    StrategyBuildingHudSnapshotFactory.TryFill(
                        building,
                        snapshot),
                    Is.True);

                Assert.That(snapshot.GetChip(0).Value, Is.EqualTo("Demolition queued"));
                Assert.That(snapshot.StatusTitle, Is.EqualTo("Demolition queued"));
                Assert.That(snapshot.StatusBody, Does.Not.Contain("Production active"));
                Assert.That(snapshot.StatusTone, Is.EqualTo(StrategyBuildingHudTone.Warning));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void PopulateBuildingSnapshot(
            StrategyBuildingHudSnapshot snapshot,
            StrategyBuildTool tool,
            bool showSecondSection)
        {
            snapshot.Reset(tool);
            snapshot.AddChip(
                "building",
                "Building",
                tool.ToString(),
                null,
                StrategyBuildingHudTone.Info);
            StrategyBuildingHudSection activity = snapshot.AddSection(
                "activity",
                "Activity");
            activity.AddRow(
                "output",
                "Output",
                "4 / 8",
                null,
                StrategyBuildingHudTone.Positive,
                "Operating normally",
                0.5f);

            if (!showSecondSection)
            {
                return;
            }

            StrategyBuildingHudSection logistics = snapshot.AddSection(
                "logistics",
                "Logistics");
            logistics.AddRow(
                "reserved",
                "Reserved",
                "2",
                null,
                StrategyBuildingHudTone.Neutral,
                "Available to haulers");
            snapshot.SetStatus(
                "Ready",
                "The building is available.",
                StrategyBuildingHudTone.Positive);
        }

        private static Text FindText(
            RectTransform root,
            string parentName,
            string childName)
        {
            Transform parent = root.Find(parentName);
            Assert.That(parent, Is.Not.Null, parentName);
            Transform child = parent.Find(childName);
            Assert.That(child, Is.Not.Null, parentName + "/" + childName);
            return child.GetComponent<Text>();
        }

        private static int CountHierarchy(RectTransform root) =>
            root.GetComponentsInChildren<Transform>(true).Length;
    }
}
