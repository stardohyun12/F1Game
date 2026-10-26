"""실제 마을(OSM 캐시)과 v5 평면을 같은 자로 잰다 — 블록 크기·교차점 간격·차길/사람길 비·차길 폭·등고선 평행·헤어핀·건물 덮음.
사용: python -X utf8 tools/map/town_metrics.py            → docs/map/town-metrics.json + 표 출력
      (make_town_v5.py 가 measure() 를 불러 v5 도 같은 자로 잰다)
좌표: 한 변 SIZE m 정사각, x=동, z=북, 원점 남서. 높이 h(x, z) m, 바다 = h ≤ 1.
"""
import json, math, os, sys
from collections import defaultdict, deque
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RT = os.path.join(ROOT, "docs", "map", "real-towns")
SIZE = 1200
TOWNS = {'tomonoura': ('도모노우라', 34.3840, 133.3815), 'camogli': ('카몰리', 44.3500, 9.1555), 'positano': ('포지타노', 40.6285, 14.4855)}
CARW = {'primary': 10, 'secondary': 9, 'tertiary': 8, 'trunk': 11, 'unclassified': 6, 'residential': 6, 'living_street': 5, 'service': 3.5, 'track': 3}
FOOT = {'footway', 'steps', 'path', 'cycleway', 'corridor', 'pedestrian'}   # pedestrian = 차 없는 돌포장 광장·길 → 사람 길로 센다


def q(v, p):
    v = sorted(v)
    return v[min(len(v) - 1, int(p * len(v)))] if v else 0


def plen(pts):
    return sum(math.dist(a, b) for a, b in zip(pts, pts[1:]))


def resample(pts, step=2.0):
    out = [pts[0]]; acc = 0.0
    for a, b in zip(pts, pts[1:]):
        d = math.dist(a, b)
        while acc + d >= step:
            t = (step - acc) / d
            a = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t); d = math.dist(a, b); acc = 0.0
            out.append(a)
        acc += d
    if math.dist(out[-1], pts[-1]) > 0.3:
        out.append(pts[-1])
    return out


def poly_area(p):
    return abs(sum(p[i][0] * p[i - 1][1] - p[i - 1][0] * p[i][1] for i in range(len(p)))) / 2


# ── 실제 마을 읽기 ──
def load_real(tid):
    title, lat, lon = TOWNS[tid]
    d = json.load(open(os.path.join(RT, tid + '.json'), encoding='utf-8'))
    dlat = SIZE / 2 / 111320; dlon = SIZE / 2 / (111320 * math.cos(math.radians(lat)))
    s, w, n, e = lat - dlat, lon - dlon, lat + dlat, lon + dlon
    P = lambda la, lo: ((lo - w) / (e - w) * SIZE, (la - s) / (n - s) * SIZE)
    E = d['elev']; N = len(E); c = SIZE / N

    def h(x, z):
        fx = min(N - 1.001, max(0, x / c - .5)); fz = min(N - 1.001, max(0, z / c - .5))
        j, i = int(fx), int(fz); tx, tz = fx - j, fz - i
        return (E[i][j] * (1 - tx) * (1 - tz) + E[i][j + 1] * tx * (1 - tz) + E[i + 1][j] * (1 - tx) * tz + E[i + 1][j + 1] * tx * tz)
    roads, blds = [], []
    for el in d['osm']:
        tg = el.get('tags', {}); g = el.get('geometry'); hw = tg.get('highway')
        if not g:
            continue
        pts = [P(p['lat'], p['lon']) for p in g]
        if hw in CARW or hw in FOOT:
            car = hw in CARW
            try:
                wt = float(tg.get('width', '').split()[0])
            except (ValueError, IndexError):
                wt = None
            roads.append({'pts': pts, 'nodes': el.get('nodes'), 'car': car, 'steps': hw == 'steps', 'cls': hw,
                          'w': wt or (CARW[hw] if car else 2.0), 'wtag': wt})
        elif 'building' in tg and len(pts) > 2:
            blds.append(pts)
    return {'name': title, 'roads': roads, 'bld': blds, 'h': h}


# ── 교차점 간격 ──
def chains_from_nodes(roads, car_only):
    """OSM: 노드 그래프의 차수 ≥3 노드 = 교차점. 교차점 사이 사슬 길이 목록 + 사슬 점."""
    adj = defaultdict(set); pos = {}
    for r in roads:
        if car_only and not r['car']:
            continue
        ns = r['nodes']
        for k, (a, b) in enumerate(zip(ns, ns[1:])):
            adj[a].add(b); adj[b].add(a); pos[a] = r['pts'][k]; pos[b] = r['pts'][k + 1]
    J = {n for n, s in adj.items() if len(s) >= 3}
    seen = set(); out = []
    for j in J:
        for nb in adj[j]:
            if (j, nb) in seen:
                continue
            path = [j, nb]; prev, cur = j, nb
            while cur not in J and len(adj[cur]) == 2:
                nxt = next(x for x in adj[cur] if x != prev); path.append(nxt); prev, cur = cur, nxt
            seen.add((j, nb)); seen.add((path[-1], path[-2]))
            pts = [pos[n] for n in path]
            out.append((plen(pts), pts, cur in J))
    return out, [pos[j] for j in J]


def chains_geometric(roads, car_only, tol=None):
    """v5: 다른 길의 점이 반폭 안에 들어오는 곳 = 교차점(8 m 안은 하나로). 길마다 교차점 사이 구간을 자른다."""
    rs = [r for r in roads if r['car'] or not car_only]
    S = [resample(r['pts'], 2.0) for r in rs]
    grid = defaultdict(list)
    for k, pts in enumerate(S):
        for i, p in enumerate(pts):
            grid[(int(p[0] // 8), int(p[1] // 8))].append((k, i))
    out, J = [], []
    for k, pts in enumerate(S):
        hits = []
        for i, p in enumerate(pts):
            gx, gz = int(p[0] // 8), int(p[1] // 8)
            for dx in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for k2, i2 in grid.get((gx + dx, gz + dz), ()):
                        if k2 != k and math.dist(p, S[k2][i2]) < (rs[k]['w'] + rs[k2]['w']) / 2 + .5:
                            hits.append(i); break
                    else:
                        continue
                    break
                else:
                    continue
                break
        cl = []
        for i in hits:
            if not cl or i - cl[-1][-1] > 4:
                cl.append([i])
            else:
                cl[-1].append(i)
        js = [c[len(c) // 2] for c in cl]
        J += [pts[i] for i in js]
        for a, b in zip(js, js[1:]):
            out.append((2.0 * (b - a), pts[a:b + 1], True))
        cuts = [0] + js + [len(pts) - 1]
        for a, b in ((cuts[0], cuts[1]), (cuts[-2], cuts[-1])):
            if b - a > 2 and (a, b) not in [(x, y) for x, y in zip(js, js[1:])]:
                out.append((2.0 * (b - a), pts[a:b + 1], False))
    return out, J


# ── 블록(길·바다로 갈린 땅 덩어리) ──
def blocks(net, cell=2.0):
    n = int(SIZE / cell)
    im = Image.new('L', (n, n), 0); dr = ImageDraw.Draw(im)
    for r in net['roads']:
        pts = [(p[0] / cell, (SIZE - p[1]) / cell) for p in r['pts']]
        if len(pts) > 1:
            dr.line(pts, fill=255, width=max(1, int(round(r['w'] / cell))))
    px = im.load()
    for i in range(n):
        for j in range(n):
            if net['h']((j + .5) * cell, SIZE - (i + .5) * cell) <= 1.0:
                px[j, i] = 255
    hasb = set()
    for b in net['bld']:
        cx = sum(p[0] for p in b) / len(b); cz = sum(p[1] for p in b) / len(b)
        hasb.add((int(cx / cell), int((SIZE - cz) / cell)))
    lab = [[0] * n for _ in range(n)]; out = []; L = 0
    for i0 in range(n):
        for j0 in range(n):
            if px[j0, i0] or lab[i0][j0]:
                continue
            L += 1; dq = deque([(i0, j0)]); lab[i0][j0] = L
            cnt = sx = sz = sxx = szz = sxz = 0; edge = False; nb = 0
            while dq:
                i, j = dq.popleft(); cnt += 1; sx += j; sz += i; sxx += j * j; szz += i * i; sxz += i * j
                if (j, i) in hasb:
                    nb += 1
                if i in (0, n - 1) or j in (0, n - 1):
                    edge = True
                for a, b in ((i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1)):
                    if 0 <= a < n and 0 <= b < n and not px[b, a] and not lab[a][b]:
                        lab[a][b] = L; dq.append((a, b))
            A = cnt * cell * cell
            if edge or nb == 0 or A < 60 or A > 25000:
                continue
            mx, mz = sx / cnt, sz / cnt
            cxx = sxx / cnt - mx * mx; czz = szz / cnt - mz * mz; cxz = sxz / cnt - mx * mz
            tr, det = cxx + czz, cxx * czz - cxz * cxz
            l1 = tr / 2 + math.sqrt(max(0, tr * tr / 4 - det)); l2 = max(0, tr - l1)
            out.append({'A': A, 'long': math.sqrt(12 * l1) * cell, 'short': math.sqrt(12 * l2) * cell, 'nb': nb})
    return out


# ── 등고선 대비 방향 · 헤어핀 ──
def grad(h, x, z, e=10.0):
    return ((h(x + e, z) - h(x - e, z)) / (2 * e), (h(x, z + e) - h(x, z - e)) / (2 * e))


def contour_stats(net, min_slope=0.06):
    par = tot = 0.0; sd = st = 0.0
    for r in net['roads']:
        if not (r['car'] or r['steps']):
            continue
        pts = resample(r['pts'], 4.0)
        for a, b in zip(pts, pts[1:]):
            mx, mz = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            if net['h'](mx, mz) <= 1:
                continue
            gx, gz = grad(net['h'], mx, mz); g = math.hypot(gx, gz)
            if g < min_slope:
                continue
            L = math.dist(a, b)
            if L < 1e-6:
                continue
            c = abs(((b[0] - a[0]) * gx + (b[1] - a[1]) * gz) / (L * g))   # 1 = 경사 직하, 0 = 등고선 평행
            if r['car']:
                tot += L; par += L * (c < 0.5)          # 등고선과 30° 안
            else:
                st += L; sd += L * (c > 0.866)          # 경사 방향과 30° 안
    return {'carParallel': par / tot if tot else 0, 'carOnSlopeM': tot, 'stepsDirect': sd / st if st else 0, 'stepsOnSlopeM': st}


def hairpins(chains, win=60.0, need=150.0):
    out = []
    for L, pts, _ in chains:
        if L < 15:
            continue
        p = resample(pts, 2.0)
        hd = [math.atan2(b[1] - a[1], b[0] - a[0]) for a, b in zip(p, p[1:])]
        dt = [(hd[i + 1] - hd[i] + math.pi) % (2 * math.pi) - math.pi for i in range(len(hd) - 1)]
        i = 0
        while i < len(dt):
            s = 0.0; j = i
            while j < len(dt) and (j - i) * 2.0 <= win:
                s += dt[j]
                if abs(s) >= math.radians(need):
                    # 꺾임이 몰린 구간만 반경으로: 앞뒤 직선 부분을 깎는다
                    a, b = i, j
                    while a < b and abs(dt[a]) < math.radians(4):
                        a += 1
                    arc = max(2.0, (b - a + 1) * 2.0)
                    out.append({'R': arc / abs(s), 'pos': p[(a + b) // 2]})
                    i = j + 5; break
                j += 1
            else:
                i += 1
    return out


def coverage(net, cell=50.0):
    area = defaultdict(float); bands = defaultdict(lambda: [0.0, 0.0])
    for b in net['bld']:
        cx = sum(p[0] for p in b) / len(b); cz = sum(p[1] for p in b) / len(b)
        area[(int(cx // cell), int(cz // cell))] += poly_area(b)
    fr = []; tb = tl = 0.0
    for (gx, gz), a in area.items():
        land = sum(net['h']((gx + (u + .5) / 5) * cell, (gz + (v + .5) / 5) * cell) > 1 for u in range(5) for v in range(5)) / 25 * cell * cell
        if land < cell * cell * 0.2:
            continue
        c = min(1.0, a / land); fr.append(c); tb += a; tl += land
        y = net['h']((gx + .5) * cell, (gz + .5) * cell)
        bd = bands['0-15' if y < 15 else '15-40' if y < 40 else '40+']; bd[0] += a; bd[1] += land
    return {'cellMedian': q(fr, .5), 'cellP90': q(fr, .9), 'overall': tb / tl if tl else 0,
            'bands': {k: round(v[0] / v[1], 3) for k, v in sorted(bands.items())}, 'builtCells': len(fr)}


def measure(net, geometric=False):
    R = net['roads']
    carL = sum(plen(r['pts']) for r in R if r['car']); footL = sum(plen(r['pts']) for r in R if not r['car'])
    stepsL = sum(plen(r['pts']) for r in R if r['steps'])
    widths = defaultdict(float)
    for r in R:
        if r['car']:
            widths[r['w']] += plen(r['pts'])
    if geometric:
        ch_car, J_car = chains_geometric(R, True); ch_all, J_all = chains_geometric(R, False)
    else:
        ch_car, J_car = chains_from_nodes(R, True); ch_all, J_all = chains_from_nodes(R, False)
    sp_car = [L for L, _, both in ch_car if both]; sp_all = [L for L, _, both in ch_all if both]
    B = blocks(net)
    hp = hairpins(ch_car)
    return {
        'name': net['name'], 'buildings': len(net['bld']),
        'carM': round(carL), 'footM': round(footL), 'stepsM': round(stepsL), 'carFootRatio': round(carL / footL, 2) if footL else None,
        'carWidthM': {str(k): round(v) for k, v in sorted(widths.items())},
        'junctionsCar': len(J_car), 'junctionsAll': len(J_all),
        'spacingCar': [round(q(sp_car, p)) for p in (.25, .5, .75)], 'spacingAll': [round(q(sp_all, p)) for p in (.25, .5, .75)],
        'blocks': len(B), 'blockArea': [round(q([b['A'] for b in B], p)) for p in (.25, .5, .75)],
        'blockLong': [round(q([b['long'] for b in B], p)) for p in (.25, .5, .75)],
        'blockShort': [round(q([b['short'] for b in B], p)) for p in (.25, .5, .75)],
        'contour': {k: round(v, 2) for k, v in contour_stats(net).items()},
        'hairpins': len(hp), 'hairpinR': [round(q([x['R'] for x in hp], p), 1) for p in (.25, .5, .75)] if hp else [],
        'coverage': {k: (round(v, 3) if isinstance(v, float) else v) for k, v in coverage(net).items()},
    }


def table(rows):
    keys = [('buildings', '건물 수'), ('carM', '차길 m'), ('footM', '사람길 m'), ('carFootRatio', '차길/사람길'), ('stepsM', '계단 m'),
            ('spacingCar', '차길 교차 간격 p25/50/75'), ('spacingAll', '모든 길 교차 간격'), ('blocks', '블록 수'),
            ('blockArea', '블록 넓이 m² p25/50/75'), ('blockLong', '블록 긴 변'), ('blockShort', '블록 짧은 변'),
            ('contour.carParallel', '비탈 차길 중 등고선 평행(30° 안)'), ('contour.stepsDirect', '비탈 계단 중 경사 직하'),
            ('hairpins', '헤어핀 수(60m 안 150°+)'), ('hairpinR', '헤어핀 반경 p25/50/75'),
            ('coverage.overall', '건물 덮음(건물 있는 50m 칸)'), ('coverage.bands', '덮음 높이대별'), ('carWidthM', '차길 폭별 길이')]
    lines = ['| 항목 | ' + ' | '.join(r['name'] for r in rows) + ' |', '|---|' + '---|' * len(rows)]
    for k, lab in keys:
        vals = []
        for r in rows:
            v = r
            for part in k.split('.'):
                v = v.get(part) if isinstance(v, dict) else None
            vals.append('/'.join(map(str, v)) if isinstance(v, list) else json.dumps(v, ensure_ascii=False) if isinstance(v, dict) else str(v))
        lines.append(f'| {lab} | ' + ' | '.join(vals) + ' |')
    return '\n'.join(lines)


if __name__ == '__main__':
    res = {}
    for tid in (sys.argv[1:] or TOWNS):
        res[tid] = measure(load_real(tid)); print(tid, json.dumps(res[tid], ensure_ascii=False))
    json.dump(res, open(os.path.join(ROOT, 'docs', 'map', 'town-metrics.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(table(list(res.values())))
