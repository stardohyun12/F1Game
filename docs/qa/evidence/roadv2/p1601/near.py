import json, math, sys
d = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
pts = {'268e': (277.2, -66.2), '4143': (246.7, -54.6), '4100': (264.9, -56.8)}
R = float(sys.argv[1]) if len(sys.argv) > 1 else 45
for r in d['roads']:
    k = r['knots']
    L = sum(math.dist(k[i][:2], k[i + 1][:2]) for i in range(len(k) - 1))
    near = {n: round(min(math.dist(q[:2], p) for q in k)) for n, p in pts.items()}
    if min(near.values()) < R:
        print(r['id'], r['kind'], r['width'], 'L=%.0f' % L, near, 'first', k[0], 'last', k[-1], 'n', len(k))
