param([string]$Executable,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $Executable){$Executable=Join-Path $root 'dist\Dialvia-win-x64\Dialvia.exe'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $root 'build\ui-tests'}
$Executable=[IO.Path]::GetFullPath($Executable);$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not (Test-Path -LiteralPath $Executable)){throw 'Build the Windows executable first.'}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$process=Start-Process -FilePath $Executable -ArgumentList @('--render-preview',('"'+$OutputDirectory+'"')) -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(20000)){$process.Kill();throw 'UI fixture rendering timed out.'}
if($process.ExitCode -ne 0){throw ('UI fixture rendering failed. See '+$OutputDirectory)}
$report=Get-Content -LiteralPath (Join-Path $OutputDirectory 'ui-audit.json') -Raw | ConvertFrom-Json
if($report.version -ne (Get-Item -LiteralPath $Executable).VersionInfo.ProductVersion){throw 'UI report does not match the executable version.'}
$expected=@('overview','projects','voice','startup','overview-compact','projects-compact','voice-compact','startup-compact','binding-project','binding-session','empty-sessions','error-sessions','recent-sessions')
foreach($name in $expected){
    $case=@($report.cases|Where-Object {$_.name -eq $name})
    if($case.Count -ne 1 -or $case[0].missingInputNames.Count -gt 0 -or $case[0].horizontalOverflows.Count -gt 0){throw ('UI layout/accessibility check failed: '+$name)}
    if(-not (Test-Path -LiteralPath (Join-Path $OutputDirectory ($name+'.png')))){throw ('Missing UI image: '+$name)}
}
Write-Output ('Validated '+$expected.Count+' UI states with demo data; no missing input names or horizontal overflow.')
