# CMD 창 작업표시줄 아이콘 깜빡임 — Stop hook + Notification hook
$signature = '[DllImport("user32.dll")] public static extern bool FlashWindow(IntPtr hwnd, bool bInvert); [DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow(); [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();'
try {
    $api = Add-Type -MemberDefinition $signature -Name 'FW' -Namespace 'PitCrew' -PassThru -ErrorAction Stop
} catch {
    # 이미 정의됐으면 그냥 가져옴
    $api = [PitCrew.FW]
}
$cmd = $api::GetConsoleWindow()
$fg  = $api::GetForegroundWindow()
if ($cmd -ne [IntPtr]::Zero -and $cmd -ne $fg) {
    for ($i = 0; $i -lt 3; $i++) {
        $api::FlashWindow($cmd, $true) | Out-Null
        Start-Sleep -Milliseconds 200
    }
}
exit 0
