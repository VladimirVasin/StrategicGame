using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyFoundingJourneyController : MonoBehaviour
    {
        private StrategyMapPreloadCoordinator preloader;
        private Camera journeyCamera;
        private StrategyInputRouter inputRouter;

        public bool IsConfigured => preloader != null;
        public bool IsMapReady => preloader != null && preloader.IsMapReady;
        public float PreparationProgress => preloader != null ? preloader.Progress : 0f;
        public string PreparationStage => preloader != null ? preloader.Stage : "Preparing settlement";
        public StrategyPreloadPhase PreloadPhase => preloader != null
            ? preloader.Phase
            : StrategyPreloadPhase.PreparingCandidate;
        public Camera JourneyCamera => journeyCamera;
        public StrategyInputRouter InputRouter => inputRouter;

        public void Configure(
            StrategyMapPreloadCoordinator preloadCoordinator,
            Camera camera,
            StrategyInputRouter router)
        {
            preloader = preloadCoordinator;
            journeyCamera = camera;
            inputRouter = router;
            ConfigureJourney();
        }

        public bool TryGetPreparedMap(out CityMapController map)
        {
            map = null;
            return preloader != null && preloader.TryGetPreparedMap(out map);
        }

        public bool CompleteJourney()
        {
            return preloader != null && preloader.CompleteFoundingJourney();
        }

        public bool ReturnToMainMenu()
        {
            return preloader != null && preloader.CancelFoundingJourney();
        }
    }
}
