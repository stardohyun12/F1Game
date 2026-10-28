import json, os
H = os.path.dirname(os.path.abspath(__file__)); R = os.path.abspath(os.path.join(H, "../../../.."))
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
hits = json.load(open(os.path.join(H, "osm_hits.json")))
ways = sorted({w for v in hits.values() for w in v})
with open(os.path.join(H, "osm_polys.txt"), "w") as f:
    for b in j["buildings"]:
        if b["osm_way"] in ways: f.write(" ".join(f"{float(x):.3f},{float(z):.3f}" for x, z in b["poly"]) + "\n")
print(ways)
