using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyForestrySfxAudio
    {
        private const string TreeFallPath = "Audio/WorkSfx/TreeFall";
        private const string TreeBreakLogsPath = "Audio/WorkSfx/TreeBreakLogs";

        private static AudioClip[] treeFallClips;
        private static AudioClip[] treeBreakLogsClips;
        private static int clipCursor;

        public static void PlayTreeFall(Vector3 worldPosition)
        {
            EnsureClipsLoaded();
            PlayClip(treeFallClips, worldPosition, 0.34f, 0.92f, 1.04f, "tree_fall", 0.18f);
        }

        public static void PlayTreeBreakLogs(Vector3 worldPosition)
        {
            EnsureClipsLoaded();
            PlayClip(treeBreakLogsClips, worldPosition, 0.32f, 0.94f, 1.06f, "tree_logs", 0.14f);
        }

        private static void EnsureClipsLoaded()
        {
            if (treeFallClips != null)
            {
                return;
            }

            treeFallClips = LoadSorted(TreeFallPath);
            treeBreakLogsClips = LoadSorted(TreeBreakLogsPath);
        }

        private static AudioClip[] LoadSorted(string path)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
            if (clips != null && clips.Length > 1)
            {
                Array.Sort(clips, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            }

            return clips;
        }

        private static void PlayClip(
            AudioClip[] clips,
            Vector3 worldPosition,
            float volume,
            float pitchMin,
            float pitchMax,
            string key,
            float cooldown)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            AudioClip clip = clips[Mathf.Abs(clipCursor++) % clips.Length];
            StrategyAudioVoicePool.Play(
                clip,
                worldPosition,
                StrategyAudioBus.ImportantEvents,
                volume,
                pitchMin,
                pitchMax,
                StrategyAudioPriority.Important,
                key,
                cooldown,
                2,
                0.84f,
                5f,
                48f);
        }
    }
}
