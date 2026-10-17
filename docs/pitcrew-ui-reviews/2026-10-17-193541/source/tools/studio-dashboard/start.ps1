param([int]$Port = 7878, [switch]$Restart)
$ErrorActionPreference = 'Stop'
$studioRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent (Split-Path -Parent $studioRoot)
$runtimeRoot = Join-Path $studioRoot '.runtime'
$pythonPath = Join-Path $studioRoot '.venv\Scripts\python.exe'
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
if (!(Test-Path -LiteralPath $pythonPath)) {
    & python -m venv (Join-Path $studioRoot '.venv')
    if ($LASTEXITCODE -ne 0) { throw 'Python 가상환경 생성 실패' }
    & $pythonPath -m pip install -r (Join-Path $studioRoot 'requirements-lock.txt')
    if ($LASTEXITCODE -ne 0) { throw '의존성 설치 실패' }
}
$listeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
foreach ($listener in $listeners) {
    $existing = Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)"
    if ($existing.CommandLine -notmatch 'studio-dashboard[/\\](server|web_host)\.py') {
        throw "포트 $Port 를 다른 프로그램이 사용하고 있습니다. 해당 프로그램을 종료하지 않았습니다."
    }
    if (!$Restart) {
        if (Test-Path -LiteralPath (Join-Path $runtimeRoot 'runtime.json')) {
            & $pythonPath -X utf8 (Join-Path $studioRoot 'runtime.py')
            if ($LASTEXITCODE -ne 0) { throw '실행 서비스 복구 실패' }
        }
        break
    }
    try {
        $snapshot = Invoke-RestMethod "http://127.0.0.1:$Port/api/hub/bootstrap" -TimeoutSec 3
        if (!$snapshot.runtime.independent -and @($snapshot.agents | Where-Object { $_.status -in @('running','waiting','stopping') }).Count) {
            throw '진행 중인 AI 작업이 있습니다. 앱에서 중단하거나 완료를 기다린 뒤 재시작하세요.'
        }
    } catch {
        throw "실행 상태를 확인하지 못했거나 작업이 진행 중입니다. 서버를 종료하지 않았습니다. $($_.Exception.Message)"
    }
    Stop-Process -Id $listener.OwningProcess
    Wait-Process -Id $listener.OwningProcess -Timeout 10 -ErrorAction SilentlyContinue
}
# Start through the Windows WMI service instead of inheriting a temporary
# terminal/tool process tree. The host handles append-only, flushed logs.
$startup = New-CimInstance -ClassName Win32_ProcessStartup -ClientOnly -Property @{ ShowWindow = [uint16]0 }
$stopFile = Join-Path $runtimeRoot "watchdog-$Port.stop"
if (Test-Path -LiteralPath $stopFile) { Remove-Item -LiteralPath $stopFile }
$commandLine = '"{0}" -X utf8 -u "{1}" --port {2}' -f $pythonPath, (Join-Path $studioRoot 'watchdog.py'), $Port
$created = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = $commandLine; CurrentDirectory = $workspaceRoot; ProcessStartupInformation = $startup }
if ($created.ReturnValue -ne 0) { throw "독립 서버 실행 실패 (WMI code $($created.ReturnValue))" }
for ($attempt=0; $attempt -lt 30; $attempt++) {
    try {
        $null = Invoke-RestMethod "http://127.0.0.1:$Port/api/hub/bootstrap" -TimeoutSec 3
        $watchState = Get-Content -LiteralPath (Join-Path $runtimeRoot "watchdog-$Port.json") -Raw | ConvertFrom-Json
        if (([DateTimeOffset]::UtcNow.ToUnixTimeSeconds() - $watchState.checkedAt) -gt 10) { throw '감시 서비스 확인 중' }
        $watchProcess = Get-CimInstance Win32_Process -Filter "ProcessId=$($watchState.pid)"
        if ($watchProcess.CommandLine -notmatch 'studio-dashboard[/\\]watchdog\.py') { throw '감시 서비스 확인 중' }
        Write-Output "PitCrew 실행됨: http://localhost:$Port/ (자동 복구 감시 활성화)"
        exit 0
    } catch { Start-Sleep -Milliseconds 300 }
}
throw '서버 시작을 확인하지 못했습니다. .runtime/server.stderr.log를 확인하세요.'
