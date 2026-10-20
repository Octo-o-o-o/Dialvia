# Navigation design and community research

## Chosen interaction

Projects are stable enough to remember as `*1`–`*9`. Chats are more numerous and their relevance changes, so the default is a project's most recently active chats, ordered by activity time descending. Opening the browser creates a snapshot; refresh or project change replaces it. This avoids renumbering while the user is choosing. Fixed chat mode remains available for long-lived destinations and migration from the personal controller.

Dates appear as today, yesterday or a date/time beside the title. A date identifies when something happened but does not uniquely identify a chat, so date dial codes add memory work without solving selection.

The browser supports project selection and chat titles, keyboard arrows, Enter and Escape. The P240 fallback is `*#` + green to browse, `*` / `#` + green to move, blank green to confirm and red to close. Fixed project codes also switch the recent-list scope. These new handset browsing codes still need physical acceptance.

## What the Xiaomi projects actually do

- [MI-AO button presets](https://github.com/fanxeon/mi-ao/blob/main/docs/BUTTON_PRESETS.md) separate physical controls from app actions. Its macOS Codex preset maps volume up/down to previous/next task through Accessibility menu actions. D-pad modes include pointer and arrows. This supports borrowing previous/next navigation, but does not demonstrate a Windows date-based chat index.
- [MiVibe Remote](https://github.com/wyuzhi/MiVibe-Remote) supports Xiaomi remote profiles on Windows and macOS. It cycles application presets, with directional pointer controls and normal volume behavior. Its Windows voice path uses a virtual microphone. App-preset switching is distinct from selecting an individual Codex chat; Dialvia uses Codex's own dictation instead.

These are primary project references, not dependencies. No code or preset files were copied. Exact remote support and behavior should be checked in their current documentation.

## P240 hardware evidence

The [P240 user guide](https://kaas.hpcloud.hp.com/pdf-public/pdf_9651782_en-US-1.pdf) identifies the central scroll key separately from side volume controls and describes local menu/call-list uses. On the development P240, central up/down woke the backlight while side volume changed the volume display. Neither SDK logging nor a read-only shared HID/RawInput probe exposed usable central-scroll input during those tests with Hub active.

This does not prove that every P240 firmware behaves identically. It does mean this release must not promise central-arrow navigation or repurpose audio volume based on an unobserved event. Future adapters can expose explicit navigation events after model-specific tests.

## Local list and fallback

The picker reads local Codex metadata through a read-only SQLite connection, at most 200 unarchived chats. It checks the schema and reads no transcripts. This is not a stable public Codex API. If the index changes or is busy, the app reports it and the binding editor still accepts a strict `codex://threads/<UUID>` link. Project codes open existing configured chats; they do not create or move projects.
