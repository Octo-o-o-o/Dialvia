Dialvia turns a Plantronics Calisto P240 into a Windows control surface for Codex.

Download **Dialvia-0.1.0-alpha.1-win-x64.zip**, extract it, and keep `Dialvia.exe` and `Interop.Plantronics.dll` together. Plantronics Hub is a separate prerequisite. The ZIP includes English/Chinese setup guides and install/remove launchers. Files are unsigned; SHA-256 sums are provided.

This first alpha includes a graphical tray control center, fixed project numbers, recent-session snapshots, optional fixed chats, voice dictation controls, notification settings, per-user startup and an opt-in native Codex Stop hook. Daily configuration uses the GUI. Only P240 is implemented; additional hardware adapters are future work.

Validation: 41 core assertions passed, including native SQLite read-only fixtures. The release EXE self-test, non-owning P240 discovery, inventory check and native notification fixture passed. GUI fixture screenshots were checked at 150% scaling. The original device controller has user confirmation of navigation and dictation producing text. New symbol-based browsing, full voice conversation, audible ringing and trusted real-Stop acceptance remain open. P240 central scroll keys produced no usable host input in the tested setup and are not mapped. The maximized-window restore bug was fixed in both implementations.

Start in detection mode, add your own target links, configure Codex shortcuts and review/trust the completion hook before enabling it. No private chat/project mappings or vendor installer are included. Do not run this app and the older personal P240 controller at the same time.
