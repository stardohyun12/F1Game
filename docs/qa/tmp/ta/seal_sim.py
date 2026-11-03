# TerrainSeal.Round 파이썬 판 — 꿰맨 뒤 열린 변·T자가 남는지 검산(유니티 없이)
import sys, json
from collections import defaultdict
sys.path.insert(0, __file__.rsplit('\\', 1)[0].rsplit('/', 1)[0])
from mesh_edges import load

idx, pos = load(sys.argv[1]); V = list(pos); tris = list(idx)

def rnd():
    first = {}; weld = []
    for i, p in enumerate(V):
        k = (round(p[0] * 1000), round(p[1] * 1000), round(p[2] * 1000)); weld.append(first.setdefault(k, i))
    cnt = {}; owner = {}
    for t in range(0, len(tris), 3):
        for e in range(3):
            a, b = weld[tris[t + e]], weld[tris[t + (e + 1) % 3]]
            if a == b: continue
            key = (a, b) if a < b else (b, a)
            if key in cnt: cnt[key] += 1
            else: cnt[key] = 1; owner[key] = t + e
    op = [k for k, n in cnt.items() if n == 1]
    grid = defaultdict(list); seen = set()
    for k in op:
        for v in k:
            if v not in seen: seen.add(v); grid[(int(V[v][0] // 1), int(V[v][2] // 1))].append(v)
    hang = {}; fixed = 0
    for k in op:
        slot = owner[k]; t = slot - slot % 3; e = slot % 3
        a, b = tris[t + e], tris[t + (e + 1) % 3]; A, B = V[a], V[b]
        abx, abz = B[0] - A[0], B[2] - A[2]; L2 = abx * abx + abz * abz
        if L2 < 1e-6: continue
        hs = []
        for gz in range(int(min(A[2], B[2]) // 1), int(max(A[2], B[2]) // 1) + 1):
            for gx in range(int(min(A[0], B[0]) // 1), int(max(A[0], B[0]) // 1) + 1):
                for v in grid.get((gx, gz), ()):
                    if v == weld[a] or v == weld[b]: continue
                    px, pz = V[v][0] - A[0], V[v][2] - A[2]; s = (px * abx + pz * abz) / L2
                    if s <= 1e-3 or s >= 1 - 1e-3: continue
                    if (abx * s - px) ** 2 + (abz * s - pz) ** 2 > 4e-6: continue
                    hs.append((s, v))
        if not hs: continue
        hs.sort(); hang.setdefault(t, [None, None, None])[e] = hs; fixed += 1
    if fixed == 0: return 0, len(op), 0
    nt = []; cen = 0
    for t in range(0, len(tris), 3):
        arr = hang.get(t)
        if arr is None: nt += tris[t:t + 3]; continue
        sides = [e for e in range(3) if arr[e] is not None]
        if len(sides) == 1:
            e = sides[0]; a, b, c = tris[t + e], tris[t + (e + 1) % 3], tris[t + (e + 2) % 3]; prev = a
            for _, v in arr[e]: nt += [prev, v, c]; prev = v
            nt += [prev, b, c]
        else:
            ring = []
            for e in range(3):
                ring.append(tris[t + e])
                if arr[e]: ring += [v for _, v in arr[e]]
            A, B, C = V[tris[t]], V[tris[t + 1]], V[tris[t + 2]]
            g = len(V); V.append(tuple((A[i] + B[i] + C[i]) / 3 for i in range(3))); cen += 1
            for i in range(len(ring)): nt += [ring[i], ring[(i + 1) % len(ring)], g]
    tris[:] = nt
    return fixed, len(op), cen

for r in range(3):
    f, o, c = rnd(); print('round', r, 'fixed', f, 'open_before', o, 'centroids', c); sys.stdout.flush()
    if f == 0: break
# 남은 열린 변 중 맵 둘레 4 m 밖
from collections import Counter
first = {}; weld = []
for i, p in enumerate(V):
    k = (round(p[0] * 1000), round(p[1] * 1000), round(p[2] * 1000)); weld.append(first.setdefault(k, i))
cnt = Counter()
for t in range(0, len(tris), 3):
    for e in range(3):
        a, b = weld[tris[t + e]], weld[tris[t + (e + 1) % 3]]; cnt[(a, b) if a < b else (b, a)] += 1
xs = [p[0] for p in V]; zs = [p[2] for p in V]; inner = []
for (a, b), n in cnt.items():
    if n != 1: continue
    m = [(V[a][i] + V[b][i]) / 2 for i in range(3)]
    if min(xs) + 4 < m[0] < max(xs) - 4 and min(zs) + 4 < m[2] < max(zs) - 4: inner.append([round(x, 1) for x in m])
print('final open', sum(1 for n in cnt.values() if n == 1), 'interior', len(inner), inner[:10], 'nonmanifold', sum(1 for n in cnt.values() if n > 2))
