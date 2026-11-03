# T자 이음매 틈 높이: 가장자리 변(긴 변) 위에 놓인 다른 가장자리 정점(매달린 정점)의 높이 − 긴 변 직선 높이
# 사용: python -X utf8 tjunc.py <asset>
import sys, json
from collections import Counter, defaultdict
sys.path.insert(0, __file__.rsplit('/', 1)[0].rsplit('\\', 1)[0])
from mesh_edges import load

idx, pos = load(sys.argv[1])
key = {}; weld = []
for p in pos:
    k = (round(p[0] * 100), round(p[1] * 100), round(p[2] * 100)); weld.append(key.setdefault(k, len(weld)))
c = Counter()
for t in range(0, len(idx), 3):
    a, b, cc = weld[idx[t]], weld[idx[t + 1]], weld[idx[t + 2]]
    for u, v in ((a, b), (b, cc), (cc, a)): c[(u, v) if u < v else (v, u)] += 1
open_e = [e for e, n in c.items() if n == 1]
bv = set(); [bv.update(e) for e in open_e]
grid = defaultdict(list)
for i in bv: grid[(int(pos[i][0] // 1), int(pos[i][2] // 1))].append(i)
gaps = []; hang = 0
for (u, v) in open_e:
    A, B = pos[u], pos[v]; dx, dz = B[0] - A[0], B[2] - A[2]; L2 = dx * dx + dz * dz
    if L2 < 1e-6: continue
    best = 0.0; found = False
    for gx in range(int(min(A[0], B[0]) // 1), int(max(A[0], B[0]) // 1) + 1):
        for gz in range(int(min(A[2], B[2]) // 1), int(max(A[2], B[2]) // 1) + 1):
            for i in grid.get((gx, gz), ()):
                if i == u or i == v: continue
                P = pos[i]; s = ((P[0] - A[0]) * dx + (P[2] - A[2]) * dz) / L2
                if s <= 1e-3 or s >= 1 - 1e-3: continue
                qx, qz = A[0] + dx * s - P[0], A[2] + dz * s - P[2]
                if qx * qx + qz * qz > 4e-6: continue
                found = True; d = abs(P[1] - (A[1] + (B[1] - A[1]) * s)); best = max(best, d)
    if found: hang += 1; gaps.append((best, ((A[0] + B[0]) / 2, (A[1] + B[1]) / 2, (A[2] + B[2]) / 2)))
gaps.sort(reverse=True)
json.dump([(round(g, 3), [round(x, 2) for x in m]) for g, m in gaps if g > .02], open(sys.argv[1].rsplit('/', 1)[-1] + '.gaps.json', 'w'))
hist = Counter('>50cm' if g > .5 else '10-50cm' if g > .1 else '2-10cm' if g > .02 else '0.5-2cm' if g > .005 else '<5mm' for g, _ in gaps)
print(json.dumps({'asset': sys.argv[1], 'open_edges': len(open_e), 'long_edges_with_hanging_vertex': hang, 'gap_hist': hist,
                  'top20': [(round(g, 3), [round(x, 1) for x in m]) for g, m in gaps[:20]]}, ensure_ascii=False))
