using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCombatHudTests
    {
        private const string ResidentHudPath =
            "SelectionHudCanvas/SelectionSideHud/ContentViewport/ScrollContent/ContextSections/ResidentHud/";
        private const string InspectHudPath =
            "Strategy World Inspect HUD/WorldInspectHudCanvas/WorldInspectPanel/";

        private GameObject root;
        private StrategyWorldSelectionController selection;
        private MethodInfo refreshResidentHud;
        private Sprite testSprite;
        private Texture2D testTexture;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Combat HUD Test");
            selection = root.AddComponent<StrategyWorldSelectionController>();
            InvokePrivate(selection, "EnsureHud");
            refreshResidentHud = GetPrivateMethod(
                typeof(StrategyWorldSelectionController),
                "RefreshResidentHud");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            if (testSprite != null)
            {
                Object.DestroyImmediate(testSprite);
            }

            if (testTexture != null)
            {
                Object.DestroyImmediate(testTexture);
            }
        }

        [Test]
        public void ResidentChipsShowLiveHealthAndAttackPoints()
        {
            StrategyResidentAgent resident = CreateResident("Combat Resident");
            resident.RestoreCombatState(60, -1);

            refreshResidentHud.Invoke(selection, new object[] { resident });

            Text health = FindText(ResidentHudPath + "HealthChip/Text");
            Text attack = FindText(ResidentHudPath + "AttackChip/Text");
            Assert.That(
                health.text,
                Is.EqualTo(StrategySelectionLocalization.Text("format.health_short", 60, 100)));
            Assert.That(
                attack.text,
                Is.EqualTo(StrategySelectionLocalization.Text("format.attack_short", 40)));

            resident.RestoreCombatState(25, -1);
            refreshResidentHud.Invoke(selection, new object[] { resident });

            Assert.That(
                health.text,
                Is.EqualTo(StrategySelectionLocalization.Text("format.health_short", 25, 100)));
        }

        [Test]
        public void ChildResidentShowsZeroAttackPoints()
        {
            StrategyResidentAgent resident = CreateResident("Child Resident");
            FieldInfo lifeStage = typeof(StrategyResidentAgent).GetField(
                "lifeStage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lifeStage, Is.Not.Null);
            lifeStage.SetValue(resident, StrategyResidentLifeStage.Child);

            refreshResidentHud.Invoke(selection, new object[] { resident });

            Assert.That(resident.CombatAttackPoints, Is.Zero);
            Assert.That(
                FindText(ResidentHudPath + "AttackChip/Text").text,
                Is.EqualTo(StrategySelectionLocalization.Text("format.attack_short", 0)));
        }

        [Test]
        public void WolfInspectorShowsAndRefreshesHealthAndAttackPoints()
        {
            StrategyWolfAgent wolf = CreateVisibleWolf();
            InvokePrivate(
                selection,
                "UpdateInspectHud",
                Vector3.zero,
                System.Array.Empty<Collider2D>());

            Text healthLabel = FindText(InspectHudPath + "Row_0/Label");
            Text healthValue = FindText(InspectHudPath + "Row_0/Value");
            Text attackLabel = FindText(InspectHudPath + "Row_1/Label");
            Text attackValue = FindText(InspectHudPath + "Row_1/Value");
            Assert.That(healthLabel.text, Is.EqualTo(StrategySelectionLocalization.Text("label.health")));
            Assert.That(
                healthValue.text,
                Is.EqualTo(StrategySelectionLocalization.Text("format.health_points", 100, 100)));
            Assert.That(attackLabel.text, Is.EqualTo(StrategySelectionLocalization.Text("label.attack_points")));
            Assert.That(attackValue.text, Is.EqualTo("20"));
            Assert.That(wolf.CombatAttackPoints, Is.EqualTo(20));

            FieldInfo combatHealth = typeof(StrategyWolfAgent).GetField(
                "combatHealth",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(combatHealth, Is.Not.Null);
            StrategyCombatHealth health = (StrategyCombatHealth)combatHealth.GetValue(wolf);
            Assert.That(health, Is.Not.Null);
            Assert.That(health.ApplyDamage(30).Applied, Is.True);

            InvokePrivate(selection, "RefreshActiveWorldInspectTarget", true);

            Assert.That(
                healthValue.text,
                Is.EqualTo(StrategySelectionLocalization.Text("format.health_points", 70, 100)));
        }

        private StrategyResidentAgent CreateResident(string name)
        {
            GameObject residentObject = new(name);
            residentObject.transform.SetParent(root.transform, false);
            return residentObject.AddComponent<StrategyResidentAgent>();
        }

        private StrategyWolfAgent CreateVisibleWolf()
        {
            GameObject wolfObject = new("Inspect Wolf");
            wolfObject.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = wolfObject.AddComponent<SpriteRenderer>();
            testTexture = new Texture2D(2, 2);
            testSprite = Sprite.Create(
                testTexture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f),
                2f);
            renderer.sprite = testSprite;
            return wolfObject.AddComponent<StrategyWolfAgent>();
        }

        private Text FindText(string path)
        {
            Text text = root.transform.Find(path)?.GetComponent<Text>();
            Assert.That(text, Is.Not.Null, path);
            return text;
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            GetPrivateMethod(target.GetType(), methodName).Invoke(target, arguments);
        }

        private static MethodInfo GetPrivateMethod(System.Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method;
        }
    }
}
