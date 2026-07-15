using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
        private StrategyInputRouter inputRouter;
        private StrategyFoundingStartSaveData foundingStart = new();
        private bool configured;

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BackupPath => SavePath + ".bak";
        public static bool HasPendingLoad => pendingLoad != null;

        public void SetInputRouter(StrategyInputRouter router)
        {
            inputRouter = router;
        }

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
                foundingStart = CopyFoundingStartData(pendingLoad.foundingStart);
                ApplyPendingLoad();
            }
        }

        private void Update()
        {
            if (!configured || inputRouter == null)
            {
                return;
            }

            if (inputRouter.GlobalSavePressed)
            {
                SaveNow();
            }
            else if (inputRouter.GlobalLoadPressed)
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
                if (!ValidateSaveData(data, out string validationReason))
                {
                    throw new InvalidDataException("Captured save is invalid: " + validationReason);
                }

                string json = JsonUtility.ToJson(data, true);
                WriteSaveAtomically(json, SavePath, BackupPath);

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
            bool loaded = TryReadSaveFromPaths(SavePath, BackupPath, out data, out reason, out bool usedBackup);
            if (loaded && usedBackup)
            {
                StrategyDebugLogger.Warn(
                    "Save",
                    "RecoveredFromBackup",
                    StrategyDebugLogger.F("primaryPath", SavePath),
                    StrategyDebugLogger.F("backupPath", BackupPath));
            }

            return loaded;
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

        internal static bool TryGetPendingTrailCells(
            int mapWidth,
            int mapHeight,
            out List<int> trailCells)
        {
            trailCells = null;
            if (pendingLoad == null
                || pendingLoad.mapWidth != mapWidth
                || pendingLoad.mapHeight != mapHeight
                || !ValidateSaveData(pendingLoad, out _))
            {
                return false;
            }

            trailCells = new List<int>(pendingLoad.trailCells);
            return true;
        }

        public void SetFoundingStartData(StrategyFoundingStartSaveData data)
        {
            foundingStart = CopyFoundingStartData(data);
        }

        public static bool TryGetPendingFoundingStartData(out StrategyFoundingStartSaveData data)
        {
            data = pendingLoad != null
                ? CopyFoundingStartData(pendingLoad.foundingStart)
                : null;
            return data != null;
        }

    }
}
