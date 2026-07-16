using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyMusicShuffleBagTests
    {
        [Test]
        public void EveryTrackPlaysOnceBeforeAnyTrackRepeats()
        {
            StrategyMusicShuffleBag bag = new(count => count - 1);
            int current = -1;

            for (int cycle = 0; cycle < 4; cycle++)
            {
                HashSet<int> played = new();
                for (int i = 0; i < 4; i++)
                {
                    current = bag.PickNext(4, current, null);
                    Assert.That(played.Add(current), Is.True, $"cycle={cycle}, track={current}");
                }

                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, played);
            }
        }

        [Test]
        public void AdjacentTracksNeverRepeatAcrossRotationBoundaries()
        {
            StrategyMusicShuffleBag bag = new(count => count - 1);
            int current = -1;

            for (int i = 0; i < 40; i++)
            {
                int next = bag.PickNext(4, current, null);
                if (current >= 0)
                {
                    Assert.That(next, Is.Not.EqualTo(current), $"selection={i}");
                }

                current = next;
            }
        }

        [Test]
        public void PreferredTracksLeadWithoutStarvingTheRemainingRotation()
        {
            StrategyMusicShuffleBag bag = new(_ => 0);
            int current = -1;
            List<int> played = new();

            for (int i = 0; i < 4; i++)
            {
                current = bag.PickNext(4, current, index => index < 2);
                played.Add(current);
            }

            Assert.That(played[0], Is.LessThan(2));
            Assert.That(played[1], Is.LessThan(2));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, played);
        }

        [Test]
        public void SingleTrackPlaylistRepeatsSafely()
        {
            StrategyMusicShuffleBag bag = new(_ => 0);
            int current = -1;

            for (int i = 0; i < 5; i++)
            {
                current = bag.PickNext(1, current, null);
                Assert.That(current, Is.EqualTo(0));
            }
        }

        [Test]
        public void PlaylistSizeChangeStartsANewCompleteRotation()
        {
            StrategyMusicShuffleBag bag = new(_ => 0);
            int current = bag.PickNext(4, -1, null);
            HashSet<int> playedAfterResize = new();

            for (int i = 0; i < 3; i++)
            {
                current = bag.PickNext(3, current, null);
                Assert.That(playedAfterResize.Add(current), Is.True, $"track={current}");
            }

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, playedAfterResize);
        }
    }
}
