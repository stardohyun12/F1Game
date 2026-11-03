#!/usr/bin/env bash
# D-231 수정: 목표 6000.3.27f2(보안 패치 — FBX SDK 버퍼 오버플로·Brotli CVE-2026-111497·PM Node.js, UUM-146734 오디오 수정 포함).
# 1) 22f1 설치 끝나길 기다림 → 2) 27f2 설치 → 3) 사본 worktree 로 호환성 시험(컴파일·Play·콘솔) → 4) 통과 + 본 에디터 과제 없을 때 본·스튜디오 전환(upgrade_unity.sh)
cd D:/Github/F1Game || exit 1
log(){ echo "[$(date '+%m/%d %H:%M')] unity-27f2 $*" | tee -a .ai_logs/team/queue.log; }
L22="$LOCALAPPDATA/Temp/hub_install_6000322.log"; L27="$LOCALAPPDATA/Temp/hub_install_6000327.log"
for i in $(seq 1 120); do grep -q "exit=" "$L22" 2>/dev/null && break; sleep 20; done
log "22f1 설치 끝: $(tail -1 "$L22" | tr -d '\r')"
"C:/Program Files/Unity Hub/Unity Hub.exe" -- --headless install --version 6000.3.27f2 --changeset 22188f149405 > "$L27" 2>&1; echo "exit=$?" >> "$L27"
EXE="C:/Program Files/Unity/Hub/Editor/6000.3.27f2/Editor/Unity.exe"
[ -f "$EXE" ] || { log "27f2 설치 실패: $(tail -3 "$L27" | tr -d '\r' | tr '\n' ' ')"; exit 1; }
log "27f2 설치 완료"
# 호환성 시험: 사본 worktree(커밋 안 함)
SP=D:/Github/F1Game_shot/F1
( cd D:/Github/F1Game_shot && git fetch -q origin && git checkout -q --detach origin/main && git checkout -- F1/Assets/UI/Fonts 2>/dev/null )
sed -i 's/6000\.3\.19f1 (7689f4515d75)/6000.3.27f2 (22188f149405)/; s/^m_EditorVersion: 6000\.3\.19f1/m_EditorVersion: 6000.3.27f2/' $SP/ProjectSettings/ProjectVersion.txt
rm -f $SP/Temp/UnityLockfile
log "사본 열기: $(powershell -NoProfile -File scripts/open-shot-editor.ps1 2>&1 | tr -d '\r')"
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
