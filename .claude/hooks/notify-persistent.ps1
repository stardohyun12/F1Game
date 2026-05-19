# Persistent Claude notification: tray icon + balloon, stays until user clicks
# Spawned detached from flash.ps1 (hook returns immediately, this runs forever until click)

$pidFile = Join-Path $PSScriptRoot 'notify.pid'
[System.IO.File]::WriteAllText($pidFile, $PID)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

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

while (-not $script:done) {
    [System.Windows.Forms.Application]::DoEvents()
    Start-Sleep -Milliseconds 200
}

$notif.Visible = $false
$notif.Dispose()
Remove-Item $pidFile -ErrorAction SilentlyContinue
