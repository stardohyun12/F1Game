"""규칙 3·4·5 잡음/색 검사 (EDC 분석 _analysis/regions.py + measure.py 를 옮김). PIL 만 쓴다(numpy 없음).

내부 해상도 판(480×270 `*_raw480.png`, 또는 큰 캡처를 정수배 표본)에서 같은 재질 영역을 잘라
(a) 1px 반점 % = 3×3 중앙값에서 휘도 ±12 넘게 튄 픽셀
(b) 중간주파 = 가우시안 0.7−3 차의 std (2~8px 무늬)
(c) 저주파   = 가우시안 3−12 차의 std (8~40px 얼룩)
(d) 1px 색잡음 = 색차(R−G, B−G) − 3×3 평균 의 std
장면 전체: 채도 평균 · 고채도(>0.5) % · 색 칸(4bit) cover90 · 휘도 p5~p95 폭.

    python -X utf8 tools/qa/surface_noise.py              # EDC 기준 + docs/qa/evidence/look5-edc
    python -X utf8 tools/qa/surface_noise.py --dir <폴더>
"""
import argparse, math, os, sys
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
EDC = os.path.join(ROOT, 'art', 'reference', 'edc-2026-10-26')
EDC_SCALE = {'edc_02': 4.24, 'edc_03': 4.85, 'edc_04': 4.22, 'edc_06': 4.24, 'edc_09': 5.63}

# 한계 (분석 문서 §6 규칙 3·4·5)
LIM = dict(speck=0.3, ratio=0.6, cnoise=1.0, low=(3, 10), sat=(0.08, 0.15), hisat=5.0, cover90=20, cover90_town=35, lumw=110)

# (이름, 파일 키, 상자 x0,y0,x1,y1) — EDC 는 456×256 내부판, 우리는 480×270 내부판
EDC_REGIONS = [
    ('EDC02 도로(눈길)', 'edc_02', (5, 140, 150, 250)),
    ('EDC06 도로', 'edc_06', (20, 185, 160, 250)),
    ('EDC06 눈 비탈', 'edc_06', (330, 150, 450, 250)),
    ('EDC04 붉은 벽', 'edc_04', (55, 0, 160, 50)),
    ('EDC04 눈길', 'edc_04', (280, 160, 450, 250)),
    ('EDC03 실내 바닥', 'edc_03', (150, 150, 260, 220)),
]
OUR_REGIONS = [   # retro-v1 640×360 상자 × 0.75 (벽은 창 없는 아래쪽으로 옮김)
    ('도로', 'a_car', (112, 210, 176, 240)),
    ('풀', 'a_car', (0, 143, 112, 180)),
    ('인도', 'a_car', (431, 169, 476, 206)),
    ('벽', 'b_street', (5, 135, 70, 190)),
]


def native(path, p):
    """p 배 확대 캡처 → 내부판 RGB Image. 각 칸 중심의 3×3 중앙값(JPEG 링잉 제거, native.py 와 같음)."""
    im = Image.open(path).convert('RGB')
    if p <= 1.01:
        return im
    med = im.filter(ImageFilter.MedianFilter(3))
    W, H = im.size
    nx, ny = int(W / p), int(H / p)
    out = Image.new('RGB', (nx, ny))
    src = med.load(); dst = out.load()
    for y in range(ny):
        sy = min(int((y + .5) * p), H - 1)
        for x in range(nx):
            dst[x, y] = src[min(int((x + .5) * p), W - 1), sy]
    return out


def std(v):
    m = sum(v) / len(v)
    return math.sqrt(sum((x - m) ** 2 for x in v) / len(v))


def blur(g, w, h, s):
    """가우시안(분리, 가장자리 복제). g = 길이 w*h 리스트."""
    r = max(1, int(math.ceil(3 * s)))
    k = [math.exp(-(i * i) / (2 * s * s)) for i in range(-r, r + 1)]
    t = sum(k); k = [x / t for x in k]
    tmp = [0.0] * (w * h)
    for y in range(h):
        row = g[y * w:(y + 1) * w]
        for x in range(w):
            tmp[y * w + x] = sum(k[j] * row[min(max(x + j - r, 0), w - 1)] for j in range(2 * r + 1))
    out = [0.0] * (w * h)
    for x in range(w):
        for y in range(h):
            out[y * w + x] = sum(k[j] * tmp[min(max(y + j - r, 0), h - 1) * w + x] for j in range(2 * r + 1))
    return out


def inner(v, w, h, m):
    return [v[y * w + x] for y in range(m, h - m) for x in range(m, w - m)]


def satv(c):
    mx = max(c); mn = min(c)
    return (mx - mn) / mx if mx > 0 else 0.0


def region(im, box):
    c = im.crop(box); w, h = c.size
    px = list(c.getdata())
    L = [.2126 * r + .7152 * g + .0722 * b for r, g, b in px]
    Li = Image.new('L', (w, h)); Li.putdata([min(255, int(x)) for x in L])
    med = list(Li.filter(ImageFilter.MedianFilter(3)).getdata())
    spk = inner([1.0 if abs(L[i] - med[i]) > 12 else 0.0 for i in range(w * h)], w, h, 1)
    g07, g3, g12 = blur(L, w, h, .7), blur(L, w, h, 3), blur(L, w, h, 12)
    mid = std(inner([g07[i] - g3[i] for i in range(w * h)], w, h, 2))
    low = std(inner([g3[i] - g12[i] for i in range(w * h)], w, h, 2))
    cn = []
    for ch in ([r - g for r, g, b in px], [b - g for r, g, b in px]):
        d = []
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                m = sum(ch[(y + dy) * w + x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1)) / 9
                d.append(ch[y * w + x] - m)
        cn.append(std(d))
    return dict(L=sum(L) / len(L), speck=100 * sum(spk) / len(spk), mid=mid, low=low, ratio=mid / max(low, 1e-6),
                cnoise=sum(cn) / 2, sat=sum(satv(p) for p in px) / len(px))


def scene(im):
    px = list(im.getdata())
    s = [satv(p) for p in px]
    cnt = {}
    for r, g, b in px:
        k = (r // 16, g // 16, b // 16); cnt[k] = cnt.get(k, 0) + 1
    acc = 0; cover90 = 0
    for v in sorted(cnt.values(), reverse=True):
        acc += v; cover90 += 1
        if acc >= .9 * len(px): break
    L = sorted(.2126 * r + .7152 * g + .0722 * b for r, g, b in px)
    return dict(sat=sum(s) / len(s), hisat=100 * sum(1 for x in s if x > .5) / len(s), cover90=cover90,
                p5=L[int(.05 * len(L))], p95=L[int(.95 * len(L))])


def ok(v, lim):
    if isinstance(lim, tuple):
        return 'O' if lim[0] <= v <= lim[1] else 'X'
    return 'O' if v <= lim else 'X'


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dir', default=os.path.join(ROOT, 'docs', 'qa', 'evidence', 'look5-edc'))
    d = ap.parse_args().dir

    def load(key, tag):
        raw = os.path.join(d, f'{key}_{tag}_raw480.png')
        if os.path.exists(raw):
            return native(raw, 1)
        big = os.path.join(d, f'{key}_{tag}.png')
        return native(big, Image.open(big).width / 480) if os.path.exists(big) else None

    print('## 표면 잡음 (규칙 3) — 한계: 반점 ≤0.3% · 중/저 ≤0.6 · 색잡음 ≤1.0 · 저주파 3~10')
    print('영역 | 휘도 | 반점% | 중간 | 저주파 | 중/저 | 색잡음 | 채도 | 판정(반점·비·색·저)')
    cache = {}
    rows = [(n, ('edc', k), b) for n, k, b in EDC_REGIONS]
    for tag in ('look0', 'look5'):
        rows += [(f'우리 L{tag[-1]} {n}', (k, tag), b) for n, k, b in OUR_REGIONS]
    for name, src, box in rows:
        if src not in cache:
            cache[src] = native(os.path.join(EDC, src[1] + '.jpg'), EDC_SCALE[src[1]]) if src[0] == 'edc' else load(*src)
        im = cache[src]
        if im is None:
            print(f'{name} | (캡처 없음)'); continue
        r = region(im, box)
        v = ''.join(ok(r[k], LIM[k]) for k in ('speck', 'ratio', 'cnoise', 'low'))
        print(f"{name} | {r['L']:.0f} | {r['speck']:.2f} | {r['mid']:.2f} | {r['low']:.2f} | {r['ratio']:.2f} | {r['cnoise']:.2f} | {r['sat']:.3f} | {v}")

    print()
    print('## 장면 색 (규칙 4·5) — 한계: 채도 0.08~0.15 · 고채도 ≤5% · cover90 ≤20(마을 ≤35) · 휘도 폭 ≤110')
    print('장면 | 채도 | 고채도% | cover90 | 휘도 p5~p95(폭) | 판정(채도·고채도·색칸·폭)')
    items = [('EDC ' + k, os.path.join(EDC, k + '.jpg'), s, False) for k, s in EDC_SCALE.items()]
    for f in sorted(os.listdir(d)):
        town = f.startswith(('b_street', 'c_town'))
        if f.endswith('_raw480.png'):
            items.append((f[:-11], os.path.join(d, f), 1, town))
        elif f.endswith('_look0.png'):
            items.append((f[:-4], os.path.join(d, f), Image.open(os.path.join(d, f)).width / 480, town))
    for name, path, p, town in items:
        s = scene(native(path, p)); w = s['p95'] - s['p5']
        v = ok(s['sat'], LIM['sat']) + ok(s['hisat'], LIM['hisat']) + ok(s['cover90'], LIM['cover90_town'] if town else LIM['cover90']) + ok(w, LIM['lumw'])
        print(f"{name} | {s['sat']:.3f} | {s['hisat']:.1f} | {s['cover90']} | {s['p5']:.0f}~{s['p95']:.0f}({w:.0f}) | {v}")


if __name__ == '__main__':
    sys.exit(main())
