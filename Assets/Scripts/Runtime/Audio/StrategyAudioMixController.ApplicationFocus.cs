using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAudioMixController
    {
        private readonly StrategyAudioFocusGate applicationAudioFocus = new();

        private void InitializeApplicationAudioFocus()
        {
            Application.runInBackground = true;
            if (applicationAudioFocus.IsInitialized)
            {
                return;
            }

            bool changed = applicationAudioFocus.Initialize(
                Application.isFocused,
                false,
                AudioListener.volume,
                out float targetVolume);
            ApplyApplicationAudioFocus(changed, targetVolume, "initialize");
        }

        private void OnEnable()
        {
            if (instance == null)
            {
                instance = this;
            }

            if (instance != this)
            {
                return;
            }

            InitializeApplicationAudioFocus();
            bool changed = applicationAudioFocus.SetWindowFocused(
                Application.isFocused,
                AudioListener.volume,
                out float targetVolume);
            ApplyApplicationAudioFocus(changed, targetVolume, "component_enabled");
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                RestoreApplicationAudioFocus("component_disabled");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (instance != this)
            {
                return;
            }

            InitializeApplicationAudioFocus();
            bool changed = applicationAudioFocus.SetWindowFocused(
                hasFocus,
                AudioListener.volume,
                out float targetVolume);
            ApplyApplicationAudioFocus(changed, targetVolume, "application_focus");
        }

        private void OnApplicationPause(bool paused)
        {
            if (instance != this)
            {
                return;
            }

            InitializeApplicationAudioFocus();
            bool changed = applicationAudioFocus.SetApplicationPaused(
                paused,
                AudioListener.volume,
                out float targetVolume);
            ApplyApplicationAudioFocus(changed, targetVolume, "application_pause");
        }

        private void RestoreApplicationAudioFocus(string reason)
        {
            bool changed = applicationAudioFocus.Restore(
                AudioListener.volume,
                out float targetVolume);
            ApplyApplicationAudioFocus(changed, targetVolume, reason);
        }

        private void ApplyApplicationAudioFocus(bool changed, float targetVolume, string reason)
        {
            if (!changed)
            {
                return;
            }

            AudioListener.volume = targetVolume;
            StrategyDebugLogger.Info(
                "Audio",
                applicationAudioFocus.IsMuted ? "MutedForFocusLoss" : "RestoredAfterFocusReturn",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("listenerVolume", targetVolume),
                StrategyDebugLogger.F("runInBackground", Application.runInBackground));
        }
    }

    internal sealed class StrategyAudioFocusGate
    {
        private bool windowFocused;
        private bool applicationPaused;
        private bool muted;
        private float volumeBeforeMute = 1f;

        public bool IsInitialized { get; private set; }
        public bool IsMuted => muted;

        public bool Initialize(
            bool hasWindowFocus,
            bool isApplicationPaused,
            float currentVolume,
            out float targetVolume)
        {
            targetVolume = currentVolume;
            if (IsInitialized)
            {
                return false;
            }

            IsInitialized = true;
            windowFocused = hasWindowFocus;
            applicationPaused = isApplicationPaused;
            return Refresh(currentVolume, out targetVolume);
        }

        public bool SetWindowFocused(bool hasFocus, float currentVolume, out float targetVolume)
        {
            windowFocused = hasFocus;
            return Refresh(currentVolume, out targetVolume);
        }

        public bool SetApplicationPaused(bool paused, float currentVolume, out float targetVolume)
        {
            applicationPaused = paused;
            return Refresh(currentVolume, out targetVolume);
        }

        public bool Restore(float currentVolume, out float targetVolume)
        {
            targetVolume = currentVolume;
            if (!muted)
            {
                return false;
            }

            muted = false;
            targetVolume = volumeBeforeMute;
            return true;
        }

        private bool Refresh(float currentVolume, out float targetVolume)
        {
            targetVolume = currentVolume;
            bool shouldMute = !windowFocused || applicationPaused;
            if (shouldMute == muted)
            {
                return false;
            }

            muted = shouldMute;
            if (shouldMute)
            {
                volumeBeforeMute = currentVolume;
                targetVolume = 0f;
            }
            else
            {
                targetVolume = volumeBeforeMute;
            }

            return true;
        }
    }
}
