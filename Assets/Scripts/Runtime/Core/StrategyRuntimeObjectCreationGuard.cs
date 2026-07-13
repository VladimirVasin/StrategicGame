using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyRuntimeObjectCreationGuard
    {
        private static bool runtimeShuttingDown;

        public static bool CanCreateSceneObjects => Application.isPlaying
            && !runtimeShuttingDown
            && (StrategyGameContext.Current == null
                || StrategyGameContext.Current.State != StrategyGameContextState.Disposed);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            runtimeShuttingDown = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterShutdownHook()
        {
            runtimeShuttingDown = false;
            Application.quitting -= MarkRuntimeShuttingDown;
            Application.quitting += MarkRuntimeShuttingDown;
        }

        private static void MarkRuntimeShuttingDown()
        {
            runtimeShuttingDown = true;
        }
    }
}
