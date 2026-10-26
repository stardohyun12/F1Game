"""마을 v5 평면 — 실제 마을 층 합성 (D-156). 고리를 먼저 그리지 않는다: 땅 → 해안 → 항구 심장 → 해안 큰길 → 비탈 → 건물 → 둥글게 → 재미 검사.
사용: python -X utf8 tools/map/make_town_v5.py
결과: docs/map/town-v5.json (bayloop-plan 호환 필드), town-v5.png (real_towns 같은 축척 1200m·색·범례), v5-vs-real.png
층마다 가져온 곳:
  지형·해안  = 아말피·포지타노(만 하나 + 곶 + 비탈 0→75m) / 항구 심장 = 도모노우라(만 안쪽, 해안선 따라 휜 격자, 엇갈린 T자)
  해안 큰길  = 아말피 SS163(곶 목 터널, 등대 곶 가지, 해안 직선) / 비탈 = 포지타노 Viale Pasitea(등고선 평행 Z 길 + 헤어핀) + 카몰리(계단이 경사 직하)
"""
import json, math, os, random, sys
from collections import defaultdict
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
import planlib as L
import town_metrics as TM

ROOT = L.ROOT
OUT = os.path.join(ROOT, 'docs', 'map')
HALF = 380                      # 760 m 사방
rnd = random.Random(156)
S = L.smooth


# ── 층 1·2: 지형·해안선 ──
def zc(x):
    """해안선 z(x): 만(가운데 북쪽으로 130 m 파고듦) + 서쪽 곶(남으로 150 m) + 동쪽은 곧게 남동으로."""
    return -230 + 130 * math.exp(-(x / 150) ** 2) - 150 * math.exp(-((x + 260) / 45) ** 2) - 0.12 * max(0.0, x - 150)


def dzc(x, e=1.0):
    return (zc(x + e) - zc(x - e)) / (2 * e)


def H(d):
    """해안에서 북으로 d m 들어간 높이 — 항구 평지 50 m(1→6 m) 뒤 비탈 30 % 까지, 꼭대기 76 m."""
    if d < 0:
        return -6.0
    if d < 50:
        return 1 + 5 * d / 50
    if d < 400:
        return 6 + 70 * S((d - 50) / 350)
    return 76 + 0.02 * (d - 400)


def h(x, z):
    d = z - zc(x)
    if d < 0:
        return -6.0
    y = H(d)
    y += 7 * math.sin(x / 85 + 0.6) * S(d / 120)                                          # 능선·골이 번갈아(등고선이 휜다)
    y -= 14 * math.exp(-((x + 150) / 22) ** 2) * S((d - 40) / 40) * (1 - S((d - 250) / 80))  # 서쪽 골짜기(아말피 협곡)
    y += 14 * math.exp(-((x + 262) / 22) ** 2) * math.exp(-((z + 250) / 70) ** 2)          # 등대 곶 등줄기
    return max(y, 1.5 + 0.03 * min(d, 200))                                                # 골짜기가 바다까지 파이지 않게


def W(u, d):
    """(해안을 따라 u, 해안에서 북으로 d) → 평면 (x, z). 같은 d = 같은 높이(등고선)."""
    return (u, zc(u) + d)


def nrm(x):
    a, b = -dzc(x), 1.0
    m = math.hypot(a, b)
    return a / m, b / m


def lerp_d(knots, u):
    k = sorted(knots)
    if u <= k[0][0]:
        return k[0][1]
    for (u0, d0), (u1, d1) in zip(k, k[1:]):
        if u <= u1:
            return d0 + (d1 - d0) * (u - u0) / (u1 - u0)
    return k[-1][1]


def hairpin(A, B, head, n=7):
    """A→B 지름 반원(머리 방향 head 쪽으로 불룩). 포지타노 헤어핀 반경 p50 14 m(측정) — 여기선 A·B 간격의 반."""
    C = ((A[0] + B[0]) / 2, (A[1] + B[1]) / 2); R = math.dist(A, B) / 2
    e1 = ((A[0] - C[0]) / R, (A[1] - C[1]) / R)
    dot = head[0] * e1[0] + head[1] * e1[1]
    e2 = (head[0] - dot * e1[0], head[1] - dot * e1[1]); m = math.hypot(*e2); e2 = (e2[0] / m, e2[1] / m)
    return [(C[0] + R * (math.cos(t) * e1[0] + math.sin(t) * e2[0]), C[1] + R * (math.cos(t) * e1[1] + math.sin(t) * e2[1]))
            for t in (math.pi * i / (n - 1) for i in range(1, n - 1))]


# ── 길 설계 ──
ROADS = []          # 설계 단계: {'id','kind','width','roughness','xy',...}
XS = {'main': ('curb', 2.0), 'village': ('white_line', 1.2), 'lane': ('gutter', 0.6), 'shortcut': ('none', 1.0), 'alley': ('none', 0.0), 'stairs': ('none', 0.0)}


def R(rid, kind, w, rough, xy, why, start=None, end=None, **kw):
    r = {'id': rid, 'kind': kind, 'width': w, 'roughness': rough, 'xy': [tuple(p) for p in xy], 'why': why}
    if start: r['start'] = start
    if end: r['end'] = end
    r.update(kw); ROADS.append(r); return r


J = lambda rid: {'join': rid}
Wl = lambda lst: [W(u, d) for u, d in lst]

# 해안 큰길(아말피형) — 서쪽 곶 목 터널에서 나와 골짜기를 감고 만 위 d=88 을 돌아 동쪽 해안 직선으로 터널까지
EAST = [(160, -147.1), (217.3, -164.8), (274.7, -182.5), (332, -200.2), (380, -215)]
MAIN = [(-238, -190), (-212, -178), (-186, -163), (-165, -130), (-152, -98)] + Wl([(u, 88) for u in range(-115, 61, 25)]) \
    + Wl([(90, 80), (115, 68)]) + [(140, -128)] + EAST
R('main', 'main', 6.0, 0.1, MAIN, '해안 큰길 — 아말피 SS163: 곶 목 터널 → 골짜기 감기 → 항구 뒤(옛 시가지 위 가장자리) → 동쪽 해안 직선 230 m → 터널',
  start={'type': '터널', 'label': '서쪽 곶 터널(옆 마을)'}, end={'type': '터널', 'label': '동쪽 터널(시내)'})

# 항구 심장(도모노우라) — 해안선과 평행한 길 2줄(부두 d=9, 가운뎃길 d≈42) + 해안에 직각인 골목, 칸마다 엇갈린 T자
R('V_quay', 'village', 5.0, 0.2, Wl([(u, 9) for u in range(-112, 123, 18)]), '부두 길 — 도모노우라 해안 길 폭 5 m(OSM 최다 폭). 서쪽 끝은 해안 흙길로 이어짐',
  end=J('V_harbor'), xs='shared')
R('V_harbor', 'village', 4.5, 0.2, [W(122, 9), W(140, 16), W(158, 28), (178, -152.6)], '동쪽 항구 길 — 부두 동쪽 끝에서 해안을 따라 큰길 해안 직선에 붙는 넓은 길(어시장 트럭, 경사 완만)', start=J('V_quay'), end=J('main'))
mid = [(u, 42 + 3 * math.sin(u / 31)) for u in range(-106, 113, 12)]
R('L_mid', 'village', 3.4, 0.45, Wl(mid), '가운뎃길 — 도모노우라 옛 상점가(돌포장, 폭 3~4 m)', xs='lane')
CROSS_U = [-100, -58, -16, 26, 68, 104]     # 간격 42 m — 도모노우라 모든 길 교차 간격 p50 46 m
for k, u in enumerate(CROSS_U):
    for lo, hi, tag in ((9, lerp_d(mid, u), 'a'), (lerp_d(mid, u), 88, 'b')):
        j1, j2 = rnd.uniform(-7, 7), rnd.uniform(-7, 7)
        R(f'L_x{k}{tag}', 'village', 3.2, 0.45, Wl([(u + j1, lo), (u + (j1 + j2) / 2 + rnd.uniform(-3, 3), (lo + hi) / 2), (u + j2, hi)]),
          '해안에 직각인 옛 골목 — 위·아래 칸이 엇갈려 T자(도모노우라)', start=J('V_quay' if tag == 'a' else 'L_mid'), end=J('L_mid' if tag == 'a' else 'main'), xs='lane')
for k, (a, b) in enumerate(zip(CROSS_U, CROSS_U[1:])):
    dd = 25 if k % 2 == 0 else 66
    R(f'A_h{k}', 'alley', 2.0, 0.3, Wl([(a + 4, dd), ((a + b) / 2, dd + rnd.uniform(-3, 3)), (b - 4, dd)]), '칸 가운데 사람 골목(도모노우라 2 m 골목)')

# 비탈(포지타노 Viale Pasitea + 카몰리) — 등고선 평행 층 3겹 + 위 짧은 2층, 끝마다 헤어핀으로 꺾어 오름
T1 = [(-112, 121), (-60, 128), (0, 135), (60, 145), (120, 158), (165, 168)]
T2 = [(165, 192), (110, 200), (50, 210), (-10, 222), (-70, 232), (-125, 240)]
T3 = [(-125, 264), (-60, 270), (0, 278), (60, 286), (120, 294), (160, 298)]
L4 = [(160, 322), (130, 326), (100, 330)]
L5 = [(100, 354), (150, 358), (200, 360), (250, 362)]
z = [W(-118, 88), W(-121, 105)]
for tier, nxt in ((T1, T2), (T2, T3), (T3, L4), (L4, L5), (L5, None)):
    pts = Wl(tier); z += pts
    if nxt:
        hd = (pts[-1][0] - pts[-2][0], pts[-1][1] - pts[-2][1]); m = math.hypot(*hd)
        z += hairpin(pts[-1], W(*nxt[0]), (hd[0] / m, hd[1] / m))
R('V_pasitea', 'village', 4.0, 0.25, z, '비탈 Z 길 — 포지타노 Viale Pasitea: 등고선 따라 가다 헤어핀 4번(R≈12 m), 차 한 대 반 폭 4 m', start=J('main'))

# 비탈 차 지름길(카몰리 salita) — 층 사이를 비스듬히 곧장, 돌길·급경사 ≤18 %
R('SC_ramp0', 'shortcut', 3.0, 0.65, [W(75, 84), W(40, 108), W(0, lerp_d(T1, 0))], '큰길 → 1층 돌길 비탈', start=J('main'), end=J('V_pasitea'))
R('SC_ramp1', 'shortcut', 3.0, 0.65, [W(-70, lerp_d(T1, -70)), W(0, (lerp_d(T1, 0) + lerp_d(T2, 0)) / 2), W(70, lerp_d(T2, 70))], '1층 → 2층 돌길 비탈', start=J('V_pasitea'), end=J('V_pasitea'))
R('SC_ramp2', 'shortcut', 3.0, 0.65, [W(110, lerp_d(T2, 110)), W(50, (lerp_d(T2, 50) + lerp_d(T3, 50)) / 2), W(-10, lerp_d(T3, -10))], '2층 → 3층 돌길 비탈', start=J('V_pasitea'), end=J('V_pasitea'))
R('SC_ramp3', 'shortcut', 3.0, 0.65, [W(30, lerp_d(T3, 30)), W(85, 322), W(140, lerp_d(L5, 140))], '3층 → 꼭대기 층 돌길 비탈(위 헤어핀 둘을 건너뜀)', start=J('V_pasitea'), end=J('V_pasitea'))

# 바깥(아말피) — 등대 곶 가지(곶 등줄기를 헤어핀 하나로 넘음), 해안 흙길, 동쪽 능선 흙길 → 꼭대기 밭
cape = [(-212, -178), (-232, -195), (-262, -206), (-288, -210)]
cape += hairpin(cape[-1], (-288, -234), (-1, 0)) + [(-288, -234), (-262, -238), (-238, -244)]
cape += hairpin(cape[-1], (-238, -268), (1, 0)) + [(-238, -268), (-262, -276), (-266, -300), (-262, -326), (-261, -346)]
R('V_cape', 'village', 4.5, 0.25, cape, '등대 곶 길 — 곶 등줄기를 헤어핀 둘(아말피 곶 길)로 넘어 끝의 등대로', start=J('main'), end={'type': '등대', 'label': '곶 끝 흰 등대'})
R('SC_shore', 'shortcut', 3.2, 0.6, [W(-112, 9), (-135, -163), (-165, -184), (-195, -214), (-215, -250), (-240, -285), (-265.5, -309)],
  '해안 흙길 — 부두 서쪽 끝에서 곶 발치를 따라(낮고 평평, 바퀴 자국·좁음)', start=J('V_quay'), end=J('V_cape'))
R('SC_ridge', 'shortcut', 3.2, 0.7, [(250, -174.9), (290, -130), (318, -70), (300, -5), (330, 55), (300, 115), W(250, 362)],
  '동쪽 능선 흙길 — 해안 직선에서 꼭대기 밭으로 곧장(급경사·마루 둔턱)', start=J('main'), end=J('V_pasitea'),
  features=[{'type': 'jump', 'near': [318, -70], 'step': 0.5, 'name': '능선 마루 둔턱'}])


# 계단(카몰리·포지타노) — 층 사이를 경사 방향으로 곧장. 사람만.
def stairs_between(lo_d, hi_d, u):
    x, zz = W(u, lo_d(u)); pts = [(x, zz)]
    for _ in range(200):
        nx, nz = nrm(x); x, zz = x + 2 * nx, zz + 2 * nz
        pts.append((x, zz))
        if zz - zc(x) >= hi_d(x):
            break
    return pts[::max(1, len(pts) // 4)] + [pts[-1]] if len(pts) > 5 else pts


SPAIRS = [(lambda u: 88, lambda u: lerp_d(T1, u), [-75, -25, 95, 140]), (lambda u: lerp_d(T1, u), lambda u: lerp_d(T2, u), [-95, -60, 100, 140]),
          (lambda u: lerp_d(T2, u), lambda u: lerp_d(T3, u), [-100, -40, 30, 125]), (lambda u: lerp_d(T3, u), lambda u: lerp_d(L5, u), [105, 145])]
SPAIRS = [(lo, hi, sorted(set(us + list(range(-115 + 17 * k, 160, 38))))) for k, (lo, hi, us) in enumerate(SPAIRS)]
for k, (lo, hi, us) in enumerate(SPAIRS):
    for u in us:
        R(f'S_{k}_{u}', 'stairs', 1.8, 0.0, stairs_between(lo, hi, u), '계단 — 경사 직하(카몰리 계단 57 %가 경사 방향)', terrainFollows=False)
for k, (A, B) in enumerate(((T1, T2), (T2, T3), (T3, L5))):     # 층 사이 가운데 높이의 사람 길(포지타노 mulattiera — 사람 길이 차길의 2배)
    us = range(-110, 151, 15) if k < 2 else range(105, 151, 15)
    R(f'A_c{k}', 'alley', 1.8, 0.3, Wl([(u, (lerp_d(A, u) + lerp_d(B, u)) / 2 + 2 * math.sin(u / 23)) for u in us]), '층 사이 사람 길 — 등고선 따라(포지타노)')
R('A_prom', 'alley', 2.5, 0.1, [W(u, 3.5) for u in range(150, 381, 20)], '동쪽 해안 산책길(카몰리 해변 길)')
for k, (a, b) in enumerate(zip(CROSS_U, CROSS_U[1:])):
    dd = 66 if k % 2 == 0 else 25
    R(f'A_g{k}', 'alley', 2.0, 0.3, Wl([(a + 4, dd), ((a + b) / 2, dd + rnd.uniform(-3, 3)), (b - 4, dd)]), '칸 가운데 둘째 사람 골목')
    um = (a + b) / 2 + rnd.uniform(-5, 5)
    R(f'A_m{k}', 'alley', 1.8, 0.3, Wl([(um, 11), (um + rnd.uniform(-3, 3), 25), (um + rnd.uniform(-3, 3), 40)]), '부두 → 가운뎃길 세로 사람 골목')
for k, u in enumerate([-80, -35, 47, 85]):      # 부두 → 가운뎃길 짧은 계단 골목은 심장 골목으로 충분 — 큰길 아래 → 부두까지 내려가는 사람 길 몇 개
    R(f'A_v{k}', 'alley', 2.0, 0.3, Wl([(u, 46), (u + rnd.uniform(-4, 4), 66), (u + rnd.uniform(-3, 3), 86)]), '큰길에서 가운뎃길로 내려가는 사람 골목')


# ── 층 7: 둥글게 + 높이 ──
GRADE = {'main': 0.08, 'village': 0.12, 'shortcut': 0.18}
SAMP = {}


def finalize():
    for r in ROADS:
        dense = L.sample({'knots': [[x, zz, 0.0] for x, zz in r['xy']]}, step=1.0)   # Catmull-Rom → 모서리가 둥글어진다
        pts = L.resample(dense, step=6.0) + ([dense[-1]] if math.dist(dense[-1][:2], L.resample(dense, step=6.0)[-1][:2]) > 2 else [])
        ys = [h(p[0], p[1]) for p in pts]
        g = GRADE.get(r['kind'])
        pin0 = pin1 = None
        for key, idx in (('start', 0), ('end', -1)):
            jr = r.get(key, {}).get('join') if isinstance(r.get(key), dict) else None
            if jr and jr in SAMP:
                q = SAMP[jr]; i = L.nearest(q, pts[idx][0], pts[idx][1]) if hasattr(L, 'nearest') else 0
                y = q[i][2] if isinstance(i, int) else q[i[0]][2]
                if idx == 0: pin0 = y
                else: pin1 = y
        if pin0 is not None: ys[0] = pin0
        if pin1 is not None: ys[-1] = pin1
        if g:
            for _ in range(3):
                for i in range(1, len(pts)):
                    ds = math.dist(pts[i][:2], pts[i - 1][:2]); ys[i] = min(max(ys[i], ys[i - 1] - g * ds), ys[i - 1] + g * ds)
                if pin1 is not None: ys[-1] = pin1
                for i in range(len(pts) - 2, -1, -1):
                    ds = math.dist(pts[i][:2], pts[i + 1][:2]); ys[i] = min(max(ys[i], ys[i + 1] - g * ds), ys[i + 1] + g * ds)
                if pin0 is not None: ys[0] = pin0
        r['knots'] = [[round(p[0], 1), round(p[1], 1), round(max(0.6, y), 2)] for p, y in zip(pts, ys)]
        r['cutFillMax'] = round(max(abs(y - h(p[0], p[1])) for p, y in zip(pts, ys)), 1)
        SAMP[r['id']] = L.sample(r, step=1.0)


# ── 층 6: 건물 채움 ──
def zone(x, zz):
    d = zz - zc(x)
    if d < 100 and -125 < x < 130:
        return 'dense'
    if d < 310 and -205 < x < 255:
        return 'medium'
    return 'sparse'


ZP = {  # 앞폭, 깊이, 틈, 들여씀, 남길 확률 — 도모노우라 0-15m 덮음 37 % / 카몰리·포지타노 비탈 24 % / 위 7~11 %
    'dense': ((4.5, 7.5), (7, 11), (0.0, 0.3), (0.2, 0.6), 1.0),
    'medium': ((6, 9), (7, 10), (1.0, 4.0), (1.0, 2.5), 0.9),
    'sparse': ((7, 11), (7, 10), (8, 18), (2, 5), 0.5),
}
G0 = -400; GN = 800


class Occ:
    def __init__(self):
        self.a = bytearray(GN * GN)

    def idx(self, x, zz):
        i, j = int(zz - G0), int(x - G0)
        return i * GN + j if 0 <= i < GN and 0 <= j < GN else None

    def disk(self, x, zz, r):
        for dz in range(-int(r) - 1, int(r) + 2):
            for dx in range(-int(r) - 1, int(r) + 2):
                if dx * dx + dz * dz <= r * r:
                    k = self.idx(x + dx, zz + dz)
                    if k is not None: self.a[k] = 1

    def rect_cells(self, p, margin):
        a = math.radians(p['rot']); ix, iz = math.cos(a), math.sin(a); tx, tz = -iz, ix
        hw, hd = p['w'] / 2 + margin, p['d'] / 2 + margin
        out = []
        for sx in range(-int(hw * 2), int(hw * 2) + 1):
            for sd in range(-int(hd * 2), int(hd * 2) + 1):
                u, v = sx / 2, sd / 2
                if abs(u) <= hw and abs(v) <= hd:
                    out.append(self.idx(p['x'] + u * tx + v * ix, p['z'] + u * tz + v * iz))
        return out

    def free(self, cells):
        return all(c is not None and not self.a[c] for c in cells)

    def mark(self, cells):
        for c in cells: self.a[c] = 1


def buildings():
    occ = Occ()
    for i in range(GN):                         # 바다·물가 1.5 m 아래는 못 지음
        for j in range(0, GN, 1):
            if h(G0 + j + .5, G0 + i + .5) <= 1.5:
                occ.a[i * GN + j] = 1
    for r in ROADS:
        m = 0.8 if r['kind'] in GRADE else 0.3
        for p in SAMP[r['id']]:
            occ.disk(p[0], p[1], r['width'] / 2 + m)
    parcels = []

    def add(p, margin):
        cells = occ.rect_cells(p, margin)
        if occ.free(cells):
            occ.mark(occ.rect_cells(p, 0.0)); parcels.append(p); return True
        return False
    order = sorted(ROADS, key=lambda r: {'alley': 0, 'village': 1, 'stairs': 2, 'shortcut': 3, 'main': 4}[r['kind']])
    for r in order:
        pts = SAMP[r['id']]; n = len(pts)
        for side in (1, -1):
            i = 3
            while i < n - 3:
                p = pts[i]; zn = zone(p[0], p[1]); (wr, dr, gr, sr, keep) = ZP[zn]
                if zn == 'medium':                          # 위로 갈수록 성김(포지타노·카몰리)
                    t = S((h(p[0], p[1]) - 20) / 45); gr = (gr[0] + 3 * t, gr[1] + 9 * t); keep = 0.9 - 0.35 * t
                w = rnd.uniform(*wr); d = rnd.uniform(*dr)
                j = min(n - 1, i + int(w / 2)); f = (pts[min(n - 1, i + 2)][0] - pts[max(0, i - 2)][0], pts[min(n - 1, i + 2)][1] - pts[max(0, i - 2)][1])
                m = math.hypot(*f) or 1; f = (f[0] / m, f[1] / m); inward = (-f[1] * side, f[0] * side)
                off = r['width'] / 2 + rnd.uniform(*sr) + d / 2 + 0.8
                c = (pts[j][0] + inward[0] * off, pts[j][1] + inward[1] * off)
                p0 = {'x': round(c[0], 2), 'z': round(c[1], 2), 'w': round(w, 2), 'd': round(d, 2), 'rot': round(math.degrees(math.atan2(inward[1], inward[0])), 1),
                      'front': {'road': r['id'], 'side': side}, 'frontMid': [round(c[0] - inward[0] * d / 2, 2), round(c[1] - inward[1] * d / 2, 2)], 'density': zn}
                if rnd.random() < keep and add(p0, 0.25 if zn == 'dense' else 0.6):
                    i += int(w + rnd.uniform(*gr)) + 1
                else:
                    i += 2
    # 블록 안쪽 채움(도모노우라 블록은 속까지 찬다, 비탈은 계단식 둘째 줄 조금)
    near = defaultdict(list)
    for r in ROADS:
        if r['kind'] == 'stairs':
            continue
        pts = SAMP[r['id']]
        for i in range(0, len(pts) - 2, 3):
            near[(int(pts[i][0] // 12), int(pts[i][1] // 12))].append((pts[i], pts[i + 2], r['id']))
    for gz in range(-HALF, HALF, 4):
        for gx in range(-HALF, HALF, 4):
            zn = zone(gx, gz)
            if rnd.random() > (0.95 if zn == 'dense' else 0.4 if zn == 'medium' else 0.06):
                continue
            best = None
            for dx in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for a, b, rid in near.get((int(gx // 12) + dx, int(gz // 12) + dz), ()):
                        dd = math.dist(a[:2], (gx, gz))
                        if best is None or dd < best[0]: best = (dd, a, b, rid)
            if best is None or best[0] > 30:
                continue
            _, a, b, rid = best
            ang = math.degrees(math.atan2(a[1] - gz, a[0] - gx)) + 180      # 안쪽 = 길에서 멀어지는 방향
            w, d = rnd.uniform(4.5, 7), rnd.uniform(6, 9)
            rr = math.radians(ang)
            p0 = {'x': gx, 'z': gz, 'w': round(w, 2), 'd': round(d, 2), 'rot': round(ang, 1), 'front': {'road': rid, 'side': 0},
                  'frontMid': [round(gx - math.cos(rr) * d / 2, 2), round(gz - math.sin(rr) * d / 2, 2)], 'density': zn, 'facing': 'back'}
            add(p0, 0.3 if zn == 'dense' else 0.8)
    for k, p in enumerate(parcels):
        p['id'] = f'P{k:03d}'; p['baseY'] = round(max(0.6, h(p['x'], p['z'])), 2)
        p['floors'] = 3 if p['density'] == 'dense' and rnd.random() < .4 else 2 if p['density'] != 'sparse' else 1 + (rnd.random() < .5)
        p.setdefault('facing', 'front')
    return parcels


# ── 장소 ──
STOPS = [
    {'n': 1, 'id': 'tea', 'name': '찻집', 'pos': list(W(30, 9)), 'cargo': 'Box', 'why': '부두 가운데 바다 쪽 찻집 — 넓은 길로는 동쪽 항구 길로 돌아 들어와야 하고, 옛 골목으로는 곧장 내려온다'},
    {'n': 2, 'id': 'top_house', 'name': '꼭대기 집(할머니)', 'pos': list(W(180, 359)), 'cargo': 'Eggs', 'why': 'PLACES 01 — 헤어핀 넷 위 마지막 층. 깨지기 쉬운 화물로 Z 길 vs 돌길 비탈'},
    {'n': 3, 'id': 'lighthouse', 'name': '등대', 'pos': [-261, -350], 'cargo': 'Glass', 'why': '서쪽 곶 끝 — 곶 길(등줄기 넘기 + 헤어핀) vs 해안 흙길(평평·거침)'},
    {'n': 4, 'id': 'hill_field', 'name': '언덕 밭', 'pos': list(W(245, 362)), 'cargo': 'Box', 'why': '꼭대기 층 동쪽 끝 밭 — Z 길 끝 vs 동쪽 능선 흙길'},
]
GARAGE = {'id': 'garage', 'name': '차고', 'pos': list(W(-95, 88)), 'why': '큰길가 서쪽 — 옛 시가지 골목엔 차를 못 세우고, Z 길 입구·곶 갈림·항구 골목 입구가 다 30~120 m 안이라 모든 배달이 여기서 갈린다'}
MART = {'id': 'mart', 'name': 'MART', 'pos': list(W(100, 76)), 'why': '큰길이 옛 시가지를 벗어나 해안 직선으로 꺾이는 모서리 — 차가 서기 쉬운 넓은 길가(카몰리 큰길가 상점)'}
PAIRS = [('garage', 'tea'), ('garage', 'top_house'), ('garage', 'lighthouse'), ('garage', 'hill_field'), ('mart', 'top_house')]
FIELDS = [{'id': 'F_hill', 'rect': [round(W(270, 380)[0]), round(W(270, 380)[1]), 40, 26], 'bands': 4, 'why': '꼭대기 층 끝 — 동쪽 능선 흙길이 올라오는 곳(아말피 꼭대기 계단밭)'}]


# ── 재미 검사: 길망 → 큰길만 vs 아무 차길 최단 → planlib 리듬(85 km/h) ──
def net_graph():
    nodes = []; adj = defaultdict(list); grid = defaultdict(list)
    for r in ROADS:
        if r['kind'] not in GRADE:
            continue
        pts = SAMP[r['id']][::2]
        base = len(nodes)
        for i, p in enumerate(pts):
            nodes.append((p, r)); grid[(int(p[0] // 4), int(p[1] // 4))].append(base + i)
            if i:
                dd = math.dist(p[:2], pts[i - 1][:2]); adj[base + i].append((base + i - 1, dd)); adj[base + i - 1].append((base + i, dd))
    for k, (p, r) in enumerate(nodes):
        for dx in (-1, 0, 1):
            for dz in (-1, 0, 1):
                for k2 in grid.get((int(p[0] // 4) + dx, int(p[1] // 4) + dz), ()):
                    if nodes[k2][1] is not r and math.dist(p[:2], nodes[k2][0][:2]) < 3.2 and abs(p[2] - nodes[k2][0][2]) < 1.5:   # 높이 다른 곳 = 입체 교차, 못 갈아탐
                        adj[k].append((k2, math.dist(p[:2], nodes[k2][0][:2])))
    return nodes, adj


def big_ok(r):
    return r['kind'] == 'main' or (r['kind'] == 'village' and r['width'] >= 4.0)


def route(nodes, adj, a, b, ok):
    import heapq
    cand = [k for k, (p, r) in enumerate(nodes) if ok(r)]
    sa = min(cand, key=lambda k: math.dist(nodes[k][0][:2], a)); sb = min(cand, key=lambda k: math.dist(nodes[k][0][:2], b))
    if math.dist(nodes[sa][0][:2], a) > 25 or math.dist(nodes[sb][0][:2], b) > 25:
        return None
    dist = {sa: 0.0}; prev = {}; pq = [(0.0, sa)]
    while pq:
        dd, k = heapq.heappop(pq)
        if k == sb: break
        if dd > dist[k]: continue
        for k2, w in adj[k]:
            if not ok(nodes[k2][1]): continue
            nd = dd + w
            if nd < dist.get(k2, 1e18):
                dist[k2] = nd; prev[k2] = k; heapq.heappush(pq, (nd, k2))
    if sb not in dist:
        return None
    path = [sb]
    while path[-1] != sa: path.append(prev[path[-1]])
    return [nodes[k] for k in reversed(path)]


ROUGH_CAP = [(0.6, 10.0), (0.4, 13.0)]   # 가설: 화물 실은 사람은 요철 0.6+ 에서 36 km/h, 0.4+ 에서 47 km/h 로 참는다(Proto_Play01 봇 상한 9~14 m/s 근거)


def drive(path, rough_caps):
    q, rs, acc, last = [], [], 0.0, None
    for p, r in path:
        if last is not None: acc += math.dist(p[:2], last[:2])
        if last is None or acc >= L.RSTEP: q.append(list(p)); rs.append(r); acc = 0.0
        last = p
    n = len(q)
    Rr = [1e9 if (i < L.RH or i >= n - L.RH) else L.radius3(q[i - L.RH], q[i], q[i + L.RH]) for i in range(n)]
    cap = []
    for r in rs:
        c = L.CAP85
        if rough_caps:
            for th, v in ROUGH_CAP:
                if r.get('roughness', 0) >= th: c = min(c, v); break
        cap.append(c)
    v = [min(cap[i], math.sqrt(L.A_LAT * Rr[i])) for i in range(n)]
    v[0] = v[-1] = 0.0
    for i in range(n - 1): v[i + 1] = min(v[i + 1], math.sqrt(v[i] ** 2 + 2 * L.ACC85 * L.RSTEP))
    for i in range(n - 2, -1, -1): v[i] = min(v[i], math.sqrt(v[i + 1] ** 2 + 2 * L.BRK85 * L.RSTEP))
    t = sum(L.RSTEP / max(0.5, (v[i] + v[i + 1]) / 2) for i in range(n - 1))
    return q, rs, Rr, v, t


def fun_check():
    nodes, adj = net_graph()
    pos = {s['id']: s['pos'] for s in STOPS}; pos['garage'] = GARAGE['pos']; pos['mart'] = MART['pos']
    cargo = {s['id']: s['cargo'] for s in STOPS}
    out = []
    for a, b in PAIRS:
        row = {'pair': f'{a}→{b}', 'cargo': cargo[b]}
        for tag, ok in (('big', big_ok), ('short', lambda r: True)):
            path = route(nodes, adj, pos[a], pos[b], ok)
            if not path:
                row[tag] = None; continue
            q, rs, Rr, v, t0 = drive(path, False)
            _, _, _, v1, t1 = drive(path, True)
            Ls = len(q) * L.RSTEP
            hp = TM.hairpins([(Ls, [(p[0], p[1]) for p in q], True)])
            st = L.runs([x >= L.STRAIGHT_R for x in Rr])
            rough = sum(L.RSTEP for r in rs if r.get('roughness', 0) >= 0.4)
            g = max((abs(q[i + 2][2] - q[i][2]) / (2 * L.RSTEP) for i in range(len(q) - 2)), default=0)
            # 숨 쉬는 곳: 요철(0.4+) 덩어리가 끝나고 60 m 안에 시작하는 R≥150 직선 80 m+
            rf = L.runs([r.get('roughness', 0) >= 0.4 or Rr[i] < 20 for i, r in enumerate(rs)]); breath = []   # 긴장 = 요철 또는 R<20 굽이
            for s0, ln in st:
                if ln >= 80 and any(0 <= s0 * L.RSTEP - (r0 * L.RSTEP + rl) <= 60 for r0, rl in rf):
                    breath.append([round(s0 * L.RSTEP), round(ln)])
            row[tag] = {'m': round(Ls), 't': round(t0, 1), 'tRough': round(t1, 1), 'hairpins': len(hp), 'straightMax': round(max((ln for _, ln in st), default=0)),
                        'roughM': round(rough), 'gradeMax': round(g * 100, 1), 'roads': list(dict.fromkeys(r['id'] for r in rs)), 'breath': breath}
        if row.get('big') and row.get('short'):
            row['ratio'] = round(row['short']['t'] / row['big']['t'], 2); row['ratioRough'] = round(row['short']['tRough'] / row['big']['tRough'], 2)
        out.append(row)
    return out


# ── 그림 (real_towns 와 같은 축척·색) ──
PX = 900; M2PX = PX / 1200


def to_px(x, zz):
    return ((x + 600) * M2PX, (600 - zz) * M2PX)


def render(plan, path):
    N = 150; small = Image.new('RGB', (N, N))
    for i in range(N):
        for j in range(N):
            x = -600 + (j + .5) * 1200 / N; zz = 600 - (i + .5) * 1200 / N; y = h(x, zz)
            if y <= 1: c = (150, 190, 210)
            else:
                t = min(1, y / 80); c = (int(238 - 60 * t), int(232 - 45 * t), int(218 - 70 * t))
            small.putpixel((j, i), c)
    img = small.resize((PX, PX), Image.Resampling.BILINEAR); dr = ImageDraw.Draw(img)
    for f in plan['fields']:
        x, zz, w, d = f['rect']; a = to_px(x - w / 2, zz + d / 2); b = to_px(x + w / 2, zz - d / 2); dr.rectangle([a, b], fill=(196, 206, 160))
    for p in plan['parcels']:
        dr.polygon([to_px(*c) for c in L.rect_corners(p)], fill=(196, 120, 95), outline=(150, 90, 70))
    coast = [to_px(x, zc(x)) for x in range(-600, 601, 4)]
    dr.line(coast, fill=(40, 100, 150), width=3)
    order = {'alley': 0, 'stairs': 1, 'shortcut': 2, 'village': 3, 'main': 4}
    for r in sorted(plan['roads'], key=lambda r: order[r['kind']]):
        pts = [to_px(p[0], p[1]) for p in L.sample(r, 1.0)]
        k, w = r['kind'], r['width']
        if k == 'stairs':
            for a, b in zip(pts, pts[1:]): dr.line([a, b], fill=(220, 90, 30), width=2)
        elif k == 'alley':
            dr.line(pts, fill=(230, 140, 60), width=1)
        else:
            col = (70, 60, 55) if k == 'main' else (110, 95, 80) if (k == 'village' and w >= 4.0) else (150, 130, 110) if k == 'village' else (170, 120, 50)
            wd = max(2, int(w * M2PX * 1.6))
            dr.line(pts, fill=col, width=wd, joint='curve')
            for e in (pts[0], pts[-1]): dr.ellipse([e[0] - wd / 2, e[1] - wd / 2, e[0] + wd / 2, e[1] + wd / 2], fill=col)
            if k == 'shortcut':
                for i in range(0, len(pts) - 1, 6): dr.line([pts[i], pts[min(len(pts) - 1, i + 2)]], fill=(240, 220, 160), width=1)
    f = TM_font(22); fs = TM_font(14); fb = TM_font(16)
    for s in plan['stops'] + [plan['garage'], plan['mart']]:
        u, v = to_px(*s['pos']); lab = str(s.get('n', 'G' if s['id'] == 'garage' else 'M'))
        dr.ellipse([u - 9, v - 9, u + 9, v + 9], fill=(40, 90, 200) if lab in 'GM' else (220, 60, 50), outline=(255, 255, 255), width=2)
        dr.text((u, v), lab, fill=(255, 255, 255), font=fs, anchor='mm'); dr.text((u + 11, v - 9), s['name'], fill=(20, 20, 20), font=fs)
    dr.rectangle([0, 0, PX, 34], fill=(255, 255, 255))
    dr.text((10, 5), f"마을 v5 (D-156 층 합성) — 건물 {len(plan['parcels'])}채, 760 m 사방", fill=(30, 30, 30), font=f)
    bar = 200 * M2PX; dr.rectangle([12, PX - 30, 12 + bar, PX - 24], fill=(30, 30, 30))
    dr.text((16 + bar, PX - 38), '200 m   고도 최고 %.0f m' % max(h(0, 380), h(300, 380)), fill=(30, 30, 30), font=fb)
    a = to_px(-HALF, HALF); b = to_px(HALF, -HALF); dr.rectangle([a, b], outline=(90, 90, 90), width=1)
    img.save(path)
    return img


def TM_font(sz):
    for fp in ('C:/Windows/Fonts/malgunbd.ttf', 'C:/Windows/Fonts/malgun.ttf'):
        if os.path.exists(fp):
            from PIL import ImageFont
            return ImageFont.truetype(fp, sz)
    from PIL import ImageFont
    return ImageFont.load_default()


def legend(width):
    Lg = Image.new('RGB', (width, 40), (255, 255, 255)); dr = ImageDraw.Draw(Lg); fs = TM_font(15)
    items = [((70, 60, 55), '큰길'), ((110, 95, 80), '마을 차길'), ((150, 130, 110), '좁은 차길'), ((170, 120, 50), '거친 지름길(v5)'),
             ((220, 90, 30), '계단'), ((230, 140, 60), '사람 길'), ((196, 120, 95), '건물'), ((150, 190, 210), '바다')]
    x = 10
    for c, t in items:
        dr.rectangle([x, 12, x + 22, 28], fill=c); dr.text((x + 28, 10), t, fill=(0, 0, 0), font=fs); x += 150
    return Lg


def to_net(plan):
    roads = []
    for r in plan['roads']:
        pts = [(p[0] + 600, p[1] + 600) for p in L.sample(r, 2.0)]
        roads.append({'pts': pts, 'car': r['kind'] in GRADE, 'steps': r['kind'] == 'stairs', 'cls': r['kind'], 'w': r['width']})
    blds = [[(c[0] + 600, c[1] + 600) for c in L.rect_corners(p)] for p in plan['parcels']]
    return {'name': '마을 v5', 'roads': roads, 'bld': blds, 'h': lambda X, Z: h(X - 600, Z - 600)}


def main():
    finalize()
    parcels = buildings()
    roads = []
    for r in ROADS:
        o = {k: v for k, v in r.items() if k not in ('xy',)}
        xsk = r.get('xs') or ('lane' if r['kind'] == 'village' and r['width'] < 4 else r['kind'])
        o.pop('xs', None)
        e, wk = XS.get(xsk, ('none', 0.0)) if xsk != 'shared' else ('none', 0.0)
        o['crossSection'] = {'left': {'edge': e, 'walk': wk}, 'right': {'edge': e, 'walk': wk}}
        if r['kind'] in ('alley', 'stairs'): o['terrainFollows'] = False
        o['closed'] = False
        roads.append(o)
    ctrl = [[x, zz, round(h(x, zz), 1)] for x in range(-HALF, HALF + 1, 40) for zz in range(-HALF, HALF + 1, 40) if zz > zc(x) + 3]
    land = [[x, round(zc(x), 1)] for x in range(-420, 421, 6)] + [[420, 440], [-420, 440]]
    juncs = []
    for r in roads:
        for key in ('start', 'end'):
            jr = r.get(key, {}).get('join') if isinstance(r.get(key), dict) else None
            if jr:
                p = r['knots'][0 if key == 'start' else -1]
                car = r['kind'] in GRADE
                juncs.append({'pos': p[:2], 'roads': [r['id'], jr], 'fillet': 5.0 if car else 2.0, 'note': '모서리 둥글게(차 5 m / 사람 2 m) — 가설'})
    plan = {
        'meta': {'name': 'town-v5', 'version': 5, 'decision': 'D-155, D-156', 'units': 'm', 'axes': 'x=동, z=북, y=높이(해면 0). 점은 [x, z, y]',
                 'rhythmModel': '최고속 85 km/h · 가속 10.7 · 제동 8 m/s² · 굽이 √(5.82·R) (planlib.rhythm) + 요철 상한 가설 ROUGH_CAP',
                 'layers': '지형→해안선→항구 심장→해안 큰길→비탈→건물→둥글게→재미 검사', 'note': 'main 은 이제 고리가 아니다(closed=false, 양 끝 터널)'},
        'terrain': {'method': 'idw', 'power': 3, 'softening': 6.0, 'cliffWidth': 8.0, 'seaFloor': -6.0, 'beaches': [], 'controlPoints': ctrl},
        'sea': {'level': 0, 'landPolygon': land},
        'roads': roads,
        'districts': [
            {'id': 'D_harbor', 'density': 'dense', 'name': '항구 심장(도모노우라)', 'poly': [list(W(u, 0)) for u in range(-125, 131, 15)] + [list(W(u, 100)) for u in range(130, -126, -15)]},
            {'id': 'D_slope', 'density': 'medium', 'name': '비탈 층층 마을(포지타노·카몰리)', 'poly': [list(W(u, 100)) for u in range(-205, 256, 20)] + [list(W(u, 310)) for u in range(255, -206, -20)]},
        ],
        'garage': GARAGE, 'mart': MART, 'stops': STOPS, 'fields': FIELDS,
        'boundary': {'play': [[-HALF, -HALF], [HALF, -HALF], [HALF, HALF], [-HALF, HALF]]},
        'parcels': parcels, 'junctions': juncs, 'trees': {'points': []},
    }
    plan['fun'] = fun_check()
    m = TM.measure(to_net(plan), geometric=True)
    plan['metrics'] = m
    json.dump(plan, open(os.path.join(OUT, 'town-v5.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    img = render(plan, os.path.join(OUT, 'town-v5.png'))
    # 나란히: v5 · 도모노우라 · 카몰리 · 포지타노 (모두 1200 m 칸)
    ims = [img] + [Image.open(os.path.join(OUT, 'real-towns', t + '.png')).convert('RGB') for t in ('tomonoura', 'camogli', 'positano')]
    sm = 600; sheet = Image.new('RGB', (sm * 4, sm + 40), (255, 255, 255))
    for k, im in enumerate(ims): sheet.paste(im.resize((sm, sm), Image.Resampling.LANCZOS), (k * sm, 0))
    sheet.paste(legend(sm * 4), (0, sm)); sheet.save(os.path.join(OUT, 'v5-vs-real.png'))
    print('parcels', len(parcels), 'roads', len(roads))
    for r in roads:
        if r.get('cutFillMax', 0) > 3: print('  cut/fill', r['id'], r['cutFillMax'])
    print(json.dumps(m, ensure_ascii=False))
    for row in plan['fun']:
        print(json.dumps(row, ensure_ascii=False))


if __name__ == '__main__':
    main()
