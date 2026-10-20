# Architecture

```text
P240 USB → Plantronics Hub COM → PlantronicsP240Adapter
                                      ↓ IDeviceAdapter
                               ControllerForm
                              ↙              ↘
                    ICodexDesktop         local notifications
                    deep links / keys     popup / optional ring
```

- `DeviceAdapters.cs`: device whitelist, SDK events, local call state and restoration of the previous default softphone. The P240 submits a buffered dial string after green; a 250 ms deferred talk action lets that string cancel the generic talk event.
- `ControllerForm.cs`: numeric browsing context, native/sidebar or optional popup navigation, voice state, settings, tray and notification consumption. Native browsing sends previous/next sidebar commands without a new window; 5 exits while browsing and 23 exits unconditionally. Target changes leave browsing and stop controller-owned voice first.
- `SettingsWindow.cs`, `SessionBrowserWindow.cs`, `DesktopTheme.cs`: native paper/ink/copper GUI, project bindings, recent/fixed routing, audio/notification preferences and startup management. Browser order is frozen until refresh or project change.
- `CodexInventory.cs`: schema-checked read-only local SQLite metadata lookup through Windows `winsqlite3.dll`, with a manual-link fallback. It reads at most 200 unarchived chat IDs, titles, directories and activity times; never transcripts or database writes.
- `CodexDesktop.cs`: validates thread links, focuses the verified packaged Codex process, checks foreground ownership and modifier state, then injects configured keys. Only minimized windows are restored, preserving an already-maximized window.
- `UserIntegration.cs`: additive voice-shortcut/hook configuration and read-only native sidebar binding resolution. Disabled or conflicting sidebar bindings are rejected. Voice-shortcut collisions fail before mutation. An ownership manifest retains user edits and unrelated hooks on removal. No hook-trust state is edited.
- `Program.cs`: single-instance startup, native hook receiver, per-user installer, diagnostics. The generated COM interface assembly is a normal adjacent dependency.
- `Models.cs`: configuration validation, atomic writes and paths.
- `P240ButtonPolicy.cs`: calibrated idle raw-7 versus active-call raw-3 green-key routing. The latter is an optional compatibility stop; real volume-up sequences are protected, with an explicit setting for indistinguishable standalone holds.
- `MicrophoneActivity.cs`: advisory, read-only Codex microphone timestamps, dictation lifecycle and bare-button policy. Idle talk/off-hook never starts voice. An old stop cannot clear a new start; a dispatched stop remains pending until fresh evidence, and duplicate/retry rules avoid rapid toggle reversals. Missing/invalid metadata remains unknown. Keyboard injection holds keys 80 ms between separate press/release calls for the Windows global-hotkey listener.

The public configuration is empty and in detection mode, with recent-session routing as its default. Local inventory stays on the machine. Dictation remains a draft until the explicit `00` handset command requests `composer.submit`; no automatic send follows recording stop. A user-checked stale recording state can be reset without sending a shortcut.

## Extending hardware

Implement `IDeviceAdapter` and add an explicit validated device profile. Translate model-specific reports into dial strings, voice buttons, connection status, call-state feedback and ringing. Preserve device identity checks and event timing tests. “USB audio works” is not sufficient evidence for control-key compatibility.

The current settings loader and factory intentionally allow only `plantronics-p240`. Other devices are an extension point, not an advertised capability. Anyvia would require a separate destination with its own pairing, grants, target IDs and audio/notification protocol; it is not implemented by sending Codex shortcuts.
