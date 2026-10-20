# Changelog

## 0.1.0-alpha.4 — 2026-10-20

- Fix premature recording reset: an older inactive microphone record never clears a new start, regardless of elapsed time.
- Retain a stop request as active/pending until fresh stop evidence arrives; suppress rapid repeats and require known continuing capture for a later explicit retry.
- Separate native key-down/key-up by 80 ms for the Windows global-hotkey listener's distinct press/release event paths, releasing keys even after partial injection.
- Add diagnostic base-button and numeric event-type logging without remapping volume keys.
- Expand core checks to 89 assertions, including delayed start and stop-confirmation regressions. Revised hardware stop/send acceptance remains pending.

## 0.1.0-alpha.3 — 2026-10-20

- Add explicit `00` + green submission of the reviewed Codex draft through `composer.submit`, with a dedicated configurable binding.
- Refuse sending while recording, in detection mode, or with missing/disabled/conflicting submit bindings. No generic Enter fallback.
- Keep dictation stop and draft submission separate; follow Codex's own empty-draft and follow-up behavior.
- Add an explicit recording-state correction action for when Codex has already stopped; it clears handset tracking without toggling dictation.
- Require explicit `0` + green to start voice; idle talk/off-hook no longer starts recording. Preserve green-only stop and browsing.
- Focus the native main chat composer before dictation and use read-only Windows capture metadata as an advisory stale-stop check, with unknown fallback and startup grace.
- Expand core checks to 82 assertions, including submission, focus resolution, idle-button policy and capture-state fixtures.

## 0.1.0-alpha.2 — 2026-10-20

- Default handset browsing to Codex's native previous/next sidebar commands, without opening a Dialvia window.
- Add `22` to enter, `2` / `8` to choose direction and move, and `5` to exit; `23` exits unconditionally. Codes need green to submit; green alone repeats a step in the current direction.
- Queue rapid green presses separately and cancel pending steps on explicit commands/exit. Use distinct SDK call IDs so stale display-clear feedback cannot terminate a later voice call.
- Honor compatible existing sidebar bindings and reject disabled or conflicting bindings without mutation.
- Keep the recent-session popup as an optional GUI setting. Project filtering applies to this local list only.
- Replace the unreliable idle red-key exit instruction with numeric exit; handle additional active-call end states and log SDK state transitions.
- Preserve direct fixed/recent slots outside browsing; do not claim unsupported long presses or central-scroll input.
- Expand core checks to 59 assertions.

## 0.1.0-alpha.1 — 2026-10-20

- Initial Windows tray app with a Plantronics Calisto P240 adapter.
- Fixed project numbers, recent-session snapshots, optional fixed chat mode, dictation and optional voice-conversation controls.
- Graphical control center, local metadata picker, session browser, audio-settings shortcut and notification preferences.
- Native asynchronous completion-hook receiver with opt-in popup/ringing.
- Per-user installation, Start menu/login startup and ownership-aware integration cleanup.
- Preserve maximized Codex windows when focusing them for a voice shortcut.
- Original D icon with SVG, PNG and multi-resolution Windows ICO assets.
- Framework-based build, non-owning device probe, fixture screenshots and 41 core test assertions.

Alpha limitations and actual hardware evidence are documented in `docs/validation.md`.
