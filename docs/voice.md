# Voice service and window behavior

Dialvia's default mode is **dictation**, using the configurable Codex command `globalDictationSingleTap`. Starting and stopping dictation produces prompt text. Dialvia does not record or transcribe it. Submission requires a separate explicit handset action.

## Sending the reviewed draft

`0` + green starts dictation. Green again (or `*0` + green) stops it. Wait for the text and review it, then **`00` + green** invokes Codex's dedicated `composer.submit` command. The default new binding is `Ctrl+Alt+Shift+F12`; it is installed additively with the other integration bindings, preserving conflicts for the user to resolve. Existing compatible submit bindings can be resolved at runtime. A newly added binding may need Codex to reopen before it loads.

Only the explicit `0` dial request starts voice. A bare talk/off-hook event while idle cannot start it; this matters because the tested SDK did not reliably distinguish the handset's red key. Green alone still stops controller-started voice or repeats browsing in its current context.

Dictation first invokes the installed client's native `focusMainChat` command, default `Alt+L`, then waits 180 ms before sending the dictation toggle. Compatible custom focus bindings are respected; disabled/conflicting bindings stop the request. Window activation alone does not ensure that the main chat composer has focus: the installed client can otherwise fall back to a floating global-dictation interface. These command details were inspected in the installed Windows client, not established as a stable external API.

The controller refuses submission while it tracks an active recording, in detection mode, or if the submit binding is missing, disabled or conflicts. It has no generic Enter fallback, does not inspect or log draft text, and reports command dispatch rather than claiming that a server accepted the message. Codex handles empty drafts, disabled controls and its configured follow-up behavior. The [official settings guide](https://learn.chatgpt.com/docs/app/settings) documents configurable Enter/follow-up behavior; the dedicated command avoids depending on a user's Enter setting.

If Codex has already stopped recording but Dialvia still believes it is recording, use **Codex 已停止：校正状态** in voice settings or the tray menu, after checking the Codex UI. This clears only the controller/handset state and sends no keyboard command. It avoids toggling dictation back on during a stale-state recovery. Automatic stop/other UI actions are not reflected by a stable public voice-status API.

As an advisory check, Dialvia reads only `LastUsedTimeStart` / `LastUsedTimeStop` from the current user's Windows microphone privacy record for the Codex package. It never reads audio or other applications' usage records. Missing, invalid or inaccessible metadata means unknown, not stopped. Only an inactive record whose stop timestamp is at or after the controller's start request can clear tracking. An older stop never clears a new recording, regardless of elapsed time. Alpha.3's two-second inactive fallback was removed after the user reported stop failure and logs showed premature state reset.

Dispatching the stop shortcut leaves the state active/pending, so submission and target changes remain blocked until fresh stop evidence arrives or the user explicitly corrects state after checking the UI. Repeated stop presses within two seconds are suppressed. A later explicit retry requires known continuing capture; unknown or stale inactive metadata cannot authorize another toggle. Keyboard injection now separates key-down from key-up by 80 ms: the installed Windows client handles the global hotkey and native release through distinct paths, so a same-batch press/release risks an ordering problem. This timing change is a compatibility mitigation, not proof of every failure's cause.

A known active capture also blocks explicit submission/start. Microphone usage is not proof of dictation mode or of a recording indicator in the current composer, and Windows may change this implementation detail. The manual correction remains available.

Inspection of the installed Windows client on 2026-10-20 found a ChatGPT/Codex authenticated streaming dictation path, `/dictation/stream`, including the `chatgpt-dictation` and `codex-desktop` WebSocket protocol labels. No exact transcription model ID was exposed by that inspected code path. This is an observation about that client version, not a stable API contract or an endpoint for third-party use. Dialvia does not call it directly.

Codex's **voice conversation** is a separate mode controlled by `composer.startVoiceMode`, `realtimeVoice.endCall` and `realtimeVoice.toggleMicrophoneMute`. Account, rollout and task availability are controlled by Codex. See the [official voice documentation](https://learn.chatgpt.com/docs/features/voice).

Windows/Codex choose microphone and playback endpoints. P240 can be selected for both; opening Sound settings does not automatically change the user's global device defaults.

## Preserving a maximized window

The initial personal controller unconditionally called `ShowWindowAsync(..., SW_RESTORE)` before focusing Codex. Windows restores maximized windows to their normal size with that flag. This was a window-activation bug, not evidence of a shortcut collision.

The corrected controller checks `IsIconic` first. Only a minimized window receives `SW_RESTORE`; a visible maximized window is simply focused. The same fix was applied to the original locally installed controller.
