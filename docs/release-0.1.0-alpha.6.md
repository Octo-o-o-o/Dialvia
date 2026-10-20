Dialvia now uses a larger transparent D icon without the old rounded background plate. The artwork grows from about 57% to 92% of the canvas height, with separate ink/ivory tray variants selected for the Windows taskbar theme and DPI. Each shell/tray ICO includes 15 native sizes.

The control center also receives a UI pass: responsive binding columns, clear disabled/focus states, accessible field names, Save only when changed, Ctrl+S, editor Cancel/Esc, Chinese validation, and explicit empty/error session states. All public screenshots and packaged artwork have been regenerated from fictional demo data.

Validation: 98 core assertions, 45 transparent icon frames/3 SVGs, and 13 local fixture UI states at Windows 150% scaling. Native fixture checks cover all settings pages, invalid save, cancel/Esc, dropdown Esc and save state. The main GUI remains a light System-DPI-aware Windows interface; mixed-monitor/high-contrast acceptance is still open. [Visual design and scope](https://github.com/Octo-o-o-o/Dialvia/blob/main/docs/visual-design.md).

Previously accepted P240 dictation start/green stop/draft and continuous native chat switching are retained. This release does not claim new handset-send, voice-conversation, audible-ring or trusted real Stop-event acceptance.

Extract **Dialvia-0.1.0-alpha.6-win-x64.zip**, keeping `Dialvia.exe` and `Interop.Plantronics.dll` together. Plantronics Hub is a separate prerequisite. Per-user installation and login startup remain available in the tray/settings. The EXE is unsigned; SHA-256 sums are included.
