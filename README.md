<p align="center"><img src="assets/brand/dialvia-icon.svg" width="112" alt="Dialvia icon" /></p>
<h1 align="center">Dialvia</h1>
<p align="center">Physical controls for Codex on Windows.</p>
<p align="center"><a href="README.zh-CN.md">简体中文</a> · <a href="https://github.com/Octo-o-o-o/Dialvia/releases">Download</a> · <a href="docs/architecture.md">Architecture</a></p>

Turn a USB handset into a small Codex control surface: dial a number to open a chat, start voice dictation, and receive a task notification. **Plantronics Calisto P240 is the first and currently the only implemented device adapter.**

> **0.1.0-alpha.1** — early Windows x64 release. The original P240 controller has real-device confirmation for project/chat navigation and dictation producing text. This project adds a graphical control center, recent-session browsing, startup management and 41 core assertions. New browsing codes, full conversation mode, audible ringing, and a trusted real Codex completion event still need device acceptance. See [validation](docs/validation.md).

![Dialvia control center with fictional demo projects](assets/screenshots/projects.png)

The screenshot uses fictional demo data. Daily configuration is graphical; JSON remains an optional advanced tool.

## Get started

1. Install Codex for Windows and **Plantronics Hub separately**, then connect a P240. The development device used Hub 3.25.2. Standard USB audio alone does not enable its full call-control interface.
2. Download and extract the [Windows x64 ZIP](https://github.com/Octo-o-o-o/Dialvia/releases). Keep `Dialvia.exe` and `Interop.Plantronics.dll` together; start the EXE from Windows Explorer. Windows with .NET Framework 4.8 and the installed Hub runtime is required. A separate .NET SDK is not needed.
3. Click the tray icon to open the control center. In **会话与项目**, choose a project from the local Codex list and optionally assign fixed project numbers. The public configuration is empty and starts in **detection mode**. It contains no developer chat IDs.
4. In **语音与通知**, select dictation or voice conversation. Configure voice shortcuts in **启动与集成**, then turn off detection mode in the overview or tray menu. If shortcuts have not loaded, restart Codex when other tasks are idle.
5. In **启动与集成**, choose **安装并开启自启**, or run `Install.cmd`. Installation uses your Windows account without elevation. Configure a completion hook only after choosing a stable executable location, then review and trust it in Codex's Hooks UI.

The app runs in the tray, including the hidden-icons area. Installation adds a Start menu shortcut and a login-startup entry. Existing Codex shortcuts are preserved; conflicting bindings stop setup with an explanation. The executable is **unsigned**.

## Controls

The P240 buffers digits locally. Enter a code, then press the green key to submit it.

| Input | Action |
|---|---|
| `1`–`9` + green | Open the corresponding recent chat in the current snapshot, or a fixed chat in fixed mode (`#1`–`#9` also work) |
| `*1`–`*9` + green | Open a project's configured existing chat (`11`–`19` also work) |
| `*#` + green | Open the recent-session browser |
| `*` / `#` + green | Move to the previous / next chat in the browser |
| Green with no digits in the browser / red | Confirm / close the browser |
| `0` + green, or green with no digits outside the browser | Start the selected voice mode |
| Red during handset-started voice, or `*0` + green | Stop that voice session |
| `#0` + green | Toggle microphone mute in a Codex voice conversation |
| Red while dialing | Clear the pending number |
| Device volume / mute keys | Retain the P240's own audio controls |

Project bindings open existing chats; they do not create new projects or chats. Dictation produces a draft for you to review and send. Finish a recording before changing targets. The controller focuses Codex before voice shortcuts and preserves an already-maximized window.

**Projects keep fixed numbers; chats default to recently active within the selected project.** The browsing snapshot stays in order until refresh or a project change, so opening a chat does not immediately change its number. The browser shows title and today/yesterday/date; keyboard Up/Down selects a chat, Left/Right selects a project, Enter opens it and Escape closes. Fixed chat numbers remain optional. Dates are display context rather than dial codes. [Design and Xiaomi remote research](docs/navigation.md).

The P240's central scroll keys produced no usable SDK or raw USB input in the tested setup; they are not mapped. Side volume keys keep their original audio behavior. The symbol-based browsing codes need final device acceptance.

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

**Why Dialvia?** “Dial” describes the first handset interaction; “via” describes a route into an existing tool and echoes the owner's Anyvia project. The device-neutral `IDeviceAdapter` boundary leaves room for other hardware, while the support table names what actually works. The original D icon uses Anyvia's paper/ink/copper palette with its own geometry.

## Build and test

Use a regular Windows terminal. No administrator privileges, package-manager downloads, or .NET SDK are required. Windows PowerShell 5.1 or PowerShell 7 can run the scripts under your current execution policy; do not change security settings just to build.

```powershell
git clone https://github.com/Octo-o-o-o/Dialvia.git
cd Dialvia
.\scripts\Test.ps1       # USB / Hub not required
.\scripts\Build.ps1      # Requires the installed Plantronics COM type library
```

`Build.ps1` runs the core tests, regenerates SVG/PNG/ICO assets, imports local COM metadata into an adjacent managed DLL, embeds the icon, then creates:

```text
dist/Dialvia-win-x64/Dialvia.exe
dist/Dialvia-0.1.0-alpha.1-win-x64.zip
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
