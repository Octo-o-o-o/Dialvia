Balance the taskbar icon with neighboring apps. The taskbar/EXE/shortcut D now occupies 80% of its transparent canvas height instead of 92.3%; compact tray and in-app header artwork remain unchanged at 92.3%. The original shape and black/copper palette are retained.

The shell proportion is an optical design choice for Dialvia, not a Windows-mandated percentage. Each role still exports 15 native ICO sizes. Asset and native-render checks now verify role-specific bounds/centering as well as transparency and palette: 45 icon frames/3 SVGs, 9 native resource renders and 13 fixture UI states pass.

The alpha.8 explicit app/shortcut identity and content-addressed icon reference are retained; new taskbar artwork gets a fresh icon filename. Install through the control center to update the shortcut. Existing settings and login startup are preserved. Device controls and their acceptance limits are unchanged.

Extract **Dialvia-0.1.0-alpha.9-win-x64.zip** with EXE and interface DLL together. Plantronics Hub remains a separate prerequisite; binaries are unsigned. SHA-256 sums are included.
