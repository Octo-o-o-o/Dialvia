# Building the Windows release

Requirements: Windows x64, .NET Framework 4.8 with its `Framework64\v4.0.30319\csc.exe`, PowerShell, and an installed Plantronics Hub COM type library for the full build. A .NET SDK, NuGet restore and administrator terminal are not required.

```powershell
.\scripts\Test.ps1
.\scripts\Build.ps1
# Optional nonstandard installed SDK location:
.\scripts\Build.ps1 -PlantronicsTypeLibrary 'D:\InstalledHub\PlantronicsCOM.dll'
```

The build uses the .NET type-library importer to generate `build/interop/Interop.Plantronics.dll` from the installed vendor runtime. The EXE uses it as a normal adjacent assembly and embeds the multi-resolution shell icon, transparent in-app mark and separate light/dark tray icons. The COM implementation must still be installed by Hub. The importer, vendor library and Hub installer are not shipped as application runtime files.

The binary is a Windows **WinExe** targeting .NET Framework, not a bundled native-AOT runtime. The output is an EXE, its generated managed interface DLL, documentation and install/remove launchers. Keep the EXE and DLL together. Executables are unsigned; no code-signing certificate or signature claim is fabricated.

`Build-Brand.ps1` uses the same original vector paths for transparent SVG, PNG and 15 native 16–256 px ICO frames per role. It uses only Windows `System.Drawing`. `Test-Brand.ps1` checks 45 frames, transparency, bounds, source matching and contrast; the build and CI run it automatically. The full release build additionally runs `Test-WindowsIcons.ps1` against the compiled EXE, extracting shell icons and rendering 8 native shell/window/tray samples to catch palette/resource mismatches. See [visual design](visual-design.md).

```powershell
.\scripts\Test-Brand.ps1
.\scripts\Test-WindowsIcons.ps1 # Checks resources from the built EXE
.\scripts\Test-UI.ps1   # Requires the built EXE; fixture data only
.\dist\Dialvia-win-x64\Dialvia.exe --preview-ui settings
.\dist\Dialvia-win-x64\Dialvia.exe --preview-ui sessions
```

`Test-UI.ps1` captures 13 states and checks input names and horizontal flow overflow. Its report records the actual Windows graphics DPI; it is not a multi-monitor certification. `--preview-ui` opens isolated fictional forms for keyboard/visual QA and neither loads private configuration nor controls hardware/Codex. The preview saves only its in-memory fixture.

Diagnostics (do not control Codex):

```powershell
.\dist\Dialvia-win-x64\Dialvia.exe --self-test "$PWD\build\self-test.json"
.\dist\Dialvia-win-x64\Dialvia.exe --probe-device "$PWD\build\device-probe.json"
.\dist\Dialvia-win-x64\Dialvia.exe --inventory-check "$PWD\build\inventory-check.json"
.\dist\Dialvia-win-x64\Dialvia.exe --render-preview "$PWD\build\gui-preview"
```

`--probe-device` registers a read-only SDK client and checks `047F:AE04`; it does not change the default softphone, send keys, record audio or dial a call. The installer rejects an inherited MSIX package identity to prevent the virtualized LocalAppData installation issue seen during development. Launch the release from Windows Explorer to install.

GitHub Actions runs core tests and brand rebuild/validation without Hub. GUI checks run locally with the built Windows EXE. A full executable is built on a Windows machine with Hub; CI does not download vendor software from archive mirrors.

`--inventory-check` writes only a count after a read-only metadata query. `--render-preview` renders the app's own forms with fictional fixture data, without connecting hardware or changing user settings. `--install-report <file>` performs the same per-user install as `--install`, returning JSON instead of a completion dialog for managed installation workflows.

`Dialvia.sln` is also supplied for Visual Studio/MSBuild with the .NET Framework 4.8 developer targeting pack. Run `Build.ps1` once first to generate the local interface assembly and brand resources. The script remains the release build path.
