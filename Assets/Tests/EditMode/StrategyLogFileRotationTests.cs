using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyLogFileRotationTests
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        [Test]
        public void RotationBoundaryIsEightMiBInclusive()
        {
            Assert.That(
                StrategyLogFileRotation.ShouldRotate(StrategyLogFileRotation.MaxActiveBytes - 1L),
                Is.False);
            Assert.That(
                StrategyLogFileRotation.ShouldRotate(StrategyLogFileRotation.MaxActiveBytes),
                Is.True);
            Assert.That(
                StrategyLogFileRotation.ShouldRotate(StrategyLogFileRotation.MaxActiveBytes + 1L),
                Is.True);
        }

        [Test]
        public void NamesUseDeterministicUtcTimestampOrdinalAndDistinctPrefixes()
        {
            DateTime utc = new DateTime(2026, 7, 13, 10, 20, 30, 456, DateTimeKind.Utc);

            string archive = StrategyLogFileRotation.BuildArchiveFileName(utc, 7);
            string live = StrategyLogFileRotation.BuildLiveFileName(utc, 4242, 7);

            Assert.That(archive, Is.EqualTo("debug-archive-20260713T102030456Z-007.log"));
            Assert.That(live, Is.EqualTo("debug-live-20260713T102030456Z-p4242-007.log"));
            Assert.That(archive, Does.Not.StartWith(StrategyLogFileRotation.LivePrefix));
            Assert.That(live, Does.Not.StartWith(StrategyLogFileRotation.ArchivePrefix));
        }

        [Test]
        public void RetentionUsesOrdinalNameAsStableTimestampTieBreakAndIgnoresLiveFiles()
        {
            string directory = CreateTemporaryDirectory();
            try
            {
                DateTime utc = new DateTime(2026, 7, 13, 10, 20, 30, DateTimeKind.Utc);
                DateTime tiedWriteTime = new DateTime(2026, 7, 13, 11, 0, 0, DateTimeKind.Utc);
                string[] archives = Enumerable.Range(0, 4)
                    .Select(ordinal => Path.Combine(
                        directory,
                        StrategyLogFileRotation.BuildArchiveFileName(utc, ordinal)))
                    .ToArray();
                for (int i = 0; i < archives.Length; i++)
                {
                    File.WriteAllText(archives[i], i.ToString(), Utf8NoBom);
                    File.SetLastWriteTimeUtc(archives[i], tiedWriteTime);
                }

                string live = Path.Combine(
                    directory,
                    StrategyLogFileRotation.BuildLiveFileName(utc, 99, 0));
                File.WriteAllText(live, "live", Utf8NoBom);
                File.SetLastWriteTimeUtc(live, tiedWriteTime.AddYears(-1));

                StrategyLogFileRotation.PruneArchives(directory, 3);

                Assert.That(File.Exists(archives[0]), Is.False);
                Assert.That(File.Exists(archives[1]), Is.True);
                Assert.That(File.Exists(archives[2]), Is.True);
                Assert.That(File.Exists(archives[3]), Is.True);
                Assert.That(File.Exists(live), Is.True, "A live fallback may belong to another process.");
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void OpeningSessionPreservesPreviousCanonicalLogInArchive()
        {
            string directory = CreateTemporaryDirectory();
            StreamWriter writer = null;
            try
            {
                string canonical = Path.Combine(directory, "debug.log");
                const string previousSession = "previous session line";
                File.WriteAllText(canonical, previousSession, Utf8NoBom);
                DateTime utc = new DateTime(2026, 7, 13, 10, 20, 30, DateTimeKind.Utc);

                bool opened = StrategyLogFileRotation.TryOpenSession(
                    canonical,
                    utc,
                    42,
                    Utf8NoBom,
                    out writer,
                    out string activePath);

                Assert.That(opened, Is.True);
                Assert.That(activePath, Is.EqualTo(canonical));
                writer.WriteLine("current session line");
                writer.Dispose();
                writer = null;

                string archiveDirectory = StrategyLogFileRotation.ResolveArchiveDirectory(canonical);
                string[] archives = Directory.GetFiles(
                    archiveDirectory,
                    StrategyLogFileRotation.ArchivePrefix + "*.log");
                Assert.That(archives, Has.Length.EqualTo(1));
                Assert.That(File.ReadAllText(archives[0], Utf8NoBom), Is.EqualTo(previousSession));
                Assert.That(File.ReadAllText(canonical, Utf8NoBom), Does.Contain("current session line"));
            }
            finally
            {
                writer?.Dispose();
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void RotationArchivesFullFileAndReopensActivePath()
        {
            string directory = CreateTemporaryDirectory();
            StreamWriter writer = null;
            try
            {
                string canonical = Path.Combine(directory, "debug.log");
                File.WriteAllText(canonical, "full active log", Utf8NoBom);
                DateTime utc = new DateTime(2026, 7, 13, 10, 20, 30, DateTimeKind.Utc);

                bool reopened = StrategyLogFileRotation.TryRotateAndReopen(
                    canonical,
                    canonical,
                    utc,
                    42,
                    Utf8NoBom,
                    out writer,
                    out string activePath);

                Assert.That(reopened, Is.True);
                Assert.That(activePath, Is.EqualTo(canonical));
                Assert.That(new FileInfo(canonical).Length, Is.EqualTo(0L));
                string archiveDirectory = StrategyLogFileRotation.ResolveArchiveDirectory(canonical);
                string archive = Directory.GetFiles(archiveDirectory).Single();
                Assert.That(File.ReadAllText(archive, Utf8NoBom), Is.EqualTo("full active log"));
            }
            finally
            {
                writer?.Dispose();
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void ExclusiveCanonicalLockUsesUniqueLiveFallbackOnWindows()
        {
            if (Path.DirectorySeparatorChar != '\\')
            {
                Assert.Ignore("FileShare.None fallback semantics are verified on Windows.");
            }

            string directory = CreateTemporaryDirectory();
            StreamWriter writer = null;
            FileStream canonicalLock = null;
            try
            {
                string canonical = Path.Combine(directory, "debug.log");
                File.WriteAllText(canonical, "locked previous session", Utf8NoBom);
                canonicalLock = new FileStream(
                    canonical,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
                DateTime utc = new DateTime(2026, 7, 13, 10, 20, 30, DateTimeKind.Utc);

                bool opened = StrategyLogFileRotation.TryOpenSession(
                    canonical,
                    utc,
                    4242,
                    Utf8NoBom,
                    out writer,
                    out string activePath);

                Assert.That(opened, Is.True, "A locked canonical log must not disable file logging.");
                Assert.That(activePath, Is.Not.EqualTo(canonical));
                Assert.That(
                    Path.GetFileName(activePath),
                    Is.EqualTo("debug-live-20260713T102030000Z-p4242-000.log"));
                writer.WriteLine("fallback session line");
                writer.Dispose();
                writer = null;
                Assert.That(File.ReadAllText(activePath, Utf8NoBom), Does.Contain("fallback session line"));
            }
            finally
            {
                writer?.Dispose();
                canonicalLock?.Dispose();
                DeleteTemporaryDirectory(directory);
            }
        }

        private static string CreateTemporaryDirectory()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "ProjectUnknown-LogRotationTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void DeleteTemporaryDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
