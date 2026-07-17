using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed partial class StrategyInGameCinematicPlayerTests
    {
        [Test]
        public void RejectedPlaybackLeavesRequestedSpeedUnchanged()
        {
            Assert.That(timeScale.CurrentScale, Is.EqualTo(2f));
            Assert.That(Time.timeScale, Is.EqualTo(2f));

            Assert.That(
                player.TryPlay(null, StrategyInGameCinematicOptions.Default, null),
                Is.False);

            Assert.That(timeScale.CurrentScale, Is.EqualTo(2f));
            Assert.That(Time.timeScale, Is.EqualTo(2f));
        }

        [Test]
        public void AnimatedCameraReturnRetainsOwnershipAndRejectsReentry()
        {
            BlockingSequence sequence = new();
            StrategyInGameCinematicOptions options = new(
                10f,
                0f,
                0.25f,
                2.39f,
                StrategyCinematicLetterboxView.DefaultMinimumBarFraction,
                0f);
            Assert.That(player.TryPlay(sequence, options, null), Is.True);
            EnsureBeginStarted(sequence);
            letterbox = player.GetComponentInChildren<StrategyCinematicLetterboxView>(true);
            Assert.That(letterbox, Is.Not.Null);

            cameraController.FocusOn(sequence.FocusCenter, sequence.FocusSize);
            letterbox.SetReveal(1f);
            sequence.MarkOpeningComplete();
            InvokePrivate(player, "StopPlaybackRoutine");
            DrivePlayStart(sequence);
            InvokePrivate(
                player,
                "CompletePlayback",
                StrategyInGameCinematicResult.Completed,
                true);

            Assert.That(player.IsPlaying, Is.True);
            Assert.That(player.CanPlay, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));
            Assert.That(inputRouter.BlockedChannels, Is.EqualTo(StrategyInputChannel.All));
            Assert.That(timeScale.IsPausedByLock, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(letterbox.IsInputShieldActive, Is.True);
            Assert.That(letterbox.Reveal, Is.EqualTo(1f).Within(0.0001f));

            BlockingSequence reentrant = new();
            Assert.That(
                player.TryPlay(reentrant, options, null),
                Is.False,
                "A second cinematic must not capture an intermediate returning view");

            Assert.That(player.Cancel(sequence, false), Is.True);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(player.CanPlay, Is.True);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(letterbox.IsInputShieldActive, Is.False);
            Assert.That(letterbox.Reveal, Is.Zero);
            AssertCameraRestored();
            AssertCameraFocusReleased();
        }
    }
}
