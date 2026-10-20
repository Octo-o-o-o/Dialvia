This revision addresses a handset dictation stop regression reported after alpha.3.

The old two-second inactive fallback could clear a newly requested recording before microphone metadata caught up. It has been removed: an old stop timestamp never confirms a new start. A dispatched stop now stays pending until fresh stop evidence arrives, keeping submission and target changes blocked. Rapid duplicate stops are suppressed, and a later explicit retry requires known continuing capture.

Keyboard press and release are now separate calls with an 80 ms hold for Codex's Windows global-hotkey listener. Diagnostic logging includes SDK base buttons and numeric event types; audio/volume buttons are not remapped.

**Controls:** `0` + green starts dictation; green again (or `*0` + green) requests stop. After recording stops and the draft appears, review it and use `00` + green to send. Native browsing remains `#` + green, followed by green alone for each next chat; `*` + green reverses and `5` + green exits.

89 core assertions pass, including delayed-start, stale-stop, pending-stop and duplicate/retry cases. The user has confirmed dictation start and smooth continuous native browsing. Alpha.4 hardware stop/send acceptance is pending; full voice conversation, audible ringing and trusted real Codex Stop delivery remain open.

Extract **Dialvia-0.1.0-alpha.4-win-x64.zip**, keeping EXE and interface DLL together. Plantronics Hub is a separate prerequisite. Files are unsigned; SHA-256 sums are included.
