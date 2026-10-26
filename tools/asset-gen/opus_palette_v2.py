"""장면 A V2 — A Short Hike 식 팔레트 텍스처 (아트2, 2026-10-27 과제 0412).
늦가을 CC0 사진 3장(Poly Haven 톤맵 JPG: autumn_field·autumn_road·autumn_hill_view) → 미디안 컷 40색 →
art/palette/autumn.png
  - 위: 단색 칸 8×8px, 8열 × 5줄 = 40색(밝기순)
  - 아래: 재질별 그라데이션 칸 8×32px(중간색 → 어두운색, 4계단 — 계단 명암) — 큰 면용
art/palette/palette.json — 재질(v5 텍스처 이름)마다 light/mid/dark 칸 번호와 UV 사각형.
  mid = 원래 텍스처 평균색에 가장 가까운 팔레트 색, light = 그 색 ×1.2, dark = ×(.62,.68,.82) — 그늘은 푸른 쪽(색 있는 그늘).
    python -X utf8 tools/asset-gen/opus_palette_v2.py
"""
import glob, json, os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
HD = os.path.join(ROOT, 'art', 'source', 'photos', 'scene-a', 'hdri')
TEX = os.path.join(ROOT, 'art', 'source', 'opus-scene-a-kit', 'tex')
OUT = os.path.join(ROOT, 'art', 'palette')
IDS = ('autumn_field', 'autumn_road', 'autumn_hill_view')
C, COLS, N = 8, 8, 40
KEEP = ('poster_a', 'poster_b', 'sign_shop', 'sign_road', 'sticker', 'record', 'slots', 'vend_front', 'window', 'parts', 'pegboard')  # 글자·그림 있는 데칼은 자기 텍스처


def lum(c): return .2126 * c[0] + .7152 * c[1] + .0722 * c[2]


def d2(a, b): return 2 * (a[0] - b[0]) ** 2 + 4 * (a[1] - b[1]) ** 2 + 3 * (a[2] - b[2]) ** 2


def main():
    os.makedirs(OUT, exist_ok=True)
    # 하늘이 반을 차지하면 팔레트가 하늘색·올리브로 쏠림 → 땅·나무 띠(45~80%)를 3배, 하늘 띠(25~45%)를 1배,
    # 땅·나무 띠는 채도 ×1.5(늦가을 잎·흙 색을 살림). 전부 같은 사진 픽셀에서 나온 색.
    from PIL import ImageEnhance
    strip = Image.new('RGB', (512 * 3, 256 * 4))
    for i, a in enumerate(IDS):
        im = Image.open(os.path.join(HD, a + '.jpg')).convert('RGB'); h = im.height
        g = ImageEnhance.Color(im.crop((0, int(h * .45), im.width, int(h * .8)))).enhance(1.5).resize((512, 256), Image.LANCZOS)
        for r in range(3): strip.paste(g, (i * 512, r * 256))
        strip.paste(im.crop((0, int(h * .25), im.width, int(h * .45))).resize((512, 256), Image.LANCZOS), (i * 512, 768))
    q = strip.quantize(colors=N, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = q.getpalette()[:N * 3]; pal = sorted([tuple(pal[i:i + 3]) for i in range(0, N * 3, 3)], key=lum)
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
    info = dict(image='autumn.png', size=[W, H], cell=C, colors=[list(c) for c in pal], sources=list(IDS), mats={})
    for k, m in enumerate(mats):
        im = Image.open(os.path.join(TEX, m + '.png')).convert('RGB').resize((1, 1), Image.BOX)
        avg = im.getpixel((0, 0))
        mid = near(avg)
        light = near(tuple(min(255, v * 1.2) for v in pal[mid]), (mid,))
        dark = near(tuple(v * f for v, f in zip(pal[mid], (.62, .68, .82))), (mid, light))
        gx, gy = (k % COLS) * C, (N // COLS) * C + (k // COLS) * GH
        steps = [pal[light], pal[mid], tuple((a + b) // 2 for a, b in zip(pal[mid], pal[dark])), pal[dark]]
        for y in range(GH):
            for x in range(C): px[gx + x, gy + y] = steps[y * 4 // GH]
        info['mats'][m] = dict(avg=list(avg), light=light, mid=mid, dark=dark,
                               light_px=cell(light), mid_px=cell(mid), dark_px=cell(dark), grad_px=[gx, gy, C, GH])
    out.save(os.path.join(OUT, 'autumn.png'))
    out.resize((W * 4, H * 4), Image.NEAREST).save(os.path.join(OUT, 'autumn_x4.png'))
    info['keep'] = list(KEEP)
    json.dump(info, open(os.path.join(OUT, 'palette.json'), 'w'), indent=1)
    print('palette', W, H, 'mats', len(mats), 'keep', len(KEEP))


if __name__ == '__main__':
    main()
