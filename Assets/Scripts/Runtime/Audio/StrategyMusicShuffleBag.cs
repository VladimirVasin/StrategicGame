using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyMusicShuffleBag
    {
        private readonly List<int> remainingTrackIndices = new();
        private readonly Func<int, int> chooseOffset;
        private int trackCount = -1;

        internal StrategyMusicShuffleBag(Func<int, int> offsetSelector = null)
        {
            chooseOffset = offsetSelector ?? ChooseRandomOffset;
        }

        internal int RemainingCount => remainingTrackIndices.Count;

        internal void Reset(int newTrackCount)
        {
            trackCount = Mathf.Max(0, newTrackCount);
            Refill();
        }

        internal int PickNext(int newTrackCount, int currentTrackIndex, Func<int, bool> isPreferred)
        {
            if (newTrackCount <= 0)
            {
                Reset(0);
                return -1;
            }

            if (trackCount != newTrackCount)
            {
                Reset(newTrackCount);
            }
            else if (remainingTrackIndices.Count == 0)
            {
                Refill();
            }

            int selected = TakeCandidate(currentTrackIndex, isPreferred, true);
            if (selected < 0)
            {
                selected = TakeCandidate(currentTrackIndex, null, true);
            }

            return selected >= 0
                ? selected
                : TakeCandidate(currentTrackIndex, null, false);
        }

        private void Refill()
        {
            remainingTrackIndices.Clear();
            for (int i = 0; i < trackCount; i++)
            {
                remainingTrackIndices.Add(i);
            }
        }

        private int TakeCandidate(
            int currentTrackIndex,
            Func<int, bool> predicate,
            bool excludeCurrent)
        {
            int candidateCount = 0;
            for (int i = 0; i < remainingTrackIndices.Count; i++)
            {
                int trackIndex = remainingTrackIndices[i];
                if ((!excludeCurrent || trackIndex != currentTrackIndex)
                    && (predicate == null || predicate(trackIndex)))
                {
                    candidateCount++;
                }
            }

            if (candidateCount <= 0)
            {
                return -1;
            }

            int selectedOffset = NormalizeOffset(chooseOffset(candidateCount), candidateCount);
            for (int i = 0; i < remainingTrackIndices.Count; i++)
            {
                int trackIndex = remainingTrackIndices[i];
                if ((excludeCurrent && trackIndex == currentTrackIndex)
                    || (predicate != null && !predicate(trackIndex)))
                {
                    continue;
                }

                if (selectedOffset-- != 0)
                {
                    continue;
                }

                remainingTrackIndices.RemoveAt(i);
                return trackIndex;
            }

            return -1;
        }

        private static int NormalizeOffset(int offset, int count)
        {
            int normalized = offset % count;
            return normalized < 0 ? normalized + count : normalized;
        }

        private static int ChooseRandomOffset(int count)
        {
            return UnityEngine.Random.Range(0, count);
        }
    }
}
