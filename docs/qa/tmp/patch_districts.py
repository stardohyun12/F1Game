import json, re
W = {"D_harbor": {"shop_house": 5, "harbor_warehouse": 2, "town_house": 2, "terrace_house": 1},
     "D_slope": {"terrace_house": 5, "town_house": 2, "shop_house": 1},
     "D_hill": {"farmhouse": 3, "detached_house": 3, "cliff_villa": 2},
     "D_cape": {"cliff_villa": 4, "detached_house": 2, "farmhouse": 1},
     "D_south": {"cliff_villa": 2, "detached_house": 2, "farmhouse": 1},
     "D_village": {"town_house": 4, "detached_house": 2, "terrace_house": 1, "shop_house": 1, "cliff_villa": 1}}
p = 'tools/map/make_plan.py'; s = open(p, encoding='utf8').read()
for k, v in W.items():
    s, n = re.subn(r'(\{"id": "%s".*?\n\s+"types": )\{[^}]*\}' % k, lambda m: m.group(1) + json.dumps(v, ensure_ascii=False), s, count=1, flags=re.S)
    assert n == 1, k
open(p, 'w', encoding='utf8', newline='').write(s)
j = 'docs/map/bayloop-plan.json'; raw = open(j, encoding='utf8').read(); d = json.loads(raw)
for x in d['districts']: x['types'] = W[x['id']]
indent = 2 if raw.startswith('{\n  "') else None
open(j, 'w', encoding='utf8', newline='').write(json.dumps(d, ensure_ascii=False, indent=indent))
print('indent', indent)
