Fix the remaining stale Windows taskbar button. Alpha.7 corrected the EXE/window resource, but the user still saw an orange taskbar entry. Native inspection confirmed the live alpha.7 window already used the two-tone brand.

Alpha.8 gives the process and installed Start-menu shortcut the same explicit `Dialvia.Desktop` app identity. The shortcut references a separate ICO whose filename derives from the icon content, so the taskbar no longer relies on the stale EXE icon-cache key. Its launch action opens the control center; login startup remains in the background. Refresh is limited to owned shortcut/icon entries, with no Explorer restart or global cache deletion.

**The user confirmed the real bottom taskbar button now shows the black/copper D.** Installed shell diagnostics and an independent canonical Windows Shell-property check passed. Nine native icon renders (including the actual runtime loader), 45 ICO frames/3 SVGs and 13 fixture UI states pass their checks.

Extract **Dialvia-0.1.0-alpha.8-win-x64.zip** with EXE and interface DLL together. Use **Install / login startup** in the control center to refresh an older installation's shortcut metadata. Plantronics Hub remains a separate prerequisite; binaries are unsigned. Existing handset controls and pending submission/voice/notification acceptance are unchanged. SHA-256 sums are included.
