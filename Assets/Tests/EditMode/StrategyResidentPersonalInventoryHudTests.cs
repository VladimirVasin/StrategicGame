using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyResidentPersonalInventoryHudTests
    {
        private GameObject root;
        private StrategyWorldSelectionController selection;
        private MethodInfo refreshMethod;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Resident Personal Inventory HUD Test");
            selection = root.AddComponent<StrategyWorldSelectionController>();

            MethodInfo ensureHud = typeof(StrategyWorldSelectionController).GetMethod(
                "EnsureHud",
                BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod = typeof(StrategyWorldSelectionController).GetMethod(
                "RefreshResidentPersonalInventoryHud",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(ensureHud, Is.Not.Null);
            Assert.That(refreshMethod, Is.Not.Null);
            ensureHud.Invoke(selection, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AdultWithEmptyProductionCatalogShowsIntentionalEmptyState()
        {
            GameObject residentObject = new("Adult Resident");
            residentObject.transform.SetParent(root.transform, false);
            StrategyResidentAgent resident = residentObject.AddComponent<StrategyResidentAgent>();

            refreshMethod.Invoke(selection, new object[] { resident });

            Assert.That(selection.IsResidentPersonalInventoryVisible, Is.True);
            Assert.That(selection.ResidentPersonalInventoryCountCopy, Is.EqualTo("0 / 6"));
            Assert.That(selection.ResidentPersonalInventoryEmptyCopy, Does.Contain("No personal items"));
            Assert.That(
                root.transform.Find(
                    "SelectionHudCanvas/SelectionSideHud/ResidentHud/ResidentPersonalInventory/PersonalItemSlot_0")
                    ?.gameObject.activeSelf,
                Is.False);
        }

        [Test]
        public void ChildDoesNotShowPersonalInventorySection()
        {
            GameObject residentObject = new("Child Resident");
            residentObject.transform.SetParent(root.transform, false);
            StrategyResidentAgent resident = residentObject.AddComponent<StrategyResidentAgent>();
            FieldInfo lifeStage = typeof(StrategyResidentAgent).GetField(
                "lifeStage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lifeStage, Is.Not.Null);
            lifeStage.SetValue(resident, StrategyResidentLifeStage.Child);

            refreshMethod.Invoke(selection, new object[] { resident });

            Assert.That(selection.IsResidentPersonalInventoryVisible, Is.False);
        }
    }
}
