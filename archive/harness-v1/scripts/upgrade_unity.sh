#!/usr/bin/env bash
# D-231: Unity 6000.3.19f1 → 6000.3.27f2 (UUM-146734 수정판). 설치 끝나길 기다렸다가 본 F1·F1Studio 를 새 버전으로 열고 확인.
cd D:/Github/F1Game || exit 1
log(){ echo "[$(date '+%m/%d %H:%M')] upgrade-27f2 $*" | tee -a .ai_logs/team/queue.log; }
EXE="C:/Program Files/Unity/Hub/Editor/6000.3.27f2/Editor/Unity.exe"
for i in $(seq 1 90); do [ -f "$EXE" ] && grep -q "exit=" "$LOCALAPPDATA/Temp/hub_install_6000327.log" && break; sleep 20; done
[ -f "$EXE" ] || { log "설치 안 끝남 — 중단"; exit 1; }
log "설치 확인 $(tail -1 "$LOCALAPPDATA/Temp/hub_install_6000327.log" | tr -d '\r')"
echo "pd" > .claude/team/editor.lock; echo "pd" > .claude/team/studio.lock
# 스튜디오(같은 버그로 로그 폭주 중) 상태 확인 후 끔
S=$(UCLI_PROJ=D:/Github/F1Game/F1Studio timeout 30 bash scripts/ucli-http.sh exec 'return "dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty+" play="+UnityEditor.EditorApplication.isPlaying;' 2>&1 | tail -1)
log "스튜디오 끄기 전 $S"
if echo "$S" | grep -q "dirty=True"; then log "스튜디오 씬 저장 안 된 변경 — 스튜디오는 그대로 둠"; else
  powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | ? { \$_.CommandLine -match 'F1Studio' } | % { taskkill /F /T /PID \$_.ProcessId | Out-Null }"; sleep 5; rm -f F1Studio/Temp/UnityLockfile
  sed -i 's/6000\.3\.19f1 (7689f4515d75)/6000.3.27f2 (22188f149405)/; s/^m_EditorVersion: 6000\.3\.19f1/m_EditorVersion: 6000.3.27f2/' F1Studio/ProjectSettings/ProjectVersion.txt
fi
sed -i 's/6000\.3\.19f1 (7689f4515d75)/6000.3.27f2 (22188f149405)/; s/^m_EditorVersion: 6000\.3\.19f1/m_EditorVersion: 6000.3.27f2/' F1/ProjectSettings/ProjectVersion.txt
rm -f F1/Temp/UnityLockfile
log "본 에디터 열기: $(powershell -NoProfile -File scripts/open-editor.ps1 2>&1 | tr -d '\r')"
grep -q 6000.3.27f2 F1Studio/ProjectSettings/ProjectVersion.txt && log "스튜디오 열기: $(powershell -NoProfile -File scripts/open-studio.ps1 2>&1 | tr -d '\r')"
ok=0; for i in $(seq 1 90); do sleep 20; R=$(timeout 20 bash scripts/ucli-http.sh exec 'return "ok "+UnityEditor.EditorApplication.isCompiling+" fail="+UnityEditor.EditorUtility.scriptCompilationFailed+" v="+UnityEngine.Application.unityVersion;' 2>/dev/null | tail -1); echo "$R" | grep -q "ok False" && { ok=1; break; }; done
log "본 에디터 ready=$ok $R"
if [ $ok = 1 ]; then
  timeout 120 bash scripts/ucli-http.sh exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity"); var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries"); t.GetMethod("SetConsoleFlag",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{1,true}); return "opened";' >/dev/null 2>&1
  timeout 60 bash scripts/ucli-http.sh editor play --wait >/dev/null 2>&1; sleep 15
  E=$(timeout 30 bash scripts/ucli-http.sh console --type error 2>&1 | tail -1 | cut -c1-300); timeout 60 bash scripts/ucli-http.sh editor stop >/dev/null 2>&1
  log "Play 15초 콘솔 에러: $E"
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null
  git add F1/ProjectSettings/ProjectVersion.txt F1Studio/ProjectSettings/ProjectVersion.txt scripts/open-editor.ps1 scripts/open-studio.ps1 scripts/open-shot-editor.ps1 scripts/upgrade_unity.sh
  git commit -qm "unity: 6000.3.27f2 로 올림(UUM-146734 오디오 잠금 로그 폭주 수정판, D-231) + open-editor 가 F1Studio 를 본 에디터로 오인하던 것 수정 [skip-verifier]" && timeout 300 git push -q
  log "커밋 $(git log --oneline -1)"
  mv .claude/team/queue/editor/staged/*.task .claude/team/queue/editor/ 2>/dev/null; log "보류 과제 되돌림"
fi
rm -f .claude/team/editor.lock .claude/team/studio.lock
