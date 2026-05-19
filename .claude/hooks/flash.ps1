# Claude alarm: detached persistent tray notification (stays until user clicks)
$logPath = Join-Path $PSScriptRoot 'flash.log'
$ts = Get-Date -Format 'HH:mm:ss'
Add-Content -Path $logPath -Value "[$ts] flash called" -ErrorAction SilentlyContinue

# Kill existing notify process so we don't pile up
$pidFile = Join-Path $PSScriptRoot 'notify.pid'
if (Test-Path $pidFile) {
    try {
        $oldPid = [int](Get-Content $pidFile -ErrorAction Stop)
        Stop-Process -Id $oldPid -Force -ErrorAction SilentlyContinue
        Remove-Item $pidFile -ErrorAction SilentlyContinue
        Add-Content -Path $logPath -Value "  killed old notify pid=$oldPid" -ErrorAction SilentlyContinue
    } catch {}
}

# Spawn new persistent notify (detached, doesn't block this hook)
$notifyScript = Join-Path $PSScriptRoot 'notify-persistent.ps1'
$proc = Start-Process powershell.exe -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-WindowStyle","Hidden","-File","`"$notifyScript`"" -PassThru -WindowStyle Hidden
Add-Content -Path $logPath -Value "  spawned notify pid=$($proc.Id)" -ErrorAction SilentlyContinue
exit 0
