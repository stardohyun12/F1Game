# 과제 0248: town-v6b 차길(main·village) 그래프 → 지점 사이 최단 경로 → 평면도
# 사용: python -X utf8 tv6_plan.py [draft]   (draft = 지점 후보만 표시)
import json, sys, os, heapq, math
from PIL import Image, ImageDraw, ImageFont
R = os.path.join(os.path.dirname(__file__), "../../..")
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
car = [r for r in j["roads"] if r["kind"] in ("main", "village")]
# 노드: 1m 격자로 스냅(이음 = 같은 칸)
def key(x, z): return (round(x), round(z))
pos, adj = {}, {}
def add(a, b, w):
    adj.setdefault(a, []).append((b, w)); adj.setdefault(b, []).append((a, w))
for r in car:
    ks = r["knots"]
    for i, (x, z, y) in enumerate(ks):
        k = key(x, z); pos[k] = (x, z, y)
        if i: p = ks[i - 1]; add(key(p[0], p[1]), k, math.dist((p[0], p[1]), (x, z)))
# 가까운 끝점끼리 이음(3m)
ends = [key(*r["knots"][0][:2]) for r in car] + [key(*r["knots"][-1][:2]) for r in car]
allk = list(pos)
for e in ends:
    for k in allk:
        if k != e and math.dist(e, k) < 3.0: add(e, k, math.dist(e, k))
def near(x, z): return min(pos, key=lambda k: (k[0] - x) ** 2 + (k[1] - z) ** 2)
def path(a, b):
    d = {a: 0}; pv = {}; q = [(0, a)]
    while q:
        c, u = heapq.heappop(q)
        if u == b: break
        if c > d[u]: continue
        for v, w in adj.get(u, []):
            if c + w < d.get(v, 1e18): d[v] = c + w; pv[v] = u; heapq.heappush(q, (c + w, v))
    out = [b]
    while out[-1] != a: out.append(pv[out[-1]])
    return out[::-1], d[b]
GO = len(sys.argv) > 1 and sys.argv[1] == "go"
PF = "tv6_pts_go.json" if GO else "tv6_pts.json"
W = json.load(open(os.path.join(os.path.dirname(__file__), PF), encoding="utf8")) if os.path.exists(os.path.join(os.path.dirname(__file__), PF)) else None
S, O = 2.0, 400   # 1px = 0.5m
img = Image.new("RGB", (int(800 * S), int(800 * S)), (40, 70, 95)); dr = ImageDraw.Draw(img)
def P(x, z): return ((x + O) * S, (O - z) * S)
# 지형 높이점
for x, z, y in j["terrain"]["controlPoints"]:
    if y > 0: c = int(min(255, 70 + y * 2.2)); dr.rectangle([*P(x - 10, z + 10), *P(x + 10, z - 10)], fill=(c, c - 10, c - 40))
for r in j["roads"]:
    col = {"main": (250, 250, 250), "village": (190, 190, 190)}.get(r["kind"], (120, 110, 100))
    dr.line([P(x, z) for x, z, y in r["knots"]], fill=col, width=3 if r["kind"] == "main" else 2)
try: font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 22)
except Exception: font = None
out = {}
if W:
    pts = W["points"]; full = []; legs = []
    for a, b in zip(pts, pts[1:]):
        p, L = path(near(*a["pos"]), near(*b["pos"])); full += p if not full else p[1:]; legs.append((a["name"], b["name"], L))
    dr.line([P(*pos[k][:2]) for k in full], fill=(255, 120, 30), width=7)
    tot = sum(l[2] for l in legs)
    for a in pts:
        x, z = a["pos"]; dr.ellipse([P(x, z)[0] - 9, P(x, z)[1] - 9, P(x, z)[0] + 9, P(x, z)[1] + 9], fill=(255, 220, 0))
        dr.text((P(x, z)[0] + 12, P(x, z)[1] - 12), f'{a["name"]} ({x:.0f},{z:.0f}) y{pos[near(x, z)][2]:.0f}', fill=(255, 255, 255), font=font)
    dr.text((20, 20), "Map_TownV6 tutorial loop  total %.0f m" % tot, fill=(255, 255, 255), font=font)
    for i, (a, b, L) in enumerate(legs): dr.text((20, 50 + i * 28), f"{a} -> {b}: {L:.0f} m", fill=(255, 255, 255), font=font)
    out = {"legs": legs, "total": tot, "path": [[round(pos[k][0], 2), round(pos[k][1], 2), round(pos[k][2], 2)] for k in full]}
    json.dump(out, open(os.path.join(os.path.dirname(__file__), "tv6_route_go.json" if GO else "tv6_route.json"), "w"), ensure_ascii=False)
    print(legs, "total", round(tot), "nodes", len(full))
    # 경사(10m 창)
    ys = out["path"]; g = []
    s = 0; acc = [0]
    for a, b in zip(ys, ys[1:]): acc.append(acc[-1] + math.dist(a[::2], b[::2]))
    print("max grade 10m-window %", round(max(abs(ys[k][1] - ys[i][1]) / max(1, acc[k] - acc[i]) * 100 for i in range(len(ys)) for k in [min(len(ys) - 1, next((m for m in range(i, len(ys)) if acc[m] - acc[i] >= 10), len(ys) - 1))]), 1))
for nm, (x, z) in [("MART", j["mart"]["pos"]), ("GARAGE", j["garage"]["pos"])] + [(s["id"], s["pos"]) for s in j["stops"]]:
    dr.rectangle([P(x, z)[0] - 6, P(x, z)[1] - 6, P(x, z)[0] + 6, P(x, z)[1] + 6], fill=(255, 60, 60)); dr.text((P(x, z)[0] + 8, P(x, z)[1] + 4), nm, fill=(255, 200, 200), font=font)
if GO: pass
elif len(sys.argv) > 1:
    # 높은 차길 노드 후보 (y 상위)
    hi = sorted(pos.values(), key=lambda p: -p[2])[:1]
    for x, z, y in sorted(pos.values(), key=lambda p: -p[2])[:400:40]: dr.text(P(x, z), "%.0f" % y, fill=(255, 255, 0), font=font)
    for gx in range(-400, 401, 100):
        dr.line([P(gx, -400), P(gx, 400)], fill=(80, 80, 80)); dr.line([P(-400, gx), P(400, gx)], fill=(80, 80, 80)); dr.text(P(gx, 0), str(gx), fill=(200, 200, 255), font=font); dr.text(P(0, gx), str(gx), fill=(200, 200, 255), font=font)
    img.resize((800, 800)).save(os.path.join(os.path.dirname(__file__), "tv6_draft.png"))
else:
    img.save(os.path.join(R, "docs/map/townv6-tutorial-loop.png"))
