# 진행 보기 창 (D-289): 팀원이 지금 뭘 하는지 한 줄씩 실시간. 닫아도 작업엔 영향 없음.
$Host.UI.RawUI.WindowTitle = "PitCrew 진행 보기"
Set-Location D:\Github\F1Game
Write-Host "== PitCrew 진행 보기 — 팀원 행동이 한 줄씩 올라옵니다. 닫아도 됩니다. ==" -ForegroundColor Cyan
Get-Content .ai_logs\team\build.live.log -Tail 20 -Wait -Encoding UTF8
