# Claude alarm: taskbar flash + tray balloon (PS 5.1 ASCII-only to avoid BOM issues)
$logPath = Join-Path $PSScriptRoot 'flash.log'
$ts = Get-Date -Format 'HH:mm:ss'
Add-Content -Path $logPath -Value "[$ts] called" -ErrorAction SilentlyContinue

# 1) Flash taskbar (only when CMD is in background)
$sig = '[DllImport("user32.dll")] public static extern bool FlashWindow(IntPtr hwnd, bool bInvert); [DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow(); [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();'
try { $api = Add-Type -MemberDefinition $sig -Name 'FW' -Namespace 'PitCrew' -PassThru -ErrorAction Stop } catch { $api = [PitCrew.FW] }
$cmd = $api::GetConsoleWindow()
$fg  = $api::GetForegroundWindow()
$isBg = ($cmd -ne [IntPtr]::Zero -and $cmd -ne $fg)
Add-Content -Path $logPath -Value "  cmd=$cmd fg=$fg isBg=$isBg" -ErrorAction SilentlyContinue
if ($isBg) {
    for ($i = 0; $i -lt 8; $i++) { $api::FlashWindow($cmd, $true) | Out-Null; Start-Sleep -Milliseconds 250 }
}

# 2) Tray balloon notification (always visible regardless of focus)
try {
    Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
    $notif = New-Object System.Windows.Forms.NotifyIcon
    $notif.Icon = [System.Drawing.SystemIcons]::Information
    $notif.BalloonTipTitle = 'Claude Code'
    $notif.BalloonTipText = 'Waiting for your input or confirmation'
    $notif.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info
    $notif.Visible = $true
    $notif.ShowBalloonTip(4000)
    Start-Sleep -Milliseconds 4500
    $notif.Visible = $false
    $notif.Dispose()
    Add-Content -Path $logPath -Value "  balloon: OK" -ErrorAction SilentlyContinue
} catch {
    Add-Content -Path $logPath -Value ("  balloon: FAIL " + $_.Exception.Message) -ErrorAction SilentlyContinue
}

exit 0
