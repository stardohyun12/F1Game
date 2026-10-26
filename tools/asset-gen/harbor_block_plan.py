"""항구 블록 평면도 + 손 조립 기록표 뼈대 (D-162). 판단은 사람이 — 이 스크립트는 바닥 모양·바다·길만 그린다.

    python -X utf8 tools/asset-gen/harbor_block_plan.py --x0 70 --z0 -320 --out art/_review/2026-10-27-harbor-kit-v0
"""
import argparse, json, math, os
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))


def load():
    return json.load(open(os.path.join(ROOT, 'docs', 'map', 'town-v6osm.json'), encoding='utf8'))


def height_fn(d):
    g = {(round(x), round(z)): y for x, z, y in d['terrain']['controlPoints']}
    st = d['terrain']['step']

    def h(x, z):  # 격자 쌍선형
        x0, z0 = math.floor(x / st) * st, math.floor(z / st) * st
        fx, fz = (x - x0) / st, (z - z0) / st
        v = [g.get((x0 + i * st, z0 + j * st), 0.0) for j in (0, 1) for i in (0, 1)]
        return (v[0] * (1 - fx) + v[1] * fx) * (1 - fz) + (v[2] * (1 - fx) + v[3] * fx) * fz
    return h


def cen(p):
    return sum(q[0] for q in p) / len(p), sum(q[1] for q in p) / len(p)


def area(p):
    return abs(sum(p[i][0] * p[i - 1][1] - p[i - 1][0] * p[i][1] for i in range(len(p)))) / 2


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--x0', type=float, required=True); ap.add_argument('--z0', type=float, required=True)
    ap.add_argument('--n', type=float, default=100); ap.add_argument('--out', required=True)
    a = ap.parse_args()
    d = load(); h = height_fn(d)
    X0, Z0, N, S, M = a.x0, a.z0, a.n, 8, 20
    W = int((N + 2 * M) * S)
    im = Image.new('RGB', (W, W), (226, 219, 200)); dr = ImageDraw.Draw(im)
    tx = lambda x, z: ((x - X0 + M) * S, (Z0 + N + M - z) * S)
    sea = tot = 0
    for i in range(int(N + 2 * M)):
        for j in range(int(N + 2 * M)):
            x, z = X0 - M + i + .5, Z0 + N + M - j - .5
            if h(x, z) <= 0:
                dr.rectangle((i * S, j * S, i * S + S, j * S + S), fill=(120, 170, 185))
                if X0 <= x < X0 + N and Z0 <= z < Z0 + N: sea += 1
            if X0 <= x < X0 + N and Z0 <= z < Z0 + N: tot += 1
    for r in d['roads']:
        pts = [tx(k[0], k[1]) for k in r['knots']]
        if len(pts) > 1: dr.line(pts, fill=(168, 156, 134), width=max(2, int(r['width'] * S)))
    bs = [b for b in d['buildings'] if X0 <= cen(b['poly'])[0] < X0 + N and Z0 <= cen(b['poly'])[1] < Z0 + N]
    bs.sort(key=lambda b: (-round(cen(b['poly'])[1] / 10), cen(b['poly'])[0]))
    rows = []
    for i, b in enumerate(bs):
        p = b['poly']; dr.polygon([tx(*q) for q in p], fill=(201, 138, 94), outline=(74, 58, 48))
        cx, cz = cen(p); dr.text(tx(cx, cz), str(i + 1), fill=(20, 20, 20), anchor='mm')
        rows.append(dict(id=i + 1, osm=b.get('osm_way'), ground_y=round(b['y'], 2), area_m2=round(area(p), 1),
                         center=[round(cx, 1), round(cz, 1)], poly=p,
                         floors=None, roof=None, wall=None, roof_col=None, ground_floor=None, notes=''))
    dr.rectangle((M * S, M * S, (M + N) * S, (M + N) * S), outline=(200, 40, 40), width=2)
    for m in range(0, int(N) + 1, 10):
        dr.text(tx(X0 + m, Z0 - 2), str(int(X0 + m)), fill=(60, 60, 60), anchor='mm')
        dr.text(tx(X0 - 2, Z0 + m), str(int(Z0 + m)), fill=(60, 60, 60), anchor='mm')
    os.makedirs(a.out, exist_ok=True)
    im.save(os.path.join(a.out, 'block_plan.png'))
    stops = [s for s in d['stops'] if X0 <= s['pos'][0] < X0 + N and Z0 <= s['pos'][1] < Z0 + N]
    json.dump(dict(block=dict(x=[X0, X0 + N], z=[Z0, Z0 + N], sea=round(sea / tot, 3), src='docs/map/town-v6osm.json',
                              stops=stops), buildings=rows),
              open(os.path.join(a.out, 'block_assembly.json'), 'w', encoding='utf8'), ensure_ascii=False, indent=1)
    print(f'{len(rows)} buildings, sea {sea / tot:.0%}, stops {[s["name"] for s in stops]}')


if __name__ == '__main__':
    main()
