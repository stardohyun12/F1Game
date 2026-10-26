"""장면 A V1b — V1 포토배싱 약점 고침 (아트2, 2026-10-27 과제 0430). V1 = opus_tex_v1_photobash.py 의 도구를 그대로 씀.
고친 것
  - 벽 바탕 = 바랜 칠(골함석 = 청록, 판자 = 크림) + 골·판자 결. 녹(Rust009 사진)은 모서리·이음매(2m 마다)·아래쪽에만,
    월드 0~3.2m 벽 면적의 ≤25% 가 되게 문턱을 자동으로 올림(실측 % 를 noise_report 에 남김).
  - 마당 길(path, 새 재질): 흙(Ground081) + 자갈(Gravel022) 덩어리 + 바퀴 자국 2줄.
  - floor 8색 이상(메디안 정리를 빼도 반점 ≤0.3% 인 쪽을 고름).
  - 드럼: 빨강 칠(PaintedMetal004)·청록 칠(PaintedMetal006) 사진, 테 근처에만 녹.
나머지(tin_roof·wood·crate·dirt·ground·metal·drum_top·tire)는 V1 그대로 다시 구움.
    python -X utf8 tools/asset-gen/opus_tex_v1b.py
"""
import datetime, hashlib, json, os, sys
from PIL import Image, ImageFilter
sys.path.insert(0, os.path.dirname(__file__))
import opus_tex_v1_photobash as V  # noqa: E402

ROOT, PH, PX_M = V.ROOT, V.PH, V.PX_M
OUT = os.path.join(ROOT, 'art', 'source', 'scene-a-v1b', 'tex')
WALL_H = 3.2  # 벽 높이(m) — 녹 면적은 이 아래만 셈


def field(src, w, h, blur, crop=None):
    """사진 밝기를 큰 얼룩 잡음 0..1 로(녹·자갈 자리 정하기용)."""
    im = V.photo(src)
    if crop: im = im.crop(crop)
    L = [V.LUM(p) for p in im.resize((w, h), Image.LANCZOS).filter(ImageFilter.GaussianBlur(blur)).getdata()]
    lo, hi = min(L), max(L); return [(v - lo) / max(hi - lo, 1) for v in L]


def composite(base, over, mask01, soft=1.0):
    m = Image.new('L', base.size); m.putdata([int(255 * v) for v in mask01])
    return Image.composite(over, base, m.filter(ImageFilter.GaussianBlur(soft)) if soft else m)


def rust_mask(w, h, near, target=.2, cap=.25, seed='Ground081'):
    """near(x, y) = 0..1 '녹이 생길 자리'(모서리·이음매·아래). 잡음과 섞어 문턱 넘는 곳만 녹. 0~WALL_H 면적 비율 ≤ cap."""
    N = field(seed, w, h, 3)
    S = [N[y * w + x] * .5 + near(x, y) * .8 for y in range(h) for x in range(w)]
    rows = [y for y in range(h) if (h - y - .5) / PX_M < WALL_H] if h / PX_M > WALL_H else list(range(h))
    t = .6
    while True:
        M = [1.0 if s > t else 0.0 for s in S]
        frac = sum(M[y * w + x] for y in rows for x in range(w)) / (len(rows) * w)
        if frac <= target or t > 1.5: break
        t += .01
    assert frac <= cap, frac
    return M, frac, sum(M) / len(M)  # (벽 0~3.2m 녹 비율, 텍스처 전체 녹 비율 — 박공 포함)


def quant_ok(im, n, cps=(2, 1, 0, 3, 4)):
    """8색 이상 + 반점 ≤0.3% 를 둘 다 만족하는 정리 횟수를 고름. 다 못 넘으면 8색 이상 중 반점이 가장 적은 것."""
    tried = []
    for cp in cps:
        o = V.quant(im, n, cp); c = len(o.getcolors(256) or []); s = V.region(o, (0, 0, o.width, o.height))['speck']
        if c >= 8 and s <= .3: return o
        tried.append((c < 8, s, cp, o))
    return min(tried, key=lambda t: t[:3])[3]


def build():
    T = V.build(); info = {}
    rust = lambda w, h, crop=None: V.clean(V.photo('Rust009'), w, h, crop, soft=True)
    # 골함석 벽: 바랜 청록 칠 + 골(사진 + 칠한 골 3px) / 녹은 아래 0.8m·2m 이음매·벽 윗선(처마 밑) 근처만
    w, h = 256, 192
    base = V.tint(V.clean(V.photo('CorrugatedSteel007A'), w, h, (0, 0, 512, 384), soft=True), (124, 168, 156), .8)
    base = V.ribs(base, 8, 3, .86).filter(ImageFilter.MedianFilter(3))
    def near_wall(x, y):
        z = (h - y - .5) / PX_M; d = min(x % 64, 64 - x % 64)
        if z > WALL_H: return max(0, (z - 5.5) / .5) * .8  # 박공: 꼭대기(지붕 닿는 곳)만
        return max(max(0, 1 - z / .8), max(0, 1 - d / 9) * .9, max(0, 1 - (WALL_H - z) / .25) * .7)
    M, frac, fall = rust_mask(w, h, near_wall)
    im = composite(base, rust(w, h, (0, 0, 512, 384)), M)
    im = V.grad_v(im, (1.04, 1.04, 1.02), (.9, .9, .92))
    im = V.shade_v(im, [(0, .04, (.78, .74, .7)), (2.95 / 6, 3.2 / 6, (.74, .82, .88)), (5.3 / 6, 1, (.82, .88, .92))])
    T['tin_wall'] = (quant_ok(im, 12), 'CorrugatedSteel007A+Rust009'); info['tin_wall'] = dict(rust_pct=round(frac * 100, 1), rust_pct_all=round(fall * 100, 1))
    # 판자 벽: 바랜 크림 칠(판자 결은 사진에서) + 아래 0.35m·판 이음(4m)만 녹·때
    w, h = 256, 96
    base = V.shade_v(V.grad_v(V.tint(V.clean(V.photo('WoodSiding008'), w, h), (216, 200, 166), .62), (1.05, 1.04, 1.0), (.88, .86, .84)),
                     [(0, .1, (.82, .78, .72))])
    def near_pl(x, y):
        z = (h - y - .5) / PX_M; d = min(x % 128, 128 - x % 128)
        return max(max(0, 1 - z / .45), max(0, 1 - d / 7) * .8)
    M, frac, _ = rust_mask(w, h, near_pl, seed='Ground079S')
    im = composite(base, V.tint(rust(w, h), (128, 78, 50), .5), M)
    T['planks'] = (quant_ok(im, 12), 'WoodSiding008+Rust009'); info['planks'] = dict(rust_pct=round(frac * 100, 1))
    T['planks_dark'] = (quant_ok(V.tint(im, (120, 94, 72), .45), 10), 'WoodSiding008+Rust009')
    # 콘크리트 바닥 — V1 과 같은 처리, 8색 이상이 되게
    im = V.blots(V.blots(V.clean(V.photo('Concrete042A'), 256, 256), 11, 6, 22, .72), 12, 4, 12, .8)
    T['floor'] = (quant_ok(im, 12, (1, 0)), 'Concrete042A')
    # 마당 길: 흙 + 자갈 덩어리 + 바퀴 자국(u 40·88px = 길 가운데 기준 ±0.75m, 폭 ≈0.4m, 가장자리는 잡음으로 흔들림)
    w, h = 128, 256
    dirt = V.tint(V.clean(V.photo('Ground081'), w, h, soft=True), (176, 150, 112), .35)
    grav = V.tint(V.clean(V.photo('Gravel022'), w, h, (0, 0, 256, 512), soft=True), (150, 140, 124), .3)  # 작게 잘라 자갈알 ≥3px
    G = field('Ground079S', w, h, 4)
    im = composite(dirt, grav, [1.0 if g > .62 else 0.0 for g in G])
    N = field('Ground037', w, h, 2); a = list(im.getdata())
    for i, p in enumerate(a):
        x, y = i % w, i // w; r = 6 + 3 * N[i]
        if abs(x - 40) < r or abs(x - 88) < r: a[i] = tuple(c * .76 for c in p)
    T['path'] = (quant_ok(V.put((w, h), a), 12), 'Ground081+Gravel022')
    # 드럼: 칠 사진 그대로(빨강 / 청록), 테(위·아래 0.12) 근처에만 녹
    for nm, src, col in (('drum_red', 'PaintedMetal004', (178, 62, 50)), ('drum_blue', 'PaintedMetal006', (70, 146, 136))):
        base = V.tint(V.clean(V.photo(src), 128, 64, (0, 0, 512, 256), soft=True), col, .35)
        def near_dr(x, y): v = 1 - (y + .5) / 64; return max(max(0, 1 - v / .14), max(0, 1 - (1 - v) / .1))
        M, _, _ = rust_mask(128, 64, near_dr, target=.22, cap=.3)
        im = composite(base, rust(128, 64), M)
        im = V.shade_v(V.grad_v(im, (1.05, 1.05, 1.03), (.82, .82, .84)), [(.3, .34, (.76, .76, .76)), (.66, .7, (.76, .76, .76))])
        T[nm] = (quant_ok(im, 10), src + '+Rust009')
    return T, info


def main():
    os.makedirs(OUT, exist_ok=True)
    T, info = build(); tiles, report = {}, {}
    for nm, (im, src) in T.items():
        im.save(os.path.join(OUT, nm + '.png'))
        if nm not in ('drum_red', 'drum_blue', 'drum_top', 'tire', 'crate'): tiles[nm] = [im.width / PX_M, im.height / PX_M]
        r = V.region(im, (0, 0, im.width, im.height))
        report[nm] = dict(size=list(im.size), colors=len(im.getcolors(256) or []), speck=round(r['speck'], 3), src=src,
                          ok=r['speck'] <= .3 and len(im.getcolors(256) or []) >= 8, **info.get(nm, {}))
    json.dump(tiles, open(os.path.join(OUT, 'tile_m.json'), 'w'), indent=1)
    json.dump(report, open(os.path.join(OUT, 'noise_report.json'), 'w'), indent=1, ensure_ascii=False)
    for k, v in report.items(): print(k, v)
    pv = os.path.join(ROOT, 'art', 'provenance.csv')
    keep = [l for l in open(pv, encoding='utf-8', newline='').readlines() if not l.startswith('scene_a_v1b_')]
    open(pv, 'w', encoding='utf-8', newline='').writelines(keep)
    import csv
    rows = []
    for nm, (im, src) in T.items():
        path = f'art/source/scene-a-v1b/tex/{nm}.png'
        urls = ' | '.join(json.load(open(os.path.join(PH, s, 'source.json')))['download_url'] for s in src.split('+'))
        rows.append([f'scene_a_v1b_{nm}', path, urls, 'CC0-1.0', datetime.date.today().isoformat(), 'opus_tex_v1b.py', 'no',
                     hashlib.sha256(open(os.path.join(ROOT, path), 'rb').read()).hexdigest(),
                     f'ambientCG {src} color map; flatten+median+lanczos+{report[nm]["colors"]}색 no dither; painted shade/rust mask by script'])
    with open(pv, 'a', newline='', encoding='utf-8') as f: csv.writer(f).writerows(rows)
    print('provenance +', len(rows))


if __name__ == '__main__':
    main()
