param([string]$Executable,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $Executable){$Executable=Join-Path $root 'dist\Dialvia-win-x64\Dialvia.exe'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $root 'build\windows-icon-tests'}
$Executable=[IO.Path]::GetFullPath($Executable);$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$test=Join-Path $OutputDirectory 'WindowsIconTests.exe'
& $compiler /nologo /target:exe /platform:x64 /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /out:$test (Join-Path $root 'tests\WindowsIconTests.cs')
if($LASTEXITCODE -ne 0){throw 'Windows icon test compilation failed.'}
& $test $Executable $OutputDirectory
if($LASTEXITCODE -ne 0){throw 'Windows shell/window/tray icon validation failed.'}
