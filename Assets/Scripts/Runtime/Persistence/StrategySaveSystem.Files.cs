using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        internal const long MaxSaveFileBytes = 32L * 1024L * 1024L;

        internal static void WriteSaveAtomically(string json, string primaryPath, string backupPath)
        {
            if (string.IsNullOrWhiteSpace(primaryPath) || string.IsNullOrWhiteSpace(backupPath))
            {
                throw new ArgumentException("Save paths must be provided.");
            }

            string saveJson = json ?? string.Empty;
            if (Encoding.UTF8.GetByteCount(saveJson) > MaxSaveFileBytes)
            {
                throw new InvalidOperationException("Save payload exceeds the supported size limit.");
            }

            string directory = Path.GetDirectoryName(primaryPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = primaryPath + ".tmp";
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                File.WriteAllText(temporaryPath, saveJson, new UTF8Encoding(false));
                if (File.Exists(primaryPath))
                {
                    File.Replace(temporaryPath, primaryPath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, primaryPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        internal static bool TryReadSaveFromPaths(
            string primaryPath,
            string backupPath,
            out StrategySaveData data,
            out string reason,
            out bool usedBackup)
        {
            data = null;
            usedBackup = false;
            bool primaryExists = File.Exists(primaryPath);
            bool backupExists = File.Exists(backupPath);
            if (!primaryExists && !backupExists)
            {
                reason = "save_not_found";
                return false;
            }

            string primaryReason = "save_not_found";
            if (primaryExists && TryReadSingleSave(primaryPath, out data, out primaryReason))
            {
                reason = string.Empty;
                return true;
            }

            string backupReason = "save_not_found";
            if (backupExists && TryReadSingleSave(backupPath, out data, out backupReason))
            {
                usedBackup = true;
                reason = string.Empty;
                return true;
            }

            data = null;
            reason = primaryExists
                ? "primary_" + primaryReason + "__backup_" + backupReason
                : "backup_" + backupReason;
            return false;
        }

        internal static bool TryDeserializeAndValidate(
            string json,
            out StrategySaveData data,
            out string reason,
            out bool migrated)
        {
            data = null;
            migrated = false;
            try
            {
                if (string.IsNullOrEmpty(json)
                    || Encoding.UTF8.GetByteCount(json) > MaxSaveFileBytes)
                {
                    reason = string.IsNullOrEmpty(json) ? "empty_or_invalid_json" : "save_too_large";
                    return false;
                }

                data = JsonUtility.FromJson<StrategySaveData>(json);
                int sourceVersion = data != null ? data.version : 0;
                if (!StrategySaveMigration.TryMigrate(data, out reason))
                {
                    data = null;
                    return false;
                }

                migrated = sourceVersion != data.version;
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

        private static bool TryReadSingleSave(string path, out StrategySaveData data, out string reason)
        {
            try
            {
                FileInfo file = new(path);
                if (file.Length > MaxSaveFileBytes)
                {
                    data = null;
                    reason = "file_too_large";
                    return false;
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                return TryDeserializeAndValidate(json, out data, out reason, out _);
            }
            catch (Exception exception)
            {
                data = null;
                reason = "read_failed_" + exception.GetType().Name;
                return false;
            }
        }
    }
}
