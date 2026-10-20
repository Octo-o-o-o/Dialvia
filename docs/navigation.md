# Navigation design and community research

## Chosen interaction

Projects are stable enough to remember as `*1`–`*9`. Chats are more numerous, so the handset defaults to Codex's own previous/next sidebar navigation instead of another window. The installed Windows client (26.1013) exposes `previousThread` and `nextThread`, labeled Previous/Next Sidebar Item, with default `Ctrl+Alt+[` / `Ctrl+Alt+]` chords. Dialvia reads compatible custom bindings, detects disabled commands and collisions, then uses the verified foreground Codex window. It does not change Codex's sidebar sorting or install new bindings for this feature.

This is immediate navigation: each move opens a chat, rather than highlighting a row for later confirmation. It is also distinct from `previousRecentThread` / `nextRecentThread`, which cycle recent chats/tabs. The [official command reference](https://learn.chatgpt.com/docs/reference/commands) documents general desktop shortcuts and customization; the specific sidebar command IDs were verified in the installed client and may need adjustment after app updates.

Outside browsing, direct numbered slots can still use fixed bindings or a local recent snapshot. Optional Dialvia list mode scopes the most recently active local chats to a project. Its snapshot remains stable until refresh or project change. This local filter does not constrain native sidebar navigation.

Dates appear as today, yesterday or a date/time beside the title. A date identifies when something happened but does not uniquely identify a chat, so date dial codes add memory work without solving selection.

Handset controls: `22` (or `*#`) + green enters browsing with downward direction. `2` / `8` + green sets up/down and moves once; **green alone repeats one step in the current direction**. `5` + green exits, and `23` + green exits regardless of mode. `*` / `#` remain previous/next direction aliases and can start browsing directly. In optional list mode, blank green confirms its selection. `0` + green leaves browsing and starts voice. Outside browsing, digits 2, 5 and 8 retain their ordinary slot meanings. Fixed project codes remain available.

The user confirmed native numeric switching but found a number-plus-green for every move awkward. Repeatable green therefore uses the one reliable observed button event, without assuming unsupported long-press timing. A queue delays each press 250 ms for a possible buffered dial request, rather than resetting a single timer and losing rapid presses. Numeric commands cancel pending blank-green actions; exit cannot leave a deferred press that later starts voice.

## What the Xiaomi projects actually do

- [MI-AO button presets](https://github.com/fanxeon/mi-ao/blob/main/docs/BUTTON_PRESETS.md) separate physical controls from app actions. Its macOS Codex preset maps volume up/down to previous/next task through Accessibility menu actions. D-pad modes include pointer and arrows. This supports borrowing previous/next navigation, but does not demonstrate a Windows date-based chat index.
- [MiVibe Remote](https://github.com/wyuzhi/MiVibe-Remote) supports Xiaomi remote profiles on Windows and macOS. It cycles application presets, with directional pointer controls and normal volume behavior. Its Windows voice path uses a virtual microphone. App-preset switching is distinct from selecting an individual Codex chat; Dialvia uses Codex's own dictation instead.

These are primary project references, not dependencies. No code or preset files were copied. Exact remote support and behavior should be checked in their current documentation.

## P240 hardware evidence

The [P240 user guide](https://kaas.hpcloud.hp.com/pdf-public/pdf_9651782_en-US-1.pdf) identifies the central scroll key separately from side volume controls and describes local menu/call-list uses. On the development P240, central up/down woke the backlight while side volume changed the volume display. Neither SDK logging nor a read-only shared HID/RawInput probe exposed usable central-scroll input during those tests with Hub active.

This does not prove that every P240 firmware behaves identically. It does mean this release must not promise central-arrow navigation or repurpose audio volume based on an unobserved event. Future adapters can expose explicit navigation events after model-specific tests.

The user confirmed alpha.1 popup opening and downward selection, but idle red did not close it. No red button appeared in that button log. Numeric exit is therefore the documented reliable path; SDK button/call-end feedback is best-effort. Additional call-state logging and active-call end variants help diagnose red without treating app-generated display-clear events as user exit. Dialvia does not hold a fake voice call open merely to arm red, which could change keypad/audio behavior.

Long-press star/hash is not implemented. The exposed idle dial interface buffers a whole string until green and provides no reliable press/release timing. Repeated characters cannot safely establish a held button. Green submission remains necessary for numeric navigation.

## Local list and fallback

The picker reads local Codex metadata through a read-only SQLite connection, at most 200 unarchived chats. It checks the schema and reads no transcripts. This is not a stable public Codex API. If the index changes or is busy, the app reports it and the binding editor still accepts a strict `codex://threads/<UUID>` link. Project codes open existing configured chats; they do not create or move projects.
