# 지형 T자 틈 지도(전/후) — 에디터 없이 .asset 을 읽어 그린다.
# 전 = 지금 지형 에셋 그대로, 후 = TerrainSeal.Round 와 같은 규칙(seal_sim.rnd)을 파이썬으로 돌린 결과.
# 사용: python -X utf8 map_tj.py <asset> <out_dir>
import sys, json, importlib.util
from collections import Counter, defaultdict
import numpy as np
from PIL import Image, ImageDraw
here = __file__.replace('\\', '/').rsplit('/', 1)[0]
sys.path.insert(0, here)
from mesh_edges import load

def weld_of(V):
    first = {}; return [first.setdefault((round(p[0] * 1000), round(p[1] * 1000), round(p[2] * 1000)), i) for i, p in enumerate(V)]

def gaps(V, tris):
    w = weld_of(V); c = Counter()
    for t in range(0, len(tris), 3):
        for e in range(3):
            a, b = w[tris[t + e]], w[tris[t + (e + 1) % 3]]
            if a != b: c[(a, b) if a < b else (b, a)] += 1
    op = [k for k, n in c.items() if n == 1]
    grid = defaultdict(list); seen = set()
    for k in op:
        for v in k:
            if v not in seen: seen.add(v); grid[(int(V[v][0] // 1), int(V[v][2] // 1))].append(v)
    out = []; xs = [p[0] for p in V]; zs = [p[2] for p in V]; mnx, mxx, mnz, mxz = min(xs), max(xs), min(zs), max(zs); inner = 0
    for (u, v) in op:
        A, B = V[u], V[v]; m = ((A[0] + B[0]) / 2, (A[1] + B[1]) / 2, (A[2] + B[2]) / 2)
        if m[1] <= .3 or m[0] < mnx + 4 or m[0] > mxx - 4 or m[2] < mnz + 4 or m[2] > mxz - 4: continue
        inner += 1
        dx, dz = B[0] - A[0], B[2] - A[2]; L2 = dx * dx + dz * dz; best = 0.0
        if L2 > 1e-6:
            for gx in range(int(min(A[0], B[0]) // 1), int(max(A[0], B[0]) // 1) + 1):
                for gz in range(int(min(A[2], B[2]) // 1), int(max(A[2], B[2]) // 1) + 1):
                    for i in grid.get((gx, gz), ()):
                        if i in (u, v): continue
                        P = V[i]; s = ((P[0] - A[0]) * dx + (P[2] - A[2]) * dz) / L2
                        if s <= 1e-3 or s >= 1 - 1e-3: continue
                        if (A[0] + dx * s - P[0]) ** 2 + (A[2] + dz * s - P[2]) ** 2 > 4e-6: continue
                        best = max(best, abs(P[1] - (A[1] + (B[1] - A[1]) * s)))
        out.append((best, m))
    return inner, out

def draw(V, marks, path, title):
    S, cell = 2, 2.0; n = 392; img = np.zeros((n * S, n * S, 3), np.uint8); top = np.full((n, n), -1e9)
    for p in V:
        ix, iz = int((p[0] + 392) // cell), int((p[2] + 392) // cell)
        if 0 <= ix < n and 0 <= iz < n and p[1] > top[iz, ix]: top[iz, ix] = p[1]
    hi = max(1.0, top.max())
    for iz in range(n):
        for ix in range(n):
            y = top[iz, ix]
            if y < .3: c = (38, 58, 92)
            else: g = 70 + 150 * min(1, y / hi); c = (int(g * .55), int(g), int(g * .45))
            img[(n - 1 - iz) * S:(n - iz) * S, ix * S:(ix + 1) * S] = c   # 위가 북(+z)
    im = Image.fromarray(img); d = ImageDraw.Draw(im)
    for g, m in sorted(marks):
        x, y = (m[0] + 392) / cell * S, (n * S - 1) - (m[2] + 392) / cell * S
        col = (255, 0, 0) if g > .10 else (255, 140, 0) if g > .02 else (255, 235, 0)
        r = 2 if g > .02 else 1
        d.rectangle([x - r, y - r, x + r, y + r], fill=col)
    d.rectangle([0, 0, n * S, 16], fill=(0, 0, 0)); d.text((4, 2), title, fill=(255, 255, 255))
    im.save(path)

if __name__ == '__main__':
    asset, out = sys.argv[1], sys.argv[2]
    idx, pos = load(asset); V = list(pos); tris = list(idx)
    inner0, g0 = gaps(V, tris)
    h = lambda gs: dict(Counter('>10cm' if g > .1 else '2-10cm' if g > .02 else '<2cm' for g, _ in gs))
    draw(V, g0, out + '/map_before_tjunc.png', 'BEFORE parcels: interior open edges %d  (red >10cm, orange 2-10cm, yellow <2cm)' % inner0)
    spec = importlib.util.spec_from_file_location('seal_core', here + '/seal_core.py'); sc = importlib.util.module_from_spec(spec); spec.loader.exec_module(sc)
    rounds = []
    for r in range(3):
        f = sc.rnd(V, tris); rounds.append(f)
        if f == 0: break
    inner1, g1 = gaps(V, tris)
    draw(V, g1, out + '/map_after_tjunc_sim.png', 'AFTER TerrainSeal rule (python twin): interior open edges %d' % inner1)
    print(json.dumps({'before_inner_open': inner0, 'before_hist': h(g0), 'rounds_fixed': rounds, 'after_inner_open': inner1, 'after_hist': h(g1), 'verts': [len(pos), len(V)], 'tris': [len(idx) // 3, len(tris) // 3]}, ensure_ascii=False))
