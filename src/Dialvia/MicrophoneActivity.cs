using System;
using Microsoft.Win32;

namespace Dialvia {
    public enum CaptureState { Unknown, Inactive, Active }
    // Advisory Windows privacy metadata, not a public Codex voice-status API.
    // No audio or other applications' records are accessed; missing/invalid data stays unknown.
    public sealed class MicrophoneActivity {
        public CaptureState State { get; private set; }
        public DateTime StoppedUtc { get; private set; }
        public static MicrophoneActivity FromFileTimes(long started, long stopped) {
            var result = new MicrophoneActivity();
            try {
                if (started <= 0 || stopped < 0) return result;
                DateTime.FromFileTimeUtc(started);
                if (stopped == 0) result.State = CaptureState.Active;
                else if (stopped >= started) { result.StoppedUtc = DateTime.FromFileTimeUtc(stopped); result.State = CaptureState.Inactive; }
            } catch (ArgumentOutOfRangeException) { }
            return result;
        }
        public static MicrophoneActivity Read() {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone\OpenAI.Codex_2p2nqsd0c76g0", false)) {
                    if (key == null) return new MicrophoneActivity();
                    object start = key.GetValue("LastUsedTimeStart"), stop = key.GetValue("LastUsedTimeStop");
                    if (!(start is long) || !(stop is long)) return new MicrophoneActivity();
                    return FromFileTimes((long)start, (long)stop);
                }
            } catch (Exception) { return new MicrophoneActivity(); }
        }
        public bool ConfirmsStopped(DateTime requestedUtc) {
            // An older stop never confirms a new recording, regardless of elapsed time.
            return State == CaptureState.Inactive && StoppedUtc >= requestedUtc;
        }
    }
    public sealed class DictationState {
        public bool Active { get; private set; }
        public bool StopPending { get; private set; }
        public DateTime RequestedUtc { get; private set; }
        DateTime stopRequestedUtc;
        public void StartRequested(DateTime now) { Active = true; StopPending = false; RequestedUtc = now; }
        public void StopRequested(DateTime now) { StopPending = true; stopRequestedUtc = now; }
        public bool CanRequestStop(MicrophoneActivity activity, DateTime now) {
            return Active && (!StopPending || (activity.State == CaptureState.Active && now - stopRequestedUtc >= TimeSpan.FromSeconds(2)));
        }
        public bool Observe(MicrophoneActivity activity) {
            if (!Active || !activity.ConfirmsStopped(RequestedUtc)) return false;
            Reset(); return true;
        }
        public void Reset() { Active = StopPending = false; }
    }
    public static class BareVoiceButton {
        public static string Action(bool browsing, bool recording) { return browsing ? "browse" : recording ? "stop" : "idle"; }
    }
}
