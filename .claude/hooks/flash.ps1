# CMD 창 작업표시줄 아이콘 깜빡임 (FlashWindow Win32 API)
# Stop hook + Notification hook 양쪽에서 호출. 창이 foreground면 깜빡 안 함.

Add-Type -Name Win32 -Namespace Native -MemberDefinition @'
[DllImport("user32.dll")]
public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
[DllImport("kernel32.dll")]
public static extern IntPtr GetConsoleWindow();
[DllImport("user32.dll")]
public static extern IntPtr GetForegroundWindow();
'@ -ErrorAction SilentlyContinue

$cmdHwnd = [Native.Win32]::GetConsoleWindow()
$fgHwnd  = [Native.Win32]::GetForegroundWindow()

# 이미 CMD가 foreground면 깜빡 의미 없음
if ($cmdHwnd -ne [IntPtr]::Zero -and $cmdHwnd -ne $fgHwnd) {
    # 3회 toggle = 작업표시줄에서 주황색으로 1~2초 지속
    for ($i = 0; $i -lt 3; $i++) {
        [Native.Win32]::FlashWindow($cmdHwnd, $true) | Out-Null
        Start-Sleep -Milliseconds 200
    }
}
exit 0
