"""장면 A V1 — EDC 식 포토배싱 텍스처 (아트2, 2026-10-27 과제 0412, 사양 docs/design/ART-TRIAL-0427.md).
CC0 사진(art/source/photos/scene-a/, opus_cc0_fetch.py 로 받음) →
  1) 조명 평탄화: 원본 ÷ 큰 가우시안(폭의 1/8) × 평균 — 사진에 찍힌 명암을 지운다
  2) 잔디테일 제거: 메디안 5 → 가우시안 1.5
  3) 부드러운 축소(LANCZOS — Nearest 축소 금지: 반점이 생김) 중요물 256, 나머지 64~128
  4) 색 바꾸기(재칠)·명암 칠하기(위 밝게, 아래·모서리 어둡게, 처마 밑 그늘 띠) — EDC 는 그림자를 텍스처에 칠한다
  5) 8~16색 줄이기(디더 없음) → tools/qa/surface_noise.py 의 region() 으로 1px 반점 ≤ 0.3% 확인
월드 박스 투영 UV(v = 월드 z) 기준이라 벽 텍스처의 아래 줄 = 땅 높이.

    python -X utf8 tools/asset-gen/opus_tex_v1_photobash.py      (PIL 만 씀)
"""
import csv, datetime, hashlib, json, os, random, sys
from PIL import Image, ImageChops, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
PH = os.path.join(ROOT, 'art', 'source', 'photos', 'scene-a')
OUT = os.path.join(ROOT, 'art', 'source', 'scene-a-v1', 'tex')
sys.path.insert(0, os.path.join(ROOT, 'tools', 'qa'))
from surface_noise import region  # noqa: E402

PX_M = 32  # 텍셀 / m


def photo(a):
    return Image.open(os.path.join(PH, a, f'{a}_1K-JPG_Color.jpg')).convert('RGB')


LUM = lambda p: .2126 * p[0] + .7152 * p[1] + .0722 * p[2]


def put(size, data):
    o = Image.new('RGB', size); o.putdata([tuple(max(0, min(255, int(c + .5))) for c in p) for p in data]); return o


def flatten(im):
    im = im.resize((512, 512 * im.height // im.width), Image.LANCZOS)  # 512 로 먼저 부드럽게 줄여 계산량을 줄임
    a = list(im.getdata()); big = list(im.filter(ImageFilter.GaussianBlur(im.width / 8)).getdata())
    mean = sum(LUM(p) for p in a) / len(a)
    return put(im.size, [tuple(c * mean / max(LUM(b), 1) for c in p) for p, b in zip(a, big)])


def clean(im, w, h, crop=None, soft=False):
    """soft: 녹 줄무늬처럼 잔무늬가 센 사진 — 줄인 뒤 한 번 더 메디안 5(덩어리만 남김)."""
    if crop: im = im.crop(crop)
    im = flatten(im).filter(ImageFilter.MedianFilter(5)).filter(ImageFilter.GaussianBlur(1.5))
    im = im.resize((w, h), Image.LANCZOS)
    return im.filter(ImageFilter.MedianFilter(5)) if soft else im


def ribs(im, period, width, k):
    """골함석 골: 세로 띠(폭 width ≥ 2px — 1px 선은 반점으로 읽힘)를 칠함."""
    w = im.width; a = list(im.getdata())
    for i, p in enumerate(a):
        if (i % w) % period < width: a[i] = tuple(c * k for c in p)
    return put(im.size, a)


def tint(im, rgb, k=1.0, lift=0.0):
    """사진 밝기 무늬는 남기고 색을 rgb 쪽으로 (k=1 이면 완전히 칠함)."""
    a = list(im.getdata()); mean = max(sum(LUM(p) for p in a) / len(a), 1)
    out = []
    for p in a:
        s = LUM(p) / mean * (1 - lift) + lift
        out.append(tuple(c * (1 - k) + t * s * k for c, t in zip(p, rgb)))
    return put(im.size, out)


def rowmul(im, f):
    """f(y) → (r,g,b) 곱 — 행 단위로 곱한다."""
    w, h = im.size; a = list(im.getdata())
    for y in range(h):
        m = f(y)
        if m is None: continue
        for i in range(y * w, y * w + w): a[i] = tuple(c * k for c, k in zip(a[i], m))
    return put(im.size, a)


def shade_v(im, rows):
    """rows = [(v0, v1, 곱)] — v 는 아래=0 위=1(월드 z 비율). 그 사이를 곱한다(선형 보간 아님: 칠한 띠)."""
    h = im.height
    def f(y):
        m = [1, 1, 1]
        for v0, v1, k in rows:
            if int(round(h * (1 - v1))) <= y < int(round(h * (1 - v0))): m = [a * b for a, b in zip(m, k)]
        return m
    return rowmul(im, f)


def grad_v(im, top, bottom):
    h = im.height
    return rowmul(im, lambda y: tuple(a * (1 - y / (h - 1)) + b * y / (h - 1) for a, b in zip(top, bottom)))


def blots(im, seed, n, r, mul):
    """큰 얼룩(기름·물자국): 원 n 개를 곱하고 가장자리를 2px 흐림 — 1px 반점이 아니라 덩어리."""
    R = random.Random(seed); w, h = im.size
    m = Image.new('L', (w, h), 255); px = m.load()
    for _ in range(n):
        cx, cy, rr = R.uniform(0, w), R.uniform(0, h), R.uniform(r * .5, r)
        sx = R.uniform(.6, 1.6)
        for y in range(int(cy - rr), int(cy + rr) + 1):
            for x in range(int(cx - rr * sx), int(cx + rr * sx) + 1):
                if ((x - cx) / sx) ** 2 + (y - cy) ** 2 < rr * rr: px[x % w, y % h] = int(255 * mul)
    m = m.filter(ImageFilter.GaussianBlur(1.2))
    return ImageChops.multiply(im, m.convert('RGB'))


def quant(im, n, clean_px=1):
    """n 색(디더 없음). clean_px: 줄인 뒤 남은 1px 외톨이를 3×3 메디안으로 지우고 같은 팔레트로 다시 맞춤."""
    for m in range(n, 17):  # 메디안이 색을 줄이므로 최종 8색 이상이 될 때까지 늘림(최대 16)
        q = im.quantize(colors=m, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
        o = q.convert('RGB')
        for _ in range(clean_px):  # 0 = 안 함, 1~3 = 메디안 3 반복 횟수
            o = o.filter(ImageFilter.MedianFilter(3)).quantize(palette=q, dither=Image.Dither.NONE).convert('RGB')
        if len(o.getcolors(256) or []) >= 8: break
    return o


# 이름: (사진, 폭, 높이, 타일 m [u, v], 만들기)  — 타일 m = 픽셀 / PX_M (드럼·타이어는 둘레 맞춤 UV 라 타일 없음)
def build():
    T = {}
    # 녹슨 골함석(민트 칠 + 녹) — 벽 0~6m 한 장: 0~0.25m 흙 튄 띠, 2.95~3.2m 처마 밑 그늘, 위로 갈수록 밝게
    im = clean(photo('CorrugatedSteel007B'), 256, 192, (0, 0, 512, 384), soft=True)  # 반만 잘라 늘림
    im = ribs(tint(im.filter(ImageFilter.MedianFilter(7)), (118, 168, 158), .3), 8, 3, .88).filter(ImageFilter.MedianFilter(3))  # 민트 쪽으로 30% — 녹이 벽 전체를 덮지 않게
    im = grad_v(im,(1.04, 1.04, 1.02), (.88, .9, .92))
    im = shade_v(im, [(0, .04, (.78, .74, .7)), (2.95 / 6, 3.2 / 6, (.72, .8, .86)), (5.3 / 6, 1, (.8, .86, .9))])
    T['tin_wall'] = (quant(im, 10, 3), 'CorrugatedSteel007B')
    # 주황 칠 골함석 지붕: 회색 골함석에 주황을 칠하고 물자국 얼룩
    im = tint(clean(photo('CorrugatedSteel005'), 256, 256), (214, 112, 62), .85)
    T['tin_roof'] = (quant(blots(im, 3, 7, 26, .86), 12), 'CorrugatedSteel005')
    # 낡은 판자 벽(아래 1.2m) — 밝은 판자 / 짙은 판자. 아래 끝 흙 띠
    base = clean(photo('WoodSiding008'), 256, 96)
    im = shade_v(grad_v(base, (1.06, 1.04, 1.0), (.86, .84, .82)), [(0, .1, (.8, .76, .7))])
    T['planks'] = (quant(im, 12), 'WoodSiding008')
    T['planks_dark'] = (quant(tint(im, (110, 82, 62), .45), 10), 'WoodSiding008')
    # 칠 벗겨진 나무(기둥·보·전신주·작업대)
    T['wood'] = (quant(tint(clean(photo('PaintedWood008C'), 128, 128), (150, 110, 74), .35), 10), 'PaintedWood008C')
    T['crate'] = (quant(tint(clean(photo('PaintedWood008C'), 64, 64, (0, 0, 512, 512)), (188, 150, 98), .5), 8), 'PaintedWood008C')
    # 콘크리트 바닥 + 기름 얼룩(큰 덩어리)
    im = clean(photo('Concrete042A'), 256, 256)
    im = blots(blots(im, 11, 6, 22, .72), 12, 4, 12, .8)
    T['floor'] = (quant(im, 12), 'Concrete042A')
    # 흙길 + 자갈 / 마른 풀
    T['dirt'] = (quant(tint(clean(photo('Ground079S'), 256, 256), (196, 160, 112), .3), 14), 'Ground079S')
    T['ground'] = (quant(tint(blots(clean(photo('Ground037'), 256, 256), 21, 5, 30, .88), (170, 160, 92), .35), 14), 'Ground037')
    # 쇠(틀·기둥 레일): 짙은 녹 쇠 / 드럼(빨강·파랑 칠 + 녹) — 드럼은 둘레 UV(가로 = 한 바퀴, 세로 = 높이), 테 2줄 칠함
    T['metal'] = (quant(clean(photo('Metal021'), 128, 128), 10), 'Metal021')
    for nm, col in (('drum_red', (176, 70, 56)), ('drum_blue', (66, 118, 160))):
        im = tint(clean(photo('Metal041B'), 128, 64, (0, 0, 512, 256), soft=True), col, .7)
        im = shade_v(grad_v(im, (1.05, 1.05, 1.03), (.8, .8, .82)), [(.3, .34, (.75, .75, .75)), (.66, .7, (.75, .75, .75)), (0, .05, (.7, .66, .62))])
        T[nm] = (quant(im, 10, 3), 'Metal041B')
    T['drum_top'] = (quant(tint(clean(photo('Metal041B'), 64, 64, (256, 256, 512, 512), soft=True), (150, 150, 146), .5), 8, 3), 'Metal041B')
    # 타이어: 고무 사진 + 트레드 블록 칠(둘레 UV)
    im = clean(photo('Rubber004'), 128, 32); a = list(im.getdata())
    for i, p in enumerate(a):
        x, y = i % 128, i // 128
        k = .8 * (.55 if 4 <= y < 28 and x % 8 < 3 else 1) * (.7 if y < 3 or y >= 29 else 1)
        a[i] = tuple(c * k for c in p)
    T['tire'] = (quant(put(im.size, a), 8), 'Rubber004')
    return T


def main():
    os.makedirs(OUT, exist_ok=True)
    T = build(); tiles, report = {}, {}
    for nm, (im, src) in T.items():
        p = os.path.join(OUT, nm + '.png'); im.save(p)
        if nm not in ('drum_red', 'drum_blue', 'drum_top', 'tire', 'crate'): tiles[nm] = [im.width / PX_M, im.height / PX_M]
        r = region(im, (0, 0, im.width, im.height))
        report[nm] = dict(size=list(im.size), colors=len(im.getcolors(256) or []), speck=round(r['speck'], 3),
                          mid=round(r['mid'], 2), low=round(r['low'], 2), src=src, ok=r['speck'] <= .3)
    json.dump(tiles, open(os.path.join(OUT, 'tile_m.json'), 'w'), indent=1)
    json.dump(report, open(os.path.join(OUT, 'noise_report.json'), 'w'), indent=1, ensure_ascii=False)
    for k, v in report.items(): print(k, v)
    # provenance.csv — 지난번 scene_a_v1_ 줄은 지우고 새 해시로 다시 씀(다른 줄은 글자 그대로 둠)
    pv = os.path.join(ROOT, 'art', 'provenance.csv')
    keep = [l for l in open(pv, encoding='utf-8', newline='').readlines() if not l.startswith('scene_a_v1_')]
    open(pv, 'w', encoding='utf-8', newline='').writelines(keep)
    rows = []
    for nm, (im, src) in T.items():
        aid = f'scene_a_v1_{nm}'
        sj = json.load(open(os.path.join(PH, src, 'source.json')))
        path = f'art/source/scene-a-v1/tex/{nm}.png'
        rows.append([aid, path, sj['download_url'], 'CC0-1.0', datetime.date.today().isoformat(), 'opus_tex_v1_photobash.py', 'no',
                     hashlib.sha256(open(os.path.join(ROOT, path), 'rb').read()).hexdigest(),
                     f'ambientCG {src} color map; flatten+median+lanczos+{report[nm]["colors"]}색 no dither; painted shade bands/blots by script'])
    with open(pv, 'a', newline='', encoding='utf-8') as f: csv.writer(f).writerows(rows)
    print('provenance +', len(rows))


if __name__ == '__main__':
    main()
