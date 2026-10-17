param([switch]$Remove)
$ErrorActionPreference = 'Stop'
$studioRoot = $PSScriptRoot
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'PitCrew.lnk'
if ($Remove) {
    if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath }
    Write-Output 'PitCrew 로그인 자동 실행을 해제했습니다. 실행 중인 작업은 유지됩니다.'
    exit 0
}
$pythonWindowless = Join-Path $studioRoot '.venv\Scripts\pythonw.exe'
$watchdogPath = Join-Path $studioRoot 'watchdog.py'
if (!(Test-Path -LiteralPath $pythonWindowless) -or !(Test-Path -LiteralPath $watchdogPath)) {
    throw '먼저 start.ps1로 PitCrew 설치를 완료하세요.'
}
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcutPath)
$link.TargetPath = $pythonWindowless
$link.Arguments = '-X utf8 "{0}" --port 7878' -f $watchdogPath
$link.WorkingDirectory = Split-Path -Parent (Split-Path -Parent $studioRoot)
$link.Description = 'PitCrew 로컬 서버와 자동 복구 감시를 로그인할 때 시작합니다.'
$link.WindowStyle = 7
$link.Save()
$stopMarker = Join-Path $studioRoot '.runtime\watchdog-7878.stop'
if (Test-Path -LiteralPath $stopMarker) { Remove-Item -LiteralPath $stopMarker }
Write-Output "로그인 자동 실행 등록: $shortcutPath"
Write-Output 'Windows 로그인 후 http://localhost:7878/ 에서 사용하세요.'
