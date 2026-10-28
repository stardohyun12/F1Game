import json, sys
from collections import Counter
d = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
print(list(d.keys()))
print(d['meta'])
t = d['terrain']
print({k: (v if not isinstance(v, list) else len(v)) for k, v in t.items()})
r = d['roads']
print(len(r), list(r[0].keys()))
print({k: (v if k != 'knots' else v[:3]) for k, v in r[0].items()})
print(Counter(x['kind'] for x in r))
print(Counter(round(x['width'], 1) for x in r if x['kind'] in ('main', 'village')))
print(t['controlPoints'][:3], t['controlPoints'][-1])
