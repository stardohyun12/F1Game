# ask-dohyun.ps1 — 도현이 확인·입력해야 할 때만 PD(팀장)가 부르는 소리 알림
# 2026-10-20 도현: "확인해야 하거나 입력해야 할 게 있을 때 소리로 알림을 주면 좋겠음"
# 팀원 턴마다 울리던 Stop 훅 알림(flash.ps1)은 꺼 둔 상태 — 이건 PD 가 명시적으로만 호출한다.
# 사용: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ask-dohyun.ps1 -Message "축척 고르기"
param([string]$Message = "확인할 것이 있어요")

# 0) 팀장 캐릭터(킬가르도)가 있으면 그쪽으로 — 울음소리 + 말풍선, 클릭하면 이 팀 화면이 열린다.
#    캐릭터: D:\Github\GameStudio\tools\aegis-pet (2026-10-19 도현). 못 보내면 아래 예전 알림으로.
$pet = 'D:\Github\GameStudio\tools\aegis-pet\ask.ps1'
if (Test-Path $pet) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $pet -Message $Message -Workspace (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    if ($LASTEXITCODE -eq 0) { exit 0 }
}

$notifyScript = Join-Path $PSScriptRoot '..\.claude\hooks\notify-persistent.ps1'
$sound = 'C:\Windows\Media\Windows Notify System Generic.wav'

# 1) 소리 — 바로, 한 번
try {
    if (Test-Path $sound) { (New-Object System.Media.SoundPlayer $sound).PlaySync() }
    else { [System.Media.SystemSounds]::Exclamation.Play() }
} catch { [console]::beep(880, 300) }

# 2) 트레이 풍선 — 분리 실행(이 스크립트는 바로 끝남). 클릭하거나 15분 뒤 사라짐.
if (Test-Path $notifyScript) {
    $env:PITCREW_NOTIFY_TEXT = $Message
    Start-Process powershell.exe -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-WindowStyle","Hidden","-File","`"$notifyScript`"" -WindowStyle Hidden | Out-Null
}
