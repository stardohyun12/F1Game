# 과제 1943 (D-146): 지금 아틀라스 → 노이즈 없는 저해상도 레트로 사본(*_retro.png). 원본은 건드리지 않는다.
#  칸(정사각 격자)마다: 중앙값으로 사진 점무늬 제거 → 큰 텍셀로 줄임(32px/m → 약 12px/m) → 2~4색으로 포스터라이즈
#  → 공용 팔레트에 맞춤 → 외톨이 텍셀 제거(최빈값) → Point 로 원래 크기로 키움(UV 그대로).
#  창틀·문·지붕 줄처럼 대비가 큰 선은 4색 안에 남고, 비슷한 색끼리의 얼룩은 한 색으로 합쳐진다.
#  지형 아틀라스(2×2 풀/흙/마른풀/바위)는 칸 평균색 큰 면 + 드문드문 그린 풀 가닥·금으로 새로 그린다.
# 실행: python -X utf8 tools/asset-gen/retro_clean.py   (PIL 만 씀)
import os, random
from PIL import Image, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), '..', '..', 'F1', 'Assets', 'Art')
# (경로, 칸 px). 1024 아틀라스 = 128px 칸 8×8(4m@32px/m), LofiAtlas 512 = 64px 칸
ATLASES = [(f'BayLoop/Buildings/{n}.png', 128) for n in ['BayLoopShared_01', 'BayLoopShared_02', 'BayLoopShared_03', 'BayLoopShared_04',
                                                          'DeferredPhotoAtlas', 'harbor_warehouse_Atlas', 'Italy_blue_Atlas', 'Italy_cream_Atlas',
                                                          'Italy_ochre_Atlas', 'TeaHousePhotoAtlas']] + \
          [(f'BayLoop/Landmarks/{n}.png', 128) for n in ['ChapelMarketPhotoAtlas', 'FishMarketPhotoAtlas', 'HarborPhotoAtlas']] + \
          [(f'BayLoop/Props/{n}.png', 128) for n in ['Props2FurnitureAtlas', 'Props2PavingAtlas', 'Props2PhotoAtlas', 'Props2RoadAtlas',
                                                      'Props2VehiclesAtlas', 'PropsPhotoAtlas', 'PropsPhotoAtlas_r2', 'PropsPhotoAtlas_r3', 'TeaHousePhotoAtlas']] + \
          [('BayLoop/People/PeoplePhotoAtlas.png', 128), ('BayLoop/Car/A_Atlas.png', 128), ('LofiPilot/LofiAtlas.png', 64),
           ('StreetFurniture/AsphaltAggregate.png', 256)]
TERRAIN = 'BayLoop/Terrain/BL_TerrainAtlas_32ppm.png'
# 줄이는 배율은 정수(한 픽셀 격자, D-123 규칙 3): 건물·소품·지형지물 4배(8px/m), 사람·차 2배(16px/m — 얼굴·차체 면이 뭉개지지 않게),
#  LofiAtlas 간판 2배, 아스팔트 4배. 화면 평균 ≈ 12px/m (가설)
DIV = {'People': 2, 'Car': 2, 'LofiPilot': 2}
MERGE = 20               # 두 색 거리(RGB)가 이보다 가까우면 한 색으로 — 사진 얼룩이 연한 무늬로 남는 것을 지운다(가설)
PALETTE_N = 48           # 공용 팔레트 색 수(가설)


def cell_colors(cell):
    """칸 대비로 색 수를 정한다: 거의 한 색이면 2, 선이 뚜렷하면 4."""
    lo, hi = cell.convert('L').getextrema()
    st = cell.convert('L').resize((8, 8), Image.BOX)
    spread = max(st.getdata()) - min(st.getdata())
    return 2 if hi - lo < 40 else (4 if spread > 70 else 3)


def clean_cell(cell, div):
    n = cell.size[0]
    small = max(4, n // div)
    c = cell.filter(ImageFilter.MedianFilter(3))   # 5 는 벽돌 줄까지 지웠다 → 3
    c = c.resize((small, small), Image.BOX)
    k = cell_colors(cell)
    q = c.quantize(colors=k, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert('RGB').filter(ImageFilter.ModeFilter(3))
    cols = sorted(q.getcolors(64) or [], reverse=True)   # (개수, 색) 많은 순
    rep = {}
    for i, (cn, col) in enumerate(cols):
        for cn2, col2 in cols[:i]:
            r2 = rep.get(col2, col2)
            if sum((a - b) ** 2 for a, b in zip(col, r2)) ** .5 < MERGE: rep[col] = r2; break
    if rep: q.putdata([rep.get(p, p) for p in q.getdata()])
    return q, small


def nearest(pal, col):
    return min(pal, key=lambda p: (p[0] - col[0]) ** 2 * 3 + (p[1] - col[1]) ** 2 * 4 + (p[2] - col[2]) ** 2 * 2)


def run():
    stage = []   # (경로, 칸 결과 목록)
    allcols = []
    for rel, cs in ATLASES:
        src = os.path.join(ROOT, rel)
        if not os.path.exists(src): print('없음', rel); continue
        if 'LofiAtlas' in rel:   # 이미 손으로 그린 깨끗한 도트(간판 글씨) — 손대면 글씨가 지워진다 → 그대로 사본
            Image.open(src).save(os.path.join(ROOT, rel[:-4] + '_retro.png')); print('copy', rel); continue
        im = Image.open(src).convert('RGBA'); W, H = im.size; cs = min(cs, W)
        cells = []
        for y in range(0, H, cs):
            for x in range(0, W, cs):
                cell = im.crop((x, y, x + cs, y + cs))
                rgb = cell.convert('RGB')
                small, sz = clean_cell(rgb, next((v for kk, v in DIV.items() if kk in rel), 4))
                cells.append((x, y, small, cell.getchannel('A')))
                allcols += [cnt_col[1] for cnt_col in small.getcolors(64) or []]
        stage.append((rel, im.size, cs, cells))
    # 공용 팔레트: 모든 칸 색을 한 줄로 모아 48색으로
    strip = Image.new('RGB', (len(allcols), 1)); strip.putdata(allcols)
    pq = strip.quantize(colors=PALETTE_N, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = [tuple(pq.getpalette()[i * 3:i * 3 + 3]) for i in range(PALETTE_N)]
    lut = {}
    for rel, (W, H), cs, cells in stage:
        out = Image.new('RGBA', (W, H))
        for x, y, small, alpha in cells:
            px = [lut.setdefault(p, nearest(pal, p)) for p in small.getdata()]
            small.putdata(px)
            big = small.resize((cs, cs), Image.NEAREST).convert('RGBA'); big.putalpha(alpha)
            out.paste(big, (x, y))
        dst = os.path.join(ROOT, rel[:-4] + '_retro.png'); out.save(dst)
        print('ok', rel, f'{len(cells)}칸', f'칸 {cs}px → {cells[0][2].size[0]}텍셀')
    terrain(pal)
    sw = Image.new('RGB', (PALETTE_N * 16, 16))
    for i, p in enumerate(pal): sw.paste(p, (i * 16, 0, i * 16 + 16, 16))
    sw.save(os.path.join(os.path.dirname(__file__), '..', '..', 'docs', 'qa', 'evidence', 'retro-v1', 'palette.png'))


def terrain(pal):
    """2×2(풀 soil / 마른풀 rock). 칸 = 64px = 16텍셀×4px(타일 1.86m → 약 8.6px/m). 평균색 큰 면 + 드문 가닥."""
    src = Image.open(os.path.join(ROOT, TERRAIN)).convert('RGB'); W = src.size[0]; cs = W // 2; T = 16; px = cs // T
    out = Image.new('RGB', (W, W))
    rnd = random.Random(1943)
    def avg(box):
        return src.crop(box).resize((1, 1), Image.BOX).getpixel((0, 0))
    def shade(c, f): return tuple(max(0, min(255, int(v * f))) for v in c)
    # 칸 위치: grass(0,0.5) soil(0.5,0.5) dry(0,0) rock(0.5,0) — uv y 위 = 이미지 위쪽
    cells = {'grass': (0, 0), 'soil': (cs, 0), 'dry': (0, cs), 'rock': (cs, cs)}
    for kind, (x0, y0) in cells.items():
        base = avg((x0, y0, x0 + cs, y0 + cs))
        if kind == 'grass': base = shade(base, 1.18)    # 풀은 사진 평균이 어둡다 → 조금 밝게(가설)
        if kind == 'rock': base = shade(base, 1.9)      # 사진 바위 평균(77,72,63)은 그늘에서 검은 덩어리(도현 4.png) → 밝은 회갈 석회암(가설)
        tile = Image.new('RGB', (T, T), base)
        if kind in ('grass', 'dry'):
            hi, lo = shade(base, 1.22), shade(base, .82)
            for _ in range(5):      # 풀 가닥: 세로 2~3텍셀, 밝은 끝 + 어두운 뿌리
                x, y = rnd.randrange(T), rnd.randrange(T); h = rnd.choice((2, 3))
                for d in range(h): tile.putpixel((x, (y + d) % T), hi if d == 0 else lo)
        elif kind == 'soil':
            for _ in range(4): tile.putpixel((rnd.randrange(T), rnd.randrange(T)), shade(base, .85))
        else:   # 바위: 짧은 금 두 줄
            for _ in range(2):
                x, y = rnd.randrange(T), rnd.randrange(T)
                for d in range(rnd.choice((3, 4))): tile.putpixel(((x + d) % T, (y + d // 2) % T), shade(base, .78))
            for _ in range(3): tile.putpixel((rnd.randrange(T), rnd.randrange(T)), shade(base, 1.12))
        out.paste(tile.resize((cs, cs), Image.NEAREST), (x0, y0))
    dst = os.path.join(ROOT, TERRAIN[:-4] + '_retro.png'); out.save(dst); print('ok', TERRAIN, f'칸 {T}텍셀×{px}px')


if __name__ == '__main__':
    os.makedirs(os.path.join(os.path.dirname(__file__), '..', '..', 'docs', 'qa', 'evidence', 'retro-v1'), exist_ok=True)
    import sys
    terrain(None) if 'terrain' in sys.argv else run()   # terrain = 지형 아틀라스만 다시
