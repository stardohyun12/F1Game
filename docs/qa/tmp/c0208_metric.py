"""과제 0208: 차 대표 영역 평균색 비교 (Blender 렌더 vs 게임 캡처). numpy 없이 Pillow 만.
차 픽셀 = Blender overlay 는 alpha>250, 게임은 _mask.png 에서 마젠타가 아닌 곳.
영역 = 주황(r 이 g·b 보다 확실히 큼) / 아이보리(밝고 무채) / 회색 중간톤. 평균 sRGB 와 CIE76 ΔE.
usage: python c0208_metric.py <blender_overlay.png> <game.png> <game_mask.png>
"""
import sys
from PIL import Image


def lab(c):
    def lin(v):
        v /= 255.0
        return ((v + 0.055) / 1.055) ** 2.4 if v > 0.04045 else v / 12.92
    r, g, b = (lin(float(x)) for x in c)
    x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883
    f = lambda t: t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    return (116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z)))


def classify(p):
    r, g, b = p[:3]
    mx, mn = max(r, g, b), min(r, g, b)
    sat = (mx - mn) / max(mx, 1)
    if r > g + 25 and r > b + 35 and sat > 0.3:
        return "orange"
    if mn > 120 and sat < 0.3:
        return "ivory"
    if mx > 45 and sat < 0.25:
        return "gray"
    return None


def regions(pixels):
    acc = {k: [0, 0, 0, 0] for k in ("orange", "ivory", "gray", "all")}
    for p in pixels:
        for k in (classify(p), "all"):
            if k:
                a = acc[k]; a[0] += p[0]; a[1] += p[1]; a[2] += p[2]; a[3] += 1
    n = acc["all"][3]
    return {k: ((a[0] / a[3], a[1] / a[3], a[2] / a[3]) if a[3] > 50 else None, a[3], a[3] / max(n, 1)) for k, a in acc.items()}


ref = list(Image.open(sys.argv[1]).convert("RGBA").getdata())
game = Image.open(sys.argv[2]).convert("RGB").getdata()
mask = Image.open(sys.argv[3]).convert("RGB").getdata()
inside = [not (m[0] > 240 and m[1] < 20 and m[2] > 240) for m in mask]
gpx = [p for p, k in zip(game, inside) if k]
if all(p[3] > 250 for p in ref[:2000]):
    # 투명 배경이 없는 렌더(threequarter 등): 같은 정사영 카메라라 게임 마스크를 그대로 쓴다 (가장자리 1~2px 어긋남 감안)
    refpx = [p for p, k in zip(ref, inside) if k]
else:
    refpx = [p for p in ref if p[3] > 250]
R, G = regions(refpx), regions(gpx)
tot = 0.0
for k in ("orange", "ivory", "gray", "all"):
    a, b = R[k][0], G[k][0]
    if a is None or b is None:
        print(f"{k:7s} ref={R[k][1]}px game={G[k][1]}px (skip)")
        continue
    la, lb = lab(a), lab(b)
    de = sum((u - v) ** 2 for u, v in zip(la, lb)) ** 0.5
    print(f"{k:7s} ref RGB {tuple(round(x) for x in a)} ({R[k][2]:.0%})  game RGB {tuple(round(x) for x in b)} ({G[k][2]:.0%})  dE76={de:.1f}  L {la[0]:.0f}->{lb[0]:.0f}")
