# Validation — 0.1.0-alpha.4

Evidence is split between the original controller on the development P240 and this extracted project. It is not a claim that every mode has passed.

| Area | Evidence / status |
|---|---|
| Original P240 project/chat routing | User confirmed a project code and a chat code both open the intended existing Codex destination. Private labels/IDs are not published. |
| Original dictation | User confirmed both recording and a resulting text draft on 2026-10-20. |
| Maximized-window bug | Cause identified as unconditional SW_RESTORE; corrected in both implementations using IsIconic. Final visual confirmation of that specific fix is pending. |
| COM call-state feedback | Original controller's managed COMCall/COMContact create/answer/terminate test passed after fixing E_FAIL. |
| Automated core tests | 89 assertions passed locally: numeric navigation, distinct rapid green actions, delay/cancel, native binding resolution and conflict handling, legacy settings, explicit submit dispatch with a fake desktop, recording/detection refusal and no fallback on foreground refusal, read-only inventory, ownership and hooks. Dictation regressions cover delayed startup with an old stop record, a stop dispatch remaining pending, duplicate suppression, known-active retry and fresh stop confirmation. |
| Alpha.3 submission | Dedicated `composer.submit` command was verified in the installed client. Missing/disabled/conflicting bindings and recording/detection guards are covered without sending a real message. Handset submission acceptance is pending. |
| Alpha.3 voice alignment | User reported that `0` + green appeared not to record and red appeared to start. Windows independently logged Codex microphone capture from the first request until the next button, which does not establish the visible composer state. The revision focuses `focusMainChat` before dictation, permits explicit-code starts only, and uses advisory inactive-capture checks to avoid reopening an already-stopped recording. Revised hardware acceptance is pending. |
| Alpha.4 stop regression | User confirmed alpha.3 starts recording but cannot stop. Logs show the two-second inactive fallback prematurely cleared one new start, and another dispatched toggle did not immediately end capture. Alpha.4 removes the stale-stop fallback, retains a pending stop until fresh confirmation, and separates key press/release by 80 ms for the client's two Windows event paths. Hardware acceptance is pending. SDK base-button events and numeric event types are logged without remapping audio buttons. |
| Graphical settings | Four control-center pages and the recent-session browser rendered and visually checked at Windows 150% scaling using fictional demo data. All public screenshots use this fixture. Startup installation and live process are checked separately. |
| Per-user deployment | Installed outside inherited MSIX identity, verified the real LocalAppData executable and matching release hash, native HKCU Run value and live independent process (`IsProcessInJob=false`). Legacy private bindings were retained locally and its old startup entry removed. The control-center window and P240 connection were observed. |
| Local recent index | The executable loaded the development machine's local inventory read-only; only the count was retained in diagnostic evidence. No private inventory is published. |
| P240 central up/down | No usable report was observed from SDK events or a read-only shared HID/RawInput probe while Hub was active. Side volume changed the handset display. This is scoped to the tested setup; central keys are not mapped. |
| Alpha.1 popup browse codes | User confirmed `*#` opens the list and `#` moves down. Red did not close it; no red button event appeared in its button log. |
| Alpha.2 native/numeric browsing | User confirmed 22/8/2 switch Codex chats directly without another window. After the green-only repeat change, they confirmed on 2026-10-20 that it felt natural and continuous switching worked. Numeric exit replaces unreliable idle-red; no long-press support is claimed. |
| Extracted EXE / resources | Release build and EXE self-test passed. The packaged EXE loaded its adjacent interface assembly and discovered Plantronics Calisto P240 without changing the default softphone. |
| Notifications | Original local queue fixture was consumed. The release EXE's native hook receiver accepted a Stop fixture, emitted `{}` and wrote exactly one generic notification. Real Codex Stop execution still needs first-time trust. |
| Full voice conversation / audible ring | Not yet accepted end to end. |
| Other devices / macOS / Linux / Anyvia | Not implemented. |

The release does not embed personal device inventories, conversation IDs, audio, credentials or local logs. Hardware-independent CI cannot replace real P240 acceptance.

The first embedded-assembly packaging candidate was quarantined by Windows Defender before execution (`Trojan:MSIL/PureLog.ARA!MTB`). It was discarded. The release uses a conventional EXE plus adjacent generated interface DLL, with no in-memory assembly loader, exclusions or security-setting changes. A Windows Defender custom scan of the final distribution completed; no newer Dialvia detection was observed and the tested binary remained accessible. This records the local check, not a general security certification.
