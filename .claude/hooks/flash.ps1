# CMD 작업표시줄 깜빡. Stop / Notification / PreToolUse(AskUserQuestion).
$signature = '[DllImport("user32.dll")] public static extern bool FlashWindow(IntPtr hwnd, bool bInvert); [DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow(); [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();'
try {
    $api = Add-Type -MemberDefinition $signature -Name 'FW' -Namespace 'PitCrew' -PassThru -ErrorAction Stop
} catch {
    $api = [PitCrew.FW]
}
$cmd = $api::GetConsoleWindow()
$fg  = $api::GetForegroundWindow()
if ($cmd -ne [IntPtr]::Zero -and $cmd -ne $fg) {
    # 8회 toggle = 약 4초 동안 작업표시줄 주황색 강조
    for ($i = 0; $i -lt 8; $i++) {
        $api::FlashWindow($cmd, $true) | Out-Null
        Start-Sleep -Milliseconds 250
    }
}
exit 0
