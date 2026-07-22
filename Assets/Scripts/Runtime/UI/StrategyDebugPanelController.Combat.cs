using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDebugPanelController
    {
        private StrategyCombatEncounterController combatEncounter;
        private StrategyNightEncounterDirector nightEncounterDirector;
        private StrategyLocalizedTextBinding combatStatusBinding;

        public void ConfigureCombat(
            StrategyCombatEncounterController encounterController,
            StrategyNightEncounterDirector director)
        {
            combatEncounter = encounterController;
            nightEncounterDirector = director;
            RefreshCombatControls();
        }

        private void BuildCombatSection(RectTransform panel)
        {
            RectTransform section = CreatePanel(
                "CombatSection",
                panel,
                SectionColor).GetComponent<RectTransform>();
            SetTopLeft(section, 28f, 564f, PanelWidth - 56f, 128f);

            Text title = CreateText(
                "CombatTitle",
                section,
                string.Empty,
                16,
                TextAnchor.MiddleLeft,
                Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopLeft(title.rectTransform, 18f, 8f, 220f, 24f);
            StrategyLocalizedTextBinding.Bind(
                title,
                StrategyLocalizationTables.Hud,
                "hud.debug.combat.title");

            Button startNow = CreateButton(
                "StartCombatNow",
                section,
                string.Empty,
                13,
                ButtonColor);
            SetTopLeft(startNow.GetComponent<RectTransform>(), 18f, 38f, 150f, 32f);
            BindButtonLabel(startNow, "hud.debug.combat.action.start_now");
            startNow.onClick.AddListener(StartCombatNow);

            Button startNight = CreateButton(
                "StartNightCombatScenario",
                section,
                string.Empty,
                13,
                ButtonColor);
            SetTopLeft(startNight.GetComponent<RectTransform>(), 178f, 38f, 160f, 32f);
            BindButtonLabel(startNight, "hud.debug.combat.action.start_night");
            startNight.onClick.AddListener(StartNightCombatScenario);

            Button reset = CreateButton(
                "ResetCombatScenario",
                section,
                string.Empty,
                13,
                ButtonColor);
            SetTopLeft(reset.GetComponent<RectTransform>(), 348f, 38f, 94f, 32f);
            BindButtonLabel(reset, "hud.debug.combat.action.reset");
            reset.onClick.AddListener(ResetCombatScenario);

            Text status = CreateText(
                "CombatStatus",
                section,
                string.Empty,
                12,
                TextAnchor.MiddleLeft,
                MutedTextColor);
            SetTopLeft(status.rectTransform, 18f, 78f, 568f, 38f);
            combatStatusBinding = StrategyLocalizedTextBinding.Bind(
                status,
                StrategyLocalizationTables.Hud,
                "hud.debug.combat.status.unavailable");
        }

        private void StartCombatNow()
        {
            FindCombatControllers();
            nightEncounterDirector?.CancelPendingEncounter();
            combatEncounter?.TryStartEncounter();
            RefreshCombatControls();
        }

        private void StartNightCombatScenario()
        {
            FindCombatControllers();
            bool armed = nightEncounterDirector != null
                && nightEncounterDirector.ArmGuaranteedNightEncounter();
            RefreshCombatControls();
            if (armed)
            {
                SetOpen(false);
            }
        }

        private void ResetCombatScenario()
        {
            FindCombatControllers();
            if (nightEncounterDirector != null)
            {
                nightEncounterDirector.ResetEncounter();
            }
            else
            {
                combatEncounter?.ResetEncounter();
            }

            RefreshCombatControls();
        }

        private void FindCombatControllers()
        {
            combatEncounter ??=
                Object.FindAnyObjectByType<StrategyCombatEncounterController>();
            nightEncounterDirector ??=
                Object.FindAnyObjectByType<StrategyNightEncounterDirector>();
        }

        private void RefreshCombatControls()
        {
            if (combatStatusBinding == null)
            {
                return;
            }

            FindCombatControllers();
            if (combatEncounter == null
                || nightEncounterDirector == null
                || !nightEncounterDirector.IsConfigured)
            {
                ConfigureCombatStatus("hud.debug.combat.status.unavailable");
                return;
            }

            if (nightEncounterDirector.HasGuaranteedNightEncounterPending)
            {
                ConfigureCombatStatus("hud.debug.combat.status.queued_for_night");
                return;
            }

            StrategyCombatEncounterStatus status = combatEncounter.Status;
            switch (status.Kind)
            {
                case StrategyCombatEncounterStatusKind.Ready:
                    ConfigureCombatStatus("hud.debug.combat.status.ready");
                    break;
                case StrategyCombatEncounterStatusKind.ControllersUnavailable:
                    ConfigureCombatStatus("hud.debug.combat.status.unavailable");
                    break;
                case StrategyCombatEncounterStatusKind.NoResidentAvailable:
                    ConfigureCombatStatus("hud.debug.combat.status.no_resident");
                    break;
                case StrategyCombatEncounterStatusKind.StagingFailed:
                    ConfigureCombatStatus("hud.debug.combat.status.stage_failed");
                    break;
                case StrategyCombatEncounterStatusKind.StartFailed:
                    ConfigureCombatStatus("hud.debug.combat.status.start_failed");
                    break;
                case StrategyCombatEncounterStatusKind.ResetComplete:
                    ConfigureCombatStatus("hud.debug.combat.status.reset_complete");
                    break;
                case StrategyCombatEncounterStatusKind.ResidentLost:
                    ConfigureCombatStatus("hud.debug.combat.status.resident_lost");
                    break;
                case StrategyCombatEncounterStatusKind.WolfDefeated:
                    ConfigureCombatStatus(
                        "hud.debug.combat.status.wolf_defeated",
                        status.ResidentHealth,
                        status.ResidentMaxHealth);
                    break;
                case StrategyCombatEncounterStatusKind.WolfRetreating:
                    ConfigureCombatStatus(
                        "hud.debug.combat.status.wolf_retreating",
                        status.ResidentHealth,
                        status.ResidentMaxHealth);
                    break;
                case StrategyCombatEncounterStatusKind.Running:
                    ConfigureCombatStatus(
                        "hud.debug.combat.status.running",
                        status.ResidentName,
                        status.ResidentHealth,
                        status.ResidentMaxHealth,
                        status.WolfHealth,
                        status.WolfMaxHealth);
                    break;
                default:
                    ConfigureCombatStatus("hud.debug.combat.status.unavailable");
                    break;
            }
        }

        private void ConfigureCombatStatus(string key, params object[] arguments)
        {
            combatStatusBinding.Configure(
                StrategyLocalizationTables.Hud,
                key,
                arguments);
        }

        private static void BindButtonLabel(Button button, string key)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label != null)
            {
                StrategyLocalizedTextBinding.Bind(
                    label,
                    StrategyLocalizationTables.Hud,
                    key);
            }
        }
    }
}
