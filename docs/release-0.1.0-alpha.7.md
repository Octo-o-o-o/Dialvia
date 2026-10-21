Fix the taskbar and title-bar icon mismatch from alpha.6. Those resources were accidentally rendered entirely copper, while the in-app header used the original ink/copper brand. EXE, shortcuts and windows now use the same two-tone D, retaining the transparent background and enlarged artwork. Light/dark notification-area variants remain available.

The release build now checks the actual binary: it extracts native shell icons and renders eight shell/window/tray samples with Windows DrawIconEx. Alpha.6 fails because its shell icon contains no ink pixels; the corrected EXE passes. Brand checks also require shell artwork to match the in-app palette at every size. All 45 ICO frames, 3 SVGs and 13 fixture UI states pass their checks.

The existing handset controls and acceptance limits are unchanged. Extract **Dialvia-0.1.0-alpha.7-win-x64.zip** with EXE and interface DLL together. Plantronics Hub is still a separate prerequisite; binaries remain unsigned. SHA-256 sums are included.
