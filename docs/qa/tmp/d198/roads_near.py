import json, os, math
R = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../.."))
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
for i, r in enumerate(j["roads"]):
    ks = r["knots"]
    if any(-75 < x < 10 and -85 < z < -15 for x, z, y in ks):
        L = sum(math.dist(a[:2], b[:2]) for a, b in zip(ks, ks[1:]))
        print(i, r["kind"], r.get("width"), "L", round(L), "from", [round(v, 1) for v in ks[0]], "to", [round(v, 1) for v in ks[-1]])
