#!/usr/bin/env bash
# 하네스 1번(토스식 "명령 하나 = 같은 품질"): 도현 코멘트 하나를 고정 절차로 고친다.
# 사용: bash scripts/team-fix.sh <코멘트id 또는 끝 몇 글자> [role]
#   role 기본: Map_* 코멘트 = programmer([EDITOR]), Studio_* = artlive(F1Studio)
# PD 는 과제 글을 쓰지 않는다 — 이 템플릿이 과제다. 새 세션 · 25턴 · 코멘트 하나.
# 3회 연속 판정 실패하면 도현에게 올린다(.claude/team/fix/<id>.tries).
set -u
cd "$(dirname "$0")/.." || exit 1
Q="${1:?usage: team-fix.sh <comment-id> [role]}"
INFO=$(python -X utf8 - "$Q" <<'EOF'
import json,glob,sys
q=sys.argv[1]
for f in glob.glob('tools/viewer/feedback/*.jsonl'):
    for l in open(f,encoding='utf-8'):
        if not l.strip(): continue
        d=json.loads(l)
        if d.get('id','').endswith(q):
            print(d['id']); print(f.replace(chr(92),'/')); print(d.get('scene','') or f.split('unity-')[-1].replace('.jsonl',''))
            raise SystemExit
print('')
EOF
)
ID=$(sed -n 1p <<<"$INFO" | tr -d '\r'); FILE=$(sed -n 2p <<<"$INFO" | tr -d '\r'); SCENE=$(sed -n 3p <<<"$INFO" | tr -d '\r')
[ -z "$ID" ] && { echo "FAIL: 코멘트 $Q 없음"; exit 2; }
ROLE="${2:-}"; [ -z "$ROLE" ] && case "$SCENE" in *Studio*) ROLE=artlive;; *) ROLE=programmer;; esac
mkdir -p .claude/team/fix
TRIES=$(( $(cat ".claude/team/fix/$ID.tries" 2>/dev/null || echo 0) + 1 ))
if [ "$TRIES" -gt 3 ]; then
  echo "STOP: $ID 3회 실패 — 도현에게 올림(새 과제 안 띄움)"; exit 4
fi
echo "$TRIES" > ".claude/team/fix/$ID.tries"
TAG=""; [ "$ROLE" = "programmer" ] && TAG="[EDITOR]"
EXTRA=""; case "$SCENE" in *Studio*) EXTRA="Unity 는 F1Studio 만 쓴다(export UCLI_PROJ=D:/Github/F1Game/F1Studio, 켜져 있음) — 쓰는 것이 허용이자 의무다. 고친 뒤 python -X utf8 tools/art/build_asset.py <건물> 로 Unity 임포트까지 하고 fb_geom·fb.py fixed 를 직접 돌린다. 본 F1 금지.";; esac
TASK="$TAG[fix $ID — $TRIES/3회차] 도현 코멘트 하나를 고정 절차로 고친다. 커밋 금지. 다른 코멘트·다른 건물 건드리지 않음(같은 원인 목록만).
코멘트 파일: $FILE  (python -X utf8 scripts/fb.py list 로 원문·스샷·thread 전부 읽기 — thread 의 PD·판정 글이 이전 실패 이유다)
$EXTRA
절차(순서 고정, 단계마다 보고서에 한 줄):
1. 보기: 원본 스샷을 Read 로 직접 본다. 도현이 무엇을 문제라고 했는지 한 줄로 다시 쓴다.
2. 원인: 그 문제가 왜 생겼는지 한 줄(추측이면 '가설'). 보고서 맨 위.
3. 고침: 원인을 고친다(증상만 덮지 않음). 원본(생성기·씬 빌더)에 남긴다.
4. 계산 검사: 공간 문제(묻힘·뜸·길·겹침)면 python -X utf8 scripts/fb_geom.py $ID → PASS 까지. 아트면 bash tools/art/bin/check.sh <건물> PASS.
4b. 25턴의 절반(12턴)이 지나면 그때까지 한 것을 보고서에 먼저 쓴다(턴 한도로 끊겨도 남게).
5. 확인: python -X utf8 scripts/fb.py fixed $ID \"<고친 것 한 줄>\" — 같은 카메라 재촬영 + 판정자 둘. 거부되면 이유를 읽고 3으로(이 과제 안에서 최대 2번).
6. 같은 원인: 맵/라이브러리의 다른 곳에 같은 원인이 남았는지 목록(고치지 말고 적기).
씬 저장·콘솔 에러 0. 끝 보고 3줄: 원인 / 결과(fb_geom·판정 YES/NO) / 같은 원인 남은 곳."
mv -f ".claude/team/sessions/$ROLE.id" ".claude/team/sessions/archive/$ROLE-fix-$(date +%H%M).id" 2>/dev/null
T0=$(date +%s)
TEAM_MAX_TURNS="${TEAM_MAX_TURNS:-25}" bash scripts/team.sh "$ROLE" "$TASK"
# 하네스 3번: 끝은 하네스가 판정 — 새 보고서·geom 결과 없으면 failed
NEWREP=$(find docs/team/reports -name "$ROLE-*.md" -newermt "@$T0" | head -1)
[ -z "$NEWREP" ] && echo "FAILED-NOREPORT: $ID 보고서 없음(턴 한도 추정)"
G="tools/viewer/feedback/geom/$ID.json"; [ -f "$G" ] && echo "geom: $(python -X utf8 -c "import json;d=json.load(open('$G',encoding='utf-8'));print('PASS' if d.get('pass') else 'FAIL '+str(len([i for i in d['items'] if i.get('problem')])))")"
# 결과: fixed 로 바뀌었으면 시도 횟수 초기화
python -X utf8 - "$FILE" "$ID" <<'EOF' && rm -f ".claude/team/fix/$ID.tries"
import json,sys
for l in open(sys.argv[1],encoding='utf-8'):
    if l.strip():
        d=json.loads(l)
        if d['id']==sys.argv[2]: sys.exit(0 if d.get('status') in ('fixed','done') else 1)
sys.exit(1)
EOF
echo "fix $ID 끝 — status: $(python -X utf8 -c "import json,sys;[print(json.loads(l).get('status')) for l in open('$FILE',encoding='utf-8') if l.strip() and json.loads(l)['id']=='$ID']")"
