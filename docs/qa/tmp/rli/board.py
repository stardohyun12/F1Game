"""전·후 비교판: 위 줄 = 전(before_*), 아래 줄 = 후, 칸 = 낮·저녁·밤. 같은 카메라. 칸마다 L(방 바닥+벽 평균 밝기)·전등 밖 L·R/B.
사용: board.py → docs/qa/evidence/room-light-inside/board.png + table.md"""
import json, subprocess, sys
from PIL import Image, ImageDraw, ImageFont
D = "D:/Github/F1Game/docs/qa/evidence/room-light-inside/"
names = [("before_day", "전 낮"), ("before_dusk", "전 저녁"), ("before_night", "전 밤"), ("day", "후 낮"), ("dusk", "후 저녁"), ("night", "후 밤")]
out = subprocess.run([sys.executable, "-X", "utf8", "D:/Github/F1Game/docs/qa/tmp/rli/measure.py"] + [D + n + ".png" for n, _ in names],
                     capture_output=True, text=True, encoding="utf-8").stdout
rows = [json.loads(l) for l in out.splitlines() if l.startswith("{")]
m = {r["png"][:-4]: r for r in rows}
W, H = 960, 540
board = Image.new("RGB", (W * 3, (H + 60) * 2), (16, 16, 16))
dr = ImageDraw.Draw(board)
try: font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 26)
except OSError: font = ImageFont.load_default()
for i, (n, label) in enumerate(names):
    x, y = (i % 3) * W, (i // 3) * (H + 60)
    board.paste(Image.open(D + n + ".png").convert("RGB").resize((W, H), Image.NEAREST), (x, y + 60))
    r = m[n]
    dr.text((x + 12, y + 14), f"{label}   L {r['all']['L']:.3f} · 전등 밖 {r['outLamp']['L']:.3f} · R/B {r['all']['R/B']:.2f} · 창 벽 밝음 {r['winWallLit%']:.0f}%", fill=(240, 240, 240), font=font)
board.save(D + "board.png")
t = ["# 방 안 빛 — 전·후 측정 (같은 카메라 (185.02, 33.02, -65.06) rot (50, 41, 0), 1× 1920×1080, 방 바닥+벽 14,745점)", "",
     "L = 0.299R+0.587G+0.114B 평균(0~1). 전등 밖 = 책상 전등에서 2.2 m 밖 7,955점. 검은 벽% = 벽 점 중 L<0.05. 창 벽 밝음% = 창 벽 점 중 L>0.45.",
     "⚠ 화면 바닥: 배경 순검정이 캡처에서 (28,28,28) = L 0.11 로 찍힌다 — 밤 전등 밖 0.124 는 그 위 +0.014.", ""]
for n, label in names:
    r = m[n]
    t.append(f"- {label}: L {r['all']['L']:.3f} rgb {r['all']['rgb']} R/B {r['all']['R/B']:.2f} · 전등 밖 L {r['outLamp']['L']:.3f} · 검은 벽 {r['blackWall%']:.1f}% · 창 벽 L {r['winWall']['L']:.3f} 밝음 {r['winWallLit%']:.1f}% · 벽 밝음 {r['wallLit%']:.1f}%")
dl, nl, no = m["day"]["all"]["L"], m["night"]["all"]["L"], m["night"]["outLamp"]["L"]
t += ["", f"- 후 낮/밤 비 = {dl / nl:.2f} (전체) · {m['day']['outLamp']['L'] / no:.2f} (전등 밖) · 전 = {m['before_day']['all']['L'] / m['before_night']['all']['L']:.2f} · {m['before_day']['outLamp']['L'] / m['before_night']['outLamp']['L']:.2f}"]
open(D + "table.md", "w", encoding="utf-8").write("\n".join(t) + "\n")
print("\n".join(t))
