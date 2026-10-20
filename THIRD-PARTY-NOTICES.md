# Third-party notices

Dialvia's source and original brand assets are MIT licensed. It is an independent community project, not an official OpenAI, HP, Poly or Plantronics product.

- **Plantronics Hub / COM runtime** must be installed separately under its vendor terms. This repository and release do not include Hub, its installer, `PlantronicsCOM.dll`, firmware or vendor services.
- The build generates `Interop.Plantronics.dll` from the locally installed COM type library using the .NET Framework type-library importer. This adjacent assembly contains managed interface metadata; it is not the vendor COM implementation.
- **Microsoft .NET Framework 4.8** and Windows system components are operating-system prerequisites, not bundled runtimes.
- Anyvia is the owner's related project. Dialvia's original D mark uses a similar paper/ink/copper palette; it does not reuse Anyvia's A geometry.

Design and integration references: [Plantronics SDK examples](https://github.com/pltdev/Samples), [Joydex](https://github.com/Mattie/joydex), [Codex commands](https://learn.chatgpt.com/docs/reference/commands). No source code from these community projects is vendored.
