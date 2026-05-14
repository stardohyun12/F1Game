# generate-obsidian-notes.ps1
#
# F1/Assets/Scripts/**/*.cs → 옵시디언/code/**/*.md 스캐폴드 생성.
# 멱등(idempotent): 기존 노트는 frontmatter `last_synced`만 갱신, 본문 보존.
#
# 사용: pwsh ./scripts/generate-obsidian-notes.ps1
#       또는 powershell.exe ./scripts/generate-obsidian-notes.ps1

param(
    [string]$ScriptRoot = "F1/Assets/Scripts",
    [string]$OutRoot    = "옵시디언/code"
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path .).Path
$scriptDir   = Join-Path $projectRoot $ScriptRoot
$outDir      = Join-Path $projectRoot $OutRoot
$utf8NoBom   = New-Object System.Text.UTF8Encoding($false)
$today       = (Get-Date -Format "yyyy-MM-dd")

if (!(Test-Path $scriptDir)) {
    Write-Error "Script root not found: $scriptDir"
    exit 1
}
if (!(Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

function Get-Category([string]$relPath) {
    $parts = $relPath -split "[\\/]"
    if ($parts.Length -eq 1) { return "root" }
    return $parts[0].ToLower()
}

function Get-FirstSummary([string]$content) {
    if ($content -match '(?s)<summary>\s*([^<]+?)\s*</summary>') {
        $s = ($matches[1] -replace "[\r\n]+", " " -replace "\s+", " ").Trim()
        if ($s.Length -gt 200) { $s = $s.Substring(0, 200) + "..." }
        return $s
    }
    return ""
}

function Get-Signature([string]$content) {
    if ($content -match '(?m)^\s*(?:public|internal)?\s*(?:static\s+|abstract\s+|sealed\s+|partial\s+)*\s*(class|interface|struct|enum)\s+(\w+)') {
        return "$($matches[1]) $($matches[2])"
    }
    return ""
}

$created = 0
$updated = 0
$files = Get-ChildItem -Path $scriptDir -Filter *.cs -Recurse -File
foreach ($f in $files) {
    $rel = $f.FullName.Substring($scriptDir.Length).TrimStart('\', '/')
    $relForward = $rel -replace '\\', '/'
    $relMd = [IO.Path]::ChangeExtension($rel, ".md")
    $outPath = Join-Path $outDir $relMd
    $outDirOnly = Split-Path $outPath -Parent
    if (!(Test-Path $outDirOnly)) {
        New-Item -ItemType Directory -Path $outDirOnly -Force | Out-Null
    }

    if (Test-Path $outPath) {
        $existing = [System.IO.File]::ReadAllText($outPath)
        $patched = [regex]::Replace($existing, 'last_synced:\s*\S+', "last_synced: $today")
        [System.IO.File]::WriteAllText($outPath, $patched, $utf8NoBom)
        $updated++
        continue
    }

    $content = [System.IO.File]::ReadAllText($f.FullName)
    $linecount = ($content -split "`n").Count
    $category = Get-Category $rel
    $summary = Get-FirstSummary $content
    $sig = Get-Signature $content
    $stem = [IO.Path]::GetFileNameWithoutExtension($f.Name)

    $body = @"
---
script: $ScriptRoot/$relForward
category: $category
phase_applied: []
tags: [code/$category]
linecount: $linecount
signature: $sig
last_synced: $today
---

# $stem

> 한 줄: $summary

## 책임 (Single Responsibility)
- (채우기)

## 의존 (들어오는 — 누가 나를 부르는가)
- (채우기)

## 의존 (나가는 — 내가 부르는 것)
- (채우기)

## 관련 결정
- (채우기)

## 관련 학습
- (채우기)

## 핫스팟 (변경 가능성 높음)
- (채우기)
"@

    [System.IO.File]::WriteAllText($outPath, $body, $utf8NoBom)
    $created++
}

Write-Output "[generate-obsidian-notes] Created: $created  Updated: $updated  Out: $outDir"
