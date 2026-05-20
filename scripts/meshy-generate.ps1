# Meshy text-to-3D 생성 → fbx 다운로드 (화물 원물 4종 재사용).
# 사용: ./scripts/meshy-generate.ps1 -Prompt "a single glass wine bottle, corked, realistic" -Name wine_bottle_01
param(
  [Parameter(Mandatory=$true)][string]$Prompt,
  [Parameter(Mandatory=$true)][string]$Name,
  [string]$OutDir = "F1/Assets/Model/AI"
)
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# JSON 바디를 UTF-8 바이트로 POST (PS 5.1은 -Body 문자열을 잘못 인코딩 → 서버가 EOF 반환)
function Post-Json($uri, $headers, $obj) {
  $bytes = [System.Text.Encoding]::UTF8.GetBytes(($obj | ConvertTo-Json))
  return Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType "application/json" -Body $bytes
}

# .env.local 에서 키 로드
$keyLine = Get-Content .env.local | Where-Object { $_ -match '^MESHY_API_KEY=' }
$key = $keyLine -replace '^MESHY_API_KEY=',''
if (-not $key) { throw "MESHY_API_KEY .env.local 에 없음" }
$headers = @{ Authorization = "Bearer $key" }

function Wait-Task($id) {
  while ($true) {
    $t = Invoke-RestMethod -Uri "https://api.meshy.ai/openapi/v2/text-to-3d/$id" -Headers $headers
    Write-Host "  status=$($t.status) progress=$($t.progress)"
    if ($t.status -eq "SUCCEEDED") { return $t }
    if ($t.status -eq "FAILED")    { throw "task $id FAILED: $($t.task_error.message)" }
    Start-Sleep -Seconds 8
  }
}

# 1) preview 태스크
$prev = Post-Json "https://api.meshy.ai/openapi/v2/text-to-3d" $headers @{ mode="preview"; prompt=$Prompt; art_style="realistic"; should_remesh=$true }
$previewId = $prev.result
Write-Host "preview task: $previewId"
$null = Wait-Task $previewId

# 2) refine (텍스처+UV 정식)
$ref = Post-Json "https://api.meshy.ai/openapi/v2/text-to-3d" $headers @{ mode="refine"; preview_task_id=$previewId }
$refineId = $ref.result
Write-Host "refine task: $refineId"
$done = Wait-Task $refineId

# 3) fbx 다운로드
$url = $done.model_urls.fbx
if (-not $url) { throw "fbx URL 없음 (model_urls: $($done.model_urls | ConvertTo-Json))" }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Force $OutDir | Out-Null }
$dest = Join-Path $OutDir "$Name.fbx"
Invoke-WebRequest -Uri $url -OutFile $dest
Write-Host "saved: $dest"
