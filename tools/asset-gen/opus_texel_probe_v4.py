"""v4 텍셀 실측: _probe/*.png(모든 텍스처를 1텍셀 흑백 체커로 바꾼 1280×720 렌더)를 게임처럼 1920×1080 Nearest 1.5배 →
구역마다 가로·세로로 '같은 색이 이어지는 길이'의 중간값 = 1080 화면에서 텍셀 1개의 크기(px). 기준 ①⑥⑫ 칸(4~5px)과 비교.

    python -X utf8 tools/asset-gen/opus_texel_probe_v4.py
"""
import json, os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-garage-kart-opus-v4')
# 구역(1080 좌표 x0, y0, x1, y1) — _probe 를 눈으로 보고 정함
REG = {
    'x1_game': {'카트 몸체·짐': (860, 800, 1060, 980), '정비소 지붕(앞)': (300, 40, 1300, 200),
                '정비소 안 뒷벽(약 15m)': (350, 300, 1000, 500), '앞 판자벽 오른쪽': (1250, 300, 1500, 700),
                '바닥 가까이(5~7m)': (100, 980, 1820, 1075)},
    'g2_34': {'정비소 지붕': (500, 230, 1300, 380), '정비소 옆벽': (300, 450, 700, 650),
              '카트(약 16m)': (1250, 600, 1420, 700), '마당 바닥': (900, 900, 1700, 1075)},
}


def runs(px, box, axis):
    x0, y0, x1, y1 = box; out = []
    outer = range(y0, y1, 2) if axis == 0 else range(x0, x1, 2)
    for j in outer:
        n, prev = 0, None
        for i in (range(x0, x1) if axis == 0 else range(y0, y1)):
            v = px[i, j] if axis == 0 else px[j, i]
            if v[3] == 0: prev, n = None, 0; continue
            b = sum(v[:3]) > 300
            if prev is None or b == prev: n += 1
            else:
                out.append(n); n = 1
            prev = b
    out = [r for r in out if r < 40]; out.sort()
    return round(out[len(out) // 2], 1) if out else None, len(out)


def main():
    rep = {}
    for view, regs in REG.items():
        im = Image.open(os.path.join(OUT, '_probe', view + '.png')).convert('RGBA')
        im = im.resize((1920, 1080), Image.NEAREST); px = im.load(); rep[view] = {}
        for name, box in regs.items():
            (h, nh), (v, nv) = runs(px, box, 0), runs(px, box, 1)
            rep[view][name] = {'가로 px': h, '세로 px': v, 'n': nh + nv}
            print(f'{view:8s} {name:20s} 가로 {h}  세로 {v}  (런 {nh + nv})')
    json.dump(rep, open(os.path.join(OUT, 'texel_probe.json'), 'w', encoding='utf8'), ensure_ascii=False, indent=1)


if __name__ == '__main__':
    main()
