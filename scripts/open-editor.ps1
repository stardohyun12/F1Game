# open-editor.ps1 — F1 Unity 에디터를 에이전트 세션 밖에서 띄운다.
# 왜: 에이전트 셸(Start-Process)로 띄운 에디터는 그 세션의 Windows Job 안에 들어가,
#     세션 재시작·기억 비우기 때 같이 꺼진다(ddakji 2026-10-20 실측). WMI 로 만들면 Job 밖에서 산다.
# 사용: powershell -NoProfile -File scripts/open-editor.ps1   (이미 떠 있으면 아무것도 안 한다)
$exe  = 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe'
$proj = 'D:\Github\F1Game\F1'
$running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -match 'projectPath' -and $_.CommandLine -match 'F1Game' }
if ($running) { "already running: PID $($running.ProcessId -join ',')"; exit 0 }
$r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = "`"$exe`" -projectPath `"$proj`"" }
if ($r.ReturnValue -ne 0) { "WMI create failed: $($r.ReturnValue)"; exit 1 }
"started PID $($r.ProcessId) (outside agent job)"
