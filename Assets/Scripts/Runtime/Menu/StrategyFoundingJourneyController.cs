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
        public string PreparationStage => preloader != null
            ? preloader.Stage
            : StrategyLocalization.Get(StrategyLocalizationTables.Founding, "founding.preparing_settlement");
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
            StrategyLocalization.LanguageChanged -= RefreshLocalizedJourney;
            StrategyLocalization.LanguageChanged += RefreshLocalizedJourney;
            ConfigureJourney();
        }

        private void RefreshLocalizedJourney()
        {
            if (!journeyViewConfigured)
            {
                return;
            }

            switch (page)
            {
                case JourneyPage.Story:
                    ShowStory(storyIndex, true);
                    break;
                case JourneyPage.Question:
                    ShowQuestion(questionIndex);
                    break;
                case JourneyPage.Summary:
                    ShowSummary();
                    break;
                case JourneyPage.Launching:
                    RefreshPreparationStatus();
                    break;
            }
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

        private void OnDestroy()
        {
            StrategyLocalization.LanguageChanged -= RefreshLocalizedJourney;
        }
    }
}
