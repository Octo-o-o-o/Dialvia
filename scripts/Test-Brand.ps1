param([string]$Report,[string]$BaselineIcon)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$build=Join-Path $root 'build\brand-tests'
New-Item -ItemType Directory -Path $build -Force | Out-Null
if(-not $Report){$Report=Join-Path $build 'result.json'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $build 'BrandTests.exe'
& $compiler /nologo /target:exe /platform:x64 /reference:System.Drawing.dll /reference:System.Xml.Linq.dll /reference:System.Web.Extensions.dll /out:$exe (Join-Path $root 'tests\BrandTests.cs')
if($LASTEXITCODE -ne 0){throw 'Brand validation compilation failed.'}
& $exe $root $Report $BaselineIcon
if($LASTEXITCODE -ne 0){throw 'Brand asset validation failed.'}
