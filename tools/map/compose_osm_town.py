"""마을 v6-OSM: 실제 OSM 조각(도모노우라 심장·카몰리 해안 띠·포지타노 비탈)을 잘라 한 해안선 위에 붙인다 (D-156, PD 0109).
데이터: docs/map/real-towns/<id>.json 캐시(OSM Overpass + SRTM 25×25). © OpenStreetMap contributors (ODbL) — docs/map/ATTRIBUTION.md.
좌표: x=동, z=북, 칸 중심 (0,0), 그림은 1200 m 사방(real_towns 와 같은 축척), 플레이 칸 760 m.
사용: python -X utf8 tools/map/compose_osm_town.py [--debug] [--v6b | --v6e]   (--v6e = v6b + 겹친 차길 정리 · 매끈한 높이, 과제 0421)
"""
import heapq, json, math, os, sys
from collections import defaultdict
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
import planlib as L
import town_metrics as TM
import road_check as RC
import drive_check as DC

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
    """v6c: 조각 안 바다는 SRTM(해면 0~3 m)이라 해면 위로 떴다 → OSM coastline 부호 거리로 물 쪽은 해면 아래, 뭍 쪽 물가는 1 m 이상."""
    h = H_raw(x, z)
    if not SEA: return h
    sd = sea_sd(x, z)
    # 해안선은 조각 사각형으로 잘린 꺾은선이라 사각형 밖에선 열린 끝 부호가 흔들린다 → 해안 가진 조각 안(+이음 틈 12 m)만
    if sd is None or not any(p.edge(x, z) > -12 for p in PCS if p.pid in SEA_PCS): return h
    if sd < 0: return min(h, max(-6.0, -1.5 + 0.12 * sd))      # 물: 물가 −1.5 m → 37 m 밖 −6 m
    return max(h, 1.0 + 0.05 * min(sd, 20.0))                  # 물가 뭍: 부두 턱 1 m 이상(해면과 맞닿아 깜빡이지 않게)


SEA, SEA_PCS = {}, set()


def build_sea(x0=-420, x1=420, z0=-420, z1=-80, step=4, reach=150.0):
    """OSM coastline(물 = 진행 방향 오른쪽) → 부호 거리 격자(뭍 +, 물 −). 꼭짓점이 가장 가까우면 양옆 방향의 합으로 부호를 정한다."""
    segs = []
    for s in COAST:
        for k in range(len(s) - 1):
            a, b = s[k], s[k + 1]
            if math.dist(a, b) < 1e-6: continue
            pv = s[k - 1] if k > 0 else None; nx = s[k + 2] if k + 2 < len(s) else None
            segs.append((a, b, pv, nx))
    def unit(p, q):
        L_ = math.dist(p, q) or 1; return ((q[0] - p[0]) / L_, (q[1] - p[1]) / L_)
    def sd(x, z):
        best, sg = 1e9, 1
        for a, b, pv, nx in segs:
            dx, dz = b[0] - a[0], b[1] - a[1]; t = ((x - a[0]) * dx + (z - a[1]) * dz) / (dx * dx + dz * dz)
            tc = min(1.0, max(0.0, t)); px, pz = a[0] + dx * tc, a[1] + dz * tc
            d = math.hypot(x - px, z - pz)
            if d < best - 1e-9:
                best = d; u = unit(a, b)
                if tc <= 0 and pv is not None: w = unit(pv, a); u = (u[0] + w[0], u[1] + w[1])
                elif tc >= 1 and nx is not None: w = unit(b, nx); u = (u[0] + w[0], u[1] + w[1])
                sg = 1 if u[0] * (z - pz) - u[1] * (x - px) >= 0 else -1     # 왼쪽 = 뭍
        return sg * best
    nx_, nz_ = int((x1 - x0) / step) + 1, int((z1 - z0) / step) + 1
    SEA.update(x0=x0, z0=z0, step=step, nx=nx_, nz=nz_, reach=reach,
               g=[[sd(x0 + i * step, z0 + j * step) for j in range(nz_)] for i in range(nx_)])


def sea_sd(x, z):
    st = SEA['step']; fx, fz = (x - SEA['x0']) / st, (z - SEA['z0']) / st
    if not (0 <= fx < SEA['nx'] - 1 and 0 <= fz < SEA['nz'] - 1): return None
    i, j = int(fx), int(fz); tx, tz = fx - i, fz - j; g = SEA['g']
    v = g[i][j] * (1 - tx) * (1 - tz) + g[i + 1][j] * tx * (1 - tz) + g[i][j + 1] * (1 - tx) * tz + g[i + 1][j + 1] * tx * tz
    return v if abs(v) < SEA['reach'] else None


def H_raw(x, z):
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
LOOSE, SEAMFIX, REMOVED = [], [], []          # v6b: 끊긴 끝 · 이음매 곡선 다듬기 · 겹쳐서 뺀 건물
YN, CF = {}, {}                               # v6b: 차길 노드 높이 · 길 따라 깎고 메운 지형 보정 격자
KEY = lambda q: (round(q[0], 1), round(q[1], 1))


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
                cs = [seg for seg, _, _ in clip_line(pts, pc)]; COAST.extend(cs)
                if cs: SEA_PCS.add(pc.pid)
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


def bezier(p0, d0, p1, d1, step=4.0, ks=0.4):
    D = math.dist(p0, p1); k = D * ks
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
                      'cls': 'seam', 'name': '이음매', 'surface': '', 'tan': (end_dir(ra['pts'], sa), end_dir(rb['pts'], sb))})
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
            loose += 1; LOOSE.append({'road': ra['id'], 'piece': ra['piece'], 'at': [round(pa[0]), round(pa[1])], 'kind': ra['kind']}); continue
        D, rb, i = best; q = rb['pts'][i]
        j0, j1 = max(0, i - 1), min(len(rb['pts']) - 1, i + 1)
        tx, tz = rb['pts'][j1][0] - rb['pts'][j0][0], rb['pts'][j1][1] - rb['pts'][j0][1]; ln = math.hypot(tx, tz) or 1
        nx, nz = -tz / ln, tx / ln
        if (pa[0] - q[0]) * nx + (pa[1] - q[1]) * nz < 0: nx, nz = -nx, -nz
        pts = bezier(pa, da, q, (nx, nz))
        ROADS.append({'id': f'seam{n}_{ra["piece"]}_{rb["piece"]}_T', 'kind': 'village', 'width': min(ra['width'], rb['width'], 6.0),
                      'roughness': ra['roughness'], 'pts': pts, 'cut': (False, False), 'piece': 'seam', 'osm': None, 'cls': 'seam', 'name': '이음매(T)', 'surface': '', 'tan': (da, (nx, nz))})
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


def cost_big(r):        # 안전한 길: 큰길 1 · 넓은 마을 차길 1.8 · 좁거나 거친 길 4 (가설) · v6c 급경사 돌길은 안전한 길이 아니다(30)
    if r.get('grade'): return 30.0
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
        if last is None or acc >= L.RSTEP: q.append((p[0], p[1], YN.get(KEY(p), H(*p)))); rs.append(r); acc = 0.0
        last = p
    n = len(q)
    if not YN:               # v6: 길 높이 = 지형 ±12 m 평균. v6b 는 solve_heights 의 매끈한 노드 높이를 그대로
        ys = [p[2] for p in q]
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
            gd = lambda i: abs(q[i + 2][2] - q[i][2]) / max(1.0, math.dist(q[i][:2], q[i + 2][:2]))     # 실제 수평거리로(샘플 간격 4~6 m)
            g = max((gd(i) for i in range(len(q) - 2) if rs[i] is rs[i + 2]), default=0)
            row[tag] = {'m': round(Ls), 't': round(t0, 1), 'tRough': round(t1, 1), 'hairpins': len(hp), 'straightMax': round(max((x for _, x in st), default=0)),
                        'roughM': round(sum(L.RSTEP for r in rs if r['roughness'] >= 0.4)), 'gradeMax': round(g * 100, 1), 'gradeP90': round(100 * sorted(gd(i) for i in range(len(q) - 2))[int(0.9 * (len(q) - 3))], 1),
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


# ── v6b ──────────────────────────────────────────────────────────────
CHAIN_DBG, CHAIN_GAIN, FREE_RATIO = [], 0.8, 0.75     # 고정 쌍은 거리 0.8 이하, 배달점 옮길 땐 0.75 이하 + 60 m 이상 줄 때만
G_ROAD, G_PIN, PIN_R = 0.15, 0.18, 20.0     # 차길 경사 상한 15 %, 헤어핀(R < 20 m) 18 % (과제 0122)
G_STEEP = 0.22                              # v6c: 계단 옆 급경사 돌길 상한 22 % (과제 0214 안 a)


def side(pts, off):
    """꺾은선을 진행 방향 오른쪽으로 off m 민다(끝점 포함, 앞뒤 방향 평균)."""
    out = []
    for i, q in enumerate(pts):
        a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
        dx, dz = b[0] - a[0], b[1] - a[1]; L_ = math.hypot(dx, dz) or 1
        out.append((q[0] + dz / L_ * off, q[1] - dx / L_ * off))
    return out


def unfold(pts, max_turn=95.0, min_seg=3.0):
    """v6d: 옆으로 민 사슬의 접힘을 편다 — 꺾임 > max_turn 도인 점, min_seg m 보다 짧은 마디를 지운다(끝점은 둔다).
    접힌 곳은 수평거리가 짧아져 같은 높이차가 경사 46 % 로 잡힌다(봇 실측, 과제 0246)."""
    pts = list(pts)
    while len(pts) > 2:
        bad = None
        for i in range(1, len(pts) - 1):
            a, b, c = pts[i - 1], pts[i], pts[i + 1]
            u, v = (b[0] - a[0], b[1] - a[1]), (c[0] - b[0], c[1] - b[1])
            lu, lv = math.hypot(*u), math.hypot(*v)
            if lu < min_seg or lv < min_seg: bad = i; break
            cs = (u[0] * v[0] + u[1] * v[1]) / (lu * lv)
            if math.degrees(math.acos(max(-1, min(1, cs)))) > max_turn: bad = i; break
        if bad is None: break
        del pts[bad]
    return pts


def solve_heights():
    """차길 노드 높이: 위 덮개 U(i)=min_j(H_j+g·길거리), 아래 받침 Lo(i)=max_j(H_j−g·길거리) 의 가운데.
    U·Lo 둘 다 어느 두 노드 사이도 경사 ≤ g 라, 가운데도 ≤ g 가 보장된다. 지형과 다른 만큼은 깎고 메운다.
    계단·사람길은 그래프에 없어서 예외(지형 그대로)."""
    YN.clear()
    nodes, adj = graph()
    n = len(nodes); idx = {KEY(q): k for k, (q, _) in enumerate(nodes)}
    h0 = [max(1.5, H(*q)) for q, _ in nodes]
    gk = [G_ROAD] * n
    for r in ROADS:
        if r['kind'] not in ('main', 'village'): continue
        dp = dens(r['pts'])
        for i in range(6, len(dp) - 6):
            if L.radius3(dp[i - 6], dp[i], dp[i + 6]) < PIN_R:
                k = idx.get(KEY(dp[i]))
                if k is not None: gk[k] = G_PIN
    for r in ROADS:                                  # v6c: 급경사 돌길은 제 상한(22 %)
        if r.get('grade'):
            for q in dens(r['pts']):
                k = idx.get(KEY(q))
                if k is not None: gk[k] = max(gk[k], r['grade'])
    def env(sign):
        dist = [sign * h for h in h0]; pq = [(v, k) for k, v in enumerate(dist)]; heapq.heapify(pq)
        while pq:
            dd, k = heapq.heappop(pq)
            if dd > dist[k]: continue
            for k2, w, _ in adj[k]:
                nd = dd + w * max(gk[k], gk[k2])
                if nd < dist[k2]: dist[k2] = nd; heapq.heappush(pq, (nd, k2))
        return [sign * v for v in dist]
    U, Lo = env(1), env(-1)
    ys = [(u + l) / 2 for u, l in zip(U, Lo)]
    for k, (q, _) in enumerate(nodes): YN[KEY(q)] = ys[k]
    # 통계: 실제 경사(노드 사이), 원래 지형 경사(8 m), 깎고 메운 깊이
    gr = sorted(abs(ys[k] - ys[k2]) / w for k in range(n) for k2, w, _ in adj[k] if w > 0.5)
    raw = []
    for r in ROADS:
        if r['kind'] not in ('main', 'village'): continue
        dp = dens(r['pts'], 4.0); hy = [H(*q) for q in dp]
        raw += [abs(hy[i + 2] - hy[i]) / 8 for i in range(len(hy) - 2)]
    raw.sort(); cf = sorted(abs(ys[k] - h0[k]) for k in range(n))
    pc = lambda a, p: round(a[min(len(a) - 1, int(p * len(a)))], 3) if a else 0
    return {'nodes': n, 'pinNodes': sum(1 for g in gk if g == G_PIN), 'gradeMax': pc(gr, 1), 'gradeP99': pc(gr, .99), 'gradeP90': pc(gr, .9),
            'rawTerrainGradeMax': pc(raw, 1), 'rawP90': pc(raw, .9), 'cutFillP50': pc(cf, .5), 'cutFillP90': pc(cf, .9), 'cutFillMax': pc(cf, 1)}


def build_cut_fill(step=4, band=15.0):
    """지형을 차길에 맞춘다: 길 폭/2+1 m 안은 길 높이, 그 밖 15 m 에 걸쳐 원래 지형으로 S 자 복귀. 가장 가까운 길이 이긴다."""
    g = {}
    for r in ROADS:
        if r['kind'] not in ('main', 'village'): continue
        hw = r['width'] / 2 + 1; R = hw + band; m = int(R / step) + 1
        for q in dens(r['pts'], 2.0):
            y = YN.get(KEY(q))
            if y is None: continue
            d0 = y - H(*q); ci, cj = int(round((q[1] + 650) / step)), int(round((q[0] + 650) / step))
            for i in range(ci - m, ci + m + 1):
                for j in range(cj - m, cj + m + 1):
                    d = math.hypot(-650 + j * step - q[0], -650 + i * step - q[1])
                    if d > R: continue
                    w = 1.0 if d <= hw else S(1 - (d - hw) / band)
                    if w > g.get((i, j), (0, 0))[0]: g[(i, j)] = (w, w * d0)
    CF['step'] = step; CF['g'] = g


def H2(x, z):
    h = H(x, z)
    if not CF: return h
    st, g = CF['step'], CF['g']; fx, fz = (x + 650) / st, (z + 650) / st
    j, i = int(math.floor(fx)), int(math.floor(fz)); tx, tz = fx - j, fz - i
    v = lambda a, b: g.get((a, b), (0, 0.0))[1]
    return h + v(i, j) * (1 - tx) * (1 - tz) + v(i, j + 1) * tx * (1 - tz) + v(i + 1, j) * (1 - tx) * tz + v(i + 1, j + 1) * tx * tz


def chain_shortcut(a, b, pos, banned, tag, free=None, stairs=False, grade=None):
    """실제 사람길(계단 제외) 여러 개를 이은 사슬 가운데, 차길 a 쪽 끝과 b 쪽 끝을 15 m 안에서 잇고
    a→b 거리를 가장 많이 줄이는 것. 사슬 평균 경사 ≤ 15 % 만(그 이상은 깎아도 너무 깊다).
    free=(조각, 최저 높이): b 를 고정하지 않고, 사슬이 닿는 그 조각 차길 끝(높이 ≥ 최저) 가운데 줄이는 거리가 가장 큰 곳을 배달점으로."""
    nodes, adj = graph()
    near = lambda pt: min(range(len(nodes)), key=lambda k: math.dist(nodes[k][0], pt))
    na, nb = near(pos[a]), near(pos[b]); da = dijk(nodes, adj, na); db = {} if free else dijk(nodes, adj, nb)
    base = da.get(nb, 1e18)
    cg = defaultdict(list)
    for k, (q, _) in enumerate(nodes): cg[(int(q[0] // 15), int(q[1] // 15))].append(k)
    def snap(q):
        best = None
        for dx in (-1, 0, 1):
            for dz in (-1, 0, 1):
                for k in cg.get((int(q[0] // 15) + dx, int(q[1] // 15) + dz), ()):
                    d = math.dist(nodes[k][0], q)
                    if d < 15 and (best is None or d < best[0]): best = (d, k)
        return best
    fid, fp, fadj = {}, [], defaultdict(list)
    def nid(q):
        k = KEY(q)
        if k not in fid: fid[k] = len(fp); fp.append(q)
        return fid[k]
    for r in ROADS:
        if r['kind'] not in (('alley', 'stairs') if stairs else ('alley',)) or r['piece'] not in ('tomo', 'camo', 'posi') or r['id'] in banned: continue
        ids = [nid(q) for q in r['pts']]
        for u, v in zip(ids, ids[1:]):
            if u != v: d = math.dist(fp[u], fp[v]); fadj[u].append((v, d, r)); fadj[v].append((u, d, r))
    sn = [snap(q) for q in fp]
    dist, prev, pq = {}, {}, []
    for f, s in enumerate(sn):
        if s and s[1] in da:
            c = da[s[1]] + s[0]; dist[f] = c; prev[f] = ('car', s[1]); pq.append((c, f))
    heapq.heapify(pq)
    while pq:
        dd, f = heapq.heappop(pq)
        if dd > dist[f]: continue
        for f2, w, r in fadj[f]:
            nd = dd + w
            if nd < dist.get(f2, 1e18): dist[f2] = nd; prev[f2] = (f, r); heapq.heappush(pq, (nd, f2))
    cands = []
    for f, dd in dist.items():
        s = sn[f]
        if not s or prev[f][0] == 'car': continue
        if free:
            if s[1] not in da: continue
            q = nodes[s[1]][0]; bs = da[s[1]]
            if nodes[s[1]][1]['piece'] != free[0] or H(*q) < free[1] or abs(q[0]) > HALF - 20 or abs(q[1]) > HALF - 20: continue
            if dd + s[0] <= FREE_RATIO * bs and bs - dd - s[0] >= 60: cands.append((dd + s[0] - bs, f, s[1], bs))
        elif s[1] in db:
            cands.append((dd + s[0] + db[s[1]], f, s[1], base))
    CHAIN_DBG.append((a, b, round(base), len(cands), sorted(cands)[:1] and round(sorted(cands)[0][0])))
    for key, f, kend, base in sorted(cands)[:60]:
        tot = key + base if free else key
        if tot > CHAIN_GAIN * base: break
        chain, rs, k = [fp[f]], [], f
        while prev[k][0] != 'car':
            k, r = prev[k]; chain.append(fp[k]); rs.append(r)
        kstart = prev[k][1]; chain.reverse()
        if stairs and any(r['kind'] == 'stairs' for r in rs): chain = side(chain, 4.5)     # v6c 안 (a): 계단은 두고 4.5 m 옆 비탈에
        pts = [nodes[kstart][0]] + chain + [nodes[kend][0]]
        if stairs: pts = unfold(pts)                                 # v6d: 민 사슬 접힘 펴기
        g_ok = grade or G_ROAD; yh = lambda k: YN.get(KEY(nodes[k][0]), H(*nodes[k][0]))
        Lc = plen(pts); dh = abs(yh(kend) - yh(kstart)) if stairs else abs(H(*pts[-1]) - H(*pts[0]))
        CHAIN_DBG.append(('cand', round(tot), round(Lc), round(dh / Lc, 3)))
        if Lc < 20 or dh / Lc > g_ok * 0.9: continue
        ways = list(dict.fromkeys(r['id'] for r in rs)); osm = list(dict.fromkeys(r['osm'] for r in rs))
        surf = [r['surface'] for r in rs if r['surface']]
        rough = min(0.6, max(0.3, max((ROUGH_SURF.get(s, 0.5) for s in surf), default=0.5)))
        if stairs: rough = 0.6                                   # 급경사 돌길(과제 0214: 0.5~0.7)
        piece = rs[0]['piece']
        ROADS.append({'id': f'sc_{tag}', 'kind': 'village', 'width': 3.2, 'roughness': rough, 'pts': pts, 'cut': (False, False), 'piece': piece,
                      'osm': osm, 'cls': '+'.join(dict.fromkeys(r['cls'] for r in rs)) + '→car', 'name': '급경사 돌길' if stairs else '골목 돌길', 'surface': surf[0] if surf else '',
                      **({'grade': g_ok} if stairs else {})})
        keys = {KEY(q) for q in chain}
        gone = [r['id'] for r in ROADS if r['id'] in ways and r['kind'] != 'stairs' and all(KEY(q) in keys for q in r['pts'])]
        ROADS[:] = [r for r in ROADS if r['id'] not in gone]
        why = ('계단 옆 골목' if any(r['kind'] == 'stairs' and min(math.dist(q, s) for q in pts[::3] for s in r['pts'][::2]) < 12 for r in ROADS if r['piece'] == piece) else '실제 골목·산책로')
        EDITS.append({'road': f'sc_{tag}', 'osm': osm, 'from': f'사람길 {len(ways)}개 사슬', 'to': f'차 가능한 돌길 3.2 m · 요철 {rough}',
                      'why': f'{a}→{b}: {why}(실제 길, 표면 {surf[0] if surf else "태그 없음"}) — 거리 {base:.0f}→{tot:.0f} m, 평균 경사 {100 * dh / Lc:.0f} %, 사슬 {Lc:.0f} m'})
        return {'pair': f'{a}→{b}', 'id': f'sc_{tag}', 'end': [round(nodes[kend][0][0], 1), round(nodes[kend][0][1], 1)], 'ways': len(ways), 'm': round(Lc), 'base': round(base), 'via': round(tot), 'avgGrade': round(dh / Lc, 3)}
    return None


def seam_radius(pts):
    dp = dens(pts, 2.0)
    return min((L.radius3(dp[i - 3], dp[i], dp[i + 3]) for i in range(3, len(dp) - 3)), default=1e9)


def fix_seams():
    """이음매 곡선 반경이 12 m 미만이면 손잡이 길이(0.25~0.6)를 바꿔 가장 완만한 것으로."""
    for r in ROADS:
        if r['piece'] != 'seam' or 'tan' not in r: continue
        R0 = seam_radius(r['pts'])
        if R0 >= 12: continue
        p0, p1 = r['pts'][0], r['pts'][-1]
        best = max(((seam_radius(bezier(p0, r['tan'][0], p1, r['tan'][1], ks=ks)), ks) for ks in (0.25, 0.3, 0.35, 0.45, 0.5, 0.6)))
        if best[0] > R0: r['pts'] = bezier(p0, r['tan'][0], p1, r['tan'][1], ks=best[1])
        SEAMFIX.append({'road': r['id'], 'R_before': round(R0, 1), 'R_after': round(max(best[0], R0), 1), 'ks': best[1] if best[0] > R0 else 0.4,
                        'at': [round((p0[0] + p1[0]) / 2), round((p0[1] + p1[1]) / 2)]})


def _shrink(poly, s=0.35):
    cx = sum(q[0] for q in poly) / len(poly); cz = sum(q[1] for q in poly) / len(poly)
    out = []
    for q in poly:
        d = math.dist(q, (cx, cz)) or 1; t = max(0.0, 1 - s / d); out.append((cx + (q[0] - cx) * t, cz + (q[1] - cz) * t))
    return out


def _seg_x(a, b, c, d):
    o = lambda p, q, r: (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0])
    return o(a, b, c) * o(a, b, d) < 0 and o(c, d, a) * o(c, d, b) < 0


def clean_overlaps():
    """건물 겹침 0: (1) 차길 노면(중심선±폭/2) 위 건물, (2) 다른 조각 건물끼리 겹침 → 조각 안쪽 깊이가 얕은 쪽을 뺀다."""
    pcd = {p.pid: p for p in PCS}
    sp = [_shrink(b['poly']) for b in BLDS]
    bb = [(min(q[0] for q in p), max(q[0] for q in p), min(q[1] for q in p), max(q[1] for q in p)) for p in sp]
    rg = defaultdict(list)
    for r in ROADS:
        if r['kind'] not in ('main', 'village'): continue
        for q in dens(r['pts'], 1.5): rg[(int(q[0] // 10), int(q[1] // 10))].append((q, r))
    drop = {}
    for i, b in enumerate(BLDS):
        x0, x1, z0, z1 = bb[i]
        for gx in range(int((x0 - 5) // 10), int((x1 + 5) // 10) + 1):
            for gz in range(int((z0 - 5) // 10), int((z1 + 5) // 10) + 1):
                for q, r in rg.get((gx, gz), ()):
                    hw = r['width'] / 2 if r['piece'] != b['piece'] else 0.0      # 같은 조각 실제 길은 중심선만(폭은 widen 이 처리)
                    if not (x0 - hw <= q[0] <= x1 + hw and z0 - hw <= q[1] <= z1 + hw): continue
                    p = sp[i]
                    if L.in_poly(q[0], q[1], p) or (hw and min(L.seg_dist(q[0], q[1], *p[k], *p[(k + 1) % len(p)])[0] for k in range(len(p))) < hw):
                        drop.setdefault(i, f"차길 {r['id']} 위"); break
                if i in drop: break
            if i in drop: break
    bg = defaultdict(list)
    for i in range(len(BLDS)): bg[(int(bb[i][0] // 20), int(bb[i][2] // 20))].append(i)
    for i, b in enumerate(BLDS):
        if i in drop: continue
        for gx in (-1, 0, 1):
            for gz in (-1, 0, 1):
                for j in bg.get((int(bb[i][0] // 20) + gx, int(bb[i][2] // 20) + gz), ()):
                    if j <= i or j in drop or BLDS[j]['piece'] == b['piece']: continue
                    A, B = bb[i], bb[j]
                    if A[1] < B[0] or B[1] < A[0] or A[3] < B[2] or B[3] < A[2]: continue
                    pa, pb = sp[i], sp[j]
                    hit = any(L.in_poly(*q, pb) for q in pa) or any(L.in_poly(*q, pa) for q in pb) or \
                        any(_seg_x(pa[k], pa[(k + 1) % len(pa)], pb[m], pb[(m + 1) % len(pb)]) for k in range(len(pa)) for m in range(len(pb)))
                    if hit:
                        ei = pcd[b['piece']].edge(*b['poly'][0]); ej = pcd[BLDS[j]['piece']].edge(*BLDS[j]['poly'][0])
                        k = i if ei < ej else j; drop[k] = f"다른 조각 건물과 겹침({b['piece']}↔{BLDS[j]['piece']})"
    for i, why in drop.items():
        REMOVED.append({'piece': BLDS[i]['piece'], 'osm': BLDS[i]['osm'], 'at': [round(BLDS[i]['poly'][0][0]), round(BLDS[i]['poly'][0][1])], 'why': why})
    BLDS[:] = [b for i, b in enumerate(BLDS) if i not in drop]
    return len(drop)


# ── v6e (과제 0421): 겹친 차길 정리 · 갈림목까지 이어지는 매끈한 높이 ─────────────
OVL, JUNC = [], []                   # 겹친 쌍 처리 기록 · 갈림목(여러 차길이 공유한 점) 목록
OVL_SAME = 0.6                       # 겹친 구간 평균 높이차 < 0.6 m → 같은 길을 두 번 그린 것(합침), 이상 → 실제 다른 층(사람길·계단으로)
K_LIM, K_LIM_STONE = 0.029, 0.059    # 경사 변화 한도(3 %/m · 돌길 6 %/m) 에서 0.1 %/m 여유


def car_samples(r):
    """차길 → 1 m 표본 [(x, z, y)], y = solve_heights 노드 높이(원래 점) 직선 보간."""
    return RC.resample([(q[0], q[1], YN.get(KEY(q), H(*q))) for q in r['pts']], 1.0)


def _rank(r):                        # 남길 쪽: 큰길 > 지름길 돌길(sc_) > 넓은 길 > 긴 길
    return (r['kind'] == 'main', r['id'].startswith('sc_'), r['width'], plen(r['pts']))


def _snap_into(o, q):
    """q 에서 길 o 위 가장 가까운 점을 o['pts'] 에 끼워 넣고 그 점을 돌려준다(이미 점이면 그 점)."""
    best = None
    for k, (a, b) in enumerate(zip(o['pts'], o['pts'][1:])):
        dx, dz = b[0] - a[0], b[1] - a[1]; ll = dx * dx + dz * dz or 1
        t = max(0.0, min(1.0, ((q[0] - a[0]) * dx + (q[1] - a[1]) * dz) / ll)); p = (a[0] + dx * t, a[1] + dz * t)
        d = math.dist(p, q)
        if best is None or d < best[0]: best = (d, k, t, p)
    d, k, t, p = best
    if t < 0.02 or math.dist(p, o['pts'][k]) < 0.5: return o['pts'][k]
    if t > 0.98 or math.dist(p, o['pts'][k + 1]) < 0.5: return o['pts'][k + 1]
    o['pts'].insert(k + 1, p); return p


PROTECT = set()                      # 배달 지름길로 바꾼 길(EDITS 에서 '돌길' 로 간 것) — 사람길로 돌리지 않는다


def push_apart(lo, hi, Q, i0, i1, taper=12.0):
    """lo 의 표본 Q[i0..i1] 을 hi 에서 멀어지는 쪽으로, 간격이 (두 폭 합/2 + 2.5 m) 가 되게 민다. 앞뒤 taper m 는 S 자로 줄인다.
    민 자리가 바다(H < 0.8)면 포기(None). 돌려주는 값 = 가장 많이 민 거리."""
    thr = (lo['width'] + hi['width']) / 2 + RC.GAP_EXTRA + 0.5
    hp = dens(hi['pts'], 1.0)
    def away(q):
        p = min(hp, key=lambda s: (s[0] - q[0]) ** 2 + (s[1] - q[1]) ** 2); d = math.dist(p, q[:2]) or 1e-3
        return d, ((q[0] - p[0]) / d, (q[1] - p[1]) / d)
    acc = [0.0]
    for a, b in zip(Q, Q[1:]): acc.append(acc[-1] + math.dist(a[:2], b[:2]))
    s0, s1 = acc[i0], acc[i1]
    need = max(thr - away(Q[k])[0] for k in range(i0, i1 + 1))
    if need <= 0: return 0.0
    out, mx = [], 0.0
    for k, q in enumerate(Q):
        s = acc[k]
        w = 1.0 if s0 <= s <= s1 else S((taper - (s0 - s)) / taper) if s0 - taper < s < s0 else S((taper - (s - s1)) / taper) if s1 < s < s1 + taper else 0.0
        if k in (0, len(Q) - 1): w = 0.0
        if w <= 0: out.append((q[0], q[1])); continue
        d, (ux, uz) = away(q); m = w * max(0.0, thr - d) if s0 <= s <= s1 else w * need
        p = (q[0] + ux * m, q[1] + uz * m)
        if H(*p) < 0.8: return None
        out.append(p); mx = max(mx, m)
    lo['pts'] = out
    return mx


def reach_ok(stops):
    nodes, adj = graph()
    near = lambda pt: min(range(len(nodes)), key=lambda k: math.dist(nodes[k][0], pt))
    st = {x['id']: x['pos'] for x in stops}; dd = dijk(nodes, adj, near(st['garage']))
    return all(math.dist(nodes[k][0], p) < 15 and k in dd for p in st.values() for k in [near(p)])


def resolve_overlaps(stops, rounds=8):
    """나란히 겹친 차길 쌍(RC.overlaps)을 정리한다. 낮은 쪽(_rank) 길의 겹친 구간을
    · 높이차 < OVL_SAME: 지운다(같은 길 두 번) — 잘린 끝은 남는 길 위로 붙인다.
    · 높이차 ≥ OVL_SAME: 사람길(경사 > 15 % 면 계단)로 돌린다 — 실제로 다른 높이의 길이 나란히 있는 것."""
    done = set()
    PROTECT.update(e['road'] for e in EDITS if 'road' in e and '돌길' in str(e.get('to', '')))
    PROTECT.update(r['id'] for r in ROADS if r['id'].startswith('sc_'))
    for rnd in range(rounds):
        car = [r for r in ROADS if r['kind'] in ('main', 'village')]
        sm = {r['id']: {'id': r['id'], 'kind': r['kind'], 'width': r['width'], 'roughness': r['roughness'], 'pts': car_samples(r)} for r in car}
        ov = [o for o in (RC.overlaps(list(sm.values()), gap_extra=1.0, run_min=5) if V6F else RC.overlaps(list(sm.values()))) if (o['a'], o['b']) not in done and (o['b'], o['a']) not in done]
        if not ov: return rnd
        byid = {r['id']: r for r in car}; touched = set()
        for o in ov:
            if o['a'] in touched or o['b'] in touched: continue
            ra, rb = byid[o['a']], byid[o['b']]
            lo, hi = (ra, rb) if _rank(ra) < _rank(rb) else (rb, ra)
            rec = [x for x in RC.overlaps([sm[lo['id']], sm[hi['id']]], both=True) if x['a'] == lo['id']] or \
                  [x for x in RC.overlaps([sm[lo['id']], sm[hi['id']]], run_min=4, both=True) if x['a'] == lo['id']]
            done.add((o['a'], o['b']))
            if not rec: continue
            x = rec[0]; S = sm[lo['id']]['pts']; i0, i1 = max(0, x['i0'] - 1), min(len(S) - 1, x['i1'] + 1)
            pre, mid, post = S[:i0 + 1], S[i0:i1 + 1], S[i1:]
            same = o['dy'] < OVL_SAME
            mlen = sum(math.dist(a[:2], b[:2]) for a, b in zip(mid, mid[1:]))
            gr = abs(mid[-1][2] - mid[0][2]) / max(1.0, mlen)
            rec0 = {k: o[k] for k in ('a', 'b', 'm', 'gapMin', 'dy', 'dyMax', 'at')}; dyv = o['dy']
            self_pair = lo['id'].split('~')[0] == hi['id'].split('~')[0]           # 한 길의 두 다리(헤어핀) — 끊지 않는다
            prot = lo['id'].split('~')[0] in PROTECT
            def try_push(why):
                nonlocal lo, hi
                if V6F:                                      # v6f: 어느 쪽을 밀지 — 민 띠가 건물에 덜 닿는 쪽(0444: ~a 68 m·~b 38 m 건물 위)
                    best = None
                    for a_, b_ in ((lo, hi), (hi, lo)):
                        Sa = car_samples(a_); r_ = [y for y in RC.overlaps([{**a_, 'pts': Sa}, {**b_, 'pts': car_samples(b_)}], run_min=4, both=True) if y['a'] == a_['id']]
                        if not r_: continue
                        keep = list(a_['pts']); mv = push_apart(a_, b_, Sa, r_[0]['i0'], r_[0]['i1'])
                        cost = bld_cost(a_) if mv is not None else 1e9; a_['pts'] = keep
                        if best is None or cost < best[0]: best = (cost, a_, b_, Sa, r_[0])
                    if best and best[0] < 1e9:
                        _, lo, hi, Sb, xb = best; moved = push_apart(lo, hi, Sb, xb['i0'], xb['i1'])
                        OVL.append({**rec0, 'keep': hi['id'], 'cut': lo['id'], 'action': 'push', 'fix': f'{why} → 끊지 않고 {lo["id"]} 를 최대 {moved:.1f} m 옆으로(건물에 덜 닿는 쪽, 닿는 표본 {best[0]})'})
                        return
                moved = push_apart(lo, hi, S, x['i0'], x['i1'])
                if moved is None:
                    OVL.append({**rec0, 'keep': hi['id'], 'cut': None, 'action': 'unresolved', 'fix': f'{why} → 옆으로 밀 자리가 바다라 그대로 둠'}); return
                OVL.append({**rec0, 'keep': hi['id'], 'cut': lo['id'], 'action': 'push', 'fix': f'{why} → 끊지 않고 {lo["id"]} 를 최대 {moved:.1f} m 옆으로 밀어 간격 확보'})
            touched |= {lo['id'], hi['id']}
            if lo.get('piece') == 'outer' and hi.get('piece') == 'outer':     # 곶 끝: 합성기가 그린 등대 길 셋 — 합치면 등대 지름길이 사라진다(0.77→0.98 실측)
                OVL.append({**rec0, 'keep': hi['id'], 'cut': None, 'action': 'unresolved', 'fix': '곶 끝 합성 길(등대 지름길 포함) — 합치면 등대 시간비 0.77→0.98, 다음 판에 곶 길 다시 그림'}); continue
            if self_pair: try_push(f'헤어핀 두 다리(높이차 {dyv:.1f} m)'); continue
            if prot and not same: try_push(f'높이차가 실제({dyv:.1f} m)지만 {lo["id"]} 는 배달 지름길'); continue
            save_roads, save_hi = list(ROADS), list(hi['pts'])
            base = {k: v for k, v in lo.items() if k not in ('pts', 'id')}
            new = []
            for tag, seg, at_end in (('a', pre, True), ('b', post, False)):
                if len(seg) < 2: continue
                pts = [(q[0], q[1]) for q in seg]
                if same:
                    s = _snap_into(hi, pts[-1] if at_end else pts[0])
                    if math.dist(s, pts[-1] if at_end else pts[0]) > 0.3: pts = pts + [s] if at_end else [s] + pts
                if plen(pts) < 6 and not same: continue
                if plen(pts) < 4: continue
                new.append({**base, 'id': f"{lo['id']}~{tag}", 'pts': pts})
            if not same:
                kind = 'stairs' if gr > 0.15 else 'alley'
                new.append({**base, 'id': f"{lo['id']}~{kind}", 'kind': kind, 'width': 2.0, 'pts': [(q[0], q[1]) for q in mid], 'grade': None})
            ROADS[:] = [r for r in ROADS if r is not lo] + new
            if not reach_ok(stops):                          # 배달점이 끊기면 되돌리고 옆으로 민다
                ROADS[:] = save_roads; hi['pts'] = save_hi
                try_push(('같은 길 두 번' if same else f'높이차 {dyv:.1f} m') + '이지만 지우면 배달점이 끊김'); continue
            fix = (f'같은 길 두 번 → {lo["id"]} 의 겹친 {mlen:.0f} m 를 지우고 끝을 {hi["id"]} 에 붙임' if same else
                   f'높이차가 실제({dyv:.1f} m) → {lo["id"]} 의 겹친 {mlen:.0f} m 를 ' + ('계단' if kind == 'stairs' else '사람길') + f'으로(경사 {100 * gr:.0f} %)')
            OVL.append({**rec0, 'keep': hi['id'], 'cut': lo['id'], 'action': 'merge' if same else ('stairs' if kind == 'stairs' else 'foot'), 'fix': fix})
    return rounds


# ── v6f (과제 0506): 주행 기준 — 갈림목 판 · 경사 한도 12/15 % · 길↔건물 겹침 0 ──────────
V6F = False
PLATES, BLDFIX, PROFILE_LEFT = [], [], []
PLATE_RMAX = 14.0


def _hull(pts):
    pts = sorted(set((round(a, 3), round(b, 3)) for a, b in pts))
    if len(pts) < 3: return pts
    cr = lambda o, a, b: (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, up = [], []
    for p in pts:
        while len(lo) >= 2 and cr(lo[-2], lo[-1], p) <= 0: lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(up) >= 2 and cr(up[-2], up[-1], p) <= 0: up.pop()
        up.append(p)
    return lo[:-1] + up[:-1]


def join_crossings():
    """노드 없이 만나는 차길을 갈림목으로: (1) 중심선이 서로 교차 → 교차점을 두 길에 끼워 공유. (2) 길 끝이 다른 길 노면(폭/2 + 1 m) 안
    → 끝을 그 길 위로 붙임(T). 둘 다 안 하면 빌더 띠 두 개가 다른 높이로 같은 자리를 지나가 옆면 턱이 된다(0506 검사 3.8 m)."""
    car = [r for r in ROADS if r['kind'] in ('main', 'village')]
    n_x = n_t = 0
    box = lambda r: (min(q[0] for q in r['pts']), max(q[0] for q in r['pts']), min(q[1] for q in r['pts']), max(q[1] for q in r['pts']))
    for ia, A in enumerate(car):
        for B in car[ia + 1:]:
            ba, bb_ = box(A), box(B)
            if ba[1] < bb_[0] or bb_[1] < ba[0] or ba[3] < bb_[2] or bb_[3] < ba[2]: continue
            k = 0
            while k < len(A['pts']) - 1:
                a, b = A['pts'][k], A['pts'][k + 1]; hit = None
                for m in range(len(B['pts']) - 1):
                    c, d = B['pts'][m], B['pts'][m + 1]
                    if KEY(a) in (KEY(c), KEY(d)) or KEY(b) in (KEY(c), KEY(d)): continue
                    if _seg_x(a, b, c, d):
                        o = lambda p, q, r: (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0])
                        t = o(c, d, a) / (o(c, d, a) - o(c, d, b)); hit = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t); break
                if hit:
                    p = _snap_into(B, hit); A['pts'].insert(k + 1, p); n_x += 1; k += 2
                else: k += 1
    for A in car:
        for at in (0, -1):
            e = A['pts'][at]
            for B in car:
                if B is A: continue
                if any(KEY(q) == KEY(e) for q in B['pts']): continue
                d = min(L.seg_dist(e[0], e[1], *u, *v)[0] for u, v in zip(B['pts'], B['pts'][1:]))
                if d < B['width'] / 2 + A['width'] / 2 + 0.5:                 # 두 띠가 겹친다

                    p = _snap_into(B, e)
                    if KEY(p) != KEY(e): A['pts'].insert(0, p) if at == 0 else A['pts'].append(p)
                    n_t += 1; break
    return {'crossings': n_x, 'tees': n_t}


def plates_of(car, seqs, P):
    """갈림목 판: 차길 2개 이상이 만나는 점(일직선으로 이어지기만 한 두 길은 빼고).
    팔(들어오는 길 한 방향)마다 다른 팔의 노면을 다 건너갈 거리 R = (wi/2 + wj/2) / sin θ + 0.5 (≤ 14 m).
    판 안(중심에서 R 안)의 팔 표본은 한 높이 변수로 묶는다. 판이 겹치면 하나로 합친다. 다각형 = 팔 끝 모서리 + 중심 원의 볼록 껍질."""
    W = {r['id']: r['width'] for r in car}
    pos = defaultdict(list)
    for r in car:
        for t, k in enumerate(seqs[r['id']]): pos[k].append((r['id'], t))
    raw = []
    for k, occ in pos.items():
        if len({rid for rid, _ in occ}) < 2: continue
        arms = []
        for rid, t in occ:
            ids = seqs[rid]
            for step in (-1, 1):
                u = t + step * 3
                if not 0 <= t + step < len(ids): continue
                u = max(0, min(len(ids) - 1, u)); q = P[ids[u]]
                dx, dz = q[0] - P[k][0], q[1] - P[k][1]; l = math.hypot(dx, dz) or 1
                arms.append({'road': rid, 't': t, 'step': step, 'dir': (dx / l, dz / l), 'w': W[rid]})
        if len(arms) == 2 and arms[0]['dir'][0] * arms[1]['dir'][0] + arms[0]['dir'][1] * arms[1]['dir'][1] < -0.87: continue   # 150° 넘게 일직선 = 그냥 이어진 길
        for a in arms:                               # 팔마다: 이 팔의 띠가 다른 팔의 띠와 겹치는 길이까지만 평평(wj/2 + wi/2·|cos|)/sin + 0.3
            Ra = max(b['w'] for b in arms if b is not a) / 2 + 0.3
            for b in arms:
                if b is a: continue
                c = a['dir'][0] * b['dir'][0] + a['dir'][1] * b['dir'][1]
                if c < -0.87: continue                                      # 맞은편(지나가는 길의 다른 쪽) — 겹치지 않음
                s = max(0.35, math.sqrt(max(0.0, 1 - c * c)))
                Ra = max(Ra, (b['w'] / 2 + a['w'] / 2 * abs(c)) / s + 0.3)
            a['R'] = min(PLATE_RMAX, Ra)
        raw.append({'k': k, 'arms': arms, 'R': max(a['R'] for a in arms)})
    groups = []
    for j in raw:
        ids, edges = {j['k']}, []
        for a in j['arms']:
            seq = seqs[a['road']]; t = a['t']; last = t
            while 0 <= t + a['step'] < len(seq) and math.dist(P[seq[t + a['step']]], P[j['k']]) <= a['R']:
                t += a['step']; ids.add(seq[t]); last = t
            edges.append({**a, 'edge': P[seq[last]], 'edgeT': last})
        groups.append({'ids': ids, 'arms': edges, 'pos': P[j['k']], 'R': j['R'], 'n': 1})
    merged = True                                    # 겹치는 판 합치기
    while merged:
        merged = False
        for a in range(len(groups)):
            for b in range(a + 1, len(groups)):
                if groups[a]['ids'] & groups[b]['ids']:
                    ga, gb = groups[a], groups.pop(b)
                    na = ga['n'] + gb['n']
                    ga['pos'] = ((ga['pos'][0] * ga['n'] + gb['pos'][0] * gb['n']) / na, (ga['pos'][1] * ga['n'] + gb['pos'][1] * gb['n']) / na)
                    ga['ids'] |= gb['ids']; ga['arms'] = ga['arms'] + gb['arms']; ga['R'] = max(ga['R'], gb['R']); ga['n'] = na
                    merged = True; break
            if merged: break
    for g in groups:                                 # 판 안 표본을 다 품는 팔 끝: 같은 길 같은 방향에서 가장 바깥 것만
        far = {}
        for a in g['arms']:
            key = (a['road'], a['step'])
            if key not in far or abs(a['edgeT'] - a['t']) > abs(far[key]['edgeT'] - far[key]['t']): far[key] = a
        arms = list(far.values()); g['arms'] = arms
        pts = []
        for a in arms:
            seq = seqs[a['road']]; e = a['edgeT']; q0 = P[seq[max(0, min(len(seq) - 1, e - a['step']))]]; q1 = P[seq[e]]
            dx, dz = q1[0] - q0[0], q1[1] - q0[1]; l = math.hypot(dx, dz) or 1; nx, nz = -dz / l, dx / l
            pts += [(q1[0] + nx * a['w'] / 2, q1[1] + nz * a['w'] / 2), (q1[0] - nx * a['w'] / 2, q1[1] - nz * a['w'] / 2)]
        for k in g['ids']:
            pts.append(P[k])
        wmax = max(a['w'] for a in arms) / 2 if arms else 2
        cx, cz = g['pos']
        pts += [(cx + wmax * math.cos(t * math.pi / 6), cz + wmax * math.sin(t * math.pi / 6)) for t in range(12)]
        g['poly'] = _hull(pts)
    return groups


_BG = {}


def _bld_index():
    if _BG.get('n') == len(BLDS) and _BG.get('id') == id(BLDS[0] if BLDS else None): return _BG['g']
    g = defaultdict(list)
    for i, b in enumerate(BLDS):
        xs = [q[0] for q in b['poly']]; zs = [q[1] for q in b['poly']]
        for gx in range(int(min(xs) // 10), int(max(xs) // 10) + 1):
            for gz in range(int(min(zs) // 10), int(max(zs) // 10) + 1): g[(gx, gz)].append(i)
    _BG.update(n=len(BLDS), id=id(BLDS[0] if BLDS else None), g=g)
    return g


def bld_cost(r):
    """길 r 의 띠(폭/2 + 어깨 1 m)에 닿는 (건물, 1 m 표본) 수."""
    return len(DC.bld_hits([{'id': r['id'], 'width': r['width'], 'pts': [(q[0], q[1], 0) for q in dens(r['pts'], 1.0)]}], [b['poly'] for b in BLDS]))


def _clip(poly, p, n, off):
    """다각형에서 (x − p)·n ≥ off 쪽만 남김(Sutherland–Hodgman 반평면)."""
    f = lambda q: (q[0] - p[0]) * n[0] + (q[1] - p[1]) * n[1] - off
    out = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        fa, fb = f(a), f(b)
        if fa >= 0: out.append(a)
        if (fa >= 0) != (fb >= 0):
            t = fa / (fa - fb); out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
    return out


def _area(poly):
    return abs(sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(poly, poly[1:] + poly[:1]))) / 2


def clear_buildings(keep_frac=0.4, min_area=12.0):
    """길↔건물 겹침 0: 차길 띠(폭/2 + 1 m)나 갈림목 판에 닿는 건물의 앞면을 길에서 물린다(반평면으로 잘라냄, 여러 번).
    남는 넓이가 원래의 40 % 미만이거나 12 m² 미만이면 지운다. 중심이 띠 안이면 지운다."""
    car = [r for r in ROADS if r['kind'] in ('main', 'village')]
    samp = defaultdict(list)
    for r in car:
        hw = r['width'] / 2 + DC.SHOULDER; d = dens(r['pts'], 0.5)
        for i, q in enumerate(d):
            a, b = d[max(0, i - 1)], d[min(len(d) - 1, i + 1)]
            samp[(int(q[0] // 10), int(q[1] // 10))].append((q, (b[0] - a[0], b[1] - a[1]), hw, r['id']))
    plates = [(pl['poly'], pl['pos']) for pl in PLATES]
    keep, n_clip, n_del = [], 0, 0
    for b in BLDS:
        poly = [tuple(q) for q in b['poly']]
        if len(poly) > 3 and poly[0] == poly[-1]: poly = poly[:-1]
        a0 = _area(poly); why = None; cut = 0
        for _ in range(12):
            xs = [q[0] for q in poly]; zs = [q[1] for q in poly]
            worst = None
            for gx in range(int((min(xs) - 9) // 10), int((max(xs) + 9) // 10) + 1):
                for gz in range(int((min(zs) - 9) // 10), int((max(zs) + 9) // 10) + 1):
                    for q, t, hw, rid in samp.get((gx, gz), ()):
                        if q[0] < min(xs) - hw or q[0] > max(xs) + hw or q[1] < min(zs) - hw or q[1] > max(zs) + hw: continue
                        ins = L.in_poly(q[0], q[1], poly)
                        dd = L.poly_edge_dist(q[0], q[1], poly)
                        pen = hw + dd if ins else hw - dd
                        if pen > 0 and (worst is None or pen > worst[0]): worst = (pen, q, t, hw, rid, ins)
            hitp = None
            for pp, c in plates:
                if any(L.in_poly(q[0], q[1], pp) for q in poly) or any(L.in_poly(q[0], q[1], poly) for q in pp):
                    hitp = (pp, c); break
            if worst is None and hitp is None: break
            if worst is not None:
                pen, q, t, hw, rid, ins = worst
                cx, cz = sum(v[0] for v in poly) / len(poly), sum(v[1] for v in poly) / len(poly)
                l = math.hypot(*t) or 1; nx, nz = -t[1] / l, t[0] / l
                if (cx - q[0]) * nx + (cz - q[1]) * nz < 0: nx, nz = -nx, -nz
                if abs((cx - q[0]) * nx + (cz - q[1]) * nz) < hw: why = f'중심이 차길 {rid} 띠 안'; poly = []; break
                poly = _clip(poly, q, (nx, nz), hw + 0.05); cut += 1
            else:
                pp, c = hitp                               # 판: 건물 중심이 가장 바깥에 있는 판 모서리의 반평면으로
                cx, cz = sum(v[0] for v in poly) / len(poly), sum(v[1] for v in poly) / len(poly)
                best = None
                for a, bb in zip(pp, pp[1:] + pp[:1]):
                    ex, ez = bb[0] - a[0], bb[1] - a[1]; l = math.hypot(ex, ez) or 1; nx, nz = ez / l, -ex / l
                    if (c[0] - a[0]) * nx + (c[1] - a[1]) * nz > 0: nx, nz = -nx, -nz
                    s = (cx - a[0]) * nx + (cz - a[1]) * nz
                    if best is None or s > best[0]: best = (s, a, (nx, nz))
                if best[0] <= 0: why = '중심이 갈림목 판 안'; poly = []; break
                poly = _clip(poly, best[1], best[2], 0.05); cut += 1
            if len(poly) < 3 or _area(poly) < max(min_area, keep_frac * a0): why = why or f'물리면 남는 넓이 {(_area(poly) if len(poly) >= 3 else 0):.0f}/{a0:.0f} m²'; poly = []; break
        else:
            why = '12번 물려도 닿음'; poly = []
        if not poly:
            n_del += 1; BLDFIX.append({'action': 'delete', 'osm': b['osm'], 'at': [round(b['poly'][0][0]), round(b['poly'][0][1])], 'why': why}); continue
        if cut:
            n_clip += 1; BLDFIX.append({'action': 'setback', 'osm': b['osm'], 'at': [round(poly[0][0]), round(poly[0][1])], 'cuts': cut, 'areaKept': round(_area(poly) / a0, 2)})
            b = {**b, 'poly': poly}
        keep.append(b)
    BLDS[:] = keep
    return {'setback': n_clip, 'deleted': n_del, 'kept': len(keep)}


def smooth_profiles(it_max=20000):
    """모든 차길 중심선을 1 m 표본으로 바꾸고, 경사 변화(2차 차분)를 한도 안으로 — 갈림목 점은 여러 길이 같은 변수를 공유하므로
    판 없이 들어오는 길 모두가 그 점 높이로 긴 곡선을 그리며 모인다. 끝끼리 일직선으로 이어진 두 길(갈림목 통과)도 한 줄로 잰다.
    방법: 위반한 3점 묶음마다 최소 이동 사영(POCS) 을 반복. 고정점 없음(지형과 다른 만큼은 build_cut_fill 이 깎고 메운다)."""
    car = [r for r in ROADS if r['kind'] in ('main', 'village')]
    S = {r['id']: car_samples(r) for r in car}
    cnt = defaultdict(int)
    for r in car:
        for k in {KEY(q) for q in S[r['id']]}: cnt[k] += 1
    nid, Y, P, seqs = {}, [], [], {}
    for r in car:
        ids = []
        for q in S[r['id']]:
            k = KEY(q)
            if ids and math.dist(P[ids[-1]], q[:2]) < 0.5 and cnt[k] < 2 and q is not S[r['id']][-1]: continue   # 0.5 m 보다 촘촘한 점(갈림목 아님) 버림
            if k not in nid: nid[k] = len(Y); Y.append(q[2]); P.append((q[0], q[1]))
            if not ids or ids[-1] != nid[k]: ids.append(nid[k])
        seqs[r['id']] = ids
    tri = []
    def add(p, i, n, lim):
        h0, h1 = math.dist(P[p], P[i]), math.dist(P[i], P[n])
        if h0 < 0.2 or h1 < 0.2: return
        hm = (h0 + h1) / 2; ap, ai, an = 1 / (h0 * hm), -(1 / h0 + 1 / h1) / hm, 1 / (h1 * hm)
        tri.append((p, i, n, lim, ap, ai, an, ap * ap + ai * ai + an * an))
    for r in car:
        ids, lim = seqs[r['id']], (K_LIM_STONE if r['roughness'] >= RC.STONE else K_LIM)
        for t in range(1, len(ids) - 1): add(ids[t - 1], ids[t], ids[t + 1], lim)
    thr = RC.through_pairs([{'id': r['id'], 'pts': S[r['id']]} for r in car])
    rb_ = {r['id']: r for r in car}
    for ra, sa, rb, sb in thr:
        A, B = seqs[ra['id']], seqs[rb['id']]
        if len(A) < 2 or len(B) < 2: continue
        lim = min(K_LIM_STONE if rb_[x['id']]['roughness'] >= RC.STONE else K_LIM for x in (ra, rb))
        add(A[1] if sa else A[-2], A[0] if sa else A[-1], B[1] if sb else B[-2], lim)
    # 경사 상한: 2 m 떨어진 두 점 사이 ≤ 그 길 상한(돌길 grade · 그 밖 헤어핀 상한 18 %) — 원래 더 가팔랐던 길은 원래 값까지(나빠지지만 않게)
    duo = []
    capN = {}
    if V6F:                                          # v6f: 표본마다 한도 12 % · 헤어핀 안쪽 15 % · 돌길 15 % (drive_check.caps 와 같은 판정)
        for r in car:
            for q, c in zip(S[r['id']], DC.caps({**r, 'pts': S[r['id']]})):
                k = nid.get(KEY(q))
                if k is not None: capN[k] = max(capN.get(k, 0), c)
    for r in car:
        ids = seqs[r['id']]; cap0 = r.get('grade') or G_PIN; j = 0
        for t in range(len(ids)):
            j = max(j, t)
            while j < len(ids) - 1 and math.dist(P[ids[j]], P[ids[t]]) < 2: j += 1
            d = math.dist(P[ids[j]], P[ids[t]])
            if d < 2: continue
            if V6F: duo.append((ids[t], ids[j], d, max(capN.get(ids[t], DC.G_CAR), capN.get(ids[j], DC.G_CAR)) - 0.002))
            else: duo.append((ids[t], ids[j], d, max(cap0, abs(Y[ids[j]] - Y[ids[t]]) / d)))
    groups = plates_of(car, seqs, P) if V6F else []
    Y0 = list(Y); it = 0
    for it in range(it_max):
        worst = 0.0
        for g in groups:                             # 갈림목 판: 판 안 표본은 모두 한 높이(옆면 턱 0)
            m = sum(Y[k] for k in g['ids']) / len(g['ids'])
            for k in g['ids']:
                worst = max(worst, abs(Y[k] - m)); Y[k] = m
        for a, b, d, cap in duo:
            g = (Y[b] - Y[a]) / d
            if g > cap: e = (g - cap) * d / 2; Y[b] -= e; Y[a] += e
            elif g < -cap: e = (-cap - g) * d / 2; Y[b] += e; Y[a] -= e
        for p, i, n, lim, ap, ai, an, ss in tri:
            k = ap * Y[p] + ai * Y[i] + an * Y[n]
            if k > lim: e = k - lim
            elif k < -lim: e = k + lim
            else: continue
            if abs(e) > worst: worst = abs(e)
            d = e / ss; Y[p] -= d * ap; Y[i] -= d * ai; Y[n] -= d * an
        if worst < 2e-4: break
    for g in groups:                                 # 마지막은 판 높이 맞춤으로 끝낸다(턱 정확히 0)
        m = sum(Y[k] for k in g['ids']) / len(g['ids'])
        for k in g['ids']: Y[k] = m
    for r in car:
        r['pts'] = [P[k] for k in seqs[r['id']]]
    for k, q in enumerate(P): YN[KEY(q)] = Y[k]
    on = defaultdict(list)
    for r in car:
        for k in set(seqs[r['id']]): on[k].append(r['id'])
    JUNC[:] = [{'pos': [round(P[k][0], 2), round(P[k][1], 2)], 'y': round(Y[k], 3), 'roads': on[k]} for k in on if len(on[k]) >= 2]
    PLATES[:] = []
    for n, g in enumerate(groups):
        y = sum(Y[k] for k in g['ids']) / len(g['ids'])
        PLATES.append({'id': f'J{n}', 'pos': [round(g['pos'][0], 2), round(g['pos'][1], 2)], 'y': round(y, 4), 'radius': round(g['R'], 2),
                       'poly': [[round(a, 2), round(b, 2)] for a, b in g['poly']], 'roads': sorted({a['road'] for a in g['arms']}),
                       'arms': [{'road': a['road'], 'edge': [round(a['edge'][0], 3), round(a['edge'][1], 3)], 'width': a['w']} for a in g['arms']]})
    mv = sorted(abs(a - b) for a, b in zip(Y, Y0))
    left = sorted(((abs(ap * Y[p] + ai * Y[i] + an * Y[n]) - lim, i) for p, i, n, lim, ap, ai, an, ss in tri), reverse=True)
    left = [(round(e, 3), round(P[i][0]), round(P[i][1])) for e, i in left if e > 0.003]
    PROFILE_LEFT[:] = left
    return {'left': len(left), 'leftTop': left[:12], 'plates': len(groups), 'samples': len(Y), 'triples': len(tri), 'throughPairs': len(thr), 'iters': it + 1, 'worstLeft': round(worst, 5),
            'moveP50': round(mv[len(mv) // 2], 3), 'moveP99': round(mv[int(len(mv) * .99)], 3), 'moveMax': round(mv[-1], 3), 'junctions': len(JUNC)}


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
    ys = [[H2(-600 + (j + .5) * 4, 600 - (i + .5) * 4) for j in range(N)] for i in range(N)]
    emax = max(max(r) for r in ys)
    for i in range(N):
        for j in range(N):
            y = ys[i][j]
            if y <= 0: c = (150, 190, 210); mask.putpixel((j, i), 255)     # v6c: Unity 해면(0)과 같은 기준
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
    build_sea()
    widen()
    tip = outer_roads()
    nseam, nloose = seams()
    stops = pick_stops(tip)
    global V6F, G_ROAD, G_PIN
    V6F = '--v6f' in sys.argv
    v6e = '--v6e' in sys.argv or V6F
    v6b = '--v6b' in sys.argv or v6e
    TAG = 'v6f' if V6F else 'v6e' if v6e else 'v6b' if v6b else 'v6osm'
    title = '마을 v6f (주행 기준: 갈림목 판·경사 12 %·건물 물림)' if V6F else '마을 v6e (겹친 길 정리 · 매끈한 높이)' if v6e else '마을 v6b (실제 조각 합성 · 길 고르기·경사 보강)' if v6b else '마을 v6-OSM (실제 조각 합성)'
    if debug:
        render(stops, os.path.join(OUT, f'town-{TAG}-debug.png'), title, True); return
    print('shortcuts', add_shortcuts(stops))
    extra = {}
    if v6b:
        fix_seams()
        extra['heights0'] = solve_heights()
        chains = []
        for sid, free in (('top_house', ('posi', 40.0)), ('hill_field', ('camo', 20.0))):
            st = {s['id']: s for s in stops}
            res = chain_shortcut('garage', sid, {k: v['pos'] for k, v in st.items()}, set(), f'garage_{sid}', free=free,
                                 **({'stairs': True, 'grade': G_STEEP} if sid == 'hill_field' else {}))   # v6c: 언덕 밭은 실제 사슬이 늘 더 길다 → 계단 옆 비탈 끝으로 배달점을 옮겨 본다
            print('relocate', sid, st[sid]['pos'], res, CHAIN_DBG[-3:])
            if res:
                EDITS.append({'stop': sid, 'from': st[sid]['pos'], 'to': res['end'],
                              'why': f'{sid}: 원래 자리엔 차로 갈 실제 골목 사슬이 없음 → 실제 골목이 닿는 차길 끝(높이 ≥ {free[1]:.0f} m)으로 배달점을 옮김'})
                st[sid]['pos'] = res['end']; chains.append(res)
        solve_heights()
        for rnd in range(3):
            fun = fun_check(stops)
            bad = [r['pair'].split('→') for r in fun if (r.get('ratioRough') or 1) > 0.8]
            print('round', rnd, [(r['pair'], r.get('ratioRough')) for r in fun])
            if len(fun) - len(bad) >= 5 or not bad: break
            for a, b in bad:
                res = chain_shortcut(a, b, {s['id']: s['pos'] for s in stops}, set(), f'{a}_{b}_{rnd}', stairs=True, grade=G_STEEP)
                print('chain', a, b, res, 'dbg', CHAIN_DBG[-4:]); chains.append(res or {'pair': f'{a}→{b}', 'id': None})
            extra['heights'] = solve_heights()
        extra['chains'] = chains
        extra['removed'] = clean_overlaps()
        if v6e:
            extra['overlapRounds'] = resolve_overlaps(stops)
        if V6F:                                      # 과제 0506: 차길 12 % · 헤어핀 15 % · 돌길(급경사 사슬 포함) 15 % — 지름길 고르기는 끝난 뒤라 경로 선택엔 영향 없음
            G_ROAD, G_PIN = DC.G_CAR, DC.G_PIN
            extra['joined'] = join_crossings(); print('joined', extra['joined'])
            for r in ROADS:
                if r['kind'] in ('main', 'village') and (r.get('grade') or r['roughness'] >= RC.STONE or r['id'].startswith('sc_')): r['grade'] = DC.G_STONE
        extra['heights'] = solve_heights()
        if v6e:
            extra['profile'] = smooth_profiles(60000 if V6F else 20000); print('profile', extra['profile'], 'overlaps', len(OVL))
        if V6F:
            extra['buildings'] = clear_buildings(); print('buildings', extra['buildings'])
        build_cut_fill()
        print('heights', extra['heights'], 'removed', extra['removed'], 'seamfix', SEAMFIX)
    fun = fun_check(stops)
    nb_in = sum(1 for b in BLDS if all(abs(q[0]) <= HALF and abs(q[1]) <= HALF for q in b['poly']))
    img = render(stops, os.path.join(OUT, f'town-{TAG}.png'), f'{title} — 건물 {nb_in}채(칸 안), 760 m 사방')
    print('posi.dy', PCS[2].dy, 'roads', len(ROADS), 'blds', len(BLDS), 'in-box', nb_in, 'seams', nseam, 'loose cut ends', nloose, 'edits', len(EDITS))
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
           'h': lambda X, Z: H2(X - 600, Z - 600)}
    m = TM.measure(net, geometric=True)
    # 저장(bayloop-plan 호환 필드 최대한)
    roads = []
    for r in ROADS:
        o = {'id': r['id'], 'kind': r['kind'], 'width': r['width'], 'roughness': r['roughness'], 'closed': False,
             'knots': [[round(q[0], 3), round(q[1], 3), round(YN.get(KEY(q), H2(*q)), 4)] if v6e and r['kind'] in ('main', 'village') else
                       [round(q[0], 2), round(q[1], 2), round(YN.get(KEY(q), H2(*q)), 2)] for q in r['pts']], 'interp': 'linear',
             'source': {'piece': r['piece'], 'osm_way': r['osm'], 'highway': r['cls'], 'name': r['name'], 'surface': r['surface']}}
        if r['kind'] in ('alley', 'stairs'): o['terrainFollows'] = False
        if r.get('out'): o['out'] = r['out']
        roads.append(o)
    cstep = 8 if v6b else 20     # v6b: 깎고 메운 길 띠(폭+30 m)가 격자에 남도록 8 m
    ctrl = [[x, z, round(H2(x, z), 1)] for x in range(-HALF, HALF + 1, cstep) for z in range(-HALF, HALF + 1, cstep)]
    plan = {
        'meta': {'name': f'town-{TAG}', 'version': 6, 'decision': 'D-157 (PD 0506 v6f)' if V6F else 'D-157 (PD 0421 v6e)' if v6e else 'D-157 (PD 0122)' if v6b else 'D-156 (PD 0109)', 'units': 'm', 'axes': 'x=동, z=북, y=높이(해면 0). 점은 [x, z, y]',
                 'source': '© OpenStreetMap contributors (ODbL) + SRTM 30 m — docs/map/ATTRIBUTION.md',
                 'rhythmModel': '최고속 85 km/h · 가속 10.7 · 제동 8 m/s² · 굽이 √(5.82·R) + 요철 상한 가설 ROUGH_CAP',
                 'note': 'knots 는 OSM 원본 꺾은선(interp=linear). 이음매(seam*)만 3차 베지어' + ('. 차길 knots y = 경사 상한(15 %/헤어핀 18 %) 매끈 높이, controlPoints = 길에 맞춰 깎고 메운 지형' if v6b else '')},
        'pieces': [{'id': p.pid, 'town': p.tid, 'srcCenter': p.c, 'rotDeg': p.rot, 'rect': p.rect, 'at': p.at, 'dy': p.dy, 'why': p.why} for p in PCS],
        'terrain': {'method': 'grid', 'step': cstep, 'blend': BLEND, 'controlPoints': ctrl},
        'sea': {'level': 0, 'coast': [[[round(q[0], 2), round(q[1], 2)] for q in c] for c in COAST],
                'piers': [[[round(q[0], 2), round(q[1], 2)] for q in c] for c in PIER]},
        'roads': roads,
        'buildings': [{'poly': [[round(q[0], 2), round(q[1], 2)] for q in b['poly']], 'y': round(H2(*b['poly'][0]), 1), 'piece': b['piece'], 'osm_way': b['osm']} for b in BLDS],
        'garage': stops[0], 'mart': stops[1], 'stops': stops[2:],
        'boundary': {'play': [[-HALF, -HALF], [HALF, -HALF], [HALF, HALF], [-HALF, HALF]]},
        'edits': EDITS, 'fun': fun, 'metrics': m,
        **({'v6b': {**extra, 'seamFix': SEAMFIX, 'looseEnds': LOOSE, 'removedBuildings': REMOVED}} if v6b else {}),
        **({'v6e': {'overlaps': OVL, 'junctions': JUNC, 'profileFinal': True,
                    'note': '차길 knots = 1 m 표본, y = 최종 노면 높이(경사 변화 ≤3 %/m · 돌길 ≤6 %/m, 갈림목 공유점 같은 높이). 빌더는 갈림목 판·경사 변화 반복 완화를 다시 하지 말 것'}} if v6e else {}),
        **({'junctions': PLATES,
            'v6f': {'gradeCaps': {'car': DC.G_CAR, 'hairpinInner': DC.G_PIN, 'stone': DC.G_STONE, 'hairpinR': DC.PIN_R},
                    'buildings': extra.get('buildings'), 'buildingFixes': BLDFIX,
                    'note': 'junctions[] = 갈림목 판: poly(볼록 다각형, [x,z]) 를 높이 y 의 수평 판으로 깐다. 판 안을 지나는 차길 knots 는 모두 정확히 y '
                            '(판 경계 arms[].edge 에서도 y) — 옆면 턱 0. 차길 knots y 는 최종(완화·리미터·판 다시 하지 말 것)'}} if V6F else {}),
    }
    json.dump(plan, open(os.path.join(OUT, f'town-{TAG}.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    others, sheet_name = (('town-v6e.png',), 'v6e-vs-v6f.png') if V6F else (('town-v6b.png',), 'v6b-vs-v6e.png') if v6e else (('town-v6osm.png',), 'v6-vs-v6b.png') if v6b else (('town-v5b.png', 'real-towns/tomonoura.png', 'real-towns/camogli.png', 'real-towns/positano.png'), 'v6osm-vs-real.png')
    ims = [Image.open(os.path.join(OUT, q)).convert('RGB') for q in others] + [img] if v6b else [img] + [Image.open(os.path.join(OUT, q)).convert('RGB') for q in others]
    sm = 900 if v6b else 600; sheet = Image.new('RGB', (sm * len(ims), sm + 40), (255, 255, 255))
    for k, im in enumerate(ims): sheet.paste(im.resize((sm, sm), Image.Resampling.LANCZOS), (k * sm, 0))
    sheet.paste(legend(sm * len(ims)), (0, sm)); sheet.save(os.path.join(OUT, sheet_name))
    if v6b:
        for c in extra.get('chains', []): print('chainres', json.dumps(c, ensure_ascii=False))
        for x in SEAMFIX: print('seamfix', json.dumps(x, ensure_ascii=False))
        for x in LOOSE: print('loose', json.dumps(x, ensure_ascii=False))
        print('removed', json.dumps(REMOVED, ensure_ascii=False))
    print(json.dumps(m, ensure_ascii=False))
    for row in fun: print(json.dumps(row, ensure_ascii=False))
    for e in EDITS: print('edit', json.dumps(e, ensure_ascii=False))


if __name__ == '__main__':
    main()
