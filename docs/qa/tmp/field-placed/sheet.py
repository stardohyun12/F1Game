# 밭 투입 시트: 위 줄 = 8b98 도현 원본 스샷 | 8b98 지금 | 94a3(f1_high) 지금, 아래 줄 = y2 마당 눈높이 | y3 입구 | t_top 위에서
# 사용: python -X utf8 docs/qa/tmp/field-placed/sheet.py
import json, os
from PIL import Image, ImageDraw
D = "docs/qa/evidence/field-placed"
orig = None
for l in open("tools/viewer/feedback/unity-Map_TownV6.jsonl", encoding="utf-8"):
    if l.strip():
        d = json.loads(l)
        if d.get("id") == "unity-Map_TownV6-20261030-195518-8b98":
            orig = os.path.join("tools/viewer/feedback", d["screenshot"])
cuts = [("8b98 before (doh)", orig), ("8b98 now", f"{D}/8b98.png"), ("94a3 now", f"{D}/94a3.png"),
        ("y2 yard eye", f"{D}/y2_yard_garden.png"), ("y3 gate", f"{D}/y3_gate.png"), ("top", f"{D}/t_top.png")]
W, H = 640, 360
sheet = Image.new("RGB", (W * 3, (H + 22) * 2), (20, 20, 20)); dr = ImageDraw.Draw(sheet)
for i, (n, p) in enumerate(cuts):
    x, y = (i % 3) * W, (i // 3) * (H + 22)
    im = Image.open(p).convert("RGB"); im.thumbnail((W, H), Image.LANCZOS)  # 비율 유지(8b98·94a3 는 1280×611)
    sheet.paste(im, (x + (W - im.width) // 2, y + (H - im.height) // 2))
    dr.text((x + 4, y + H + 5), n, fill=(230, 230, 230))
sheet.save(f"{D}/sheet.png"); print(sheet.size, orig)
