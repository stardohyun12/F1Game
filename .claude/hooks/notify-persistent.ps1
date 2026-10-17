# Persistent Claude notification: tray icon + balloon, stays until user clicks
# Spawned detached from flash.ps1 (hook returns immediately, this runs forever until click)

$pidFile = Join-Path $PSScriptRoot 'notify.pid'
[System.IO.File]::WriteAllText($pidFile, $PID)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Audible alarm — play system sound once (도현 요청: 3번 시끄러움 → 1번)
try {
    [System.Media.SystemSounds]::Exclamation.Play()
} catch {}

$notif = New-Object System.Windows.Forms.NotifyIcon
$notif.Icon = [System.Drawing.SystemIcons]::Information
$notif.Text = 'Claude Code: click to dismiss'
$notif.BalloonTipTitle = 'Claude Code'
$notif.BalloonTipText = 'Waiting for input - click this icon to dismiss'
$notif.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info
$notif.Visible = $true
$notif.ShowBalloonTip(10000)

$script:done = $false
$dismiss = {
    $script:done = $true
}
$notif.add_Click($dismiss)
$notif.add_BalloonTipClicked($dismiss)
$notif.add_DoubleClick($dismiss)

# 2026-10-13: dismiss 를 안 누르면 다음 알람이 전부 건너뛰어졌음(pid 파일이 살아 있어서) → 15분 뒤 자동 종료, 5분마다 풍선 재표시
$started = Get-Date
$lastTip = Get-Date
while (-not $script:done) {
    [System.Windows.Forms.Application]::DoEvents()
    Start-Sleep -Milliseconds 200
    if (((Get-Date) - $started).TotalMinutes -ge 15) { break }
    if (((Get-Date) - $lastTip).TotalMinutes -ge 5) { $notif.ShowBalloonTip(10000); $lastTip = Get-Date }
}

$notif.Visible = $false
$notif.Dispose()
Remove-Item $pidFile -ErrorAction SilentlyContinue
