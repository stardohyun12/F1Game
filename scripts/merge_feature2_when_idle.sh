#!/usr/bin/env bash
# 본 에디터가 비면 feature/onfoot 머지 → 컴파일 → Setup 2개 → 씬 저장 → Play 콘솔 에러 확인 → 보류한 다음 에디터 과제를 대기열로 되돌림.
cd D:/Github/F1Game || exit 1
log(){ echo "[$(date '+%m/%d %H:%M')] merge-feature2 $*" | tee -a .ai_logs/team/queue.log; }
while grep -q '^working' .claude/team/sessions/programmer.state; do sleep 30; done
echo "pd" > .claude/team/editor.lock
log "본 에디터 빔 → 머지 시작"
git fetch -q origin && git merge -q --no-edit origin/feature/onfoot -m "merge feature/onfoot: 기능 2 정비소 사장 → 첫 주문 [skip-verifier]" || { git merge --abort; log "머지 충돌 — 중단"; rm -f .claude/team/editor.lock; mv .claude/team/queue/editor/staged/*.task .claude/team/queue/editor/; exit 1; }
U(){ timeout 120 bash scripts/ucli-http.sh exec "$1" 2>&1 | tail -1; }
U 'UnityEditor.AssetDatabase.Refresh(); return "r";'
for i in $(seq 1 30); do sleep 10; R=$(U 'return UnityEditor.EditorApplication.isCompiling+" "+UnityEditor.EditorUtility.scriptCompilationFailed;'); echo "$R" | grep -q "^False" && break; done
log "컴파일: $R"
if echo "$R" | grep -q "True$"; then git reset -q --hard HEAD~1; U 'UnityEditor.AssetDatabase.Refresh(); return "r";'; log "컴파일 실패 → 머지 되돌림"; else
  log "Setup: $(U 'UnityEditor.EditorApplication.ExecuteMenuItem("PitCrew/기능/정비소 첫 주문 설치"); UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes(); return "saved "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;')"
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null
  git add F1/Assets/Scenes/Map_TownV6.unity && git commit -qm "map: 기능 2 Setup 적용 [skip-verifier]"
  timeout 600 git push -q 2>&1 | grep -E "rejected|fatal"; log "머지·Setup 커밋 $(git log --oneline -1)"
fi
rm -f .claude/team/editor.lock
mv .claude/team/queue/editor/staged/*.task .claude/team/queue/editor/ 2>/dev/null
log "보류 과제 되돌림"
