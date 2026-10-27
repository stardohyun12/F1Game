"""건물 밝히기 후처리 — 여섯 장을 함께 보고 공용 32색 팔레트(median cut)를 만들어 디더 없이 줄이고, 전후 밝기·1px 반점을 잰다 (아트2, 과제 1134). PIL 만.
    python tools/asset-gen/opus_bldg_light_post.py
밝기 L = 0.299R + 0.587G + 0.114B (0~255). '전체' = 텍스처 전 픽셀 평균(PD 측정과 같은 방식), '벽' 등 = opus_bldg_light.py 가 남긴 면 분류 지도로 고른 픽셀.
1px 반점 = 위·아래·왼·오른 네 이웃과 모두 색이 다른 픽셀의 비율.
"""
import json, os, struct
from PIL import Image, ImageChops

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
OUT = J('art', 'export', 'scene-a2-bldg-light'); WORK = J('art', 'models', 'opus_bldg_light')
PAIRS = [('far_town', 'far_town_baked32.png', J('art', 'export', 'scene-a2-far', 'far_town_baked32.png')),
         ('far_terrace', 'far_terrace_baked32.png', J('art', 'export', 'scene-a2-far', 'far_terrace_baked32.png'))] + \
        [(f'background_building_0{i}', f'background_building_0{i}.png', J('art', 'export', 'scene-a2-kit', f'background_building_0{i}.png')) for i in range(1, 5)]
CLS = {1: 'wall', 2: 'roof', 3: 'window', 4: 'flat', 5: 'ter_top', 6: 'ter_wall'}


def load_npy_u8(p):  # numpy 없이 uint8 2차원 .npy 읽기
    b = open(p, 'rb').read(); hl = struct.unpack('<H', b[8:10])[0]; hdr = b[10:10 + hl].decode()
    shape = tuple(int(v) for v in hdr.split('(')[1].split(')')[0].split(',') if v.strip())
    return Image.frombytes('L', (shape[1], shape[0]), b[10 + hl:10 + hl + shape[0] * shape[1]])


def luma(im, mask=None):
    g = im.convert('L')
    h = g.histogram(mask)
    n = sum(h); return round(sum(i * c for i, c in enumerate(h)) / n, 1) if n else None, n


def mean_rgb(im):
    st = [im.getchannel(k).histogram() for k in range(3)]; n = im.width * im.height
    return [round(sum(i * c for i, c in enumerate(h)) / n) for h in st]


def speckle(im):
    im = im.convert('RGB'); w, h = im.size
    diff = lambda dx, dy: ImageChops.difference(im, ImageChops.offset(im, dx, dy)).convert('L').point(lambda v: 255 if v else 0)
    m = diff(1, 0)
    for d in ((-1, 0), (0, 1), (0, -1)): m = ImageChops.multiply(m, diff(*d))
    return round(m.histogram()[255] / (w * h) * 100, 2)


raw = {k: Image.open(os.path.join(OUT, f)).convert('RGB') for k, f, _ in PAIRS}
# 공용 팔레트: 각 장을 폭 256 으로 줄여(가장 가까운 점) 세로로 이어 median cut
thumbs = [im.resize((256, max(1, im.height * 256 // im.width)), Image.NEAREST) for im in raw.values()]
strip = Image.new('RGB', (256, sum(t.height for t in thumbs))); y = 0
for t in thumbs: strip.paste(t, (0, y)); y += t.height
pal_img = strip.quantize(32, method=Image.Quantize.MEDIANCUT)
pal = pal_img.getpalette()[:96]
open(os.path.join(OUT, 'palette32.gpl'), 'w').write('GIMP Palette\nName: scene-a2-bldg-light\nColumns: 8\n#\n' +
                                                     ''.join(f'{pal[i]} {pal[i + 1]} {pal[i + 2]} c{i // 3}\n' for i in range(0, 96, 3)))
res = {}
for k, f, before_p in PAIRS:
    q = raw[k].quantize(palette=pal_img, dither=Image.Dither.NONE).convert('RGB'); q.save(os.path.join(OUT, f))
    b = Image.open(before_p).convert('RGB'); cm = load_npy_u8(os.path.join(WORK, f'{k}_cls.npy'))
    cls = {}
    for cid, nm in CLS.items():
        L, n = luma(q, cm.point(lambda v, c=cid: 255 if v == c else 0))
        if n: cls[nm] = dict(L=L, px_pct=round(n / (q.width * q.height) * 100, 1))
    res[k] = dict(before=dict(file=os.path.relpath(before_p, R).replace('\\', '/'), size=list(b.size), mean_rgb=mean_rgb(b), L=luma(b)[0], speckle_1px_pct=speckle(b),
                              colors=len(b.getcolors(1 << 20) or [])),
                  after=dict(file=f'art/export/scene-a2-bldg-light/{f}', size=list(q.size), mean_rgb=mean_rgb(q), L=luma(q)[0], speckle_1px_pct=speckle(q),
                             colors=len(q.getcolors(1 << 20) or []), by_class=cls))
    a = res[k]['after']; print(k, 'before', res[k]['before']['mean_rgb'], res[k]['before']['L'], 'sp', res[k]['before']['speckle_1px_pct'],
                                '| after', a['mean_rgb'], a['L'], 'sp', a['speckle_1px_pct'], {c: v['L'] for c, v in cls.items()}, flush=True)
json.dump(dict(luma='0.299R+0.587G+0.114B', palette='art/export/scene-a2-bldg-light/palette32.gpl (median cut, 6장 공용)', files=res),
          open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
kp = os.path.join(OUT, 'kit.json'); kit = json.load(open(kp, encoding='utf-8')); kit['palette'] = 'palette32.gpl'; kit['measure'] = 'measure.json'
for f in kit['files']: f['L_before'], f['L_after'] = res[f['name']]['before']['L'], res[f['name']]['after']['L']
json.dump(kit, open(kp, 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
