# 방 주변 150 m 안 OSM 건물 폴리곤 → polys.txt (id y x,z x,z ...)
import json, os, math
H = os.path.dirname(os.path.abspath(__file__)); R = os.path.abspath(os.path.join(H, "../../../.."))
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
CX, CZ = -117.7, -11.2
rem = set(str(w) for w in j.get("removed", {}).get("osm_ways", []))
out = []
for b in j["buildings"]:
    p = b["poly"]; cx = sum(q[0] for q in p) / len(p); cz = sum(q[1] for q in p) / len(p)
    if math.hypot(cx - CX, cz - CZ) > 150: continue
    oid = str(b.get("osm_way", "?"))
    if oid in rem: continue
    out.append(oid + " " + str(b["y"]) + " " + " ".join(f"{q[0]},{q[1]}" for q in p))
open(os.path.join(H, "polys.txt"), "w", encoding="utf8").write("\n".join(out) + "\n")
print(len(out), "buildings; keys", list(j["buildings"][0].keys()))
print("terrain", json.dumps(j["terrain"])[:600])
print("v6b.heights", type(j["v6b"]["heights"]).__name__, json.dumps(j["v6b"]["heights"])[:300])
