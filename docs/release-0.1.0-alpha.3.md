This release completes the handset dictation workflow with an explicit send action, alongside native Codex sidebar browsing.

**Dictate and send:** `0` + green starts dictation; green again (or `*0` + green) stops it. Wait for the text and review it, then **`00` + green** sends the current draft using Codex's dedicated `composer.submit` command. Setup adds `Ctrl+Alt+Shift+F12` by default and preserves existing bindings. A newly added binding may need Codex to reopen before it loads.

**Browse continuously:** `#` + green starts downward browsing; then each green press moves one item. `*` + green reverses direction. `5` + green exits; `23` + green exits unconditionally. The user confirmed that this direct native switching was smooth and continuous presses worked. Fixed project codes and direct slots remain available outside browsing.

82 core assertions passed, including recording/detection guards, dedicated submit dispatch, disabled/conflicting bindings, foreground refusal, composer focus and advisory microphone-state handling. Submission has no generic Enter fallback and does not inspect or log draft text. Revised handset voice/send acceptance is pending; full voice conversation, audible ringing and trusted real Codex Stop delivery remain open.

If Codex has already stopped but the controller's voice state is stale, an explicit correction button in the tray/voice settings resets handset tracking without sending a shortcut or toggling dictation.

Only `0` + green starts recording. Idle bare green/red events cannot open it. Dictation first focuses the current main composer; read-only Windows microphone timestamps provide an advisory stop check so an already-stopped recording is not toggled back on. This metadata is not a stable public voice-status API; missing or invalid data stays unknown.

Download and extract **Dialvia-0.1.0-alpha.3-win-x64.zip**, keeping the EXE and interface DLL together. Plantronics Hub is a separate prerequisite. Files are unsigned; SHA-256 sums are included. Central-scroll and long-press star/hash are not supported by the observed input interface; idle red-key exit remains unreliable.
