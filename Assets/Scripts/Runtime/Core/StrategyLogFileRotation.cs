using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyLogFileRotation
    {
        public const long MaxActiveBytes = 8L * 1024L * 1024L;
        public const int ArchiveRetentionCount = 3;

        internal const string ArchivePrefix = "debug-archive-";
        internal const string LivePrefix = "debug-live-";
        private const string LogDirectoryName = "Logs";
        private const string CanonicalLogFileName = "debug.log";
        private const int MaximumNameAttempts = 1000;

        public static bool ShouldRotate(long activeLength)
        {
            return activeLength >= MaxActiveBytes;
        }

        internal static string BuildArchiveFileName(DateTime utcNow, int ordinal)
        {
            return ArchivePrefix + FormatUtc(utcNow) + "-" + FormatOrdinal(ordinal) + ".log";
        }

        internal static string BuildLiveFileName(DateTime utcNow, int processId, int ordinal)
        {
            return LivePrefix
                + FormatUtc(utcNow)
                + "-p"
                + processId.ToString(CultureInfo.InvariantCulture)
                + "-"
                + FormatOrdinal(ordinal)
                + ".log";
        }

        internal static string ResolveArchiveDirectory(string canonicalPath)
        {
            string root = Path.GetDirectoryName(canonicalPath) ?? ".";
            if (string.Equals(
                    Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                    LogDirectoryName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(root, "StrategyDebug");
            }

            return Path.Combine(root, LogDirectoryName, "StrategyDebug");
        }

        internal static string ResolveBuildCanonicalPath(
            string applicationDataPath,
            string persistentDataPath)
        {
            try
            {
                string gameDirectory = Directory.GetParent(applicationDataPath)?.FullName;
                if (!string.IsNullOrWhiteSpace(gameDirectory))
                {
                    return Path.Combine(gameDirectory, LogDirectoryName, CanonicalLogFileName);
                }
            }
            catch
            {
                // Invalid or unavailable player paths fall back to Unity's writable data directory.
            }

            return ResolvePersistentCanonicalPath(persistentDataPath);
        }

        internal static string ResolvePersistentCanonicalPath(string persistentDataPath)
        {
            string root = string.IsNullOrWhiteSpace(persistentDataPath) ? "." : persistentDataPath;
            return Path.Combine(root, CanonicalLogFileName);
        }

        internal static bool TryOpenSessionWithFallback(
            string preferredCanonicalPath,
            string fallbackCanonicalPath,
            DateTime utcNow,
            int processId,
            Encoding encoding,
            out StreamWriter writer,
            out string activePath,
            out string selectedCanonicalPath)
        {
            selectedCanonicalPath = preferredCanonicalPath;
            if (TryOpenSession(
                    preferredCanonicalPath,
                    utcNow,
                    processId,
                    encoding,
                    out writer,
                    out activePath))
            {
                return true;
            }

            if (PathsEqual(preferredCanonicalPath, fallbackCanonicalPath))
            {
                writer = null;
                activePath = string.Empty;
                return false;
            }

            selectedCanonicalPath = fallbackCanonicalPath;
            return TryOpenSession(
                fallbackCanonicalPath,
                utcNow,
                processId,
                encoding,
                out writer,
                out activePath);
        }

        internal static bool TryOpenSession(
            string canonicalPath,
            DateTime utcNow,
            int processId,
            Encoding encoding,
            out StreamWriter writer,
            out string activePath)
        {
            writer = null;
            activePath = canonicalPath;

            bool canonicalReady = TryPreservePreviousCanonical(canonicalPath, utcNow);
            if (canonicalReady && TryOpenWriter(canonicalPath, FileMode.Create, encoding, out writer))
            {
                return true;
            }

            return TryOpenFallback(
                canonicalPath,
                utcNow,
                processId,
                encoding,
                out writer,
                out activePath);
        }

        internal static bool TryRotateAndReopen(
            string activePath,
            string canonicalPath,
            DateTime utcNow,
            int processId,
            Encoding encoding,
            out StreamWriter writer,
            out string reopenedPath)
        {
            writer = null;
            reopenedPath = activePath;

            bool archived = TryArchiveNonEmptyFile(activePath, utcNow);
            FileMode reopenMode = archived ? FileMode.Create : FileMode.Append;
            if (TryOpenWriter(activePath, reopenMode, encoding, out writer))
            {
                return true;
            }

            return TryOpenFallback(
                canonicalPath,
                utcNow,
                processId,
                encoding,
                out writer,
                out reopenedPath);
        }

        internal static bool TryOpenFallback(
            string canonicalPath,
            DateTime utcNow,
            int processId,
            Encoding encoding,
            out StreamWriter writer,
            out string activePath)
        {
            writer = null;
            activePath = string.Empty;
            string root = Path.GetDirectoryName(canonicalPath) ?? ".";

            try
            {
                Directory.CreateDirectory(root);
            }
            catch
            {
                return false;
            }

            for (int ordinal = 0; ordinal < MaximumNameAttempts; ordinal++)
            {
                string candidate = Path.Combine(root, BuildLiveFileName(utcNow, processId, ordinal));
                if (File.Exists(candidate))
                {
                    continue;
                }

                if (TryOpenWriter(candidate, FileMode.CreateNew, encoding, out writer))
                {
                    activePath = candidate;
                    return true;
                }

                if (!File.Exists(candidate))
                {
                    return false;
                }
            }

            return false;
        }

        internal static void PruneArchives(string archiveDirectory, int retainCount)
        {
            if (retainCount < 0)
            {
                retainCount = 0;
            }

            List<ArchiveFile> archives;
            try
            {
                archives = Directory
                    .EnumerateFiles(archiveDirectory, ArchivePrefix + "*.log", SearchOption.TopDirectoryOnly)
                    .Select(CreateArchiveFile)
                    .OrderByDescending(file => file.LastWriteUtc)
                    .ThenByDescending(file => file.Name, StringComparer.Ordinal)
                    .ToList();
            }
            catch
            {
                return;
            }

            for (int i = retainCount; i < archives.Count; i++)
            {
                try
                {
                    File.Delete(archives[i].Path);
                }
                catch
                {
                    // Archive cleanup is best-effort and must never interrupt logging.
                }
            }
        }

        private static bool TryPreservePreviousCanonical(string canonicalPath, DateTime utcNow)
        {
            try
            {
                if (!File.Exists(canonicalPath) || new FileInfo(canonicalPath).Length == 0L)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return TryArchiveNonEmptyFile(canonicalPath, utcNow);
        }

        private static bool TryArchiveNonEmptyFile(string sourcePath, DateTime utcNow)
        {
            try
            {
                if (!File.Exists(sourcePath) || new FileInfo(sourcePath).Length == 0L)
                {
                    return true;
                }

                string archiveDirectory = ResolveArchiveDirectory(sourcePath);
                Directory.CreateDirectory(archiveDirectory);

                for (int ordinal = 0; ordinal < MaximumNameAttempts; ordinal++)
                {
                    string archivePath = Path.Combine(
                        archiveDirectory,
                        BuildArchiveFileName(utcNow, ordinal));
                    if (File.Exists(archivePath))
                    {
                        continue;
                    }

                    try
                    {
                        File.Move(sourcePath, archivePath);
                        TryStampArchiveAsNewest(archivePath, utcNow);
                        PruneArchives(archiveDirectory, ArchiveRetentionCount);
                        return true;
                    }
                    catch (IOException)
                    {
                        if (File.Exists(archivePath))
                        {
                            continue;
                        }

                        return false;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void TryStampArchiveAsNewest(string archivePath, DateTime utcNow)
        {
            try
            {
                File.SetLastWriteTimeUtc(archivePath, utcNow.ToUniversalTime());
            }
            catch
            {
                // A preserved archive remains usable even when its timestamp cannot be updated.
            }
        }

        private static bool TryOpenWriter(
            string path,
            FileMode mode,
            Encoding encoding,
            out StreamWriter writer)
        {
            writer = null;
            FileStream stream = null;
            try
            {
                string directory = Path.GetDirectoryName(path) ?? ".";
                Directory.CreateDirectory(directory);
                stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read);
                writer = new StreamWriter(stream, encoding)
                {
                    AutoFlush = false
                };
                stream = null;
                return true;
            }
            catch
            {
                writer?.Dispose();
                stream?.Dispose();
                writer = null;
                return false;
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            try
            {
                StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                return string.Equals(
                    Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    comparison);
            }
            catch
            {
                return string.Equals(left, right, StringComparison.Ordinal);
            }
        }

        private static ArchiveFile CreateArchiveFile(string path)
        {
            DateTime lastWriteUtc;
            try
            {
                lastWriteUtc = File.GetLastWriteTimeUtc(path);
            }
            catch
            {
                lastWriteUtc = DateTime.MinValue;
            }

            return new ArchiveFile(path, Path.GetFileName(path), lastWriteUtc);
        }

        private static string FormatUtc(DateTime value)
        {
            return value.ToUniversalTime().ToString(
                "yyyyMMdd'T'HHmmssfff'Z'",
                CultureInfo.InvariantCulture);
        }

        private static string FormatOrdinal(int ordinal)
        {
            return Math.Max(0, ordinal).ToString("D3", CultureInfo.InvariantCulture);
        }

        private readonly struct ArchiveFile
        {
            public ArchiveFile(string path, string name, DateTime lastWriteUtc)
            {
                Path = path;
                Name = name;
                LastWriteUtc = lastWriteUtc;
            }

            public string Path { get; }
            public string Name { get; }
            public DateTime LastWriteUtc { get; }
        }
    }
}
