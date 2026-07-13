using System;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyGameContextTests
    {
        private GameObject contextObject;
        private StrategyGameContext context;

        [SetUp]
        public void SetUp()
        {
            contextObject = new GameObject("Test Strategy Game Context");
            context = contextObject.AddComponent<StrategyGameContext>();
        }

        [TearDown]
        public void TearDown()
        {
            if (contextObject != null)
            {
                UnityEngine.Object.DestroyImmediate(contextObject);
            }

            StrategyGameContextTestService[] services = UnityEngine.Object.FindObjectsByType<StrategyGameContextTestService>(
                FindObjectsInactive.Include);
            for (int i = 0; i < services.Length; i++)
            {
                if (services[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(services[i].gameObject);
                }
            }
        }

        [Test]
        public void RegisteredServiceCanBeResolved()
        {
            StrategyGameContextTestService service = new GameObject("Registered Service")
                .AddComponent<StrategyGameContextTestService>();

            context.Register(service);

            Assert.That(context.TryResolve(out StrategyGameContextTestService resolved), Is.True);
            Assert.That(resolved, Is.SameAs(service));
        }

        [Test]
        public void DifferentServiceForSameTypeIsRejected()
        {
            StrategyGameContextTestService first = new GameObject("First Service")
                .AddComponent<StrategyGameContextTestService>();
            StrategyGameContextTestService second = new GameObject("Second Service")
                .AddComponent<StrategyGameContextTestService>();
            context.Register(first);

            Assert.Throws<InvalidOperationException>(() => context.Register(second));
        }

        [Test]
        public void BootstrapStateTransitionsAreIdempotent()
        {
            Assert.That(context.BeginBootstrap(), Is.True);
            Assert.That(context.BeginBootstrap(), Is.False);
            Assert.That(context.State, Is.EqualTo(StrategyGameContextState.Configuring));

            context.CompleteBootstrap();
            context.CompleteBootstrap();

            Assert.That(context.IsReady, Is.True);
            Assert.That(context.FailureReason, Is.Empty);
        }

        [Test]
        public void BootstrapFailureIsRecordedAndTerminal()
        {
            context.BeginBootstrap();
            context.FailBootstrap(new InvalidOperationException("fixture failure"));

            Assert.That(context.State, Is.EqualTo(StrategyGameContextState.Failed));
            Assert.That(context.FailureReason, Does.Contain("fixture failure"));
            Assert.That(context.BeginBootstrap(), Is.False);
        }
    }

    public sealed class StrategyGameContextTestService : MonoBehaviour
    {
    }
}
