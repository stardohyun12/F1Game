# places.json(+before) → 장소별 채널 크기 막대 png
import json, sys
from PIL import Image, ImageDraw, ImageFont
d = sys.argv[1]
P = json.load(open(d + "/places.json", encoding="utf-8"))
try: B = json.load(open(d + "/before/places.json", encoding="utf-8"))
except Exception: B = []
rows = [(p["name"], p["earDbFS"], p["cmd"]["cutoffHz"], p["label"]) for p in P] + [(b["name"], b["earDbFS"], 22000, "예전") for b in B]
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 15); small = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 12)
W, rowh = 1300, 46; H = 70 + rowh * len(rows) + 30
img = Image.new("RGB", (W, H), "white"); g = ImageDraw.Draw(img)
L = 330; lo, hi = -80, -15
X = lambda v: L + (max(min(v, hi), lo) - lo) / (hi - lo) * (W - L - 40)
g.text((10, 10), "장소별 귀(AudioListener)에 닿는 크기 dBFS — 채널 하나씩 켜고 1.2초 평균(갈매기는 23 m 거리에서 한 번 울림)", fill="black", font=font)
for v in range(lo, hi + 1, 5):
    g.line([X(v), 45, X(v), H - 25], fill="#eee"); g.text((X(v) - 10, H - 22), str(v), fill="#666", font=small)
cols = [("wind", "#1f6fd1", "바람"), ("waves", "#13a39a", "파도"), ("gull", "#b45309", "갈매기")]
for j, (k, c, n) in enumerate(cols): g.rectangle([W - 330 + j * 100, 32, W - 316 + j * 100, 42], fill=c); g.text((W - 312 + j * 100, 28), n, fill="black", font=small)
for i, (name, e, hz, lab) in enumerate(rows):
    y = 55 + i * rowh
    if lab == "예전": g.rectangle([0, y - 2, W, y + rowh - 6], fill="#f6f6f6")
    g.text((10, y + 8), f"{name}  [{lab}]  LP {hz:.0f}Hz", fill="black", font=font)
    for j, (k, c, n) in enumerate(cols):
        yy = y + j * 12
        g.rectangle([L, yy, X(e[k]), yy + 10], fill=c); g.text((X(e[k]) + 4, yy - 3), f"{e[k]:.1f}", fill="#333", font=small)
img.save(d + "/places_levels.png"); print("ok", len(rows))
