# terrace7: 카트 카메라 3컷 + 위 직교 1장(진입로 중심선·컷 자리·경사 글자) 시트
import json
from PIL import Image, ImageDraw, ImageFont
D = "D:/Github/F1Game/docs/qa/evidence/terrace7/"
T = "D:/Github/F1Game/docs/qa/tmp/terrace7/"
def P(x, z): return ((x - 218) * 25, (-40 - z) * 25)   # top_raw: 25 px/m, 좌상단 (218,-40)
try:
    F = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 24); Fs = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 20)
except Exception:
    F = Fs = ImageFont.load_default()
rows = json.load(open(T + "drive.json", encoding="utf-8"))["rows"]
log = open(D + "render_evidence.txt", encoding="utf-8").read().splitlines()
cars = {}
for l in log:
    if l.startswith("c") and "car=(" in l:
        a = l.split("car=(")[1].split(")")[0].split(","); cars[l.split(".png")[0]] = (float(a[0]), float(a[2]))
top = Image.open(D + "top_raw.png").convert("RGB"); d = ImageDraw.Draw(top)
d.line([P(r["x"], r["z"]) for r in rows], fill=(255, 230, 0), width=5)
for r in rows:
    if abs(r["s"] % 5) < 1e-3:
        cx, cy = P(r["x"], r["z"]); d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=(255, 230, 0))
        d.text((cx + 8, cy - 12), f"s{r['s']:.0f} y{r['y']:.2f}", fill=(255, 255, 255), font=Fs, stroke_width=2, stroke_fill=(0, 0, 0))
for i, k in enumerate(["c1_mainroad", "c2_arc", "c3_yard"]):
    if k in cars:
        cx, cy = P(*cars[k]); d.ellipse([cx - 15, cy - 15, cx + 15, cy + 15], fill=(20, 20, 20)); d.text((cx - 7, cy - 16), str(i + 1), fill=(255, 255, 255), font=F)
d.rectangle([0, 0, 700, 66], fill=(0, 0, 0))
d.text((8, 4), "노랑 = 차 진입로 중심선(폭 4.5 m, R 11 m, 최대 경사 8.7%)", fill=(255, 230, 0), font=Fs)
d.text((8, 34), "숫자 = 카트 컷 자리 · 25 px = 1 m · 왼쪽 위 = 차고 마당", fill=(255, 255, 255), font=Fs)
top.save(D + "top_drive.png")
names = [("c1_mainroad.png", "1 큰길 — 갈림목 6 m 전"), ("c2_arc.png", "2 원호 중간(s 7, 왼쪽 = 막다른 옛 토막)"), ("c3_yard.png", "3 마당 8 m 전(왼쪽 = 차고)")]
W, H = 800, 450
tw = W * 3; th = 900 * tw // 1200
sheet = Image.new("RGB", (tw, H + th), (30, 30, 30))
for i, (f, t) in enumerate(names):
    im = Image.open(D + f).convert("RGB").resize((W, H)); dd = ImageDraw.Draw(im)
    dd.rectangle([0, 0, W, 36], fill=(0, 0, 0)); dd.text((8, 4), t, fill=(255, 255, 255), font=Fs)
    sheet.paste(im, (i * W, 0))
sheet.paste(top.resize((tw, th)), (0, H))
sheet = sheet.resize((sheet.width // 2, sheet.height // 2))
sheet.save(D + "sheet.png")
print("ok", sheet.size)
