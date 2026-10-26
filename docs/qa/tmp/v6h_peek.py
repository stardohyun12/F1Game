# 과제 0640 — v6h json 구조 훑기
import json
from collections import Counter
d = json.load(open('docs/map/town-v6h.json', encoding='utf8'))
print(list(d.keys()))
for k, v in d.items():
    if isinstance(v, dict): print(k, 'dict', list(v.keys())[:20])
    elif isinstance(v, list): print(k, 'list', len(v))
print(json.dumps(d['junctions'][0])[:1000])
print(json.dumps(d.get('v6f', {}))[:500])
for k in ('v6e', 'v6g', 'v6h'): print(k, json.dumps(d.get(k, {}))[:300])
r = d['roads'][0]; print({k: (v if k != 'knots' else v[:2]) for k, v in r.items()})
print(Counter(r['kind'] for r in d['roads']))
print(Counter(tuple(sorted(r.keys())) for r in d['roads']))
print([r['id'] for r in d['roads'] if 'light' in r['id'] or 'lh' in r['id'].lower() or 'short' in r['id'] or 'new' in r['id']])
print(Counter(tuple(sorted(j.keys())) for j in d['junctions']))
print(Counter(tuple(sorted(a.keys())) for j in d['junctions'] for a in j['arms']))
