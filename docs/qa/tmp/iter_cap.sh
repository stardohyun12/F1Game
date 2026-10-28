#!/usr/bin/env bash
# artlive 캡처 한 번: Play 켬 → iter_cap.cs(3캠) → sNN_pair.png → Play 끔.
# 사용: bash docs/qa/tmp/iter_cap.sh <단계 번호> <짧은 설명> [에셋=depot]
#   예) bash docs/qa/tmp/iter_cap.sh 1 form
# pair: 직전 단계(sNN-1_*)가 있으면 캠마다 [직전 | 이번] 3줄, 없으면 이번 3컷을 한 줄로.
set -eu
N=$1; D=$2; A=${3:-depot}
U="bash scripts/ucli-http.sh"
$U exec "UnityEditor.SessionState.SetString(\"iter.cap\", \"$N|$D|$A\"); return \"arg ok\";"
$U editor play --wait
sleep 3   # 첫 프레임·A7 빛(Play 중 RP 교체) 반영 대기
$U exec @docs/qa/tmp/iter_cap.cs
$U editor stop
python -X utf8 - "$N" "$D" "docs/qa/evidence/iter-$A" <<'EOF'
import sys, glob, os
from PIL import Image, ImageDraw
n, d, out = int(sys.argv[1]), sys.argv[2], sys.argv[3]
cams = ["road", "front", "34"]
cur = [os.path.join(out, f"s{n:02d}_{d}_{c}.png") for c in cams]
prev = []
for c in cams:
    g = sorted(glob.glob(os.path.join(out, f"s{n-1:02d}_*_{c}.png")))
    prev.append(g[0] if g else None)
def lab(im, t):
    dr = ImageDraw.Draw(im); dr.rectangle([0, 0, 8 * len(t) + 12, 22], fill=(0, 0, 0)); dr.text((6, 5), t, fill=(255, 255, 255)); return im
W, H = 1280, 720
if any(prev):
    sheet = Image.new("RGB", (W * 2, H * 3), (40, 40, 40))
    for i, c in enumerate(cams):
        if prev[i]: sheet.paste(lab(Image.open(prev[i]).convert("RGB"), os.path.basename(prev[i])), (0, H * i))
        sheet.paste(lab(Image.open(cur[i]).convert("RGB"), os.path.basename(cur[i])), (W, H * i))
else:
    sheet = Image.new("RGB", (W * 3, H), (40, 40, 40))
    for i, c in enumerate(cams): sheet.paste(lab(Image.open(cur[i]).convert("RGB"), os.path.basename(cur[i])), (W * i, 0))
p = os.path.join(out, f"s{n:02d}_pair.png"); sheet.save(p); print("pair", p, sheet.size)
EOF
