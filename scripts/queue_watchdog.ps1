# 10분마다(작업 스케줄러): 대기열이 죽었으면 다시 띄우고, 팀원이 'working' 인데 로그가 20분 넘게 멈추면 도현 스레드에 알림.
Set-Location D:\Github\F1Game
$out = & powershell -NoProfile -ExecutionPolicy Bypass -File scripts\start_queues.ps1
$py = "C:SERSTX40APPDATAocalprogramspythonpython313python.exe"
$restarted = $out | ? { $_ -match 'started' }
if ($restarted) {
  Add-Content .ai_logs\team\queue.log ("[{0}] watchdog: 대기열 재기동 {1}" -f (Get-Date -Format 'MM/dd HH:mm'), ($restarted -join ', '))
  if ($py) { & $py -X utf8 scripts\tg_notify.py ("🔁 대기열이 죽어 있어서 다시 띄움: " + ($restarted -join ', ')) | Out-Null }
}
$st = Get-Content .claude\team\sessions\programmer.state -ErrorAction SilentlyContinue
$log = Get-Item .ai_logs\team\programmer.live.log -ErrorAction SilentlyContinue
if ($st -match '^working' -and $log -and ((Get-Date) - $log.LastWriteTime).TotalMinutes -gt 20) {
  $flag = ".ai_logs\team\stall.flag"
  if (-not (Test-Path $flag) -or (Get-Item $flag).LastWriteTime -lt $log.LastWriteTime) {
    Set-Content $flag "x"
    if ($py) { & $py -X utf8 scripts\tg_notify.py ("⚠️ 팀원(programmer) 로그가 " + [int]((Get-Date) - $log.LastWriteTime).TotalMinutes + "분째 멈춤 — PD 확인 필요") | Out-Null }
  }
}
