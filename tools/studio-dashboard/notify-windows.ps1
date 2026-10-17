# PitCrew Windows notification: tray balloon (same mechanism as .claude/hooks/notify-persistent.ps1).
# Clicking opens the conversation in the browser. Exits after 2 minutes.
param([string]$Title = 'PitCrew', [string]$Body = '', [string]$Url = 'http://localhost:7878/')
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
try { [System.Media.SystemSounds]::Asterisk.Play() } catch {}
$icon = New-Object System.Windows.Forms.NotifyIcon
$icon.Icon = [System.Drawing.SystemIcons]::Information
$icon.Text = 'PitCrew - 클릭하면 대화를 엽니다'
$icon.BalloonTipTitle = $Title
$icon.BalloonTipText = if ($Body) { $Body } else { ' ' }
$icon.Visible = $true
$script:done = $false
$open = { Start-Process $Url; $script:done = $true }
$icon.add_BalloonTipClicked($open); $icon.add_Click($open)
$icon.add_BalloonTipClosed({ $script:done = $true })
$icon.ShowBalloonTip(15000)
$started = Get-Date
while (-not $script:done -and ((Get-Date) - $started).TotalSeconds -lt 120) {
    [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 200
}
$icon.Visible = $false; $icon.Dispose()
