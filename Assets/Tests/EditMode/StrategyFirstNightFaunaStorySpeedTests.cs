using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyFirstNightFaunaStorySpeedTests
    {
        private GameObject root;
        private StrategyInputRouter inputRouter;
        private StrategyTimeScaleController timeScale;
        private StrategyFirstNightFaunaStoryController story;
        private float previousTimeScale;
        private float previousFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            root = new GameObject("First Night Fauna Story Speed Test Root");
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(root.transform, false);

            inputRouter = root.AddComponent<StrategyInputRouter>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(actions, Is.Not.Null);
            Assert.That(inputRouter.Configure(actions), Is.True, inputRouter.ConfigurationError);

            timeScale = root.AddComponent<StrategyTimeScaleController>();
            timeScale.Configure();

            GameObject storyObject = new("First Night Fauna Story");
            storyObject.transform.SetParent(root.transform, false);
            story = storyObject.AddComponent<StrategyFirstNightFaunaStoryController>();
            story.Configure(timeScale, inputRouter);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            Time.timeScale = previousTimeScale;
            Time.fixedDeltaTime = previousFixedDeltaTime;
        }

        [Test]
        public void SuccessfulShowForcesX1WhilePausedAndCleanupResumesAtX1()
        {
            timeScale.SetRequestedScale(3f);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(3f));
            Assert.That(Time.timeScale, Is.EqualTo(3f));

            Assert.That(story.TryShow(null), Is.True);

            Assert.That(story.IsOpen, Is.True);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(1f));
            Assert.That(timeScale.IsPausedByLock, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));

            story.RestoreResolvedState(true);

            Assert.That(story.IsOpen, Is.False);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
        }

        [Test]
        public void RejectedShowLeavesRequestedSpeedUnchanged()
        {
            story.RestoreResolvedState(true);
            timeScale.SetRequestedScale(3f);

            Assert.That(story.TryShow(null), Is.False);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(3f));
            Assert.That(Time.timeScale, Is.EqualTo(3f));
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
        }
    }
}
