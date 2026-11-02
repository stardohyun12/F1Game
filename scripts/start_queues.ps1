# 창 없이(conhost --headless — 기본 터미널이 Windows Terminal 이라 창이 뜨던 것 방지) 대기열 3줄을 Hermes 작업(job)과 무관한 독립 프로세스로 띄움 — WMI Create(부모 = WmiPrvSE). 게이트웨이 재시작·에이전트 종료에도 안 죽음.
$bash = "C:\Users\rtx40\AppData\Local\hermes\tools\git-2.53.0+3-win32-x64\bin\bash.exe"
foreach ($l in "editor","studio","feature") {
  $running = Get-CimInstance Win32_Process -Filter "Name='bash.exe'" | ? { $_.CommandLine -match "bash scripts/team_queue.sh $l" -and $_.CommandLine -notmatch 'Get-CimInstance' }
  if ($running) { "$l already running"; continue }
  $cmd = "conhost.exe --headless `"$bash`" -lc `"export PYTHONUTF8=1 PYTHONIOENCODING=utf-8; cd /d/Github/F1Game && bash scripts/team_queue.sh $l >> .ai_logs/team/queue-$l.out 2>&1`""
  $r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = $cmd; CurrentDirectory = "D:\Github\F1Game" }
  "$l started PID $($r.ProcessId) (outside agent job)"
}
# Codex 아트 줄(D-259)
$ar = Get-CimInstance Win32_Process -Filter "Name='bash.exe'" | ? { $_.CommandLine -match 'art_queue.sh' -and $_.CommandLine -notmatch 'Get-CimInstance' }
if (-not $ar) {
  $r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = "conhost.exe --headless `"$bash`" -lc `"export PYTHONUTF8=1 PYTHONIOENCODING=utf-8; cd /d/Github/F1Game && bash scripts/art_queue.sh >> .ai_logs/team/queue-art.out 2>&1`""; CurrentDirectory = "D:\Github\F1Game" }
  "art started PID $($r.ProcessId)"
}
# 텔레그램 고정 상태 메시지(1분마다 갱신, D-254)
$py = "C:\Users\rtx40\AppData\Local\Programs\Python\Python313\python.exe"
$sp = Get-Content .claude\team\tg_status.pid -ErrorAction SilentlyContinue
if (-not ($sp -and (Get-Process -Id $sp -ErrorAction SilentlyContinue))) {
  $r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = "conhost.exe --headless `"$py`" -X utf8 D:\Github\F1Game\scripts\tg_status.py"; CurrentDirectory = "D:\Github\F1Game" }
  "status started PID $($r.ProcessId)"
}
