#!/usr/bin/env bash
# 밤 자율 줄(D-221). 사용: bash scripts/night_chain.sh <editor|studio|feature>
# 각 줄은 앞 작업(같은 역할)이 끝나길 기다렸다가 과제를 차례로 돌리고, 하나 끝날 때마다 커밋·푸시(작업 유실 방지).
cd D:/Github/F1Game || exit 1
LINE=$1
S=.claude/team/sessions
waitidle(){ for i in $(seq 1 400); do grep -q '^working' "$S/$1.state" 2>/dev/null || return 0; sleep 30; done; }
log(){ echo "[$(date +%H:%M)] night/$LINE $*" | tee -a .ai_logs/team/night.log; }
commit_main(){ # $1 = 메시지
  git checkout -- F1/Assets/UI/Fonts 2>/dev/null
  for p in F1/Assets/Editor F1/Assets/Scripts F1/Assets/Scenes/Map_TownV6.unity F1/Assets/Scenes/Map_TownV6_Mats docs/map docs/qa/evidence docs/team/reports F1Studio/Assets/Art art/_review/2026-10-31-trees art/_review/2026-10-31-house-types tools/asset-gen tools/viewer/feedback; do git add "$p" 2>/dev/null; done
  git commit -qm "$1 [skip-verifier]" 2>/dev/null && { timeout 600 git push -q 2>&1 | grep -E "rejected|GH0" ; log "커밋 $(git log --oneline -1)"; }
}
run(){ # $1 역할 $2 과제파일 $3 커밋메시지 [$4 = feature]
  waitidle "$1"; sleep 60; waitidle "$1"
  rm -f .claude/team/editor.lock
  mv -f "$S/$1.id" "$S/archive/$1-$(basename "$2" .task).id" 2>/dev/null
  log "시작 $2"
  if [ "$1" = feature ]; then UCLI_PROJ=D:/Github/F1Game_shot/F1 bash scripts/team_feature.sh feature "$(cat "$2")"
  else bash scripts/team.sh "$1" "$(cat "$2")"; fi
  rm -f .claude/team/editor.lock
  log "끝 $2 ($(cut -c1-20 "$S/$1.state"))"
  if [ "$4" = feature ]; then
    ( cd D:/Github/F1Game_shot && git checkout -- F1/Assets/UI/Fonts 2>/dev/null; rm -rf F1/Assets/_Recovery; git add F1/Assets && git commit -qm "$3 [skip-verifier]" && timeout 300 git push -q -f origin feature/onfoot ) && log "feature 커밋"
    commit_main "docs: $3 증거·보고"
  else commit_main "$3"; fi
}
case $LINE in
  editor)  run programmer $S/pd-parcels4-1031.task "map: 필지 4판 — 자체 판정·결함 고침(밤 자율)"
           run programmer $S/pd-roadfast-1031.task "editor: 길 그리기 적용 구역 모드(밤 자율)" ;;
  studio)  run artlive $S/artlive-housetypes-1031.task "art: 배경 집 유형 후보판(밤 자율)" ;;
  feature) ( cd D:/Github/F1Game_shot && git checkout -- F1/Assets/UI/Fonts docs 2>/dev/null; git fetch -q origin && git checkout -q -B feature/onfoot origin/main )
           UCLI_PROJ=D:/Github/F1Game_shot/F1 timeout 60 bash scripts/ucli-http.sh exec 'UnityEditor.AssetDatabase.Refresh(); return "r";' >/dev/null 2>&1; sleep 120
           UCLI_PROJ=D:/Github/F1Game_shot/F1 timeout 60 bash scripts/ucli-http.sh exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity"); return "o";' >/dev/null 2>&1
           run feature $S/feature2-1031.task "feature2: 정비소 사장 → 첫 주문 → 출발·도착(밤 자율)" feature ;;
esac
log "줄 끝"
