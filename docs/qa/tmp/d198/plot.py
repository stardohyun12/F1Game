# D-198 배치 확인용: 네 자리 발자국 + OSM 건물 + 길 (위에서 본 그림) + 겹치는 osm_way 목록
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFont
R = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../.."))
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
# id, x, z, yaw, (minx, minz, maxx, maxz) 프리팹 로컬
P = json.load(open(os.path.join(os.path.dirname(__file__), "places.json"), encoding="utf8"))
X0, X1, Z0, Z1, S = -150, -10, -60, 40, 6
img = Image.new("RGB", (int((X1 - X0) * S), int((Z1 - Z0) * S)), (120, 140, 100)); dr = ImageDraw.Draw(img)
def T(x, z): return ((x - X0) * S, (Z1 - z) * S)
def foot(p, m=0.0):
    q = math.radians(p["yaw"]); c, s = math.cos(q), math.sin(q); a, b, cc, d = p["b"]
    pts = [(a - m, b - m), (cc + m, b - m), (cc + m, d + m), (a - m, d + m)]
    # Unity Y 회전(왼손): local (u,v) → world x = u cos + v sin, z = -u sin + v cos
    return [(p["x"] + u * c + v * s, p["z"] - u * s + v * c) for u, v in pts]
def inside(pt, poly):
    x, z = pt; n = len(poly); r = False
    for i in range(n):
        x1, z1 = poly[i]; x2, z2 = poly[(i + 1) % n]
        if (z1 > z) != (z2 > z) and x < (x2 - x1) * (z - z1) / (z2 - z1 + 1e-12) + x1: r = not r
    return r
for r in j["roads"]:
    col = {"main": (250, 250, 250), "village": (200, 200, 200)}.get(r["kind"], (150, 120, 90))
    w = max(1, int(float(r.get("width", 4)) * S))
    dr.line([T(x, z) for x, z, y in r["knots"]], fill=col, width=w)
hits = {}
for b in j["buildings"]:
    poly = [(float(a), float(c)) for a, c in b["poly"]]
    cx = sum(p[0] for p in poly) / len(poly); cz = sum(p[1] for p in poly) / len(poly)
    if not (X0 - 30 < cx < X1 + 30 and Z0 - 30 < cz < Z1 + 30): continue
    hit = None
    for p in P:
        f = foot(p, 0.5)
        # 겹침: 폴리곤 꼭짓점이 발자국 안 / 발자국 꼭짓점이 폴리곤 안 / 격자 샘플
        xs = [q[0] for q in f]; zs = [q[1] for q in f]
        ov = any(inside(v, f) for v in poly) or any(inside(v, poly) for v in f)
        if not ov:
            for k in range(41):
                for l in range(41):
                    pt = (min(xs) + (max(xs) - min(xs)) * k / 40, min(zs) + (max(zs) - min(zs)) * l / 40)
                    if inside(pt, f) and inside(pt, poly): ov = True; break
                if ov: break
        if ov: hit = p["id"]; hits.setdefault(p["id"], []).append(b["osm_way"])
    dr.polygon([T(*v) for v in poly], fill=(230, 60, 60) if hit else (170, 170, 175), outline=(60, 60, 60))
for p in P:
    dr.polygon([T(*v) for v in foot(p)], outline=(255, 150, 0), width=3)
    fx, fz = p["x"], p["z"]; q = math.radians(p["yaw"])
    dr.line([T(fx, fz), T(fx + 6 * math.sin(q), fz + 6 * math.cos(q))], fill=(0, 0, 255), width=3)  # 정면 +Z
    dr.text(T(fx, fz), p["id"], fill=(0, 0, 0))
for gx in range(X0, X1 + 1, 10): dr.line([T(gx, Z0), T(gx, Z1)], fill=(100, 120, 90))
for gz in range(Z0, Z1 + 1, 10): dr.line([T(X0, gz), T(X1, gz)], fill=(100, 120, 90))
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "plot.png")
img.save(out); print(json.dumps(hits))
json.dump([{"id": p["id"], "foot": foot(p)} for p in P], open(os.path.join(os.path.dirname(__file__), "placed.json"), "w"))
json.dump(hits, open(os.path.join(os.path.dirname(__file__), "osm_hits.json"), "w"))
# 길 여유: 발자국 점 ~ 길 중심선 거리 − 폭/2 (음수 = 길 위에 올라탐)
def seg(p, a, b):
    ax, az = a; bx, bz = b; px, pz = p; dx, dz = bx - ax, bz - az; L = dx * dx + dz * dz
    t = 0 if L == 0 else max(0, min(1, ((px - ax) * dx + (pz - az) * dz) / L)); return math.hypot(px - ax - t * dx, pz - az - t * dz)
for p in P:
    f = foot(p); xs = [q[0] for q in f]; zs = [q[1] for q in f]; pts = []
    for k in range(31):
        for l in range(31):
            pt = (min(xs) + (max(xs) - min(xs)) * k / 30, min(zs) + (max(zs) - min(zs)) * l / 30)
            if inside(pt, f): pts.append(pt)
    pts += f
    for ri, r in enumerate(j["roads"]):
        kn = [(float(a), float(b)) for a, b, c in r["knots"]]
        if min(math.hypot(x - p["x"], z - p["z"]) for x, z in kn) > 80: continue
        cl = min(min(seg(q, kn[i], kn[i + 1]) for i in range(len(kn) - 1)) for q in pts) - float(r.get("width", 4)) / 2
        if cl < 2: print(p["id"], "road", ri, r["kind"], "w", r.get("width"), "clear", round(cl, 2), "knots", len(kn), "from", kn[0], "to", kn[-1])
