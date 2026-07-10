using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategySaveSystem : MonoBehaviour
    {
        private const string SaveFileName = "strategy-save.json";

        private static StrategySaveData pendingLoad;

        private CityMapController map;
        private StrategyBuildPlacementController placement;
        private StrategyPopulationController population;
        private bool configured;

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static bool HasPendingLoad => pendingLoad != null;

        public void Configure(
            CityMapController mapController,
            StrategyBuildPlacementController placementController,
            StrategyPopulationController populationController)
        {
            map = mapController;
            placement = placementController;
            population = populationController;
            configured = map != null && placement != null && population != null;
            if (configured && pendingLoad != null)
            {
                ApplyPendingLoad();
            }
        }

        private void Update()
        {
            if (!configured || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveNow();
            }
            else if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                RequestLoad();
            }
        }

        public bool SaveNow()
        {
            if (!configured)
            {
                return false;
            }

            try
            {
                StrategySaveData data = CaptureSaveData();
                string json = JsonUtility.ToJson(data, true);
                string temporaryPath = SavePath + ".tmp";
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(SavePath))
                {
                    File.Replace(temporaryPath, SavePath, null);
                }
                else
                {
                    File.Move(temporaryPath, SavePath);
                }

                StrategyEventLogHudController.Notify("Game saved", new Color(0.58f, 0.86f, 0.66f));
                StrategyDebugLogger.Info(
                    "Save",
                    "Saved",
                    StrategyDebugLogger.F("path", SavePath),
                    StrategyDebugLogger.F("buildings", data.buildings.Count),
                    StrategyDebugLogger.F("sites", data.constructionSites.Count),
                    StrategyDebugLogger.F("residents", data.residents.Count));
                return true;
            }
            catch (Exception exception)
            {
                StrategyEventLogHudController.Notify("Save failed", new Color(0.94f, 0.48f, 0.42f));
                StrategyDebugLogger.Warn("Save", "SaveFailed", StrategyDebugLogger.F("error", exception.Message));
                return false;
            }
        }

        public bool RequestLoad()
        {
            if (!TryReadSave(out StrategySaveData data, out string reason))
            {
                StrategyEventLogHudController.Notify(
                    reason == "save_not_found" ? "No save file found" : "Save cannot be loaded",
                    new Color(0.94f, 0.48f, 0.42f));
                StrategyDebugLogger.Warn("Save", "LoadRejected", StrategyDebugLogger.F("reason", reason));
                return false;
            }

            PreparePendingLoad(data);
            StrategyDebugLogger.Info("Save", "LoadRequested", StrategyDebugLogger.F("path", SavePath));
            SceneManager.LoadScene(StrategySceneCatalog.GameplaySceneName);
            return true;
        }

        public static bool TryReadSave(out StrategySaveData data, out string reason)
        {
            data = null;
            if (!File.Exists(SavePath))
            {
                reason = "save_not_found";
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<StrategySaveData>(File.ReadAllText(SavePath));
                if (ValidateSaveData(data, out reason))
                {
                    return true;
                }

                data = null;
                return false;
            }
            catch (Exception exception)
            {
                data = null;
                reason = "read_failed_" + exception.GetType().Name;
                return false;
            }
        }

        public static void PreparePendingLoad(StrategySaveData data)
        {
            pendingLoad = data;
        }

        public static void ClearPendingLoad()
        {
            pendingLoad = null;
        }

        public static bool TryGetPendingMapSeed(out int seed)
        {
            seed = pendingLoad != null ? Mathf.Max(1, pendingLoad.mapSeed) : 0;
            return seed > 0;
        }

        private static bool ValidateSaveData(StrategySaveData data, out string reason)
        {
            if (data == null)
            {
                reason = "empty_or_invalid_json";
                return false;
            }

            if (data.version != StrategySaveData.CurrentVersion)
            {
                reason = "unsupported_version_" + data.version;
                return false;
            }

            if (data.mapSeed <= 0 || data.mapWidth <= 0 || data.mapHeight <= 0)
            {
                reason = "invalid_map_metadata";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
