"""0604 진단: json 차길 그래프로 차고→등대 안전/지름 경로의 길 순서. 사용: python -X utf8 docs/qa/tmp/lh_route.py v6g"""
import json, math, heapq, sys
tag = sys.argv[1] if len(sys.argv) > 1 else 'v6g'
d = json.load(open(f'docs/map/town-{tag}.json', encoding='utf-8'))
g = d['garage']['pos']; lh = [s for s in d['stops'] if s['id'] == 'lighthouse'][0]['pos']
car = [r for r in d['roads'] if r['kind'] in ('main', 'village')]
K = lambda q: (round(q[0], 1), round(q[1], 1))
adj = {}
for r in car:
    for a, b in zip(r['knots'], r['knots'][1:]):
        w = math.dist(a[:2], b[:2])
        adj.setdefault(K(a), []).append((K(b), w, r)); adj.setdefault(K(b), []).append((K(a), w, r))


def cb(r):
    if r['roughness'] >= 0.4 and r['width'] < 4: return 4.0
    return 1.0 if r['kind'] == 'main' else 1.8 if (r['width'] >= 5 and r['roughness'] < 0.4) else 4.0


def route(ok):
    sa = min(adj, key=lambda k: math.dist(k, g)); sb = min(adj, key=lambda k: math.dist(k, lh))
    dist = {sa: 0}; prev = {}; pq = [(0, sa)]
    while pq:
        dd, k = heapq.heappop(pq)
        if k == sb: break
        if dd > dist[k]: continue
        for k2, w, r in adj[k]:
            nd = dd + w * ok(r)
            if nd < dist.get(k2, 1e18): dist[k2] = nd; prev[k2] = (k, r); heapq.heappush(pq, (nd, k2))
    seq = []; k = sb; m = 0
    while k != sa:
        k0, r = prev[k]; m += math.dist(k, k0)
        if not seq or seq[-1][0] != r['id']: seq.append([r['id'], 0, k])
        seq[-1][1] += math.dist(k, k0); k = k0
    return round(m), [(s, round(l), p) for s, l, p in reversed(seq)]


for name, ok in (('big', cb), ('short', lambda r: 1.0)):
    print(name, route(ok))
