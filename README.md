<p align="center"><img src="assets/brand/dialvia-icon.svg" width="112" alt="Dialvia icon" /></p>
<h1 align="center">Dialvia</h1>
<p align="center">Physical controls for Codex on Windows.</p>
<p align="center"><a href="README.zh-CN.md">简体中文</a> · <a href="https://github.com/Octo-o-o-o/Dialvia/releases">Download</a> · <a href="docs/architecture.md">Architecture</a></p>

Turn a USB handset into a small Codex control surface: dial a number to open a chat, start voice dictation, and receive a task notification. **Plantronics Calisto P240 is the first and currently the only implemented device adapter.**

> **0.1.0-alpha.8** — fixes the stale taskbar button after alpha.7: process and Start-menu shortcut now share an explicit app ID and the shortcut points to a separate content-addressed icon. The user confirmed the actual taskbar button now shows the ink/copper D. Nine native resource renders, 45 icon-frame and 13 UI-state checks pass. Previous handset acceptance and pending submission are unchanged. See [visual design](docs/visual-design.md) and [validation](docs/validation.md).

![Dialvia control center with fictional demo projects](assets/screenshots/projects.png)

The screenshot uses fictional demo data. Daily configuration is graphical; JSON remains an optional advanced tool.

## Get started

1. Install Codex for Windows and **Plantronics Hub separately**, then connect a P240. The development device used Hub 3.25.2. Standard USB audio alone does not enable its full call-control interface.
2. Download and extract the [Windows x64 ZIP](https://github.com/Octo-o-o-o/Dialvia/releases). Keep `Dialvia.exe` and `Interop.Plantronics.dll` together; start the EXE from Windows Explorer. Windows with .NET Framework 4.8 and the installed Hub runtime is required. A separate .NET SDK is not needed.
3. Click the tray icon to open the control center. In **会话与项目**, choose a project from the local Codex list and optionally assign fixed project numbers. The public configuration is empty and starts in **detection mode**. It contains no developer chat IDs.
4. In **语音与通知**, select dictation or voice conversation. Configure voice shortcuts in **启动与集成**, then turn off detection mode in the overview or tray menu. If shortcuts have not loaded, restart Codex when other tasks are idle.
5. In **启动与集成**, choose **安装并开启自启**, or run `Install.cmd`. Installation uses your Windows account without elevation. Configure a completion hook only after choosing a stable executable location, then review and trust it in Codex's Hooks UI.

The app runs in the tray, including the hidden-icons area. Installation adds a Start menu shortcut and a login-startup entry. The shortcut and running app share a stable Windows app ID; its explicit icon file has a content-derived filename so an update does not reuse a stale EXE icon-cache key. Reinstall an older copy through the control center to update its shortcut metadata. Existing Codex shortcuts are preserved; conflicting bindings stop setup with an explanation. The executable is **unsigned**.

## Controls

The P240 buffers digits locally. Enter a code, then press the green key to submit it.

| Input | Action |
|---|---|
| `1`–`9` + green | Open the corresponding recent chat in the current snapshot, or a fixed chat in fixed mode (`#1`–`#9` also work) |
| `*1`–`*9` + green | Open a project's configured existing chat (`11`–`19` also work) |
| `22` or `*#` + green | Enter native Codex sidebar navigation (default) |
| `2` / `8` + green while browsing | Set the direction to up / down and move one sidebar item |
| `*` / `#` + green | Previous / next; also enters browsing |
| `5` + green while browsing; `23` + green anywhere | Leave browsing, keeping the current chat |
| Green with no digits while browsing | Move one more item in the current direction; optional popup mode confirms its selected item |
| `0` + green | Start the selected voice mode in the current chat |
| Green with no digits while idle | Show a hint; never start recording |
| `00` + green | Explicitly send the current Codex draft after ending dictation and reviewing the text |
| Green during handset-started voice, or `*0` + green | Stop that voice session; red is best-effort |
| `#0` + green | Toggle microphone mute in a Codex voice conversation |
| Red while dialing | Clear the pending number |
| Device volume / mute keys | Normal audio controls; see the active-call compatibility caveat below |

Project bindings open existing chats; they do not create new projects or chats. Dictation produces a draft for you to review and send. Finish a recording before changing targets. The controller focuses Codex before voice shortcuts and preserves an already-maximized window.

For a complete handset dictation flow: `0` + green starts, green again (or `*0` + green) stops, then wait for the text, review it and use **`00` + green** to send. Setup binds Codex's dedicated `composer.submit` command, defaulting to `Ctrl+Alt+Shift+F12`; there is no generic Enter fallback. Sending is refused while Dialvia still tracks a recording or when the submit binding is missing, disabled or conflicting. A new binding may need a Codex restart to load. The native command retains Codex's own empty-draft/disabled-submit behavior and configured follow-up behavior.

Submission first focuses the native main composer and lets focus settle. The tray reports that the shortcut was sent; it cannot acknowledge that Codex actually submitted the draft. Test a newly installed binding after reopening Codex and when the chat can accept submission. Ordinary submit may do nothing while the current task is in a stop/disabled-submit state; Dialvia does not bypass that behavior.

If Codex has already stopped recording but the controller still reports recording, use **Codex 已停止：校正状态** in voice settings/the tray after checking the UI. This resets handset tracking without sending a shortcut.

Before dictation, Dialvia invokes Codex's native `focusMainChat` command (default `Alt+L`) and allows focus to settle. It uses read-only Windows microphone-usage metadata as an advisory stop check to avoid toggling an already-stopped dictation back on. This metadata is not a stable public voice-status API and does not prove that the current composer is recording. Missing or invalid records remain unknown. See [voice behavior](docs/voice.md).

A stop request stays pending until a known microphone-stop timestamp is at or after this recording's start request. An old inactive record never clears a new start, even after a delay. Rapid repeated stop presses are suppressed; a later explicit retry requires evidence of continuing capture. Keyboard press/release are separated by 80 ms for Codex's Windows global-hotkey listener.

**P240 active-call compatibility:** in the observed test, the same green key produced raw 7 (`Talk`) while idle, but raw 3 (`VolumeUpHeld`) during the controller's call. Alpha.5 treats the latter as a stop request only while a local call and controller voice are active. A preceding real volume-up event protects its following hold sequence. A standalone volume hold with the same report remains indistinguishable and may stop recording; disable **P240 通话中接听键兼容** in voice settings if it affects the device. Idle raw 3 never starts voice. This is scoped to the calibrated setup, not every P240/Hub combination.

**Projects keep fixed numbers; handset browsing defaults to Codex's own sidebar order.** It invokes the installed client's `previousThread` / `nextThread` commands and respects supported custom bindings. Each move opens the destination immediately; it does not merely highlight a row for later confirmation. Disabled or conflicting bindings produce an explanation. No extra Codex bindings or restart are required for the default setup.

For repeated browsing, use `#` + green once, then press **green alone** for each next item. To reverse direction, use `*` + green (or `2` + green), then continue with green alone. `22` starts in the downward direction without moving. Rapid green presses are queued separately rather than resetting one debounce timer. Numeric exit cancels pending steps so they cannot start dictation after leaving browsing.

Direct `1`–`9` dialing outside browsing still follows the recent/fixed setting. In optional **Dialvia list** mode, its project-filtered snapshot stays in order until refresh or project change. This local filter does not change native sidebar navigation. The popup shows titles and dates and supports keyboard navigation. [Design and Xiaomi remote research](docs/navigation.md).

The P240's central scroll keys produced no usable SDK or raw USB input in the tested setup; they are not mapped. Ordinary side-volume controls retain audio behavior, with the ambiguous held-report caveat described above. Idle red-key presses did not close the alpha.1 popup, so **use `5` + green or `23` + green to exit**. Red remains best-effort for call-state events. Long presses of `*` / `#` are not implemented: the available dial-string interface supplies neither reliable press/release timing nor per-digit host events. Numeric codes need green to submit; repeated native browsing uses green alone.

## Voice and notifications

- **Dictation is the default.** Audio is captured and transcribed by Codex/ChatGPT using your signed-in account. Dialvia sends the configured shortcuts and does not provide its own STT backend or require an OpenAI API key. The exact transcription model is not exposed by the inspected client. [Details](docs/voice.md).
- **Conversation mode** triggers Codex's own voice-chat commands where available for the account and task. Availability is controlled by Codex.
- Microphone and playback endpoints follow Windows/Codex selection. The settings button opens Windows Sound; it does not silently replace your system defaults.
- Popup notifications are enabled; handset ringing is opt-in. Ringing is suppressed during voice tracked by Dialvia and in detection mode.
- **Configure completion hook** adds an asynchronous `Stop` handler invoking `Dialvia.exe --notify`. Codex requires first-time review/trust. The existing `config.toml` `notify` value is untouched. The notification contains only a generic completion message.

See [Codex voice](https://learn.chatgpt.com/docs/features/voice), [commands and shortcuts](https://learn.chatgpt.com/docs/reference/commands), and [Hooks review](https://learn.chatgpt.com/docs/hooks).

## Compatibility and scope

| Device / target | Status |
|---|---|
| Plantronics Calisto P240 (`047F:AE04`) via Hub COM | Implemented; original controller physically exercised |
| Other Poly / Plantronics devices | Not supported by the current whitelist; require model-specific mapping and testing |
| Generic USB HID keypads / phones / other control surfaces | Future adapters; no generic-HID claim |
| Codex desktop on Windows x64 | Implemented destination |
| Anyvia | Feasible future integration; not implemented here |
| macOS / Linux | Not supported by this release |

**Why Dialvia?** “Dial” describes the first handset interaction; “via” describes a route into an existing tool and echoes the owner's Anyvia project. The device-neutral `IDeviceAdapter` boundary leaves room for other hardware, while the support table names what actually works. The original D icon uses Anyvia's paper/ink/copper palette with its own geometry. All icon backgrounds are transparent; the tray selects an ink or ivory mark from the Windows taskbar theme. [Asset sizes and UI checks](docs/visual-design.md).

## Build and test

Use a regular Windows terminal. No administrator privileges, package-manager downloads, or .NET SDK are required. Windows PowerShell 5.1 or PowerShell 7 can run the scripts under your current execution policy; do not change security settings just to build.

```powershell
git clone https://github.com/Octo-o-o-o/Dialvia.git
cd Dialvia
.\scripts\Test.ps1       # USB / Hub not required
.\scripts\Build.ps1      # Requires the installed Plantronics COM type library
```

`Build.ps1` runs the core and brand checks, regenerates transparent SVG/PNG/ICO assets, imports local COM metadata into an adjacent managed DLL, embeds shell/brand/light-tray/dark-tray resources, then creates:

```text
dist/Dialvia-win-x64/Dialvia.exe
dist/Dialvia-0.1.0-alpha.8-win-x64.zip
dist/SHA256SUMS.txt
```

Pass `-PlantronicsTypeLibrary <installed PlantronicsCOM.dll>` for a nonstandard Hub path. GitHub Actions runs the hardware-independent tests and icon export; it does not download or redistribute Hub. [Build details](docs/build.md).

## Local data and removal

- Settings, logs and integration ownership: `%LOCALAPPDATA%\Dialvia`.
- Short-lived notification queue shared with packaged Codex: `Documents\Codex\Dialvia\notifications`.
- Run `Uninstall.cmd` or `Dialvia.exe --uninstall` to remove startup and the exact unchanged bindings/hook owned by this app. User data and Plantronics Hub remain.
- Do not run the earlier personal P240 controller and Dialvia together; both can become the Hub default softphone. Existing private mappings can be entered using the graphical binding editor.

The recent-session picker reads only local metadata (ID, title, project directory and activity time; up to 200 unarchived chats) through a read-only connection to Codex's local index. This is an unofficial schema dependency; manual links remain available when it changes. No transcript is read or uploaded.

No telemetry, credentials, audio recording files, or developer-specific chat inventory are distributed. See [security and data handling](SECURITY.md), [MIT license](LICENSE), and [third-party notices](THIRD-PARTY-NOTICES.md).
