param([string]$PlantronicsTypeLibrary,[switch]$SkipTests)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $SkipTests){& (Join-Path $PSScriptRoot 'Test.ps1')}
& (Join-Path $PSScriptRoot 'Build-Brand.ps1')
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not $PlantronicsTypeLibrary){$PlantronicsTypeLibrary=Join-Path ${env:ProgramFiles(x86)} 'Plantronics\PlantronicsCOM.dll'}
if(-not (Test-Path -LiteralPath $PlantronicsTypeLibrary)){throw 'Install Plantronics Hub separately, or pass -PlantronicsTypeLibrary. Vendor runtime is not included.'}
$build=Join-Path $root 'build\interop'; $dist=Join-Path $root 'dist\Dialvia-win-x64'
New-Item -ItemType Directory -Path $build,$dist -Force | Out-Null
$importer=Join-Path $build 'Import-Plantronics.exe'
& $compiler /nologo /codepage:65001 /target:exe /platform:x64 /out:$importer (Join-Path $PSScriptRoot 'Import-Plantronics.cs')
if($LASTEXITCODE -ne 0){throw 'COM importer compilation failed.'}
Push-Location $build
try{& $importer $PlantronicsTypeLibrary *> (Join-Path $build 'import.log');if($LASTEXITCODE -ne 0){throw 'COM type library import failed; see build/interop/import.log.'}}finally{Pop-Location}
$interop=Join-Path $build 'Interop.Plantronics.dll'
$icon=Join-Path $root 'assets\brand\dialvia.ico'
$brand=Join-Path $root 'assets\brand\dialvia-256.png'
$manifest=Join-Path $root 'src\Dialvia\app.manifest'
$exe=Join-Path $dist 'Dialvia.exe'
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'src\Dialvia') -Filter '*.cs' -File | ForEach-Object {$_.FullName})
& $compiler /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /utf8output /out:$exe /win32icon:$icon /win32manifest:$manifest /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:$interop /resource:"$icon,Dialvia.AppIcon" /resource:"$brand,Dialvia.BrandImage" $sources
if($LASTEXITCODE -ne 0){throw 'Application compilation failed.'}
Copy-Item -LiteralPath $interop -Destination $dist -Force
Copy-Item -LiteralPath (Join-Path $root 'config.example.json'),(Join-Path $root 'README.md'),(Join-Path $root 'README.zh-CN.md'),(Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD-PARTY-NOTICES.md'),(Join-Path $root 'SECURITY.md'),(Join-Path $root 'CHANGELOG.md') -Destination $dist -Force
Copy-Item -LiteralPath (Join-Path $root 'docs'),(Join-Path $root 'assets') -Destination $dist -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'scripts\Install.cmd') -Destination (Join-Path $dist 'Install.cmd') -Force
Copy-Item -LiteralPath (Join-Path $root 'scripts\Uninstall.cmd') -Destination (Join-Path $dist 'Uninstall.cmd') -Force
$version='0.1.0-alpha.4'
$zip=Join-Path $root ('dist\Dialvia-'+$version+'-win-x64.zip')
if(Test-Path -LiteralPath $zip){Remove-Item -LiteralPath $zip}
Compress-Archive -LiteralPath $dist -DestinationPath $zip
$hashes=@($exe,(Join-Path $dist 'Interop.Plantronics.dll'),$zip) | ForEach-Object {$hash=Get-FileHash -LiteralPath $_ -Algorithm SHA256; $hash.Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($_)}
[IO.File]::WriteAllLines((Join-Path $root 'dist\SHA256SUMS.txt'),$hashes,[Text.UTF8Encoding]::new($false))
Get-Item -LiteralPath $exe,$zip | Select-Object FullName,Length
