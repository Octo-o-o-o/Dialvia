# Changelog

## 0.1.0-alpha.8 — 2026-10-21

- Fix the remaining stale taskbar button: the running process and installed shortcut now share explicit `Dialvia.Desktop` identity. The shortcut explicitly references a separate, content-addressed two-tone ICO instead of relying on the cached EXE icon.
- Open the control center when the Start-menu/taskbar shortcut relaunches; keep login startup in the background. Refresh only the owned shortcut/icon entries, with no Explorer restart or global cache deletion.
- Add a read-only `--shell-check` report for process/shortcut identity, target, arguments and icon bytes. Independently verify the shortcut through the canonical Windows Shell property name.
- Expand native icon checks to 9 renders by exercising the actual runtime AppIcon loader. The user confirmed the real bottom taskbar button is now ink/copper.

## 0.1.0-alpha.7 — 2026-10-21

- Fix the icon resource mismatch introduced in alpha.6: the EXE/taskbar/title-bar mark was entirely copper while the in-app brand was ink/copper. Use the two-tone brand for shell and window resources, retaining transparency and enlarged geometry.
- Validate shell exports against the in-app/light-tray palette at every size. Add 8 native Windows renders from actual EXE shell and embedded resources, run automatically by the release build. The alpha.6 EXE fails this regression check; alpha.7 passes.
- Regenerate public screenshots and release materials; keep the light/dark notification-area variants and existing device controls.

## 0.1.0-alpha.6 — 2026-10-21

- Remove the icon background plate and enlarge the original D artwork from 57% to 92% of canvas height. Provide separate shell, light-tray and dark-tray resources at 15 native ICO sizes, plus transparent SVG/PNG exports.
- Select the tray resource from taskbar DPI/theme and refresh on display/preferences changes. Refresh only the installed EXE/shortcut shell entries during installation.
- Improve disabled/focus states, accessible input names, responsive binding columns and long-title tooltips. Save is enabled only for changed settings; Ctrl+S saves and Esc closes.
- Add a fixed save/cancel bar and Chinese validation in the binding editor; add explicit empty/error states and prevent dropdown Enter from opening a chat.
- Add reproducible checks for 45 icon frames/3 SVGs and 13 fictional UI states. Native fixture checks covered all settings pages, cancel, validation, dropdown Esc and save state at 150% Windows scaling.
- Retain the existing handset controls and known acceptance limits; this is a visual/UI update, not new hardware or submission acceptance.

## 0.1.0-alpha.5 — 2026-10-20

- Fix the observed P240 green-key stop route: raw 7 while idle, raw 3 during the controller's local call. Only active controller voice can use the compatibility stop.
- Add the GUI/config compatibility switch and protect real volume-up followed by held-event sequences; document indistinguishable standalone volume holds.
- The user confirmed start, single-green stop and draft text. Handset submission remains pending acceptance.
- Focus the main composer before submission; report shortcut dispatch without implying message submission. Document a fresh client/submittable-chat acceptance step.
- Expand core checks to 98 assertions, including the observed device-event sequence, volume controls and focus-before-submit behavior.

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
