"""fb0344: 능선으로 올라오는 마을길 3개 — 지형·건물 격자(fb0344_grid.cs 덤프)에서 경사 벌점 A* → 경사 ≤12% 높이 프로필."""
import heapq, json, math, sys
X0, Z0 = 212, -134
T, R, B = [], [], []
for line in open('docs/qa/tmp/fb0344_grid.txt'):
    row = [c.split(',') for c in line.split()]
    T.append([float(c[0]) for c in row]); R.append([float(c[1]) for c in row]); B.append([int(c[2]) for c in row])
NZ, NX = len(T), len(T[0])
G = 0.12
# 건물 3 m 안은 막음
blk = [[False] * NX for _ in range(NZ)]
for j in range(NZ):
    for i in range(NX):
        if B[j][i]:
            for dj in range(-2, 3):
                for di in range(-2, 3):
                    if 0 <= j + dj < NZ and 0 <= i + di < NX and di * di + dj * dj <= 5: blk[j + dj][i + di] = True
def H(i, j): return R[j][i] if R[j][i] > -90 else T[j][i]
def ij(x, z): return int(round(x - X0)), int(round(z - Z0))
MOVES = [(1,0),(0,1),(-1,0),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1),(2,1),(1,2),(-2,1),(-1,2),(2,-1),(1,-2),(-2,-1),(-1,-2)]
def astar(s, e, thr=0.10):
    si, sj = ij(*s); ei, ej = ij(*e)
    ang = [math.atan2(dj, di) for di, dj in MOVES]
    pq = [(0, si, sj, -1)]; cost = {(si, sj, -1): 0}; prev = {}; endst = None
    while pq:
        f, i, j, dk = heapq.heappop(pq)
        if (i, j) == (ei, ej): endst = (i, j, dk); break
        c0 = cost[(i, j, dk)]
        if f - math.hypot(i - ei, j - ej) > c0 + 1e-6: continue
        for k, (di, dj) in enumerate(MOVES):
            a, b = i + di, j + dj
            if not (0 <= a < NX and 0 <= b < NZ): continue
            near_end = abs(a - ei) + abs(b - ej) < 3 or abs(a - si) + abs(b - sj) < 3
            d = math.hypot(di, dj); g = abs(H(a, b) - H(i, j)) / d
            turn = 0.0
            if dk >= 0:
                da = abs((ang[k] - ang[dk] + math.pi) % (2 * math.pi) - math.pi)
                turn = 6.0 * da * da
            c = d * (1 + 60 * max(0, g - thr) + (2.0 if R[b][a] > -90 and not near_end else 0) + (80.0 if blk[b][a] and not near_end else 0)) + turn
            nc = c0 + c; st = (a, b, k)
            if nc < cost.get(st, 1e18):
                cost[st] = nc; prev[st] = (i, j, dk)
                heapq.heappush(pq, (nc + math.hypot(a - ei, b - ej), a, b, k))
    p = [endst]
    while p[-1][:2] != (si, sj): p.append(prev[p[-1]])
    return [(q[0] + X0, q[1] + Z0) for q in reversed(p)]
def smooth(pts, n=3):
    out = list(pts)
    for _ in range(n):
        out = [out[0]] + [((out[k-1][0] + 2*out[k][0] + out[k+1][0]) / 4, (out[k-1][1] + 2*out[k][1] + out[k+1][1]) / 4) for k in range(1, len(out) - 1)] + [out[-1]]
    return out
def terr(x, z):
    i, j = x - X0, z - Z0; a, b = int(i), int(j); u, v = i - a, j - b
    h = lambda p, q: T[q][p]
    return (h(a,b)*(1-u)+h(a+1,b)*u)*(1-v) + (h(a,b+1)*(1-u)+h(a+1,b+1)*u)*v
def profile(pts, y0, y1):
    s = [0.0]
    for k in range(1, len(pts)): s.append(s[-1] + math.dist(pts[k], pts[k-1]))
    L = s[-1]
    # 지형을 따르되 끝점 고정 + 경사 ≤ G (앞·뒤 패스 번갈아)
    y = [terr(*p) for p in pts]; y[0], y[-1] = y0, y1
    for _ in range(50):
        for k in range(1, len(y)): y[k] = min(max(y[k], y[k-1] - G*(s[k]-s[k-1])), y[k-1] + G*(s[k]-s[k-1]))
        y[-1] = y1
        for k in range(len(y)-2, -1, -1): y[k] = min(max(y[k], y[k+1] - G*(s[k+1]-s[k])), y[k+1] + G*(s[k+1]-s[k]))
        y[0] = y0
    # 3점 평균으로 꺾임 녹이기(경사 한도 안쪽)
    for _ in range(6): y = [y[0]] + [(y[k-1] + 2*y[k] + y[k+1]) / 4 for k in range(1, len(y)-1)] + [y[-1]]
    for _ in range(50):
        for k in range(1, len(y)): y[k] = min(max(y[k], y[k-1] - G*(s[k]-s[k-1])), y[k-1] + G*(s[k]-s[k-1]))
        y[-1] = y1
        for k in range(len(y)-2, -1, -1): y[k] = min(max(y[k], y[k+1] - G*(s[k+1]-s[k])), y[k+1] + G*(s[k+1]-s[k]))
        y[0] = y0
    gmax = max(abs(y[k]-y[k-1]) / max(1e-6, s[k]-s[k-1]) for k in range(1, len(y)))
    return y, L, gmax
ROADS = [
    ("ridge_up_a", (250.0, -112.0), (258.0, -61.0), "03:47 (262,-95) 여기 길 하나 더"),
    ("ridge_up_a2", (250.0, -112.0), (287.0, -63.0), "alt"),
    ("ridge_up_b", (245.0, -62.0), (234.0, -52.0), "03:48 (238,-55) 자연스럽게 올라오게"),
    ("ridge_up_c", (248.0, -61.0), (291.0, -37.0), "03:49 (265,-44) 올라오기 쉽게"),
]
out = []
for rid, s, e, why in ROADS:
    for thr in (0.10, 0.08, 0.06, 0.05, 0.04):
        path = astar(s, e, thr); sm = smooth(path, 6)
        si, sj = ij(*s); ei, ej = ij(*e)
        Lp = sum(math.dist(sm[k], sm[k-1]) for k in range(1, len(sm)))
        if abs(H(ei, ej) - H(si, sj)) / Lp <= 0.105: break
    print(rid, 'thr', thr)
    # 2 m 마다 knot
    ks = [sm[0]]
    for p in sm[1:]:
        if math.dist(p, ks[-1]) >= 2.0: ks.append(p)
    if ks[-1] != sm[-1]: ks[-1] = sm[-1]
    si, sj = ij(*s); ei, ej = ij(*e)
    y, L, gm = profile(ks, H(si, sj), H(ei, ej))
    cut = [y[k] - terr(*ks[k]) for k in range(len(ks))]
    def rad(a, b, c):
        A, B, C = math.dist(b, c), math.dist(a, c), math.dist(a, b)
        cr = abs((b[0]-a[0])*(c[1]-a[1]) - (b[1]-a[1])*(c[0]-a[0]))
        return 1e9 if cr < 1e-9 else A*B*C / (2*cr)
    rmin = min(rad(ks[k-1], ks[k], ks[k+1]) for k in range(1, len(ks)-1))
    print(rid, 'min radius', round(rmin, 1))
    nb = sum(1 for p in ks if blk[ij(*p)[1]][ij(*p)[0]])
    print(f"{rid}: bldg {nb} {L:.0f} m, y {y[0]:.1f}->{y[-1]:.1f}, rise/len {(y[-1]-y[0])/L*100:.1f}%, max grade {gm*100:.1f}%, fill/cut max {max(cut):+.1f}/{min(cut):+.1f} m, knots {len(ks)}")
    out.append({"id": rid, "kind": "village", "width": 4.5, "roughness": 0.0, "closed": False,
                "knots": [[round(p[0], 2), round(p[1], 2), round(yy, 2)] for p, yy in zip(ks, y)],
                "why": why, "embank": {"shoulder": 1.0, "cut": 2.0, "fill": 3.0}})
json.dump(out, open('docs/qa/tmp/fb0344_roads.json', 'w', encoding='utf8'), ensure_ascii=False, indent=1)
