import json, numpy as np
T = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
E = json.load(open('docs/map/env-buildings.json', encoding='utf-8'))
lm = {b['id']: np.array(b['world']) for b in E['landmarks']}
for r in T['roads']:
    k = np.array(r['knots'], float)[:, :2]
    ds = {n: round(float(np.min(np.linalg.norm(k - p, axis=1))), 1) for n, p in lm.items()}
    if min(ds.values()) < 30:
        L = np.sum(np.linalg.norm(np.diff(k, axis=0), axis=1))
        print(r['id'], r['kind'], r['width'], round(L), ds, r.get('closed'))
