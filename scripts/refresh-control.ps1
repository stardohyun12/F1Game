# docs/CONTROL.md 의 자동 섹션 갱신 — 날짜/D-day(status) + 최근 git(activity).
# 사용: ./scripts/refresh-control.ps1   (리포 루트에서)
#
# 인코딩 주의:
#  - PS5.1은 .ps1 파일·텍스트파일을 ANSI로 읽어 한글을 깨뜨림.
#  - 그래서 이 스크립트는 한글 리터럴을 출력에 넣지 않는다 (제목은 CONTROL.md에 정적).
#    생성 콘텐츠 = ASCII(날짜·D-day) + git 출력(커밋, OutputEncoding=UTF8로 보정)뿐.
#  - CONTROL.md 입출력은 .NET UTF-8(BOM 없음)로.
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$path = (Resolve-Path "docs/CONTROL.md").Path

$today = [datetime]::Today
$dday  = [int](([datetime]"2026-06-24") - $today).TotalDays
$statusStr = $today.ToString("yyyy-MM-dd") + " | D-" + $dday

$commits = git log --oneline -5 --no-decorate
$activityLines = ($commits | ForEach-Object {
  "- ``" + $_.Substring(0,7) + "`` " + $_.Substring(8)
}) -join "`n"

$utf8 = New-Object System.Text.UTF8Encoding $false
$text = [System.IO.File]::ReadAllText($path, $utf8)

$statusBlock   = "<!-- AUTO:status -->" + $statusStr + "<!-- /AUTO:status -->"
$activityBlock = "<!-- AUTO:activity -->`n" + $activityLines + "`n<!-- /AUTO:activity -->"

$text = [regex]::Replace($text, '(?s)<!-- AUTO:status -->.*?<!-- /AUTO:status -->',     { param($m) $statusBlock })
$text = [regex]::Replace($text, '(?s)<!-- AUTO:activity -->.*?<!-- /AUTO:activity -->', { param($m) $activityBlock })

[System.IO.File]::WriteAllText($path, $text, $utf8)
Write-Host ("CONTROL.md updated: " + $statusStr)
