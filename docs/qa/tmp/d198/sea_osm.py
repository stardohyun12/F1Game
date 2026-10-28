# sea_block.txt 의 Walls_0 맞은 점 → 가장 가까운 OSM 건물(town-v6b.json buildings poly 변) 로 이름 붙이기
import json, math, os
H = os.path.dirname(os.path.abspath(__file__)); R = os.path.abspath(os.path.join(H, "../../../.."))
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
def segd(p, a, b):
    dx, dz = b[0] - a[0], b[1] - a[1]; L = dx * dx + dz * dz
    t = 0 if L == 0 else max(0, min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L))
    return math.hypot(p[0] - a[0] - t * dx, p[1] - a[1] - t * dz)
out = {}
for l in open(os.path.join(H, "sea_block.txt"), encoding="utf8"):
    s = l.split()
    if len(s) < 2 or s[1] == "VISIBLE": continue
    yaw = int(s[0])
    if s[1] != "Walls_0":
        out.setdefault(s[1], set()).add(yaw); continue
    p = (float(s[2]), float(s[3]))
    best = min(((min(segd(p, b["poly"][i], b["poly"][i + 1]) for i in range(len(b["poly"]) - 1)), b.get("osm_way")) for b in j["buildings"]), key=lambda x: x[0])
    k = "osm_way " + str(best[1]) + " (" + s[5] + ", 변거리 " + str(round(best[0], 2)) + ")"
    out.setdefault(k, set()).add(yaw)
for k, v in sorted(out.items(), key=lambda kv: min(kv[1])):
    print(k, "yaw", min(v), "~", max(v))
