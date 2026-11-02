#!/usr/bin/env bash
# 증거 묶음 만들기(2026-11-03 팀 재편, docs/team/FLOW.md §4) — 카드당 하나. 하네스가 찍는다(작업자가 고른 사진이 아니라).
# 사용: bash tools/evidence/bundle.sh <카드ID> [after|before] [초=10]
#   before = 시작 장면 정지 사진 1장 + 로그 검사(작업 시작 전에 PD 가 찍음)
#   after  = 시작 장면 → start.cs(카드 폴더에 있으면 실행: 순간이동·입력 흉내) → 클립(초) + 정지 사진 + frameCount + 로그 검사
# 결과: docs/team/cards/<ID>/evidence/<label>/{clip.mp4, strip.png, shot.png, meta.json} + docs/team/cards/<ID>/evidence.json(요약·기계 판정)
# 본 에디터를 쓴다 → editor.lock 이 비어 있을 때만(다른 [EDITOR] 과제와 동시 금지).
set -u
cd "$(dirname "$0")/../.." || exit 1
ID="${1:?카드ID}"; LABEL="${2:-after}"; SECS="${3:-10}"; FPS=12
CARD=$(ls -d docs/team/cards/"$ID"* 2>/dev/null | head -1)
[ -d "$CARD" ] || { echo "FAIL: 카드 폴더 없음 docs/team/cards/$ID*"; exit 2; }
U="bash scripts/ucli-http.sh"
FF=$(command -v ffmpeg || ls /c/Users/*/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg*/ffmpeg-*/bin/ffmpeg.exe 2>/dev/null | head -1)
LOCK=.claude/team/editor.lock
if [ -s "$LOCK" ] && [ "$(cat "$LOCK")" != "evidence" ] && [ "$(cat "$LOCK")" != "${EV_LOCK_OWNER:-}" ]; then echo "FAIL: 에디터 잠금 $(cat "$LOCK") — 끝난 뒤 다시"; exit 3; fi
OWN=0; [ -s "$LOCK" ] || { echo evidence > "$LOCK"; OWN=1; }
cleanup(){ $U editor stop >/dev/null 2>&1; git checkout -- F1/Assets/UI/Fonts 2>/dev/null; [ $OWN = 1 ] && rm -f "$LOCK"; }
trap cleanup EXIT
OUT="$CARD/evidence/$LABEL"; rm -rf "$OUT"; mkdir -p "$OUT/frames"
# 이전 판의 결과가 남아 기계 PASS 로 읽히지 않게 이 라벨 기록부터 지운다
python -X utf8 -c "import json,sys;p=sys.argv[1]+'/evidence.json'
try: e=json.load(open(p,encoding='utf-8'))
except Exception: sys.exit()
e.pop(sys.argv[2],None); e['machine']='-'; json.dump(e,open(p,'w',encoding='utf-8'),ensure_ascii=False,indent=1)" "$CARD" "$LABEL"
PLACE=$(grep -m1 -E '^start_place:' "$CARD/card.md" | sed 's/^start_place:[[:space:]]*//' | tr -d '\r')
$U editor stop >/dev/null 2>&1; sleep 2
$U editor play --wait >/dev/null || { echo "FAIL: Play 진입 실패"; exit 1; }
$U exec 'Application.runInBackground = true; return "rib";' >/dev/null
sleep 3
[ -n "$PLACE" ] && echo "시작 장면: $($U exec "return DebugWindow.GoPlace($PLACE);")"
sleep 2
if [ "$LABEL" = "after" ] && [ -f "$CARD/start.cs" ]; then echo "start.cs: $($U exec @"$CARD/start.cs" | tail -1 | cut -c1-200)"; fi
[ "$LABEL" = "before" ] && SECS=2
sed -e "s#__DIR__#$OUT/frames#" -e "s#__SECS__#$SECS#" -e "s#__FPS__#$FPS#" tools/evidence/clip.cs.tmpl > "$OUT/clip.cs"
echo "clip: $($U exec @"$OUT/clip.cs" | tail -1)"
META=""
for i in $(seq 1 $(( SECS * 3 + 40 ))); do
  sleep 1; META=$($U exec 'return UnityEditor.SessionState.GetString("ev.done","");' 2>/dev/null | tail -1)
  case "$META" in \{*) break;; esac
done
case "$META" in \{*) ;; *) echo "FAIL: 클립이 끝나지 않음"; exit 1;; esac
printf '%s\n' "$META" > "$OUT/meta.json"
CONS=$($U console --type error 2>/dev/null | tail -1 | cut -c1-2000)
$U editor stop >/dev/null 2>&1
N=$(ls "$OUT/frames"/f*.png 2>/dev/null | wc -l)
LAST=$(ls "$OUT/frames"/f*.png 2>/dev/null | tail -1)
[ -n "$LAST" ] && cp "$LAST" "$OUT/shot.png"
if [ "$LABEL" = "after" ] && [ "$N" -gt 2 ] && [ -n "$FF" ]; then
  "$FF" -v error -y -framerate $FPS -i "$OUT/frames/f%03d.png" -c:v libx264 -pix_fmt yuv420p -crf 26 "$OUT/clip.mp4"
  # 그림만 볼 수 있는 검증 일꾼용: 클립 전체에서 고르게 8장 → 4×2 한 장(칸 = 시간 순, 왼쪽 위 → 오른쪽 아래)
  K=$(( (N + 7) / 8 ))
  "$FF" -v error -y -i "$OUT/clip.mp4" -vf "select='not(mod(n\,$K))',scale=480:-1,tile=4x2" -fps_mode vfr -frames:v 1 "$OUT/strip.png"
fi
rm -rf "$OUT/frames" "$OUT/clip.cs"
python -X utf8 - "$CARD" "$LABEL" "$SECS" "$CONS" <<'EOF'
import json,sys,os,datetime
card,label,secs,cons=sys.argv[1],sys.argv[2],float(sys.argv[3]),sys.argv[4]
d=f"{card}/evidence/{label}"
m=json.load(open(f"{d}/meta.json",encoding="utf-8"))
adv=m["frame_end"]-m["frame_start"]
m["frames_advanced"]=adv
m["frames_ok"]= adv >= max(20, secs*10) and m["unique_frames"] >= secs*10    # 초당 10프레임 이상 실제로 돌았나
m["log_ok"]= m["errors"]==0
m["console_errors_after"]=cons
for k in ("clip.mp4","strip.png","shot.png"):
    m[k.split('.')[0]]= f"{d}/{k}" if os.path.exists(f"{d}/{k}") else None
m["made"]=datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
json.dump(m,open(f"{d}/meta.json","w",encoding="utf-8"),ensure_ascii=False,indent=1)
p=f"{card}/evidence.json"
try: ev=json.load(open(p,encoding="utf-8"))
except Exception: ev={}
ev[label]=m
ev["machine"]="PASS" if ev.get("after",{}).get("frames_ok") and ev.get("after",{}).get("log_ok") else ("FAIL" if "after" in ev else "-")
json.dump(ev,open(p,"w",encoding="utf-8"),ensure_ascii=False,indent=1)
print(f"{label}: frames {m['frame_start']}→{m['frame_end']} (+{adv}, uniq {m['unique_frames']}) · 캡처 {m['captured']}장 · 경고 {m['warnings']} · 에러 {m['errors']} · 기계 {ev['machine']}")
print("  ", m.get("clip") or "", m.get("strip") or "", m.get("shot") or "")
EOF
