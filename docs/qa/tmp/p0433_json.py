# 과제 0433: ridge_up_* 원문 + (250,-112) 근처 길
import json, math, sys
d = json.load(open('docs/map/town-v6b.json', encoding='utf8'))
for r in d['roads']:
    if r['id'].startswith('ridge_up_'):
        print(r['id'], r['width'], r['knots'][:3], '...', r['knots'][-2:])
px, pz = float(sys.argv[1]), float(sys.argv[2])
best = []
for r in d['roads']:
    ks = r['knots']
    for i in range(1, len(ks)):
        a, b = ks[i - 1], ks[i]
        dx, dz = b[0] - a[0], b[1] - a[1]
        L = dx * dx + dz * dz or 1e-9
        t = max(0, min(1, ((px - a[0]) * dx + (pz - a[1]) * dz) / L))
        qx, qz = a[0] + t * dx, a[1] + t * dz
        best.append((round(math.hypot(px - qx, pz - qz), 1), r['id'], r.get('kind'), r.get('width'), round(qx, 1), round(qz, 1), round(a[2] + t * (b[2] - a[2]), 2)))
best.sort()
seen = set()
for b in best:
    if b[1] in seen: continue
    seen.add(b[1]); print(b)
    if len(seen) >= 5: break
