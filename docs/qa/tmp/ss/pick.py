import json, math, numpy as np
from collections import Counter
P = json.load(open('docs/map/streetscape-plan.json', encoding='utf-8'))
T = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
roads = {r['id']: r for r in T['roads']}
els = P['elements']
disc = [e for e in els if e['kind'] != 'curb_segment']

def poly(rid):
    k = np.array(roads[rid]['knots'], float)
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(k[:, :2], axis=0), axis=1))]
    return k, d

# tomo: per road, best 300m window by discrete count
best = []
for rid in set(e['road_id'] for e in disc if e['zone'] == 'tomo'):
    if rid not in roads: continue
    k, d = poly(rid)
    st = sorted(e['station_m'] for e in disc if e['road_id'] == rid and 'station_m' in e)
    for s0 in np.arange(0, max(1, d[-1] - 300) + 1, 10):
        n = sum(1 for s in st if s0 <= s <= s0 + 300)
        best.append((n, rid, s0, d[-1], roads[rid]['width'], roads[rid].get('kind')))
best.sort(reverse=True)
print('TOMO', best[:8])

# posi hairpins: max heading change within 25m
hp = []
for rid in set(e['road_id'] for e in disc if e['zone'] == 'posi'):
    if rid not in roads: continue
    k, d = poly(rid)
    if d[-1] < 120: continue
    for i in range(len(k)):
        j = np.searchsorted(d, d[i] + 25)
        if j >= len(k) - 1 or i == 0: continue
        a = k[i, :2] - k[i - 1, :2]; b = k[j + 1, :2] - k[j, :2]
        ang = math.degrees(math.acos(np.clip(a @ b / np.linalg.norm(a) / np.linalg.norm(b), -1, 1)))
        if ang > 130:
            n = sum(1 for e in disc if e['road_id'] == rid and abs(e['station_m'] - d[i]) <= 100)
            hp.append((n, round(ang), rid, round(d[i]), round(d[-1])))
hp.sort(reverse=True)
print('POSI', hp[:8])

g = [x for x in P['groups'] if x['zone'] == 'ridge']
for x in g: print('RIDGE', x['id'], x['anchor_id'], x['anchor_xz'], x['road_id'], len(x['element_ids']), x['why'])
print(Counter(e['road_id'] for e in els if e['zone'] == 'ridge').most_common(10))
