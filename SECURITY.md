# Data and security

Dialvia is a local Windows controller. It has no telemetry service, API-key store or network listener. Codex handles its own audio/account communication; Plantronics Hub handles its own device/vendor communication.

- Target links must be `codex://threads/<UUID>` and numbered slots must be 1–9. The UI does not accept arbitrary executable or web targets.
- Keyboard injection checks the foreground packaged Codex process and refuses when physical modifier keys are held. It does not elevate privileges.
- Completion notifications carry a generic local message. The queue rejects oversized and stale payloads and deletes consumed files.
- Hook installation preserves unrelated hooks and never modifies Codex trust records. Review the exact hook in Codex before trusting it.
- Settings and action logs stay in the user's local data folder. Logs may contain user-provided target labels; do not attach them to public issues without reviewing them.
- The recent-session picker opens Codex's local SQLite index with READONLY flags. It reads up to 200 unarchived IDs, titles, project directories and activity times; no transcripts. The index is an unofficial schema dependency with manual-link fallback. Nothing is uploaded or included in public screenshots.
- Startup, bindings and hooks are opt-in UI actions. Uninstall removes owned entries and retains user data.

Please do not post tokens, full private chat links, recordings or personal configuration in public issues. For a security-sensitive report, use the maintainer contact shown on the GitHub profile rather than publishing exploit details or credentials.
