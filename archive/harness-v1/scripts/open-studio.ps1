# open-studio.ps1 — F1Studio(건물 스튜디오 전용) Unity 에디터를 에이전트 세션 밖(WMI, Job 밖)에서 띄운다. 메인 F1 에디터와 동시에 떠도 된다.
# 사용: powershell -NoProfile -File scripts/open-studio.ps1   (이미 떠 있으면 아무것도 안 한다)
# 처음 열 때는 패키지 받기·임포트로 몇 분 걸린다. 준비 확인: UCLI_PROJ=D:/Github/F1Game/F1Studio bash scripts/ucli-http.sh status
$exe  = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe'
$proj = 'D:\Github\F1Game\F1Studio'
if (-not (Test-Path "$proj\Assets\Art\Library")) { "F1Studio 정션 없음 — 먼저 bash scripts/studio-link.sh"; exit 1 }
$running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -match 'F1Studio' }
if ($running) { "already running: PID $($running.ProcessId -join ',')"; exit 0 }
$r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = "`"$exe`" -projectPath `"$proj`"" }
if ($r.ReturnValue -ne 0) { "WMI create failed: $($r.ReturnValue)"; exit 1 }
"started PID $($r.ProcessId) (outside agent job)"
