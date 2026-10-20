This revision fixes the observed P240 green-key stop route. **The user confirmed dictation start, single-green stop and resulting draft text work.**

The same physical green key was reported by Hub as raw 7 (`Talk`) while idle, but raw 3 (`VolumeUpHeld`) during the controller's call. Previous adapters ignored the latter. Alpha.5 routes it to stop only while the local call and controller voice are active, keeping the fresh-stop confirmation and submit guards.

Real volume-up followed by held reports is protected, including repeated holds. An isolated volume hold producing the same signal remains indistinguishable and may stop voice; turn off **P240 通话中接听键兼容** in voice settings if needed. This compatibility path is scoped to the tested P240/Hub setup.

**Dictation:** `0` + green starts; green again requests stop. After the text appears, review it and use `00` + green to send. **Browsing:** `#` + green, then green alone for each next chat; `*` + green reverses and `5` + green exits.

97 core assertions pass, covering calibrated device events, audio-button sequences, lifecycle and submit guards. Handset submission is still pending acceptance; full voice conversation, audible ringing and trusted real Codex Stop delivery remain open.

Extract **Dialvia-0.1.0-alpha.5-win-x64.zip**, keeping EXE and interface DLL together. Plantronics Hub is a separate prerequisite. Files are unsigned; SHA-256 sums are included.
