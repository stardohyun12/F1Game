# TerrainSeal.Round(F1/Assets/Editor/TerrainSeal.cs) 의 파이썬 쌍둥이 — V·tris 를 그 자리에서 고치고 고친 큰 변 수를 돌려준다.
from collections import defaultdict

def rnd(V, tris):
    first = {}; weld = []
    for i, p in enumerate(V):
        weld.append(first.setdefault((round(p[0] * 1000), round(p[1] * 1000), round(p[2] * 1000)), i))
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
    if fixed == 0: return 0
    nt = []
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
            g = len(V); V.append(tuple((A[i] + B[i] + C[i]) / 3 for i in range(3)))
            for i in range(len(ring)): nt += [ring[i], ring[(i + 1) % len(ring)], g]
    tris[:] = nt
    return fixed
