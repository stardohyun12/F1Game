"""0604 진단: json 안에서 arm.edgeIndex 가 knots 의 edge 자리를 가리키나, RC.load pts 와 knots 수가 같나."""
import json, math, sys
sys.path.insert(0, 'tools/map')
import road_check as RC
d = json.load(open('docs/map/town-v6h.json', encoding='utf-8'))
kn = {r['id']: r['knots'] for r in d['roads']}
_, rs = RC.load('v6h'); pts = {r['id']: r['pts'] for r in rs}
for j in d['junctions']:
    if j['id'] not in ('J35', 'J53', 'J10'): continue
    for a in j['arms']:
        k = kn[a['road']]; e = a['edgeIndex']
        print(j['id'], a['road'], e, len(k), len(pts[a['road']]), round(math.dist(k[e][:2], a['edge'][:2]), 2),
              round(math.dist(pts[a['road']][min(e, len(pts[a['road']]) - 1)][:2], a['edge'][:2]), 2))
