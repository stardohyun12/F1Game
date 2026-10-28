import json, os, math
R = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../.."))
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
for i, r in enumerate(j["roads"]):
    ks = r["knots"]
    if any(-100 < x < -25 and -30 < z < 25 for x, z, y in ks) and r["kind"] in ("main", "village"):
        print(i, r["id"], r["kind"], r.get("width"), [(round(x, 1), round(z, 1)) for x, z, y in ks])
