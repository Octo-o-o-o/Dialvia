# Validation — 0.1.0-alpha.2

Evidence is split between the original controller on the development P240 and this extracted project. It is not a claim that every mode has passed.

| Area | Evidence / status |
|---|---|
| Original P240 project/chat routing | User confirmed a project code and a chat code both open the intended existing Codex destination. Private labels/IDs are not published. |
| Original dictation | User confirmed both recording and a resulting text draft on 2026-10-20. |
| Maximized-window bug | Cause identified as unconditional SW_RESTORE; corrected in both implementations using IsIconic. Final visual confirmation of that specific fix is pending. |
| COM call-state feedback | Original controller's managed COMCall/COMContact create/answer/terminate test passed after fixing E_FAIL. |
| Automated core tests | 59 assertions passed locally: numeric navigation context, three distinct rapid green actions, delay/cancel behavior, preserved direct slots, native sidebar key resolution/customization/disabled commands/collisions, recent snapshots, read-only SQLite fixtures, ownership and hooks. CI repeats these tests. |
| Graphical settings | Four control-center pages and the recent-session browser rendered and visually checked at Windows 150% scaling using fictional demo data. All public screenshots use this fixture. Startup installation and live process are checked separately. |
| Per-user deployment | Installed outside inherited MSIX identity, verified the real LocalAppData executable and matching release hash, native HKCU Run value and live independent process (`IsProcessInJob=false`). Legacy private bindings were retained locally and its old startup entry removed. The control-center window and P240 connection were observed. |
| Local recent index | The executable loaded the development machine's local inventory read-only; only the count was retained in diagnostic evidence. No private inventory is published. |
| P240 central up/down | No usable report was observed from SDK events or a read-only shared HID/RawInput probe while Hub was active. Side volume changed the handset display. This is scoped to the tested setup; central keys are not mapped. |
| Alpha.1 popup browse codes | User confirmed `*#` opens the list and `#` moves down. Red did not close it; no red button event appeared in its button log. |
| Alpha.2 native/numeric browsing | User confirmed that 22, 8 and 2 switch Codex chats directly without another list window. They found repeated numeric submission awkward. The new green-only repeat interaction is covered by queue checks and still needs handset acceptance. Numeric exit replaces unreliable idle-red; no long-press support is claimed. |
| Extracted EXE / resources | Release build and EXE self-test passed. The packaged EXE loaded its adjacent interface assembly and discovered Plantronics Calisto P240 without changing the default softphone. |
| Notifications | Original local queue fixture was consumed. The release EXE's native hook receiver accepted a Stop fixture, emitted `{}` and wrote exactly one generic notification. Real Codex Stop execution still needs first-time trust. |
| Full voice conversation / audible ring | Not yet accepted end to end. |
| Other devices / macOS / Linux / Anyvia | Not implemented. |

The release does not embed personal device inventories, conversation IDs, audio, credentials or local logs. Hardware-independent CI cannot replace real P240 acceptance.

The first embedded-assembly packaging candidate was quarantined by Windows Defender before execution (`Trojan:MSIL/PureLog.ARA!MTB`). It was discarded. The release uses a conventional EXE plus adjacent generated interface DLL, with no in-memory assembly loader, exclusions or security-setting changes. A Windows Defender custom scan of the final distribution completed; no newer Dialvia detection was observed and the tested binary remained accessible. This records the local check, not a general security certification.
