# UI 1판 | 2판 오후 | 2판 노을 비교판 — 행마다 1×(640×360) 세 장 + 그 아래 같은 자리 3× 확대(Nearest).
# 사용: python board.py  → docs/qa/evidence/ui-v2/compare.png
import json, sys
from PIL import Image, ImageDraw, ImageFont

V1 = "docs/qa/evidence/ui-v1/"
V2 = "docs/qa/evidence/ui-v2/"
F = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 22)
# (행 이름, 1판 파일, 2판 파일, 3× 자리(x, y, w, h) — 위 왼쪽 원점, 640×360 기준; 1판/2판이 다르면 두 개)
ROWS = json.load(open("docs/qa/tmp/uiv2/rows.json", encoding="utf-8"))
GAP, LBL = 8, 30
cols = ["1판", "2판 오후", "2판 노을"]


def load(p):
    try: return Image.open(p).convert("RGB")
    except Exception: return Image.new("RGB", (640, 360), (40, 40, 40))


blocks = []
for r in ROWS:
    name, f1, f2 = r["name"], r["v1"], r["v2"]
    box1 = r["crop_v1"]; box2 = r.get("crop_v2", box1)
    ims = [load(V1 + f1) if f1 else None, load(V2 + "aft/" + f2), load(V2 + "dusk/" + f2)]
    boxes = [box1, box2, box2]
    cw, ch = box2[2] * 3, box2[3] * 3
    W = 3 * 640 + 2 * GAP
    H = LBL + 360 + GAP + max(ch, box1[3] * 3) + GAP
    b = Image.new("RGB", (W, H), (24, 24, 28)); d = ImageDraw.Draw(b)
    d.text((6, 3), name, font=F, fill=(240, 240, 240))
    for i, im in enumerate(ims):
        x0 = i * (640 + GAP)
        d.text((x0 + 430, 3), cols[i] + (" (없음)" if im is None else ""), font=F, fill=(200, 200, 160))
        if im is None: continue
        b.paste(im, (x0, LBL))
        x, y, w, h = boxes[i]
        d.rectangle([x0 + x - 1, LBL + y - 1, x0 + x + w, LBL + y + h], outline=(255, 0, 255))
        crop = im.crop((x, y, x + w, y + h)).resize((w * 3, h * 3), Image.NEAREST)
        b.paste(crop, (x0, LBL + 360 + GAP))
    blocks.append(b)
W = max(b.width for b in blocks); H = sum(b.height for b in blocks) + GAP * len(blocks)
out = Image.new("RGB", (W, H), (12, 12, 14)); y = 0
for b in blocks: out.paste(b, (0, y)); y += b.height + GAP
out.save(V2 + "compare.png"); print("compare", out.size)
