"""HUD·말풍선 영역을 원본 1:1(축소 없음)로 잘라 모은다 — 세계 격자와 UI 격자를 눈으로 비교하려고.
사용: python -I crops.py <rawdir> <outdir>"""
import sys, os
from PIL import Image, ImageDraw

raw, out = sys.argv[1], sys.argv[2]
CW, CH = 760, 380
SPEC = [
    # (라벨, 폴더, 파일, 중심x비율, 중심y비율)
    ("shorthike03 bubble", "shorthike", "03.jpg", 0.42, 0.28),
    ("toem04 bubble", "toem", "04.jpg", 0.48, 0.38),
    ("webfishing05 chatbox", "webfishing", "05.jpg", 0.46, 0.80),
    ("mailtime01 box", "mailtime", "01.jpg", 0.50, 0.76),
    ("liligator03 box", "liligator", "03.jpg", 0.50, 0.82),
    ("wheelworld00 speed BL", "wheelworld", "00.jpg", 0.14, 0.88),
    ("wheelworld00 top R", "wheelworld", "00.jpg", 0.88, 0.14),
    ("wheelworld04 NPC prompt", "wheelworld", "04.jpg", 0.46, 0.55),
    ("dredge02 top+left", "dredge", "02.jpg", 0.50, 0.08),
    ("dredge02 BL tool", "dredge", "02.jpg", 0.12, 0.88),
    ("jalopy02 dash", "jalopy", "02.jpg", 0.50, 0.58),
    ("ds21 timer", "deathstranding", "21.jpg", 0.52, 0.12),
]
os.makedirs(out, exist_ok=True)
def crop(spec):
    lab, g, f, cx, cy = spec
    im = Image.open(os.path.join(raw, g, f)).convert("RGB")
    W, H = im.size
    x0 = int(min(max(cx * W - CW / 2, 0), W - CW)); y0 = int(min(max(cy * H - CH / 2, 0), H - CH))
    c = im.crop((x0, y0, x0 + CW, y0 + CH))
    ImageDraw.Draw(c).rectangle([0, 0, 330, 16], fill=(0, 0, 0))
    ImageDraw.Draw(c).text((4, 2), f"{lab} src{W}x{H} @({x0},{y0})", fill=(255, 255, 0))
    return c, (W, H)
for n in range(0, len(SPEC), 6):
    part = SPEC[n:n + 6]
    sheet = Image.new("RGB", (CW * 2, CH * 3), (20, 20, 20))
    for i, s in enumerate(part):
        c, sz = crop(s)
        sheet.paste(c, ((i % 2) * CW, (i // 2) * CH))
        print(s[0], sz)
    sheet.save(os.path.join(out, f"crops_{n // 6 + 1}.jpg"), quality=88)
