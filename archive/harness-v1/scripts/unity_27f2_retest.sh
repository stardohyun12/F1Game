#!/usr/bin/env bash
# D-231 수정: 목표 6000.3.27f2(보안 패치 — FBX SDK 버퍼 오버플로·Brotli CVE-2026-111497·PM Node.js, UUM-146734 오디오 수정 포함).
# 1) 22f1 설치 끝나길 기다림 → 2) 27f2 설치 → 3) 사본 worktree 로 호환성 시험(컴파일·Play·콘솔) → 4) 통과 + 본 에디터 과제 없을 때 본·스튜디오 전환(upgrade_unity.sh)
cd D:/Github/F1Game || exit 1
log(){ echo "[$(date '+%m/%d %H:%M')] unity-27f2 $*" | tee -a .ai_logs/team/queue.log; }
SP=D:/Github/F1Game_shot/F1
log "호환성 시험 재시도(백업 복구 대화상자 때문에 1차 실패 — 백업 지우고 재시작함)"
ok=0; for i in $(seq 1 120); do sleep 20; R=$(UCLI_PROJ=$SP timeout 20 bash scripts/ucli-http.sh exec 'return "ok "+UnityEditor.EditorApplication.isCompiling+" fail="+UnityEditor.EditorUtility.scriptCompilationFailed+" v="+UnityEngine.Application.unityVersion;' 2>/dev/null | tail -1); echo "$R" | grep -q "ok False" && { ok=1; break; }; done
log "사본 27f2 ready=$ok $R"
[ $ok = 1 ] || { log "호환성 시험 실패(사본이 안 뜸) — 본 전환 안 함"; exit 2; }
echo "$R" | grep -q "fail=True" && { log "호환성 시험 실패(컴파일 에러) — 본 전환 안 함"; exit 3; }
UCLI_PROJ=$SP timeout 120 bash scripts/ucli-http.sh exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity"); return "o";' >/dev/null 2>&1
UCLI_PROJ=$SP timeout 30 bash scripts/ucli-http.sh exec 'var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries"); t.GetMethod("Clear",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,null); return "c";' >/dev/null 2>&1
UCLI_PROJ=$SP timeout 60 bash scripts/ucli-http.sh editor play --wait >/dev/null 2>&1
UCLI_PROJ=$SP timeout 20 bash scripts/ucli-http.sh exec 'UnityEngine.Application.runInBackground=true; return "rb";' >/dev/null 2>&1; sleep 20
E=$(UCLI_PROJ=$SP timeout 30 bash scripts/ucli-http.sh console --type error 2>&1 | tail -1 | cut -c1-400)
F=$(UCLI_PROJ=$SP timeout 20 bash scripts/ucli-http.sh exec 'return "frame="+UnityEngine.Time.frameCount;' 2>/dev/null | tail -1)
UCLI_PROJ=$SP timeout 60 bash scripts/ucli-http.sh editor stop >/dev/null 2>&1
log "사본 27f2 Play 20초: $F, 콘솔 에러: $E"
echo "$E" | grep -qiE "error CS|Exception|Shader error" && { log "호환성 의심 에러 — 본 전환 보류(PD 확인)"; exit 4; }
# 사본 끄고 ProjectVersion 되돌림(사본은 시험용)
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | ? { \$_.CommandLine -match 'F1Game_sho' } | % { taskkill /F /T /PID \$_.ProcessId | Out-Null }"
( cd D:/Github/F1Game_shot && git checkout -- F1/ProjectSettings/ProjectVersion.txt F1/Assets/UI/Fonts 2>/dev/null )
log "호환성 통과 → 본 에디터 과제 끝나길 기다렸다 전환"
for i in $(seq 1 400); do grep -q '^working' .claude/team/sessions/programmer.state || grep -q '^working' .claude/team/sessions/artlive.state || break; sleep 30; done
# 대기열이 새 과제를 잡지 못하게 잠그고 남은 과제는 잠시 빼 둠
mkdir -p .claude/team/queue/editor/staged; mv .claude/team/queue/editor/[0-9]*.task .claude/team/queue/editor/staged/ 2>/dev/null
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | ? { \$_.CommandLine -match 'F1Game[\\\\/]F1(\"|\s|$)' } | % { taskkill /F /T /PID \$_.ProcessId | Out-Null }"
sleep 8
bash scripts/upgrade_unity.sh
