#!/usr/bin/env bash
# 증거 묶음 만들기(2026-11-03 팀 재편, docs/team/FLOW.md §4) — 카드당 하나. 하네스가 찍는다(작업자가 고른 사진이 아니라).
# 사용: bash tools/evidence/bundle.sh <카드ID> [after|before] [초=10]
#   before = 시작 장면 정지 사진 1장 + 로그 검사(작업 시작 전에 PD 가 찍음)
#   after  = 시작 장면 → start.cs(카드 폴더에 있으면 실행: 순간이동·입력 흉내) → 클립(초) + 정지 사진 + frameCount + 로그 검사
#            + D-277 기준별 증거: 카드 폴더 crit1.cs·crit2.cs·… 가 있으면 각각 새 Play → start_place 순간이동 → critN.cs → 2초 → 캡처 1장(1× + 3× nearest)
# 결과: docs/team/cards/<ID>/evidence/<label>/{clip.mp4, strip.png, shot.png, critN.png, critN_3x.png, meta.json} + docs/team/cards/<ID>/evidence.json(요약·기계 판정)
# 본 에디터를 쓴다 → editor.lock 이 비어 있을 때만(다른 [EDITOR] 과제와 동시 금지).
# 드라이런: EV_DRY=1 → 에디터를 안 건드리고 crit 분기(파일 탐색·명령 순서)만 echo 로 보여 준다.
set -u
cd "$(dirname "$0")/../.." || exit 1
ID="${1:?카드ID}"; LABEL="${2:-after}"; SECS="${3:-10}"; FPS=12
CARD=$(ls -d docs/team/cards/"$ID"* 2>/dev/null | head -1)
[ -d "$CARD" ] || { echo "FAIL: 카드 폴더 없음 docs/team/cards/$ID*"; exit 2; }
U="bash scripts/ucli-http.sh"
FF=$(command -v ffmpeg || ls /c/Users/*/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg*/ffmpeg-*/bin/ffmpeg.exe 2>/dev/null | head -1)
PLACE=$(grep -m1 -E '^start_place:' "$CARD/card.md" | sed 's/^start_place:[[:space:]]*//' | tr -d '\r')
OUT="$CARD/evidence/$LABEL"

# D-277 기준별 증거: critN.cs 마다 새 Play 에서 그 기준이 보이는 순간을 만들고 1장. 검증 일꾼은 기준 N 을 critN.png 로 판정한다.
# (C-003 이 3번 불통과한 원인 = 10초 클립 하나에 긴 흐름을 욱여넣어 기준 2·3 이 화면에 안 나옴)
crit_shots(){
  local crits n f res i
  crits=$(ls "$CARD"/crit[0-9]*.cs 2>/dev/null | sort -V)
  [ -n "$crits" ] || { echo "crit: 없음(critN.cs 가 카드 폴더에 없음 — strip 으로만 판정)"; return 0; }
  for f in $crits; do
    n=$(basename "$f" .cs | sed 's/^crit//')
    if [ "${EV_DRY:-}" = 1 ]; then
      echo "[dry] crit$n: editor stop → editor play --wait → runInBackground → GoPlace($PLACE) → exec @$f → 2초 → shot.cs.tmpl → $OUT/crit$n.png → 3× $OUT/crit${n}_3x.png"
      continue
    fi
    $U editor stop >/dev/null 2>&1; sleep 2
    $U editor play --wait >/dev/null || { echo "crit$n: FAIL Play 진입 실패"; continue; }
    $U exec 'Application.runInBackground = true; return "rib";' >/dev/null; sleep 3
    [ -n "$PLACE" ] && $U exec "return DebugWindow.GoPlace($PLACE);" >/dev/null; sleep 2
    res=$($U exec @"$f" | tail -1 | cut -c1-200)
    sleep 2
    sed -e "s#__OUT__#$OUT/crit$n.png#" tools/evidence/shot.cs.tmpl > "$OUT/shot.cs"
    $U exec @"$OUT/shot.cs" >/dev/null
    for i in 1 2 3 4 5 6 7 8 9 10; do sleep 1; [ -f "$OUT/crit$n.png" ] && break; done
    rm -f "$OUT/shot.cs"
    if [ -f "$OUT/crit$n.png" ]; then
      [ -n "$FF" ] && "$FF" -v error -y -i "$OUT/crit$n.png" -vf "scale=iw*3:ih*3:flags=neighbor" "$OUT/crit${n}_3x.png"
      echo "crit$n: $OUT/crit$n.png · $res"
    else
      echo "crit$n: FAIL 캡처 없음 · $($U exec 'return UnityEditor.SessionState.GetString("ev.shot","");' 2>/dev/null | tail -1 | cut -c1-120) · $res"
    fi
  done
  $U editor stop >/dev/null 2>&1
}

if [ "${EV_DRY:-}" = 1 ]; then echo "[dry] bundle $ID $LABEL: 클립·Play 생략, crit 분기만"; crit_shots; exit 0; fi
LOCK=.claude/team/editor.lock
if [ -s "$LOCK" ] && [ "$(cat "$LOCK")" != "evidence" ] && [ "$(cat "$LOCK")" != "${EV_LOCK_OWNER:-}" ]; then echo "FAIL: 에디터 잠금 $(cat "$LOCK") — 끝난 뒤 다시"; exit 3; fi
OWN=0; [ -s "$LOCK" ] || { echo evidence > "$LOCK"; OWN=1; }
cleanup(){ $U editor stop >/dev/null 2>&1; git checkout -- F1/Assets/UI/Fonts 2>/dev/null; [ $OWN = 1 ] && rm -f "$LOCK"; }
trap cleanup EXIT
rm -rf "$OUT"; mkdir -p "$OUT/frames"
# 이전 판의 결과가 남아 기계 PASS 로 읽히지 않게 이 라벨 기록부터 지운다
python -X utf8 -c "import json,sys;p=sys.argv[1]+'/evidence.json'
try: e=json.load(open(p,encoding='utf-8'))
except Exception: sys.exit()
e.pop(sys.argv[2],None); e['machine']='-'; json.dump(e,open(p,'w',encoding='utf-8'),ensure_ascii=False,indent=1)" "$CARD" "$LABEL"
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
[ "$LABEL" = "after" ] && crit_shots   # 기준별 사진(D-277) — 클립 뒤, 각각 새 Play
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
m["fps"]=round(m["unique_frames"]/max(0.1,m["seconds"]),1)
# 멈춤 검출이 목적(성능 판정 아님): 캡처(PNG 저장)가 에디터를 느리게 해 6~13fps 가 정상 범위였다(2026-11-03 실측) → 초당 2프레임 이상이면 「돈다」
m["frames_ok"]= adv >= max(20, secs*2) and m["unique_frames"] >= max(20, secs*2)
m["log_ok"]= m["errors"]==0
m["console_errors_after"]=cons
for k in ("clip.mp4","strip.png","shot.png"):
    m[k.split('.')[0]]= f"{d}/{k}" if os.path.exists(f"{d}/{k}") else None
# D-277 기준별 사진: critN.png (+ critN_3x.png). 카드 폴더의 critN.cs 수와 비교해 빠진 번호를 적는다
import glob as _g
crit_cs=sorted(int(os.path.basename(p)[4:-3]) for p in _g.glob(f"{card}/crit[0-9]*.cs"))
crit_png=sorted(int(os.path.basename(p)[4:-4]) for p in _g.glob(f"{d}/crit[0-9]*.png") if "_3x" not in p)
m["crit"]={str(n): {"png": f"{d}/crit{n}.png", "png3x": f"{d}/crit{n}_3x.png" if os.path.exists(f"{d}/crit{n}_3x.png") else None} for n in crit_png}
m["crit_missing"]=[n for n in crit_cs if n not in crit_png]
m["crit_ok"]= not m["crit_missing"]
m["made"]=datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
json.dump(m,open(f"{d}/meta.json","w",encoding="utf-8"),ensure_ascii=False,indent=1)
p=f"{card}/evidence.json"
try: ev=json.load(open(p,encoding="utf-8"))
except Exception: ev={}
ev[label]=m
ev["machine"]="PASS" if ev.get("after",{}).get("frames_ok") and ev.get("after",{}).get("log_ok") else ("FAIL" if "after" in ev else "-")
json.dump(ev,open(p,"w",encoding="utf-8"),ensure_ascii=False,indent=1)
print(f"{label}: frames {m['frame_start']}→{m['frame_end']} (+{adv}, uniq {m['unique_frames']}) · 캡처 {m['captured']}장 · 경고 {m['warnings']} · 에러 {m['errors']} · 기준 사진 {len(m['crit'])}장(빠짐 {m['crit_missing'] or '없음'}) · 기계 {ev['machine']}")
print("  ", m.get("clip") or "", m.get("strip") or "", m.get("shot") or "")
EOF
