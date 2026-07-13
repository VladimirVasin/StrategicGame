using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static void VerifyRuntimeInput(
            StrategyGameContext context,
            bool requireNoActiveContexts = true)
        {
            StrategyInputRouter[] routers = Object.FindObjectsByType<StrategyInputRouter>(
                FindObjectsInactive.Include);
            Require(routers.Length == 1, "Exactly one StrategyInputRouter must exist in the active scene");
            Require(routers[0].IsConfigured, "StrategyInputRouter is not configured: " + routers[0].ConfigurationError);
            Require(routers[0].IsAvailable, "StrategyInputRouter is not active and enabled");
            if (requireNoActiveContexts)
            {
                Require(routers[0].ActiveContextCount == 0, "Input contexts leaked during bootstrap");
            }

            if (context != null)
            {
                Require(
                    context.TryResolve(out StrategyInputRouter registered) && registered == routers[0],
                    "Gameplay input router is not registered in StrategyGameContext");
            }

            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            Require(eventSystems.Length == 1, "Exactly one EventSystem must exist in the active scene");
            InputSystemUIInputModule[] modules = Object.FindObjectsByType<InputSystemUIInputModule>(
                FindObjectsInactive.Include);
            Require(modules.Length == 1, "Exactly one InputSystemUIInputModule must exist in the active scene");
            Require(
                modules[0].actionsAsset == InputSystem.actions,
                "UI input module is not bound to the project-wide input asset");
        }
    }
}
