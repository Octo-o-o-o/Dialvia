# Voice service and window behavior

Dialvia's default mode is **dictation**, using the configurable Codex command `globalDictationSingleTap`. Starting and stopping dictation produces prompt text. The controller does not record, transcribe or submit that text itself.

Inspection of the installed Windows client on 2026-10-20 found a ChatGPT/Codex authenticated streaming dictation path, `/dictation/stream`, including the `chatgpt-dictation` and `codex-desktop` WebSocket protocol labels. No exact transcription model ID was exposed by that inspected code path. This is an observation about that client version, not a stable API contract or an endpoint for third-party use. Dialvia does not call it directly.

Codex's **voice conversation** is a separate mode controlled by `composer.startVoiceMode`, `realtimeVoice.endCall` and `realtimeVoice.toggleMicrophoneMute`. Account, rollout and task availability are controlled by Codex. See the [official voice documentation](https://learn.chatgpt.com/docs/features/voice).

Windows/Codex choose microphone and playback endpoints. P240 can be selected for both; opening Sound settings does not automatically change the user's global device defaults.

## Preserving a maximized window

The initial personal controller unconditionally called `ShowWindowAsync(..., SW_RESTORE)` before focusing Codex. Windows restores maximized windows to their normal size with that flag. This was a window-activation bug, not evidence of a shortcut collision.

The corrected controller checks `IsIconic` first. Only a minimized window receives `SW_RESTORE`; a visible maximized window is simply focused. The same fix was applied to the original locally installed controller.
