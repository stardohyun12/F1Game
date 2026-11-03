# 1초 기록(tsv) → 그래프 png. 사용: python -X utf8 graph.py in.tsv out.png [t0 t1]
import sys, math
from PIL import Image, ImageDraw, ImageFont

src, out = sys.argv[1], sys.argv[2]
rows = [l.rstrip("\n").split("\t") for l in open(src, encoding="utf-8")]
head, rows = rows[0], [r for r in rows[1:] if len(r) == len(rows[0])]
col = {h: i for i, h in enumerate(head)}
if len(sys.argv) > 4:
    a, b = float(sys.argv[3]), float(sys.argv[4]); rows = [r for r in rows if a <= float(r[col["t"]]) <= b]
f = lambda r, k: float(r[col[k]])
T = [f(r, "t") for r in rows]
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 15); small = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 12)
W, H = 1400, 820; L, R = 80, 30
img = Image.new("RGB", (W, H), "white"); d = ImageDraw.Draw(img)
t0, t1 = T[0], T[-1] if T[-1] > T[0] else T[0] + 1
X = lambda t: L + (t - t0) / (t1 - t0) * (W - L - R)

def panel(y0, y1, lo, hi, title, series, ticks, log=False):
    Y = (lambda v: y1 - (math.log10(max(v, 1)) - math.log10(lo)) / (math.log10(hi) - math.log10(lo)) * (y1 - y0)) if log else (lambda v: y1 - (min(max(v, lo), hi) - lo) / (hi - lo) * (y1 - y0))
    d.rectangle([L, y0, W - R, y1], outline="#bbb")
    d.text((L, y0 - 20), title, fill="black", font=font)
    for tv in ticks:
        yy = Y(tv); d.line([L, yy, W - R, yy], fill="#eee"); d.text((8, yy - 8), str(tv), fill="#666", font=small)
    for name, key, colr in series:
        pts = [(X(T[i]), Y(f(r, key))) for i, r in enumerate(rows)]
        if len(pts) > 1: d.line(pts, fill=colr, width=2)
        for p in pts: d.ellipse([p[0] - 2, p[1] - 2, p[0] + 2, p[1] + 2], fill=colr)
    lx = W - R - 300
    for j, (name, key, colr) in enumerate(series):
        d.line([lx, y0 + 12 + j * 18, lx + 24, y0 + 12 + j * 18], fill=colr, width=3); d.text((lx + 30, y0 + 4 + j * 18), name, fill="black", font=small)

panel(40, 320, -70, 5, "크기 (dB) — 바람·파도는 명령값(바람 0 dB = 최대 0.3), 귀 = 리스너에 실제로 닿은 전체(dBFS)",
      [("바람 명령 dB", "windDb", "#1f6fd1"), ("파도 명령 dB", "waveDb", "#13a39a"), ("귀 전체 dBFS", "earDbFS", "#222")], [0, -10, -20, -30, -40, -50, -60, -70])
panel(370, 560, 500, 22000, "로우패스 (Hz, 로그) — 실내일수록 낮음(먹먹)", [("로우패스 Hz", "cutoffHz", "#c2410c")], [800, 2000, 5000, 22000], log=True)
panel(610, 760, 0, 1, "판정 0~1 (0.7초에 걸쳐 섞음)", [("실내", "indoor", "#7c3aed"), ("골목", "alley", "#d97706"), ("트임", "open", "#16a34a"), ("카트 속도", "speed", "#dc2626")], [0, 0.5, 1])
# 아래: 장소 이름 띠
prev = None
for i, r in enumerate(rows):
    lab = r[col["label"]]
    if lab != prev: d.line([X(T[i]), 40, X(T[i]), 760], fill="#ddd"); d.text((X(T[i]) + 3, 768), lab, fill="#333", font=small); prev = lab
for tv in range(int(t0), int(t1) + 1, 10): d.text((X(tv) - 8, 795), str(tv - int(t0)) + "s", fill="#666", font=small)
img.save(out); print("saved", out, len(rows), "rows")
