#!/usr/bin/env bash
# shot.sh <장소번호> <dusk 0|1> <파일이름> [대기초=2.5] — 장소 이동 → 대기 → 게임 카메라 그대로 스틸 2장
cd /d/Github/F1Game || exit 1
D=art/_review/2026-11-02-insta/_scripts
python $D/run.py tp.cs >/dev/null; cp $D/_last_exec.cs $D/_tp_exec.cs
python $D/run.py cap.cs >/dev/null; cp $D/_last_exec.cs $D/_cap_exec.cs
bash scripts/ucli-http.sh exec "UnityEditor.SessionState.SetString(\"insta.tp\",\"$1|$2\"); UnityEditor.SessionState.SetString(\"insta.name\",\"$3\"); return 1;" >/dev/null
bash scripts/ucli-http.sh exec @$D/_tp_exec.cs
sleep "${4:-2.5}"
bash scripts/ucli-http.sh exec @$D/_cap_exec.cs
