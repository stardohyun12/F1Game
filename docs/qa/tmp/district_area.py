import json, sys
sys.path.insert(0, "tools/map")
import planlib as L
p = json.load(open("docs/map/bayloop-plan.json", encoding="utf8"))
D = json.loads(open("docs/qa/tmp/districts.json", encoding="utf8").read())
t = L.Terrain(p)
cnt = {}; tot = 0
for x in range(-184, 161, 4):
    for z in range(-212, 263, 4):
        if t.h(x, z) < 1.0:
            continue
        tot += 1
        d = next((d for d in D if L.in_poly(x, z, d["poly"])), None)
        k = d["density"] if d else "none"
        cnt[k] = cnt.get(k, 0) + 1
print(tot * 16, {k: round(v / tot, 3) for k, v in cnt.items()})
