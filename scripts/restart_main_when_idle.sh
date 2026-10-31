#!/usr/bin/env bash
# Unity 6000.3.19f1 오디오 버그(UUM-146734, "Access version should be odd when acquiring lock" 무한 반복 → 메모리 부족 크래시) 임시 대처:
# 본 에디터 과제가 끝나면 다음 과제 전에 본 에디터를 재시작(재시작하면 멈춤). 보류한 과제는 다시 대기열로.
cd D:/Github/F1Game || exit 1
log(){ echo "[$(date '+%m/%d %H:%M')] restart-main $*" | tee -a .ai_logs/team/queue.log; }
while grep -q '^working' .claude/team/sessions/programmer.state; do sleep 30; done
echo "pd" > .claude/team/editor.lock
D=$(timeout 30 bash scripts/ucli-http.sh exec 'return "dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty+" play="+UnityEditor.EditorApplication.isPlaying;' 2>&1 | tail -1)
log "재시작 전 $D"
if echo "$D" | grep -q "dirty=True"; then log "씬 저장 안 된 변경 — 재시작 보류, PD 확인 필요"; rm -f .claude/team/editor.lock; exit 2; fi
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | ? { \$_.CommandLine -match 'F1Game[\\\\/]F1' -and \$_.CommandLine -notmatch 'F1Game_sho|F1Studio' } | % { taskkill /F /T /PID \$_.ProcessId | Out-Null }"
sleep 8; rm -f F1/Temp/UnityLockfile
powershell -NoProfile -File scripts/open-editor.ps1 >/dev/null 2>&1
ok=0; for i in $(seq 1 60); do sleep 20; timeout 20 bash scripts/ucli-http.sh exec 'return "ok "+UnityEditor.EditorApplication.isCompiling;' 2>/dev/null | grep -q "ok False" && { ok=1; break; }; done
log "재시작 ready=$ok"
timeout 60 bash scripts/ucli-http.sh exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity"); var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries"); t.GetMethod("SetConsoleFlag",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{1,true}); return "opened";' >/dev/null 2>&1
git checkout -- F1/Assets/UI/Fonts 2>/dev/null
rm -f .claude/team/editor.lock
mv .claude/team/queue/editor/staged/*.task .claude/team/queue/editor/ 2>/dev/null
log "보류 과제 되돌림"
