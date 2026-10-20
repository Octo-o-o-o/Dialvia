param([string]$Report)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$build=Join-Path $root 'build\tests'
New-Item -ItemType Directory -Path $build -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw '.NET Framework x64 compiler is required.'}
$sources=@('src\Dialvia\Models.cs','src\Dialvia\CodexDesktop.cs','src\Dialvia\CodexInventory.cs','src\Dialvia\UserIntegration.cs','tests\CoreTests.cs') | ForEach-Object {Join-Path $root $_}
$exe=Join-Path $build 'CoreTests.exe'
& $compiler /nologo /codepage:65001 /target:exe /platform:x64 /reference:System.Web.Extensions.dll /out:$exe $sources
if($LASTEXITCODE -ne 0){throw 'Core test compilation failed.'}
if(-not $Report){$Report=Join-Path $build 'result.json'}
& $exe $Report
if($LASTEXITCODE -ne 0){throw 'Core tests failed.'}
