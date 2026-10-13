param([string]$Code, [string]$CodeFile, [string]$Command='exec')
$ErrorActionPreference='Stop'
$instance = Get-ChildItem "$env:USERPROFILE/.unity-cli/instances" -Filter '*.json' | ForEach-Object {Get-Content -LiteralPath $_.FullName | ConvertFrom-Json} | Where-Object {$_.projectPath.Replace('\','/').TrimEnd('/') -eq 'D:/Github/F1Game/F1' -and $_.state -ne 'stopped'} | Select-Object -First 1
if (!$instance) {throw 'No live F1 Unity connector registered'}
if ($CodeFile) {$Code=Get-Content -Raw -LiteralPath $CodeFile}
$params=if ($Command -eq 'exec') {@{code=$Code}} else {@{}}
$body=@{command=$Command;params=$params}|ConvertTo-Json -Depth 6
$result=Invoke-RestMethod -Uri "http://127.0.0.1:$($instance.port)/command" -Method Post -Body $body -ContentType 'application/json; charset=utf-8' -TimeoutSec 180
$result|ConvertTo-Json -Depth 8
if (!$result.success) {exit 1}
