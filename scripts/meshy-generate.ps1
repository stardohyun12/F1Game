# Meshy text-to-3D 생성 → refine → remesh(저폴리) → fbx 다운로드 (화물 원물 재사용).
# 사용: ./scripts/meshy-generate.ps1 -Prompt "a carton of eggs, realistic" -Name eggs_carton_01 -TargetPolycount 2000
param(
  [Parameter(Mandatory=$true)][string]$Prompt,
  [Parameter(Mandatory=$true)][string]$Name,
  [int]$TargetPolycount = 2000,
  [string]$OutDir = "F1/Assets/Model/AI"
)
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# .env.local 키 (PS5.1은 JSON 바디를 UTF-8 바이트로 보내야 — 안 그럼 서버 EOF)
$key = ((Get-Content .env.local | Where-Object { $_ -match '^MESHY_API_KEY=' }) -replace '^MESHY_API_KEY=','').Trim()
if (-not $key) { throw "MESHY_API_KEY .env.local 에 없음" }
$headers = @{ Authorization = "Bearer $key" }

function Post-Json($uri, $obj) {
  $bytes = [System.Text.Encoding]::UTF8.GetBytes(($obj | ConvertTo-Json))
  return Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType "application/json" -Body $bytes
}
function Wait-Task($uri, $id) {
  while ($true) {
    $t = Invoke-RestMethod -Uri "$uri/$id" -Headers $headers
    Write-Host "  $id $($t.status) $($t.progress)"
    if ($t.status -eq "SUCCEEDED") { return $t }
    if ($t.status -eq "FAILED")    { throw "task $id FAILED: $($t.task_error.message)" }
    Start-Sleep -Seconds 8
  }
}

$t2d = "https://api.meshy.ai/openapi/v2/text-to-3d"

# 1) preview
$prev = Post-Json $t2d @{ mode="preview"; prompt=$Prompt; art_style="realistic"; should_remesh=$true }
Write-Host "preview=$($prev.result)"
$null = Wait-Task $t2d $prev.result

# 2) refine (UV+텍스처 정식) — 일시실패 1회 재시도
$refineDone = $null
for ($i=1; $i -le 2 -and -not $refineDone; $i++) {
  $ref = Post-Json $t2d @{ mode="refine"; preview_task_id=$prev.result }
  Write-Host "refine attempt $i = $($ref.result)"
  try { $refineDone = Wait-Task $t2d $ref.result } catch { Write-Host "  refine 실패: $_" }
}
if (-not $refineDone) { throw "refine 2회 실패" }
$refineId = $refineDone.task_id; if (-not $refineId) { $refineId = $ref.result }

# 3) remesh (저폴리 — Kenney 결)
$rm = "https://api.meshy.ai/openapi/v1/remesh"
$remesh = Post-Json $rm @{ input_task_id=$refineId; target_formats=@("fbx"); target_polycount=$TargetPolycount; decimation_mode=2 }
Write-Host "remesh=$($remesh.result)"
$done = Wait-Task $rm $remesh.result

# 4) fbx 다운로드
$url = $done.model_urls.fbx
if (-not $url) { throw "fbx URL 없음" }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Force $OutDir | Out-Null }
$dest = Join-Path $OutDir "$Name.fbx"
Invoke-WebRequest -Uri $url -OutFile $dest
Write-Host "SAVED $dest"
