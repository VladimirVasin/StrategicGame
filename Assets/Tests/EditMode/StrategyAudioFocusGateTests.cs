using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyAudioFocusGateTests
    {
        [Test]
        public void FocusLossMutesAndFocusReturnRestoresPreviousVolume()
        {
            StrategyAudioFocusGate gate = new();
            float volume = 0.37f;

            ApplyInitialize(gate, true, false, ref volume);
            Assert.That(ApplyWindowFocus(gate, false, ref volume), Is.True);
            Assert.That(volume, Is.Zero);
            Assert.That(gate.IsMuted, Is.True);

            Assert.That(ApplyWindowFocus(gate, true, ref volume), Is.True);
            Assert.That(volume, Is.EqualTo(0.37f));
            Assert.That(gate.IsMuted, Is.False);
        }

        [Test]
        public void FocusReturnDoesNotRestoreWhileApplicationPauseIsStillActive()
        {
            StrategyAudioFocusGate gate = new();
            float volume = 0.42f;

            ApplyInitialize(gate, true, false, ref volume);
            ApplyWindowFocus(gate, false, ref volume);
            ApplyApplicationPause(gate, true, ref volume);

            Assert.That(ApplyWindowFocus(gate, true, ref volume), Is.False);
            Assert.That(volume, Is.Zero);
            Assert.That(gate.IsMuted, Is.True);

            Assert.That(ApplyApplicationPause(gate, false, ref volume), Is.True);
            Assert.That(volume, Is.EqualTo(0.42f));
            Assert.That(gate.IsMuted, Is.False);
        }

        [Test]
        public void RepeatedLossCallbacksDoNotOverwriteCapturedVolume()
        {
            StrategyAudioFocusGate gate = new();
            float volume = 0.73f;

            ApplyInitialize(gate, true, false, ref volume);
            ApplyWindowFocus(gate, false, ref volume);
            Assert.That(ApplyWindowFocus(gate, false, ref volume), Is.False);
            Assert.That(volume, Is.Zero);

            ApplyWindowFocus(gate, true, ref volume);
            Assert.That(volume, Is.EqualTo(0.73f));
        }

        [Test]
        public void RestoreReturnsCapturedVolumeDuringTeardown()
        {
            StrategyAudioFocusGate gate = new();
            float volume = 0.24f;

            ApplyInitialize(gate, false, false, ref volume);
            Assert.That(volume, Is.Zero);

            bool changed = gate.Restore(volume, out float restoredVolume);

            Assert.That(changed, Is.True);
            Assert.That(restoredVolume, Is.EqualTo(0.24f));
            Assert.That(gate.IsMuted, Is.False);
        }

        [Test]
        public void ZeroVolumeRemainsZeroAfterFocusRoundTrip()
        {
            StrategyAudioFocusGate gate = new();
            float volume = 0f;

            ApplyInitialize(gate, true, false, ref volume);
            ApplyWindowFocus(gate, false, ref volume);
            ApplyWindowFocus(gate, true, ref volume);

            Assert.That(volume, Is.Zero);
        }

        private static bool ApplyInitialize(
            StrategyAudioFocusGate gate,
            bool focused,
            bool paused,
            ref float volume)
        {
            bool changed = gate.Initialize(focused, paused, volume, out float targetVolume);
            volume = targetVolume;
            return changed;
        }

        private static bool ApplyWindowFocus(
            StrategyAudioFocusGate gate,
            bool focused,
            ref float volume)
        {
            bool changed = gate.SetWindowFocused(focused, volume, out float targetVolume);
            volume = targetVolume;
            return changed;
        }

        private static bool ApplyApplicationPause(
            StrategyAudioFocusGate gate,
            bool paused,
            ref float volume)
        {
            bool changed = gate.SetApplicationPaused(paused, volume, out float targetVolume);
            volume = targetVolume;
            return changed;
        }
    }
}
