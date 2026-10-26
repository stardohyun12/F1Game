"""장면 A V2 — A Short Hike 식 팔레트 텍스처 (아트2, 2026-10-27 과제 0412).
늦가을 CC0 사진 3장(Poly Haven 톤맵 JPG: autumn_field·autumn_road·autumn_hill_view) → 미디안 컷 40색 →
art/palette/autumn.png
  - 위: 단색 칸 8×8px, 8열 × 5줄 = 40색(밝기순)
  - 아래: 재질별 그라데이션 칸 8×32px(중간색 → 어두운색, 4계단 — 계단 명암) — 큰 면용
art/palette/palette.json — 재질(v5 텍스처 이름)마다 light/mid/dark 칸 번호와 UV 사각형.
  mid = 원래 텍스처 평균색에 가장 가까운 팔레트 색, light = 그 색 ×1.2, dark = ×(.62,.68,.82) — 그늘은 푸른 쪽(색 있는 그늘).
    python -X utf8 tools/asset-gen/opus_palette_v2.py
"""
import glob, json, os, sys
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
HD = os.path.join(ROOT, 'art', 'source', 'photos', 'scene-a', 'hdri')
TEX = os.path.join(ROOT, 'art', 'source', 'opus-scene-a-kit', 'tex')
OUT = os.path.join(ROOT, 'art', 'palette')
IDS = ('autumn_field', 'autumn_road', 'autumn_hill_view')
C, COLS, N = 8, 8, 40
# V2b (과제 0430, 인자 b): 따뜻한 색이 빠져 화면이 카키 한 색 → 빨강·주황·청록이 든 CC0 사진 4장을 더해 48색.
#   낙엽 ScatteredLeaves009 · 바랜 노랑+녹 PaintedMetal009 · 빨강 칠 PaintedMetal004 · 청록 칠+녹 PaintedMetal006 (ambientCG)
#   바닥류는 흰색 대신 황토·회갈 목표색으로, 밝은 칸 배율 1.2 → 1.12(바닥류 1.06).
B = sys.argv[1:2] == ['b']
EXTRA = ('ScatteredLeaves009', 'PaintedMetal009', 'PaintedMetal004', 'PaintedMetal006') if B else ()
PH = os.path.join(ROOT, 'art', 'source', 'photos', 'scene-a')
if B: N = 48
SUF = '_b' if B else ''
TARGET = dict(floor=(132, 118, 98), dirt=(178, 148, 100), ground=(150, 138, 86), stone=(140, 130, 116), apron=(170, 148, 108)) if B else {}
LIGHT_K = 1.12 if B else 1.2
KEEP = ('poster_a', 'poster_b', 'sign_shop', 'sign_road', 'sticker', 'record', 'slots', 'vend_front', 'window', 'parts', 'pegboard')  # 글자·그림 있는 데칼은 자기 텍스처


def lum(c): return .2126 * c[0] + .7152 * c[1] + .0722 * c[2]


def d2(a, b): return 2 * (a[0] - b[0]) ** 2 + 4 * (a[1] - b[1]) ** 2 + 3 * (a[2] - b[2]) ** 2


def main():
    os.makedirs(OUT, exist_ok=True)
    # 하늘이 반을 차지하면 팔레트가 하늘색·올리브로 쏠림 → 땅·나무 띠(45~80%)를 3배, 하늘 띠(25~45%)를 1배,
    # 땅·나무 띠는 채도 ×1.5(늦가을 잎·흙 색을 살림). 전부 같은 사진 픽셀에서 나온 색.
    from PIL import ImageEnhance
    strip = Image.new('RGB', (512 * 3, 256 * (4 + (len(EXTRA) + 2) // 3)))
    for j, a in enumerate(EXTRA + EXTRA[:(-len(EXTRA)) % 3]):  # 빈 칸(검정)이 팔레트를 끌지 않게 앞 사진으로 채움
        strip.paste(Image.open(os.path.join(PH, a, f'{a}_1K-JPG_Color.jpg')).convert('RGB').resize((512, 256), Image.LANCZOS),
                    ((j % 3) * 512, (4 + j // 3) * 256))
    for i, a in enumerate(IDS):
        im = Image.open(os.path.join(HD, a + '.jpg')).convert('RGB'); h = im.height
        g = ImageEnhance.Color(im.crop((0, int(h * .45), im.width, int(h * .8)))).enhance(1.5).resize((512, 256), Image.LANCZOS)
        for r in range(3): strip.paste(g, (i * 512, r * 256))
        strip.paste(im.crop((0, int(h * .25), im.width, int(h * .45))).resize((512, 256), Image.LANCZOS), (i * 512, 768))
    def mc(im, n):
        p = im.quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).getpalette()[:n * 3]
        return [tuple(p[i:i + 3]) for i in range(0, n * 3, 3)]
    if EXTRA:  # 한 통에 섞으면 넓은 올리브가 빨강·청록을 삼킴 → 풍경 32색 + 따뜻한 사진 16색을 따로 뽑아 합침
        pal = mc(strip.crop((0, 0, 1536, 1024)), N - 16) + mc(strip.crop((0, 1024, 1536, strip.height)), 16)
    else:
        pal = mc(strip, N)
    pal = sorted(pal, key=lum)
    mats = sorted(os.path.splitext(os.path.basename(p))[0] for p in glob.glob(os.path.join(TEX, '*.png')))
    mats = [m for m in mats if m not in KEEP]
    GH = 32; rows_g = (len(mats) + COLS - 1) // COLS
    W, H = COLS * C, (N // COLS) * C + rows_g * GH
    out = Image.new('RGB', (W, H), (0, 0, 0)); px = out.load()
    for i, c in enumerate(pal):
        x0, y0 = (i % COLS) * C, (i // COLS) * C
        for y in range(y0, y0 + C):
            for x in range(x0, x0 + C): px[x, y] = c
    near = lambda t, skip=(): min((j for j in range(N) if j not in skip), key=lambda j: d2(pal[j], t))
    cell = lambda j: [(j % COLS) * C, (j // COLS) * C, C, C]  # x, y(위에서), w, h — 픽셀
    info = dict(image=f'autumn{SUF}.png', size=[W, H], cell=C, colors=[list(c) for c in pal], sources=list(IDS + EXTRA), mats={})
    for k, m in enumerate(mats):
        im = Image.open(os.path.join(TEX, m + '.png')).convert('RGB').resize((1, 1), Image.BOX)
        avg = im.getpixel((0, 0))
        mid = near(TARGET.get(m, avg))
        lk = 1.06 if m in TARGET else LIGHT_K
        if B:  # 밝은 칸은 mid 보다 밝은 색 중 색상(비율)이 가장 비슷한 것 — 초록 쪽으로 새지 않게
            up = [j for j in range(N) if j != mid and lum(pal[mid]) + 6 < lum(pal[j]) < lum(pal[mid]) * lk + 30] or [mid]
            ratio = lambda c: [v / max(sum(c), 1) for v in c]
            hd = lambda j: sum((a - b) ** 2 for a, b in zip(ratio(pal[j]), ratio(pal[mid])))
            light = min(up, key=hd)
            if m in TARGET or hd(light) > .001: light = mid  # 같은 색상의 밝은 색이 없으면(또는 바닥류) 위 면도 mid — 초록·노랑으로 새는 것 막음
        else:
            light = near(tuple(min(255, v * lk) for v in pal[mid]), (mid,))
        dark = near(tuple(v * f for v, f in zip(pal[mid], (.62, .68, .82))), (mid, light))
        gx, gy = (k % COLS) * C, (N // COLS) * C + (k // COLS) * GH
        steps = [pal[light], pal[mid], tuple((a + b) // 2 for a, b in zip(pal[mid], pal[dark])), pal[dark]]
        for y in range(GH):
            for x in range(C): px[gx + x, gy + y] = steps[y * 4 // GH]
        info['mats'][m] = dict(avg=list(avg), light=light, mid=mid, dark=dark,
                               light_px=cell(light), mid_px=cell(mid), dark_px=cell(dark), grad_px=[gx, gy, C, GH])
    out.save(os.path.join(OUT, f'autumn{SUF}.png'))
    out.resize((W * 4, H * 4), Image.NEAREST).save(os.path.join(OUT, f'autumn{SUF}_x4.png'))
    info['keep'] = list(KEEP)
    json.dump(info, open(os.path.join(OUT, f'palette{SUF}.json'), 'w'), indent=1)
    print('palette', W, H, 'mats', len(mats), 'keep', len(KEEP))


if __name__ == '__main__':
    main()
