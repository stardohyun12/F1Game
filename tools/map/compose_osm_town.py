"""마을 v6-OSM: 실제 OSM 조각(도모노우라 심장·카몰리 해안 띠·포지타노 비탈)을 잘라 한 해안선 위에 붙인다 (D-156, PD 0109).
데이터: docs/map/real-towns/<id>.json 캐시(OSM Overpass + SRTM 25×25). © OpenStreetMap contributors (ODbL) — docs/map/ATTRIBUTION.md.
좌표: x=동, z=북, 칸 중심 (0,0), 그림은 1200 m 사방(real_towns 와 같은 축척), 플레이 칸 760 m.
사용: python -X utf8 tools/map/compose_osm_town.py [--debug]
"""
import heapq, json, math, os, sys
from collections import defaultdict
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
import planlib as L
import town_metrics as TM

ROOT = L.ROOT
OUT = os.path.join(ROOT, 'docs', 'map')
RT = os.path.join(OUT, 'real-towns')
HALF = 380
SRC_LL = {'tomonoura': (34.3840, 133.3815), 'camogli': (44.3500, 9.1555), 'positano': (40.6285, 14.4855)}

# 조각: (id, 원본 마을, 원본 중심(0..1200 m, x 동·z 북), 회전°(반시계), 잘라낼 사각형(회전 뒤 중심 기준 x0,x1,z0,z1), 놓을 곳, 이유)
PIECES = [
    ('tomo', 'tomonoura', (560, 533), 0.0, (-260, 220, -270, 170), (-70, -100),
     '만(배 대는 곳) + 옛 격자 골목 + 만을 감싸는 큰길 — 심장. 동쪽 해안·서쪽 산은 뺌'),
    ('camo', 'camogli', (507, 560), 47.6, (-330, 300, -150, 330), (490, -170),
     '해변 따라 늘어선 띠(해안선이 동서가 되게 47.6° 회전) — 항구 옆 길게 붙은 집들과 해안 큰길'),
    ('posi', 'positano', (373, 673), 0.0, (-253, 253, -200, 210), (-110, 290),
     '비탈 스위치백(Via Pasitea 헤어핀 고리 + 계단 다발) — 뒤 비탈'),
]
VS = 0.55          # 높이 배율(가설): SRTM 48 m 격자는 깎아 앉힌 길 단을 못 담아 길 경사가 실제의 2~3배로 나온다
BLEND = 45.0       # 조각 가장자리 고도 섞는 폭 (m)
CARW = TM.CARW
FOOT = TM.FOOT
ROUGH_CAP = [(0.6, 10.0), (0.4, 13.0)]   # v5 와 같은 가설(화물 실은 사람의 요철 참을성)
ROUGH_SURF = {'sett': 0.5, 'cobblestone': 0.5, 'unhewn_cobblestone': 0.6, 'paving_stones': 0.4, 'stone': 0.5,
              'unpaved': 0.6, 'gravel': 0.6, 'fine_gravel': 0.5, 'dirt': 0.6, 'ground': 0.6, 'compacted': 0.5, 'grass': 0.6}


def S(t):
    t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)


# ── 원본 읽기 ──
def load_src(tid):
    lat, lon = SRC_LL[tid]
    d = json.load(open(os.path.join(RT, tid + '.json'), encoding='utf-8'))
    dlat = 600 / 111320; dlon = 600 / (111320 * math.cos(math.radians(lat)))
    s, w, n, e = lat - dlat, lon - dlon, lat + dlat, lon + dlon
    P = lambda la, lo: ((lo - w) / (e - w) * 1200, (la - s) / (n - s) * 1200)
    E = d['elev']; N = len(E); c = 1200 / N

    def h(x, z):
        fx = min(N - 1.001, max(0, x / c - .5)); fz = min(N - 1.001, max(0, z / c - .5))
        j, i = int(fx), int(fz); tx, tz = fx - j, fz - i
        return E[i][j] * (1 - tx) * (1 - tz) + E[i][j + 1] * tx * (1 - tz) + E[i + 1][j] * (1 - tx) * tz + E[i + 1][j + 1] * tx * tz
    ways = []
    for el in d['osm']:
        g = el.get('geometry')
        if g:
            ways.append((el.get('tags', {}), [P(p['lat'], p['lon']) for p in g], el.get('id')))
    return ways, h


class Piece:
    def __init__(self, pid, tid, c, rot, rect, at, why):
        self.pid, self.tid, self.c, self.rect, self.at, self.why = pid, tid, c, rect, at, why
        self.ca, self.sa = math.cos(math.radians(rot)), math.sin(math.radians(rot)); self.rot = rot
        self.ways, self.hs = load_src(tid)
        self.dy = 0.0

    def local(self, sx, sz):          # 원본 → 조각 좌표
        x, z = sx - self.c[0], sz - self.c[1]
        return x * self.ca - z * self.sa, x * self.sa + z * self.ca

    def world(self, lx, lz):
        return lx + self.at[0], lz + self.at[1]

    def to_src(self, X, Z):
        lx, lz = X - self.at[0], Z - self.at[1]
        return lx * self.ca + lz * self.sa + self.c[0], -lx * self.sa + lz * self.ca + self.c[1]

    def inside(self, lx, lz, m=0.0):
        x0, x1, z0, z1 = self.rect
        return x0 + m <= lx <= x1 - m and z0 + m <= lz <= z1 - m

    def edge(self, X, Z):            # 조각 안쪽 가장자리까지 거리(밖이면 음수)
        lx, lz = X - self.at[0], Z - self.at[1]
        x0, x1, z0, z1 = self.rect
        return min(lx - x0, x1 - lx, lz - z0, z1 - lz)

    def h(self, X, Z):
        return VS * (self.hs(*self.to_src(X, Z)) + self.dy)


PCS = [Piece(*p) for p in PIECES]


# ── 바탕 지형(조각 밖): 해안선 + 서쪽 등대 곶 + 오르는 비탈 ──
def zc(x):
    return -265 - 95 * math.exp(-((x + 378) / 45) ** 2) + 0.04 * x


def h_base(x, z):
    d = z - zc(x)
    if d < 0:
        return max(-6.0, d * 0.4)
    return 1.5 + 6 * S(d / 25) + 0.17 * max(0, d - 25) + 18 * math.exp(-((x + 378) / 35) ** 2) * S(d / 60)


GRID = {}


def build_grid(step=10):
    """조각 밖 고도 = 조각 안쪽 표본 + 먼 곳 바탕 지형 표본의 IDW(10 m 격자로 한 번 굽는다) — 조각 가장자리 절벽 방지."""
    smp = []
    for pc in PCS:
        x0, x1, z0, z1 = pc.rect
        for lx in range(int(x0) + 15, int(x1) - 14, 25):
            for lz in range(int(z0) + 15, int(z1) - 14, 25):
                X, Z = pc.world(lx, lz); smp.append((X, Z, pc.h(X, Z)))
    for X in range(-640, 641, 40):
        for Z in range(-640, 641, 40):
            if all(pc.edge(X, Z) < -90 for pc in PCS) or Z < zc(X) - 20:
                smp.append((X, Z, h_base(X, Z)))
    GRID['step'] = step; GRID['n'] = n = 1300 // step
    g = []
    for i in range(n + 1):
        Z = -650 + i * step; row = []
        for j in range(n + 1):
            X = -650 + j * step; sw = sy = 0.0
            for sx, sz, sh in smp:
                d2 = (X - sx) ** 2 + (Z - sz) ** 2
                if d2 > 40000: continue
                w = 1 / (d2 + 25) ** 1.5; sw += w; sy += w * sh
            row.append(sy / sw if sw else h_base(X, Z))
        g.append(row)
    GRID['g'] = g


def h_fill(x, z):
    st, n, g = GRID['step'], GRID['n'], GRID['g']
    fx = min(n - 1.001, max(0, (x + 650) / st)); fz = min(n - 1.001, max(0, (z + 650) / st))
    j, i = int(fx), int(fz); tx, tz = fx - j, fz - i
    v = g[i][j] * (1 - tx) * (1 - tz) + g[i][j + 1] * tx * (1 - tz) + g[i + 1][j] * (1 - tx) * tz + g[i + 1][j + 1] * tx * tz
    return min(v, -1.0) if z < zc(x) else v


def H(x, z):
    ws, hs = [], []
    for p in PCS:
        e = p.edge(x, z)
        if e > 0:
            ws.append(S(e / BLEND)); hs.append(p.h(x, z))
    sw = sum(ws)
    if sw >= 1:
        return sum(w * y for w, y in zip(ws, hs)) / sw
    return sum(w * y for w, y in zip(ws, hs)) + (1 - sw) * (h_fill(x, z) if GRID else h_base(x, z))


# ── 조각 자르기 ──
def clip_line(pts, pc):
    """선을 조각 사각형으로 자른다. [(점들, 시작이 잘림?, 끝이 잘림?)]"""
    loc = [pc.local(*q) for q in pts]
    out, cur, cut0 = [], [], False
    for i, q in enumerate(loc):
        ins = pc.inside(*q)
        if ins:
            if not cur and i:
                cur.append(_bnd(loc[i - 1], q, pc)); cut0 = True
            elif not cur:
                cut0 = False
            cur.append(q)
        elif cur:
            cur.append(_bnd(cur[-1], q, pc)); out.append((cur, cut0, True)); cur = []
    if cur:
        out.append((cur, cut0, False))
    return [([pc.world(*q) for q in c], a, b) for c, a, b in out if len(c) > 1]


def _bnd(a, b, pc):
    lo, hi = 0.0, 1.0          # a→b 중 사각형 경계(이분법)
    ina = pc.inside(*a)
    for _ in range(30):
        m = (lo + hi) / 2; q = (a[0] + (b[0] - a[0]) * m, a[1] + (b[1] - a[1]) * m)
        if pc.inside(*q) == ina: lo = m
        else: hi = m
    return (a[0] + (b[0] - a[0]) * lo, a[1] + (b[1] - a[1]) * lo)


def plen(p):
    return sum(math.dist(a, b) for a, b in zip(p, p[1:]))


DBG = []
ROADS, BLDS, COAST, WATER, GREEN, PIER, EDITS = [], [], [], [], [], [], []


def kind_of(hw):
    if hw in ('primary', 'secondary', 'trunk', 'tertiary'): return 'main'
    if hw in CARW: return 'village'
    if hw == 'steps': return 'stairs'
    return 'alley'


def cut_pieces():
    for pc in PCS:
        for tg, pts, oid in pc.ways:
            hw = tg.get('highway')
            if hw in CARW or hw in FOOT:
                try: wt = float(tg.get('width', '').split()[0])
                except (ValueError, IndexError): wt = None
                k = kind_of(hw); w = wt or (CARW[hw] if hw in CARW else 2.0)
                rough = ROUGH_SURF.get(tg.get('surface', ''), 0.6 if hw == 'track' else 0.0)
                for seg, c0, c1 in clip_line(pts, pc):
                    if plen(seg) < 3: continue
                    ROADS.append({'id': f'{pc.pid}_{oid}_{len(ROADS)}', 'kind': k, 'width': w, 'roughness': rough, 'pts': seg,
                                  'cut': (c0, c1), 'piece': pc.pid, 'osm': oid, 'cls': hw, 'name': tg.get('name', ''), 'surface': tg.get('surface', '')})
            elif 'building' in tg and len(pts) > 2:
                cx = sum(q[0] for q in pts) / len(pts); cz = sum(q[1] for q in pts) / len(pts)
                if pc.inside(*pc.local(cx, cz)):
                    BLDS.append({'poly': [pc.world(*pc.local(*q)) for q in pts], 'piece': pc.pid, 'osm': oid})
            elif tg.get('natural') == 'coastline':
                COAST.extend(seg for seg, _, _ in clip_line(pts, pc))
            elif tg.get('man_made') in ('pier', 'breakwater'):
                PIER.extend(seg for seg, _, _ in clip_line(pts, pc))
            elif tg.get('landuse') or tg.get('natural') in ('wood', 'scrub'):
                cx = sum(q[0] for q in pts) / len(pts); cz = sum(q[1] for q in pts) / len(pts)
                if len(pts) > 2 and pc.inside(*pc.local(cx, cz)):
                    GREEN.append([pc.world(*pc.local(*q)) for q in pts])


def seam_offsets():
    """포지타노 아래 가장자리를 도모노우라 위 가장자리 높이에 맞춘다(중앙값 차)."""
    tomo, camo, posi = PCS
    xs = range(-150, 120, 10)
    a = sorted(tomo.hs(*tomo.to_src(x, tomo.at[1] + tomo.rect[3] - 5)) for x in xs)
    b = sorted(posi.hs(*posi.to_src(x, posi.at[1] + posi.rect[2] + 5)) for x in xs)
    posi.dy = round(a[len(a) // 2] - b[len(b) // 2], 1)


# ── 좁은 차길: 폭 3.2 m 미만 → 그 줄 건물 0.5~1 m 물리거나 사람길로 ──
def seg_d(p, a, b):
    return L.seg_dist(p[0], p[1], a[0], a[1], b[0], b[1])[0]


def widen():
    for r in ROADS:
        if r['kind'] not in ('main', 'village') or r['width'] >= 3.2: continue
        half = 1.6 + 0.3            # 3.2 m 길 + 벽 여유 0.3 m
        hits = []
        x0 = min(q[0] for q in r['pts']) - 6; x1 = max(q[0] for q in r['pts']) + 6
        z0 = min(q[1] for q in r['pts']) - 6; z1 = max(q[1] for q in r['pts']) + 6
        for b in BLDS:
            if not any(x0 <= q[0] <= x1 and z0 <= q[1] <= z1 for q in b['poly']): continue
            dmin, best = 1e9, None
            for q in b['poly']:
                for a, c in zip(r['pts'], r['pts'][1:]):
                    d = seg_d(q, a, c)
                    if d < dmin: dmin, best = d, (a, c)
            if dmin < half: hits.append((b, dmin, best))
        need = max((half - d for _, d, _ in hits), default=0)
        if need > 1.0:
            EDITS.append({'road': r['id'], 'osm': r['osm'], 'from': r['width'], 'to': 'alley', 'why': f'벽까지 {half - need:.1f} m — 1 m 넘게 물려야 해서 사람길로'})
            r['kind'] = 'alley'; continue
        for b, d, (a, c) in hits:
            mv = max(0.5, half - d)
            cx = sum(q[0] for q in b['poly']) / len(b['poly']); cz = sum(q[1] for q in b['poly']) / len(b['poly'])
            tx, tz = c[0] - a[0], c[1] - a[1]; ln = math.hypot(tx, tz) or 1; nx, nz = -tz / ln, tx / ln
            if (cx - a[0]) * nx + (cz - a[1]) * nz < 0: nx, nz = -nx, -nz
            b['poly'] = [(q[0] + nx * mv, q[1] + nz * mv) for q in b['poly']]
        EDITS.append({'road': r['id'], 'osm': r['osm'], 'from': r['width'], 'to': 3.2, 'why': f'건물 {len(hits)}채를 0.5~1 m 물림' if hits else '건물 없음 — 폭만 넓힘'})
        r['width'] = 3.2


# ── 이음매: 잘린 차길 끝끼리 부드러운 곡선(3차 베지어)으로 ──
def end_dir(pts, at_start):
    a, b = (pts[0], pts[min(3, len(pts) - 1)]) if at_start else (pts[-1], pts[max(0, len(pts) - 4)])
    dx, dz = a[0] - b[0], a[1] - b[1]; ln = math.hypot(dx, dz) or 1
    return dx / ln, dz / ln          # 끝에서 바깥쪽


def bezier(p0, d0, p1, d1, step=4.0):
    D = math.dist(p0, p1); k = D * 0.4
    c0 = (p0[0] + d0[0] * k, p0[1] + d0[1] * k); c1 = (p1[0] + d1[0] * k, p1[1] + d1[1] * k)
    n = max(4, int(D / step))
    return [((1 - t) ** 3 * p0[0] + 3 * (1 - t) ** 2 * t * c0[0] + 3 * (1 - t) * t * t * c1[0] + t ** 3 * p1[0],
             (1 - t) ** 3 * p0[1] + 3 * (1 - t) ** 2 * t * c0[1] + 3 * (1 - t) * t * t * c1[1] + t ** 3 * p1[1]) for t in (i / n for i in range(n + 1))]


def seams(maxd=120.0):
    ends = []
    for r in ROADS:
        if r['kind'] not in ('main', 'village'): continue
        for k, at_start in ((0, True), (1, False)):
            if r['cut'][k]:
                ends.append((r, at_start))
    cand = []
    for i, (ra, sa) in enumerate(ends):
        pa = ra['pts'][0 if sa else -1]; da = end_dir(ra['pts'], sa)
        for rb, sb in ends[i + 1:]:
            if rb['piece'] == ra['piece']: continue
            pb = rb['pts'][0 if sb else -1]; db = end_dir(rb['pts'], sb)
            D = math.dist(pa, pb)
            if D > maxd: continue
            ux, uz = (pb[0] - pa[0]) / (D or 1), (pb[1] - pa[1]) / (D or 1)
            align = (da[0] * ux + da[1] * uz) + (-(db[0] * ux + db[1] * uz))    # 둘 다 서로를 향할수록 큼(최대 2)
            if align < 0.4: continue
            big = ra['kind'] == 'main' and rb['kind'] == 'main'
            cand.append((D * (2.6 - align) - (60 if big else 0), ra, sa, rb, sb))
    print('seam ends', len(ends), 'cands', len(cand), {pc: sum(1 for r, _ in ends if r['piece'] == pc) for pc in ('tomo', 'camo', 'posi')})
    used = set(); n = 0
    for _, ra, sa, rb, sb in sorted(cand, key=lambda c: c[0]):
        if (id(ra), sa) in used or (id(rb), sb) in used: continue
        used.add((id(ra), sa)); used.add((id(rb), sb))
        pa = ra['pts'][0 if sa else -1]; pb = rb['pts'][0 if sb else -1]
        pts = bezier(pa, end_dir(ra['pts'], sa), pb, end_dir(rb['pts'], sb))
        kind = 'main' if ra['kind'] == 'main' and rb['kind'] == 'main' else 'village'
        ROADS.append({'id': f'seam{n}_{ra["piece"]}_{rb["piece"]}', 'kind': kind, 'width': min(ra['width'], rb['width']),
                      'roughness': max(ra['roughness'], rb['roughness']), 'pts': pts, 'cut': (False, False), 'piece': 'seam', 'osm': None,
                      'cls': 'seam', 'name': '이음매', 'surface': ''})
        n += 1
    # 짝 없는 끝: 다른 조각 차길의 가장 가까운 꼭짓점으로 T 자 연결(70 m 안, 방향 맞을 때)
    loose = 0
    for ra, sa in ends:
        if (id(ra), sa) in used: continue
        pa = ra['pts'][0 if sa else -1]; da = end_dir(ra['pts'], sa); best = None
        for rb in ROADS:
            if rb['kind'] not in ('main', 'village') or rb['piece'] in (ra['piece'],) or rb['piece'] == 'seam': continue
            for i, q in enumerate(rb['pts']):
                D = math.dist(pa, q)
                if 8 < D < 70 and ((q[0] - pa[0]) * da[0] + (q[1] - pa[1]) * da[1]) / D > 0.45 and (best is None or D < best[0]):
                    best = (D, rb, i)
        if not best:
            loose += 1; continue
        D, rb, i = best; q = rb['pts'][i]
        j0, j1 = max(0, i - 1), min(len(rb['pts']) - 1, i + 1)
        tx, tz = rb['pts'][j1][0] - rb['pts'][j0][0], rb['pts'][j1][1] - rb['pts'][j0][1]; ln = math.hypot(tx, tz) or 1
        nx, nz = -tz / ln, tx / ln
        if (pa[0] - q[0]) * nx + (pa[1] - q[1]) * nz < 0: nx, nz = -nx, -nz
        pts = bezier(pa, da, q, (nx, nz))
        ROADS.append({'id': f'seam{n}_{ra["piece"]}_{rb["piece"]}_T', 'kind': 'village', 'width': min(ra['width'], rb['width'], 6.0),
                      'roughness': ra['roughness'], 'pts': pts, 'cut': (False, False), 'piece': 'seam', 'osm': None, 'cls': 'seam', 'name': '이음매(T)', 'surface': ''})
        used.add((id(ra), sa)); n += 1
    return n, loose


# ── 바깥 길: 아말피형 해안 큰길(서쪽 곶 → 칸 밖), 등대 길 ──
def contour_pts(x0, x1, y, step=8):
    out = []
    for x in range(int(x0), int(x1) + 1, step) if x1 > x0 else range(int(x0), int(x1) - 1, -step):
        z = zc(x)
        while h_base(x, z) < y and z < 600: z += 1
        out.append((float(x), z))
    return out


def outer_roads():
    # 도모노우라 큰길 중 서쪽 끝(잘린 끝) 찾기 → 거기서 서쪽 곶 목을 돌아 칸 밖으로
    mains = [r for r in ROADS if r['piece'] == 'tomo' and r['kind'] == 'main']
    ends = [(r, s) for r in mains for s in (True, False) if r['cut'][0 if s else 1]]
    r0, s0 = min(ends, key=lambda e: e[0]['pts'][0 if e[1] else -1][0])
    p0 = r0['pts'][0 if s0 else -1]
    west = contour_pts(-640, -440, 9) + contour_pts(-436, -340, 12)
    link = bezier(west[-1], (1, 0), p0, end_dir(r0['pts'], s0))
    ROADS.append({'id': 'main_west', 'kind': 'main', 'width': 7.0, 'roughness': 0.0, 'pts': west + link[1:], 'cut': (False, False), 'piece': 'outer',
                  'osm': None, 'cls': 'outer', 'name': '해안 큰길(서쪽 곶 → 칸 밖)', 'surface': '', 'out': 'start'})
    # 동쪽: 카몰리 큰길의 동쪽 끝 → 칸 밖까지 해안 따라
    cm = [r for r in ROADS if r['piece'] == 'camo' and r['kind'] == 'main']
    ends = [(r, s) for r in cm for s in (True, False) if r['cut'][0 if s else 1]]
    r1, s1 = max(ends, key=lambda e: e[0]['pts'][0 if e[1] else -1][0])
    p1 = r1['pts'][0 if s1 else -1]
    if p1[0] < 640:
        d = end_dir(r1['pts'], s1); tail = bezier(p1, d, (680.0, p1[1] + d[1] * 60), (1, 0))
        ROADS.append({'id': 'main_east', 'kind': 'main', 'width': 7.0, 'roughness': 0.0, 'pts': tail, 'cut': (False, False), 'piece': 'outer',
                      'osm': None, 'cls': 'outer', 'name': '해안 큰길(동쪽 → 칸 밖)', 'surface': '', 'out': 'end'})
    # 등대 곶 길: 큰길(곶 목)에서 곶 끝까지 좁은 돌길 굽이
    tip = (-378.0, zc(-378) + 14)
    neck = min(west, key=lambda q: abs(q[0] + 378))
    cape = [neck, (neck[0] + 22, (neck[1] + tip[1]) / 2 + 10), (neck[0] - 18, (neck[1] + tip[1]) / 2 - 18), tip]
    pts = []
    for a, b in zip(cape, cape[1:]):
        pts += bezier(a, (0, -1), b, (0, 1))[:-1] if not pts else bezier(a, (0, -1), b, (0, 1))[1:-1]
    pts.append(tip)
    ROADS.append({'id': 'cape_road', 'kind': 'village', 'width': 5.0, 'roughness': 0.0, 'pts': pts, 'cut': (False, False), 'piece': 'outer',
                  'osm': None, 'cls': 'outer', 'name': '등대 곶 길(포장, 헤어핀)', 'surface': 'asphalt'})
    # 등대 지름길(가설): 곶 동쪽 기슭 해안 산책로를 차 가능한 좁은 돌길로 — 카몰리·포지타노 해안 산책로가 실제로 좁은 포장 돌길
    st = min(west, key=lambda q: abs(q[0] + 345))
    ks = [st] + [(x, zc(x) + 7) for x in (-352, -362, -370)] + [tip]
    sh = []
    for i in range(len(ks) - 1):
        p0, p1 = ks[i], ks[i + 1]; pm = ks[max(0, i - 1)]; pn = ks[min(len(ks) - 1, i + 2)]
        for t in (j / 8 for j in range(8)):
            sh.append(tuple(0.5 * ((2 * p0[k]) + (-pm[k] + p1[k]) * t + (2 * pm[k] - 5 * p0[k] + 4 * p1[k] - pn[k]) * t * t + (-pm[k] + 3 * p0[k] - 3 * p1[k] + pn[k]) * t ** 3) for k in (0, 1)))
    sh.append(tip)
    ROADS.append({'id': 'cape_shore', 'kind': 'village', 'width': 3.2, 'roughness': 0.5, 'pts': sh, 'cut': (False, False), 'piece': 'outer',
                  'osm': None, 'cls': 'outer', 'name': '곶 기슭 해안 돌길(등대 지름길)', 'surface': 'sett'})
    EDITS.append({'road': 'cape_shore', 'osm': None, 'from': '없음(바탕 지형)', 'to': '차 가능한 돌길 3.2 m · 요철 0.5',
                  'why': '등대: 큰길+곶 길(헤어핀 3)뿐 → 곶 기슭 해안 산책로를 좁은 돌길로. 실제 카몰리·포지타노 해안 산책로가 좁은 포장 돌길'})
    return tip


# ── 길 그래프·재미 검사 ──
def dens(pts, step=2.0):
    out = [pts[0]]
    for a, b in zip(pts, pts[1:]):
        n = max(1, int(math.dist(a, b) / step))
        out += [(a[0] + (b[0] - a[0]) * i / n, a[1] + (b[1] - a[1]) * i / n) for i in range(1, n + 1)]
    return out


def graph():
    key = lambda q: (round(q[0], 1), round(q[1], 1))
    nodes, idx, adj = [], {}, defaultdict(list)
    def nid(q, r):
        k = key(q)
        if k not in idx: idx[k] = len(nodes); nodes.append((q, r))
        return idx[k]
    for r in ROADS:
        if r['kind'] not in ('main', 'village'): continue
        dp = dens(r['pts'])
        ids = [nid(q, r) for q in dp]
        for a, b in zip(ids, ids[1:]):
            if a == b: continue
            d = math.dist(nodes[a][0], nodes[b][0]); adj[a].append((b, d, r)); adj[b].append((a, d, r))
    return nodes, adj


def cost_big(r):        # 안전한 길: 큰길 1 · 넓은 마을 차길 1.8 · 좁거나 거친 길 4 (가설)
    return 1.0 if r['kind'] == 'main' else 1.8 if (r['width'] >= 5.0 and r['roughness'] < 0.4) else 4.0


def cost_short(r):      # 지름길: 거리 그대로
    return 1.0


def route(nodes, adj, a, b, ok):
    cand = range(len(nodes))
    sa = min(cand, key=lambda k: math.dist(nodes[k][0], a)); sb = min(cand, key=lambda k: math.dist(nodes[k][0], b))
    dist = {sa: 0.0}; prev = {}; pq = [(0.0, sa)]
    while pq:
        dd, k = heapq.heappop(pq)
        if k == sb: break
        if dd > dist[k]: continue
        for k2, w, r in adj[k]:
            nd = dd + w * ok(r)
            if nd < dist.get(k2, 1e18): dist[k2] = nd; prev[k2] = (k, r); heapq.heappush(pq, (nd, k2))
    if sb not in dist: return None
    path = [(nodes[sb][0], None)]; k = sb
    while k != sa:
        k, r = prev[k]; path.append((nodes[k][0], r))
    path.reverse()
    return [(p, r or path[i - 1][1]) for i, (p, r) in enumerate(path)]


def drive(path, rough):
    q, rs, acc, last = [], [], 0.0, None
    for p, r in path:
        if last is not None: acc += math.dist(p, last)
        if last is None or acc >= L.RSTEP: q.append((p[0], p[1], H(*p))); rs.append(r); acc = 0.0
        last = p
    n = len(q)
    ys = [p[2] for p in q]   # 길 높이 = 지형 ±12 m 평균(이음매·격자 뾰족함 제거, 나머지는 깎고 메움)
    q = [(p[0], p[1], sum(ys[max(0, i - 3):i + 4]) / len(ys[max(0, i - 3):i + 4])) for i, p in enumerate(q)]
    Rr = [1e9 if (i < L.RH or i >= n - L.RH) else L.radius3(q[i - L.RH], q[i], q[i + L.RH]) for i in range(n)]
    cap = []
    for r in rs:
        c = L.CAP85
        if rough:
            for th, v in ROUGH_CAP:
                if r['roughness'] >= th: c = min(c, v); break
        cap.append(c)
    v = [min(cap[i], math.sqrt(L.A_LAT * Rr[i])) for i in range(n)]; v[0] = v[-1] = 0.0
    for i in range(n - 1): v[i + 1] = min(v[i + 1], math.sqrt(v[i] ** 2 + 2 * L.ACC85 * L.RSTEP))
    for i in range(n - 2, -1, -1): v[i] = min(v[i], math.sqrt(v[i + 1] ** 2 + 2 * L.BRK85 * L.RSTEP))
    return q, rs, Rr, sum(L.RSTEP / max(0.5, (v[i] + v[i + 1]) / 2) for i in range(n - 1))


def fun_check(stops):
    nodes, adj = graph()
    pos = {s['id']: s['pos'] for s in stops}; cargo = {s['id']: s.get('cargo', '') for s in stops}
    out = []
    for a, b in (('garage', 'tea'), ('garage', 'top_house'), ('garage', 'lighthouse'), ('garage', 'hill_field'), ('mart', 'top_house')):
        row = {'pair': f'{a}→{b}', 'cargo': cargo[b]}
        for tag, ok in (('big', cost_big), ('short', cost_short)):
            path = route(nodes, adj, pos[a], pos[b], ok)
            if not path: row[tag] = None; continue
            q, rs, Rr, t0 = drive(path, False); _, _, _, t1 = drive(path, True)
            Ls = len(q) * L.RSTEP
            hp = TM.hairpins([(Ls, [(p[0], p[1]) for p in q], True)])
            st = L.runs([x >= L.STRAIGHT_R for x in Rr])
            g = max((abs(q[i + 2][2] - q[i][2]) / (2 * L.RSTEP) for i in range(len(q) - 2) if rs[i] is rs[i + 2]), default=0)
            row[tag] = {'m': round(Ls), 't': round(t0, 1), 'tRough': round(t1, 1), 'hairpins': len(hp), 'straightMax': round(max((x for _, x in st), default=0)),
                        'roughM': round(sum(L.RSTEP for r in rs if r['roughness'] >= 0.4)), 'gradeMax': round(g * 100, 1), 'gradeP90': round(100 * sorted(abs(q[i + 2][2] - q[i][2]) / (2 * L.RSTEP) for i in range(len(q) - 2))[int(0.9 * (len(q) - 3))], 1),
                        'narrowM': round(sum(L.RSTEP for r in rs if r['width'] < 4)), 'snapM': round(math.dist(path[0][0], pos[a]) + math.dist(path[-1][0], pos[b]))}
        if row.get('big') and row.get('short'):
            row['ratio'] = round(row['short']['t'] / row['big']['t'], 2); row['ratioRough'] = round(row['short']['tRough'] / row['big']['tRough'], 2)
        out.append(row)
    return out


def dijk(nodes, adj, src):
    dist = {src: 0.0}; pq = [(0.0, src)]
    while pq:
        dd, k = heapq.heappop(pq)
        if dd > dist[k]: continue
        for k2, w, r in adj[k]:
            nd = dd + w
            if nd < dist.get(k2, 1e18): dist[k2] = nd; heapq.heappush(pq, (nd, k2))
    return dist


def add_shortcuts(stops):
    """지름길이 없는 배달 쌍(시간비 ≥ 0.95 예상)에 한해: 실제 OSM 사람길(계단 제외) 중 차길 둘을 잇고
    경사 ≤ 28 %인 것 하나를 '차 가능한 좁은 돌길'(3.2 m, 요철 0.5)로 바꾼다. 가장 많이 줄이는 것 하나."""
    pos = {s['id']: s['pos'] for s in stops}
    done = []
    for a, b in (('garage', 'top_house'), ('garage', 'hill_field'), ('garage', 'lighthouse'), ('mart', 'top_house')):
        nodes, adj = graph()
        near = lambda pt: min(range(len(nodes)), key=lambda k: math.dist(nodes[k][0], pt))
        na, nb = near(pos[a]), near(pos[b])
        da, db = dijk(nodes, adj, na), dijk(nodes, adj, nb)
        base = da.get(nb, 1e18)
        grid = defaultdict(list)
        for k, (q, _) in enumerate(nodes): grid[(int(q[0] // 20), int(q[1] // 20))].append(k)
        def snap(q):
            best = None
            for dx in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for k in grid.get((int(q[0] // 20) + dx, int(q[1] // 20) + dz), ()):
                        d = math.dist(nodes[k][0], q)
                        if d < 20 and (best is None or d < best[0]): best = (d, k)
            return best
        cand = None
        for r in ROADS:
            if r['kind'] != 'alley' or r['piece'] not in ('tomo', 'camo', 'posi') or not 25 < plen(r['pts']) < 420: continue
            s0, s1 = snap(r['pts'][0]), snap(r['pts'][-1])
            if not s0 or not s1 or s0[1] == s1[1]: continue
            Ls = plen(r['pts']) + s0[0] + s1[0]
            via = min(da.get(s0[1], 1e18) + Ls + db.get(s1[1], 1e18), da.get(s1[1], 1e18) + Ls + db.get(s0[1], 1e18))
            gain = base - via
            DBG.append((round(gain), a, b, r['cls'], r['piece']))
            if gain < 0.12 * base: continue
            dp = dens(r['pts'], 4.0); hy = [H(*q) for q in dp]
            gm = max((abs(hy[i + 2] - hy[i]) / 8 for i in range(len(hy) - 2)), default=0)
            if gm > 0.28: continue
            if cand is None or gain > cand[0]: cand = (gain, r, s0[1], s1[1], gm)
        if not cand: done.append((a, b, None)); continue
        gain, r, k0, k1, gm = cand
        r['pts'] = [nodes[k0][0]] + r['pts'] + [nodes[k1][0]]
        EDITS.append({'road': r['id'], 'osm': r['osm'], 'from': f"사람길({r['cls']})", 'to': '차 가능한 돌길 3.2 m · 요철 0.5',
                      'why': f'{a}→{b} 지름길 없음 → 실제 사람길을 좁은 돌길로(거리 {base:.0f}→{base - gain:.0f} m, 경사 최대 {gm * 100:.0f} %)'})
        r.update(kind='village', width=3.2, roughness=0.5, cls=r['cls'] + '→car')
        done.append((a, b, r['id']))
    return done


def car_nodes(pid=None):
    return [(q, r) for r in ROADS if r['kind'] in ('main', 'village') and (pid is None or r['piece'] == pid) for q in r['pts']]


def pick_stops(tip):
    tomo, camo, posi = PCS
    def near(pt, pool):
        return min(pool, key=lambda e: math.dist(e[0], pt))[0]
    allc = car_nodes()
    mains = [e for e in allc if e[1]['kind'] == 'main']
    def inbox(e, m=20): return abs(e[0][0]) < HALF - m and abs(e[0][1]) < HALF - m
    tea = near(tomo.world(*tomo.local(*_px('tomonoura', 545, 640))), [e for e in car_nodes('tomo')])
    top = max([e for e in car_nodes('posi') if inbox(e)], key=lambda e: H(*e[0]))[0]
    fld = max([e for e in car_nodes('camo') if inbox(e) and e[0][0] < 300 and H(*e[0]) < 40], key=lambda e: H(*e[0]))[0]
    gar = near((-90, -110), mains)
    mart = near((260, -150), [e for e in mains if e[1]['piece'] == 'camo'] or mains)
    P = lambda q: [round(q[0], 1), round(q[1], 1)]
    return [{'id': 'garage', 'name': '차고', 'pos': P(gar)}, {'id': 'mart', 'name': 'MART', 'pos': P(mart)},
            {'id': 'tea', 'n': 1, 'name': '항구 찻집', 'cargo': 'Box', 'pos': P(tea)},
            {'id': 'top_house', 'n': 2, 'name': '꼭대기 집(할머니)', 'cargo': 'Eggs', 'pos': P(top)},
            {'id': 'lighthouse', 'n': 3, 'name': '등대', 'cargo': 'Glass', 'pos': P(tip)},
            {'id': 'hill_field', 'n': 4, 'name': '언덕 밭', 'cargo': 'Box', 'pos': P(fld)}]


def _px(tid, u, v):            # real_towns 그림 px(900) → 원본 m
    return u / 0.75, 1200 - v / 0.75


# ── 그림 (real_towns 와 같은 축척·색) ──
PX = 900; M2PX = PX / 1200


def to_px(x, z):
    return ((x + 600) * M2PX, (600 - z) * M2PX)


def render(stops, path, title, debug=False):
    N = 300; small = Image.new('RGB', (N, N)); mask = Image.new('L', (N, N))
    ys = [[H(-600 + (j + .5) * 4, 600 - (i + .5) * 4) for j in range(N)] for i in range(N)]
    emax = max(max(r) for r in ys)
    for i in range(N):
        for j in range(N):
            y = ys[i][j]
            if y <= 1: c = (150, 190, 210); mask.putpixel((j, i), 255)
            else:
                t = min(1, y / max(80, emax)); c = (int(238 - 60 * t), int(232 - 45 * t), int(218 - 70 * t))
            small.putpixel((j, i), c)
    img = small.resize((PX, PX), Image.Resampling.BILINEAR)
    edge = mask.resize((PX, PX), Image.Resampling.BILINEAR).point(lambda v: 255 if v > 127 else 0).filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(3))
    img.paste((40, 100, 150), (0, 0), edge)
    dr = ImageDraw.Draw(img)
    for g in GREEN: dr.polygon([to_px(*q) for q in g], fill=(196, 206, 160))
    for b in BLDS: dr.polygon([to_px(*q) for q in b['poly']], fill=(196, 120, 95), outline=(150, 90, 70))
    for p in PIER: dr.line([to_px(*q) for q in p], fill=(120, 120, 120), width=4)
    for r in ROADS:
        if r['kind'] in ('alley', 'stairs'):
            pts = [to_px(*q) for q in r['pts']]
            dr.line(pts, fill=(220, 90, 30) if r['kind'] == 'stairs' else (230, 140, 60), width=2 if r['kind'] == 'stairs' else 1)
    for r in sorted((r for r in ROADS if r['kind'] in ('main', 'village')), key=lambda r: (r['kind'] == 'main', r['width'])):
        pts = [to_px(*q) for q in r['pts']]
        col = (70, 60, 55) if r['kind'] == 'main' else (110, 95, 80) if r['width'] >= 5 else (150, 130, 110)
        dr.line(pts, fill=col, width=max(2, int(r['width'] * M2PX * 1.6)), joint='curve')
        if r['roughness'] >= 0.4:
            for i in range(0, len(pts) - 1, 3): dr.line([pts[i], pts[i + 1]], fill=(240, 220, 160), width=1)
    fs = TM.__dict__.get('_f')
    f = font(22); fs = font(14); fb = font(16)
    if debug:
        for pc in PCS:
            x0, x1, z0, z1 = pc.rect
            dr.polygon([to_px(*pc.world(a, b)) for a, b in ((x0, z0), (x1, z0), (x1, z1), (x0, z1))], outline=(200, 0, 200))
    for s in stops:
        u, v = to_px(*s['pos']); lab = str(s.get('n', 'G' if s['id'] == 'garage' else 'M'))
        dr.ellipse([u - 9, v - 9, u + 9, v + 9], fill=(40, 90, 200) if lab in 'GM' else (220, 60, 50), outline=(255, 255, 255), width=2)
        dr.text((u, v), lab, fill=(255, 255, 255), font=fs, anchor='mm'); dr.text((u + 11, v - 9), s['name'], fill=(20, 20, 20), font=fs)
    dr.rectangle([0, 0, PX, 34], fill=(255, 255, 255))
    dr.text((10, 5), title, fill=(30, 30, 30), font=f)
    bar = 200 * M2PX; dr.rectangle([12, PX - 30, 12 + bar, PX - 24], fill=(30, 30, 30))
    dr.text((16 + bar, PX - 38), f'200 m   고도 최고 {emax:.0f} m', fill=(30, 30, 30), font=fb)
    a = to_px(-HALF, HALF); b = to_px(HALF, -HALF); dr.rectangle([a, b], outline=(90, 90, 90), width=1)
    img.save(path)
    return img


def font(sz):
    from PIL import ImageFont
    for fp in ('C:/Windows/Fonts/malgunbd.ttf', 'C:/Windows/Fonts/malgun.ttf'):
        if os.path.exists(fp): return ImageFont.truetype(fp, sz)
    return ImageFont.load_default()


def legend(width):
    Lg = Image.new('RGB', (width, 40), (255, 255, 255)); dr = ImageDraw.Draw(Lg); fs = font(15)
    items = [((70, 60, 55), '큰길'), ((110, 95, 80), '마을 차길'), ((150, 130, 110), '좁은 차길'), ((240, 220, 160), '돌길 점선(요철 0.4+)'),
             ((220, 90, 30), '계단'), ((230, 140, 60), '사람 길'), ((196, 120, 95), '건물'), ((150, 190, 210), '바다')]
    x = 10
    for c, t in items:
        dr.rectangle([x, 12, x + 22, 28], fill=c); dr.text((x + 28, 10), t, fill=(0, 0, 0), font=fs); x += 170
    return Lg


def main():
    debug = '--debug' in sys.argv
    seam_offsets()
    build_grid()
    cut_pieces()
    widen()
    tip = outer_roads()
    nseam, nloose = seams()
    stops = pick_stops(tip)
    title = '마을 v6-OSM (실제 조각 합성)'
    nb_in = sum(1 for b in BLDS if all(abs(q[0]) <= HALF and abs(q[1]) <= HALF for q in b['poly']))
    img = render(stops, os.path.join(OUT, 'town-v6osm' + ('-debug' if debug else '') + '.png'), f'{title} — 건물 {nb_in}채(칸 안), 760 m 사방', debug)
    print('posi.dy', PCS[2].dy, 'roads', len(ROADS), 'blds', len(BLDS), 'in-box', nb_in, 'seams', nseam, 'loose cut ends', nloose, 'edits', len(EDITS))
    if debug: return
    print('shortcuts', add_shortcuts(stops)); print('dbg', sorted(DBG, reverse=True)[:12], len(DBG))
    fun = fun_check(stops)
    # 측정: 칸 안(760 m)만 잘라 실제 마을과 같은 함수로
    def inb(q): return abs(q[0]) <= HALF and abs(q[1]) <= HALF
    net_roads = []
    for r in ROADS:
        segs, cur = [], []
        for q in dens(r['pts'], 2.0):
            if inb(q): cur.append((q[0] + 600, q[1] + 600))
            elif cur: segs.append(cur); cur = []
        if cur: segs.append(cur)
        for sg in segs:
            if len(sg) > 1:
                net_roads.append({'pts': sg, 'car': r['kind'] in ('main', 'village'), 'steps': r['kind'] == 'stairs', 'cls': r['cls'], 'w': r['width']})
    net = {'name': '마을 v6-OSM', 'roads': net_roads, 'bld': [[(q[0] + 600, q[1] + 600) for q in b['poly']] for b in BLDS if all(inb(q) for q in b['poly'])],
           'h': lambda X, Z: H(X - 600, Z - 600)}
    m = TM.measure(net, geometric=True)
    # 저장(bayloop-plan 호환 필드 최대한)
    roads = []
    for r in ROADS:
        o = {'id': r['id'], 'kind': r['kind'], 'width': r['width'], 'roughness': r['roughness'], 'closed': False,
             'knots': [[round(q[0], 2), round(q[1], 2), round(H(*q), 2)] for q in r['pts']], 'interp': 'linear',
             'source': {'piece': r['piece'], 'osm_way': r['osm'], 'highway': r['cls'], 'name': r['name'], 'surface': r['surface']}}
        if r['kind'] in ('alley', 'stairs'): o['terrainFollows'] = False
        if r.get('out'): o['out'] = r['out']
        roads.append(o)
    ctrl = [[x, z, round(H(x, z), 1)] for x in range(-HALF, HALF + 1, 20) for z in range(-HALF, HALF + 1, 20)]
    plan = {
        'meta': {'name': 'town-v6osm', 'version': 6, 'decision': 'D-156 (PD 0109)', 'units': 'm', 'axes': 'x=동, z=북, y=높이(해면 0). 점은 [x, z, y]',
                 'source': '© OpenStreetMap contributors (ODbL) + SRTM 30 m — docs/map/ATTRIBUTION.md',
                 'rhythmModel': '최고속 85 km/h · 가속 10.7 · 제동 8 m/s² · 굽이 √(5.82·R) + 요철 상한 가설 ROUGH_CAP',
                 'note': 'knots 는 OSM 원본 꺾은선(interp=linear). 이음매(seam*)만 3차 베지어'},
        'pieces': [{'id': p.pid, 'town': p.tid, 'srcCenter': p.c, 'rotDeg': p.rot, 'rect': p.rect, 'at': p.at, 'dy': p.dy, 'why': p.why} for p in PCS],
        'terrain': {'method': 'grid', 'step': 20, 'blend': BLEND, 'controlPoints': ctrl},
        'sea': {'level': 0},
        'roads': roads,
        'buildings': [{'poly': [[round(q[0], 2), round(q[1], 2)] for q in b['poly']], 'y': round(H(*b['poly'][0]), 1), 'piece': b['piece'], 'osm_way': b['osm']} for b in BLDS],
        'garage': stops[0], 'mart': stops[1], 'stops': stops[2:],
        'boundary': {'play': [[-HALF, -HALF], [HALF, -HALF], [HALF, HALF], [-HALF, HALF]]},
        'edits': EDITS, 'fun': fun, 'metrics': m,
    }
    json.dump(plan, open(os.path.join(OUT, 'town-v6osm.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    ims = [img] + [Image.open(os.path.join(OUT, q)).convert('RGB') for q in ('town-v5b.png', 'real-towns/tomonoura.png', 'real-towns/camogli.png', 'real-towns/positano.png')]
    sm = 600; sheet = Image.new('RGB', (sm * len(ims), sm + 40), (255, 255, 255))
    for k, im in enumerate(ims): sheet.paste(im.resize((sm, sm), Image.Resampling.LANCZOS), (k * sm, 0))
    sheet.paste(legend(sm * len(ims)), (0, sm)); sheet.save(os.path.join(OUT, 'v6osm-vs-real.png'))
    print(json.dumps(m, ensure_ascii=False))
    for row in fun: print(json.dumps(row, ensure_ascii=False))
    for e in EDITS: print('edit', json.dumps(e, ensure_ascii=False))


if __name__ == '__main__':
    main()
