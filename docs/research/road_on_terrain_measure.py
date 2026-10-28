"""D-203 리서치: 현재 씬 메시(읽기만)로 '길 가장자리에서 지형이 길보다 높은 구간' 길이를 잰다.
실행: python -X utf8 docs/research/road_on_terrain_measure.py   (리포 루트에서)
 - 지형 = Map_TownV6_Mats/Mesh_TownV6_Terrain_loopfix.asset (393x393, 2 m 격자, BuildBayLoop.Gray.cs TerrainGray 와 같은 삼각형 분할)
 - 길 = Mesh_ROAD_*.asset (Ribbon 의 보이는 띠: 앞 2n 꼭짓점 = 점마다 (왼, 오른) 가장자리), loopfix 사본이 있으면 그쪽
 - 길 종류·폭 = docs/map/town-v6b.json
"""
import json, os, re, struct, math, sys, collections

ROOT = os.getcwd()
M = os.path.join(ROOT, 'F1/Assets/Scenes/Map_TownV6_Mats')
GN, GX0, GS = 393, -392.0, 2.0


def read_mesh_positions(path, stride=24):
    txt = open(path, encoding='utf-8').read()
    m = re.search(r'm_VertexCount:\s*(\d+)', txt)
    nv = int(m.group(1))
    h = re.search(r'_typelessdata:\s*([0-9a-f]+)', txt).group(1)
    b = bytes.fromhex(h)
    assert len(b) >= nv * stride, (len(b), nv)
    out = []
    for k in range(nv):
        out.append(struct.unpack_from('<3f', b, k * stride))
    return out


# ---------------- 지형
scene = open(os.path.join(ROOT, 'F1/Assets/Scenes/Map_TownV6.unity'), encoding='utf-8').read()
TERR_FILE = None
for nm in sys.argv[1:] or ('Mesh_TownV6_Terrain_d203.asset', 'Mesh_TownV6_Terrain_loopfix.asset', 'Mesh_TownV6_Terrain.asset'):
    mp = os.path.join(M, nm + '.meta')
    if os.path.exists(mp) and re.search(r'guid: (\w+)', open(mp).read()).group(1) in scene: TERR_FILE = nm; break
print('terrain asset used by scene:', TERR_FILE)
T = read_mesh_positions(os.path.join(M, TERR_FILE))
assert len(T) == GN * GN
H = [[T[i * GN + j][1] for j in range(GN)] for i in range(GN)]
assert abs(T[1][2] - GX0 - GS) < 1e-3   # i*GN+j 순서 확인(j 가 z)


def terr(x, z, grid=None):
    g = grid or H
    fx = (x - GX0) / GS; fz = (z - GX0) / GS
    fx = min(max(fx, 0), GN - 1.001); fz = min(max(fz, 0), GN - 1.001)
    i = int(fx); j = int(fz); u = fx - i; v = fz - j
    a = g[i][j]; b = g[i + 1][j]; c = g[i][j + 1]; d = g[i + 1][j + 1]
    if u + v <= 1: return a + u * (b - a) + v * (c - a)
    return d + (1 - u) * (b - d) + (1 - v) * (c - d)


# 깎기 전 자연 지형(controlPoints 8 m → 쌍선형 → 2 m 격자 → 3x3 평균 4번) = BuildGray 와 같은 식
D = json.load(open(os.path.join(ROOT, 'docs/map/town-v6b.json'), encoding='utf-8'))
cps = D['terrain']['controlPoints']; cstep = D['terrain']['step']
cN = int(round(math.sqrt(len(cps)))); cx0 = cps[0][0]
cH = [0.0] * len(cps)
for p in cps:
    i = int(round((p[0] - cx0) / cstep)); j = int(round((p[1] - cx0) / cstep)); cH[i * cN + j] = p[2]


def cp_at(x, z):
    fx = min(max((x - cx0) / cstep, 0), cN - 1.001); fz = min(max((z - cx0) / cstep, 0), cN - 1.001)
    i = int(fx); j = int(fz); u = fx - i; v = fz - j
    a = cH[i * cN + j]; b = cH[(i + 1) * cN + j]; c = cH[i * cN + j + 1]; d = cH[(i + 1) * cN + j + 1]
    return (a + (b - a) * u) * (1 - v) + (c + (d - c) * u) * v


N0 = [[cp_at(GX0 + i * GS, GX0 + j * GS) for j in range(GN)] for i in range(GN)]
for _ in range(4):
    s = [r[:] for r in N0]
    for i in range(1, GN - 1):
        for j in range(1, GN - 1):
            N0[i][j] = sum(s[i + di][j + dj] for di in (-1, 0, 1) for dj in (-1, 0, 1)) / 9.0

# ---------------- 길
kinds = {}; widths = {}
for r in D['roads']:
    kinds[r['id']] = r['kind']; widths[r['id']] = r['width']
scene = open(os.path.join(ROOT, 'F1/Assets/Scenes/Map_TownV6.unity'), encoding='utf-8').read()
in_scene = set(re.findall(r'm_Name: (ROAD_[^\r\n]+)', scene))
files = {}
for fn in os.listdir(M):
    if not fn.endswith('.asset') or '_col' in fn: continue
    m = re.match(r'Mesh_(?:Mesh_)?(ROAD_.+?)(_loopfix)?\.asset$', fn)
    if not m: continue
    nm = m.group(1)
    if nm not in files or m.group(2): files[nm] = fn
roads = []
for nm, fn in files.items():
    if nm not in in_scene: continue
    P = read_mesh_positions(os.path.join(M, fn))
    n = len(P) // 6 if len(P) % 6 == 0 else len(P) // 2   # 띠+치마 = 6n, 띠만 = 2n
    band = P[:2 * n]
    rid = nm[5:]
    kind = kinds.get(rid, 'village' if rid.startswith('join_') else '?')
    pts = []
    for k in range(n):
        L = band[2 * k]; R = band[2 * k + 1]
        pts.append((L, R, ((L[0] + R[0]) / 2, (L[1] + R[1]) / 2, (L[2] + R[2]) / 2)))
    cx = [p[2][0] for p in pts]; cz = [p[2][2] for p in pts]
    if not pts or max(abs(v) for v in cx + cz) > 390: continue   # 지형 상자(±392) 밖 조각은 뺀다
    roads.append(dict(id=rid, kind=kind, pts=pts, file=fn))
print('roads in scene mesh files:', len(roads), 'of', len(in_scene), 'ROAD_ objects; json roads', len(D['roads']))

# ---------------- 다른 길이 덮은 점은 센다(갈림목 겹침) — 공간 해시
CELL = 8.0
grid = collections.defaultdict(list)
for ri, r in enumerate(roads):
    for (L, R, C) in r['pts']:
        hw = math.hypot(L[0] - R[0], L[2] - R[2]) / 2
        grid[(int(C[0] // CELL), int(C[2] // CELL))].append((ri, C[0], C[1], C[2], hw))


def covered(ri, x, y, z):
    cx, cz = int(x // CELL), int(z // CELL)
    for dx in (-1, 0, 1):
        for dz in (-1, 0, 1):
            for (rj, px, py, pz, hw) in grid.get((cx + dx, cz + dz), ()):
                if rj == ri: continue
                if (px - x) ** 2 + (pz - z) ** 2 < max(hw - 0.15, 0.2) ** 2 and abs(py - y) < 0.3: return True
    return False


THR = [0.05, 0.10, 0.30, 1.0]
res = {k: dict(len=0.0, over={t: 0.0 for t in THR}, center_over=0.0, hang=0.0, cut=collections.Counter(), grade_len=collections.Counter(), cross=collections.Counter()) for k in ('main', 'village', 'alley', 'stairs', '?')}
bad_pts = []   # (x,z,diff) 그림용
allpts = []; cs_list = []; cy_list = []
worst = []
for ri, r in enumerate(roads):
    pts = r['pts']; kd = r['kind']; R_ = res.setdefault(kd, res['?'])
    for k in range(len(pts)):
        L, R, C = pts[k]
        seg = 0.0
        if k > 0: seg += math.hypot(C[0] - pts[k - 1][2][0], C[2] - pts[k - 1][2][2]) / 2
        if k + 1 < len(pts): seg += math.hypot(C[0] - pts[k + 1][2][0], C[2] - pts[k + 1][2][2]) / 2
        if seg <= 0: continue
        R_['len'] += seg
        dl = terr(L[0], L[2]) - L[1]; dr = terr(R[0], R[2]) - R[1]; dc = terr(C[0], C[2]) - C[1]
        cl = covered(ri, L[0], L[1], L[2]); cr = covered(ri, R[0], R[1], R[2])
        d = max(dl if not cl else -9, dr if not cr else -9)
        for t in THR:
            if d >= t: R_['over'][t] += seg
        if dc >= 0.10 and not covered(ri, C[0], C[1], C[2]): R_['center_over'] += seg
        if min(L[1] - terr(L[0], L[2]), R[1] - terr(R[0], R[2])) >= 0.5: R_['hang'] += seg
        if d >= 0.10: bad_pts.append((C[0], C[2], d, kd)); worst.append((d, r['id'], C[0], C[2]))
        allpts.append((C[0], C[2], kd, d, r['id'], seg, dc, min(L[1] - terr(L[0], L[2]), R[1] - terr(R[0], R[2]))))
        # 자연 지형 대비 깎기·메우기(옹벽이 필요할 높이): 가장자리에서 자연 지형 − 길
        nl = terr(L[0], L[2], N0) - L[1]; nr = terr(R[0], R[2], N0) - R[1]
        h = max(abs(nl), abs(nr))
        b = '<0.5' if h < .5 else '<1.5' if h < 1.5 else '<3' if h < 3 else '>=3'
        R_['cut'][b] += seg
        w = math.hypot(L[0] - R[0], L[2] - R[2])
        cs = abs(terr(L[0], L[2], N0) - terr(R[0], R[2], N0)) / max(w, 0.5)   # 길 폭 가로질러 자연 지형 경사
        cs_list.append(cs); cy_list.append(C[1])
        R_['cross']['<5%' if cs < .05 else '<10%' if cs < .10 else '<20%' if cs < .20 else '>=20%'] += seg
        if 0 < k < len(pts) - 1:
            pa, pb = pts[k - 1][2], pts[k + 1][2]
            run = math.hypot(pb[0] - pa[0], pb[2] - pa[2])
            if run > 0.6:
                g = abs(pb[1] - pa[1]) / run
                R_['grade_len']['<8%' if g < .08 else '<12%' if g < .12 else '<15%' if g < .15 else '>=15%'] += seg

out = {}
tot = dict(len=0.0, o5=0.0, o10=0.0, o30=0.0, o100=0.0)
for kd, v in res.items():
    if v['len'] == 0: continue
    out[kd] = dict(len_m=round(v['len']), over5=round(v['over'][0.05]), over10=round(v['over'][0.10]), over30=round(v['over'][0.30]), over100=round(v['over'][1.0]),
                   center_over10=round(v['center_over']), hang50=round(v['hang']),
                   cut={k: round(x) for k, x in v['cut'].items()}, cross={k: round(x) for k, x in v['cross'].items()}, grade={k: round(x) for k, x in v['grade_len'].items()})
    tot['len'] += v['len']; tot['o5'] += v['over'][0.05]; tot['o10'] += v['over'][0.10]; tot['o30'] += v['over'][0.30]; tot['o100'] += v['over'][1.0]
car = [k for k in ('main', 'village') if k in res]
cl = sum(res[k]['len'] for k in car); c10 = sum(res[k]['over'][0.10] for k in car)
print(json.dumps(out, ensure_ascii=False, indent=1))
print('ALL  length %.0f m | edge terrain>=5cm %.0f | >=10cm %.0f (%.1f%%) | >=30cm %.0f | >=1m %.0f' % (tot['len'], tot['o5'], tot['o10'], 100 * tot['o10'] / tot['len'], tot['o30'], tot['o100']))
print('CAR  length %.0f m | >=10cm %.0f (%.1f%%)' % (cl, c10, 100 * c10 / cl if cl else 0))
worst.sort(reverse=True)
print('worst 8:', [(round(d, 2), i, round(x), round(z)) for d, i, x, z in worst[:8]])
json.dump(dict(summary=out, total_len=tot['len'], over10=tot['o10'], car_len=cl, car_over10=c10), open('docs/research/road_on_terrain_measure.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
json.dump(bad_pts, open('docs/research/road_on_terrain_badpts.json', 'w'))

# ---------------- 스샷 자리(feedback 03:27:40 point = 198.9, -77.4) 반경 45 m
NX, NZ, NR = 198.9, -77.4, 45.0
loc = collections.defaultdict(lambda: dict(len=0.0, o10=0.0, maxd=-9, hang=0.0, cen=0.0))
for (x, z, kd, d, rid, seg, dc, lowgap) in allpts:
    if (x - NX) ** 2 + (z - NZ) ** 2 > NR * NR: continue
    v = loc[(rid, kd)]; v['len'] += seg; v['maxd'] = max(v['maxd'], d)
    if d >= 0.10: v['o10'] += seg
    if dc >= 0.10: v['cen'] += seg
    if lowgap >= 0.5: v['hang'] += seg
print('--- 스샷 자리 반경 45 m (길 id, 종류: 길이 / 가장자리 10cm↑ / 한복판 10cm↑ / 지형이 0.5m↑ 아래로 뜸 / 최대차)')
for (rid, kd), v in sorted(loc.items(), key=lambda t: -t[1]['len']):
    print('%-28s %-8s %5.0f m | over10 %4.0f | center %4.0f | hang %4.0f | max %.2f' % (rid, kd, v['len'], v['o10'], v['cen'], v['hang'], v['maxd']))

# ---------------- 그림: 위에서 본 길 (회색 정상 · 빨강 차길 10cm↑ · 주황 걷는 길 10cm↑)
import zlib
S = 2; W = int(784 * S)
img = bytearray(b'\xf4\xf1\xe8' * (W * W))


def px(x, z, c, r=1):
    i = int((x + 392) * S); j = int((392 - z) * S)
    for a in range(-r, r + 1):
        for b in range(-r, r + 1):
            ii, jj = i + a, j + b
            if 0 <= ii < W and 0 <= jj < W: img[(jj * W + ii) * 3:(jj * W + ii) * 3 + 3] = bytes(c)


for (x, z, kd, d, rid, seg, dc, lg) in allpts:   # 먼저 정상 길
    if d < 0.10: px(x, z, (150, 150, 150) if kd in ('main', 'village') else (205, 205, 205), 1 if kd in ('main', 'village') else 0)
for (x, z, kd, d, rid, seg, dc, lg) in allpts:
    if d >= 0.10: px(x, z, (220, 30, 30) if kd in ('main', 'village') else (240, 150, 40), 1 if kd in ('main', 'village') else 0)
for dx in range(-60, 61): px(NX + dx * 0.75, NZ + 45, (30, 90, 220), 0); px(NX + dx * 0.75, NZ - 45, (30, 90, 220), 0)   # 스샷 자리 표시(파랑 사각)
for dz in range(-60, 61): px(NX + 45, NZ + dz * 0.75, (30, 90, 220), 0); px(NX - 45, NZ + dz * 0.75, (30, 90, 220), 0)
raw = b''.join(b'\x00' + bytes(img[j * W * 3:(j + 1) * W * 3]) for j in range(W))


def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)


png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', W, W, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
open('docs/research/2026-10-29-road-on-terrain-map.png', 'wb').write(png)
print('png written', len(png))

# ---------------- 차길(main·village) 가장자리 10cm↑ 점의 원인 분류
# 층 = 12 m 안에 '다른 차길' 점이 있고 높이 차 0.4 m↑ (겹쳐 지나가는 다른 층 → 깎기가 서로 덮어씀)
cgrid = collections.defaultdict(list)
for k, (x, z, kd, d, rid, seg, dc, lg) in enumerate(allpts):
    if kd in ('main', 'village'): cgrid[(int(x // 12), int(z // 12))].append((rid, x, z, cy_list[k]))
cause = collections.Counter(); cause_n = collections.Counter()
for k, (x, z, kd, d, rid, seg, dc, lg) in enumerate(allpts):
    if kd not in ('main', 'village') or d < 0.10: continue
    layer = False; near_other = False
    for dx in (-1, 0, 1):
        for dz in (-1, 0, 1):
            for (rj, px_, pz_, py_) in cgrid.get((int(x // 12) + dx, int(z // 12) + dz), ()):
                if rj == rid: continue
                dist = math.hypot(px_ - x, pz_ - z)
                if dist < 12:
                    near_other = True
                    if abs(py_ - cy_list[k]) > 0.4: layer = True
    steep = cs_list[k] >= 0.20
    c = 'layer(겹친 다른 층)' if layer else ('junction(다른 차길 12m 안·같은 높이)' if near_other else ('cross>=20%' if steep else 'other'))
    cause[c] += seg; cause_n[c] += 1
print('--- 차길 10cm↑ 원인(m):', {k: round(v) for k, v in cause.items()})
# 한복판(center)이 지형에 묻힌 차길
cen = sum(seg for (x, z, kd, d, rid, seg, dc, lg), cs_ in zip(allpts, cs_list) if kd in ('main', 'village') and dc >= 0.10)
print('차길 한복판 10cm↑ 묻힘:', round(cen), 'm')
# 보이는 도로 폭 안 가장자리 중 '길이 지형 위로 0.5 m↑ 뜬' 값
hang = sum(seg for (x, z, kd, d, rid, seg, dc, lg) in allpts if kd in ('main', 'village') and lg >= 0.5)
print('차길 가장자리가 지형보다 0.5m↑ 높이 뜬(치마가 가리는):', round(hang), 'm')
# 10cm↑ 가 연속 구간 몇 개? (같은 길 id 에서 연속 점 묶음, 끊김 5 m 초과 허용)
runs = 0; prev = None
for (x, z, kd, d, rid, seg, dc, lg) in allpts:
    if kd not in ('main', 'village'): prev = None; continue
    if d >= 0.10:
        if prev is None or prev[0] != rid or math.hypot(prev[1] - x, prev[2] - z) > 5: runs += 1
        prev = (rid, x, z)
print('차길 10cm↑ 연속 구간 수(근사):', runs)

# ================= 시뮬: 제안 1안(원뿔 깎기 + 1 m 격자 + 투영 높이)으로 지금 길 모양에 적용하면 10cm↑ 가 얼마나 남나
G1 = 1.0; NG = 785          # 1 m 격자 ±392
SH, SC, SF, CLR, REACH = 1.0, 1 / 2., 1 / 3., 0.03, 14.0   # 어깨 1 m · 깎기 2:1 · 메우기 3:1 · 길 아래 3 cm · 영향 14 m
cand = {}                   # (i,j) -> (e, yr)
car_roads = [r for r in roads if r['kind'] in ('main', 'village')]
for r in car_roads:
    P = [(c[0], c[1], c[2], math.hypot(l[0] - rr[0], l[2] - rr[2]) / 2) for (l, rr, c) in r['pts']]
    for k in range(len(P) - 1):
        ax, ay, az, ah = P[k]; bx, by, bz, bh = P[k + 1]
        dx, dz = bx - ax, bz - az; L2 = dx * dx + dz * dz
        if L2 < 1e-6: continue
        reach = max(ah, bh) + REACH
        i0 = int((min(ax, bx) - reach + 392) / G1); i1 = int((max(ax, bx) + reach + 392) / G1) + 1
        j0 = int((min(az, bz) - reach + 392) / G1); j1 = int((max(az, bz) + reach + 392) / G1) + 1
        for i in range(max(0, i0), min(NG - 1, i1) + 1):
            x = -392 + i * G1
            for j in range(max(0, j0), min(NG - 1, j1) + 1):
                z = -392 + j * G1
                t = ((x - ax) * dx + (z - az) * dz) / L2; t = 0 if t < 0 else 1 if t > 1 else t
                px_, pz_ = ax + t * dx, az + t * dz
                e = math.hypot(x - px_, z - pz_) - (ah + t * (bh - ah))
                if e > REACH: continue
                o = cand.get((i, j))
                if o is None or e < o[0]: cand[(i, j)] = (e, ay + t * (by - ay))
H1 = {}


def n1(i, j): return terr(-392 + i * G1, -392 + j * G1, N0)


def h1(i, j):
    v = H1.get((i, j))
    if v is not None: return v
    n = n1(i, j); c = cand.get((i, j))
    if c is None: v = n
    else:
        e, yr = c; s = max(0.0, e - SH); y0 = yr - CLR
        v = min(max(n, y0 - s * SF), y0 + s * SC)
    H1[(i, j)] = v; return v


def terr1(x, z):
    fx = (x + 392) / G1; fz = (z + 392) / G1
    i = min(max(int(fx), 0), NG - 2); j = min(max(int(fz), 0), NG - 2); u = fx - i; v = fz - j
    a = h1(i, j); b = h1(i + 1, j); c = h1(i, j + 1); d = h1(i + 1, j + 1)
    return a + u * (b - a) + v * (c - a) if u + v <= 1 else d + (1 - u) * (b - d) + (1 - v) * (c - d)


after = collections.Counter(); alen = 0.0; wall_len = 0.0; slope_len = 0.0; side_len = 0.0
for ri, r in enumerate(roads):
    if r['kind'] not in ('main', 'village'): continue
    pts = r['pts']
    for k in range(len(pts)):
        L, R, C = pts[k]
        seg = 0.0
        if k > 0: seg += math.hypot(C[0] - pts[k - 1][2][0], C[2] - pts[k - 1][2][2]) / 2
        if k + 1 < len(pts): seg += math.hypot(C[0] - pts[k + 1][2][0], C[2] - pts[k + 1][2][2]) / 2
        if seg <= 0: continue
        alen += seg
        cl = covered(ri, L[0], L[1], L[2]); cr = covered(ri, R[0], R[1], R[2])
        d = max(terr1(L[0], L[2]) - L[1] if not cl else -9, terr1(R[0], R[2]) - R[1] if not cr else -9)
        for t in (0.03, 0.05, 0.10, 0.30):
            if d >= t: after[t] += seg
        for P_, cv in ((L, cl), (R, cr)):
            side_len += seg
            hh = abs(terr(P_[0], P_[2], N0) - P_[1])
            if hh > 1.2: wall_len += seg
            elif hh > 0.5: slope_len += seg
print('--- 시뮬(1안) 차길 %.0f m: 가장자리 지형이 길보다 3cm↑ %.0f · 5cm↑ %.0f · 10cm↑ %.0f · 30cm↑ %.0f  (지금은 10cm↑ %.0f)' % (alen, after[0.03], after[0.05], after[0.10], after[0.30], c10))
print('    자연지형(N0)과 가장자리 높이차: 1.2 m 넘는 변 길이 %.0f m (석축 후보) · 0.5~1.2 m %.0f m · 변 합계 %.0f m' % (wall_len, slope_len, side_len))
print('    수정된 1 m 격자 점', len(cand), '/ 전체', NG * NG)

# 시뮬 뒤에도 10cm↑ 로 남는 자리: 겹친 층이 원인인가
rest = collections.Counter(); restpts = []
for ri, r in enumerate(roads):
    if r['kind'] not in ('main', 'village'): continue
    pts = r['pts']
    for k in range(len(pts)):
        L, R, C = pts[k]
        seg = 0.0
        if k > 0: seg += math.hypot(C[0] - pts[k - 1][2][0], C[2] - pts[k - 1][2][2]) / 2
        if k + 1 < len(pts): seg += math.hypot(C[0] - pts[k + 1][2][0], C[2] - pts[k + 1][2][2]) / 2
        if seg <= 0: continue
        cl = covered(ri, L[0], L[1], L[2]); cr = covered(ri, R[0], R[1], R[2])
        d = max(terr1(L[0], L[2]) - L[1] if not cl else -9, terr1(R[0], R[2]) - R[1] if not cr else -9)
        if d < 0.10: continue
        layer = False; near = False
        for dx in (-1, 0, 1):
            for dz in (-1, 0, 1):
                for (rj, px_, pz_, py_) in cgrid.get((int(C[0] // 12) + dx, int(C[2] // 12) + dz), ()):
                    if rj == r['id']: continue
                    if math.hypot(px_ - C[0], pz_ - C[2]) < 12:
                        near = True
                        if abs(py_ - C[1]) > 0.4: layer = True
        rest['layer' if layer else 'near-same-level' if near else 'other'] += seg
        restpts.append((round(C[0]), round(C[2]), r['id'], round(d, 2)))
print('--- 시뮬 뒤 10cm↑ 남은 원인(m):', {k: round(v) for k, v in rest.items()})
print('    예:', restpts[:6])
