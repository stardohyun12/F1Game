#!/usr/bin/env bash
# D-275 세계 모델 1층 — 세계 스냅숏. 에디터(에디트 모드)에서 WorldSnapshot.Dump() 를 받아 docs/world/WORLD.md 에 쓴다.
# 사용: bash scripts/world_snapshot.sh      (team_queue.sh 가 build 과제 시작 직전에 부름; 실패해도 과제는 진행)
# 실패(에디터 꺼짐·컴파일 중·Play 중·예외)면 옛 WORLD.md 를 그대로 둔다(exit 1). 씬은 안 바꾼다(읽기만).
cd D:/Github/F1Game || exit 1
OUT=docs/world/WORLD.md; mkdir -p docs/world
U="bash scripts/ucli-http.sh"
RES=$(timeout 120 $U exec 'if (UnityEditor.EditorApplication.isPlaying) return "FAIL: Play 중"; if (UnityEditor.EditorApplication.isCompiling) return "FAIL: 컴파일 중"; return WorldSnapshot.Dump();' 2>&1)
if [ -z "$RES" ] || printf '%s' "$RES" | head -1 | grep -q '^FAIL'; then
  echo "world_snapshot: 실패 — 옛 $OUT 유지. $(printf '%s' "$RES" | head -2 | tr '\n' ' ' | cut -c1-300)" >&2; exit 1
fi
{
  echo "<!-- 자동 생성: scripts/world_snapshot.sh — 손으로 고치지 말 것 -->"
  echo "생성 $(date '+%Y-%m-%d %H:%M') · 커밋 $(git rev-parse --short HEAD 2>/dev/null) · 브랜치 $(git rev-parse --abbrev-ref HEAD 2>/dev/null)"
  echo
  printf '%s\n' "$RES"
} > "$OUT.tmp" && mv -f "$OUT.tmp" "$OUT"
echo "world_snapshot: $OUT $(wc -l < "$OUT")줄"
