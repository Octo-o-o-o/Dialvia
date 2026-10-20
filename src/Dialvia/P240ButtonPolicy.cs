using System;

namespace Dialvia {
    // Calibrated on P240 047F:AE04 with Hub 3.25.2: green is raw 7 while idle,
    // but raw 3 (named VolumeUpHeld by the type library) during our local call.
    public sealed class P240ButtonPolicy {
        DateTime volumeUpAt = DateTime.MinValue;
        public DeviceButton? Translate(int button, bool localCallActive, DateTime now) {
            if (button == 1) { volumeUpAt = now; return null; }
            if (button == 7) return DeviceButton.ToggleVoice;
            if (button == 18) return DeviceButton.StartVoice;
            if (button == 19 || button == 33) return DeviceButton.EndVoice;
            if (button == 3 && now - volumeUpAt <= TimeSpan.FromSeconds(2)) { volumeUpAt = now; return null; }
            if (button == 3 && localCallActive && now - volumeUpAt > TimeSpan.FromSeconds(2)) return DeviceButton.EndVoiceCompatibility;
            return null;
        }
    }
}
