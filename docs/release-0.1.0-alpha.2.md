This update makes handset browsing switch Codex's own sidebar items directly, without opening a Dialvia window.

- `22` (or `*#`) + green enters browsing.
- `2` / `8` + green chooses up/down and moves one sidebar item. Then **green alone** repeats a step in that direction.
- `5` + green exits browsing; `23` + green exits regardless of mode.
- `*` / `#` remain previous/next direction aliases. `0` + green leaves browsing and starts voice.
- Outside browsing, digits retain their normal fixed/recent slot meanings. Project codes are unchanged.

The graphical settings include native Codex mode (default) and the optional local recent-list popup. Native mode follows Codex's own sidebar order; local project filters apply only to Dialvia's recent list. Existing compatible sidebar shortcuts are respected, with disabled/conflicting bindings rejected. Default setup adds no bindings and needs no Codex restart.

The user confirmed native numeric switching, but found repeating a digit-plus-green awkward. This update therefore allows green-only repeated steps, queues rapid presses separately, and cancels deferred actions when exiting. The user also confirmed alpha.1 popup movement, but idle red did not close it. Numeric exit is reliable; red remains best-effort SDK feedback. Central scroll and long-press star/hash are unsupported by the observed interface. Numeric codes still need green to submit.

59 core assertions passed, including rapid-green queues, cancellation, numeric context and native binding resolution. The executable self-test and graphical fixture checks passed. The user confirmed on P240 that repeated-green navigation felt natural and continuous switching worked. Full voice conversation, audible ringing and trusted real Codex Stop delivery remain open. See the repository validation record.

Download and extract **Dialvia-0.1.0-alpha.2-win-x64.zip**, keeping the EXE and interface DLL together. Plantronics Hub remains a separate prerequisite. Files are unsigned; SHA-256 sums are included. Private targets and logs are not distributed.
