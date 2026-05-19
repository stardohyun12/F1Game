# Claude alarm: detached persistent tray notification (stays until user clicks)
$logPath = Join-Path $PSScriptRoot 'flash.log'
$ts = Get-Date -Format 'HH:mm:ss'
Add-Content -Path $logPath -Value "[$ts] flash called" -ErrorAction SilentlyContinue

# If notify already active (not dismissed), skip — prevents multiple sounds
# when several hooks (Notification + PreToolUse + Stop) trigger near-simultaneously.
$pidFile = Join-Path $PSScriptRoot 'notify.pid'
if (Test-Path $pidFile) {
    try {
        $oldPid = [int](Get-Content $pidFile -ErrorAction Stop)
        $proc = Get-Process -Id $oldPid -ErrorAction SilentlyContinue
        if ($proc) {
            Add-Content -Path $logPath -Value "  already active pid=$oldPid, skip new spawn" -ErrorAction SilentlyContinue
            exit 0
        }
        # PID file stale (process died without cleanup) — remove and proceed
        Remove-Item $pidFile -ErrorAction SilentlyContinue
        Add-Content -Path $logPath -Value "  stale pid file cleaned" -ErrorAction SilentlyContinue
    } catch {}
}

# Spawn new persistent notify (detached, doesn't block this hook)
$notifyScript = Join-Path $PSScriptRoot 'notify-persistent.ps1'
$proc = Start-Process powershell.exe -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-WindowStyle","Hidden","-File","`"$notifyScript`"" -PassThru -WindowStyle Hidden
Add-Content -Path $logPath -Value "  spawned notify pid=$($proc.Id)" -ErrorAction SilentlyContinue
exit 0
