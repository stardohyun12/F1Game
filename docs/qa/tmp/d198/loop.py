# D-198 튜토리얼 고리 재설계: 새 정비소 → 새 보관소 → 광장 → 상점가 → 찻집 → 해안 → 새 정비소
# 길 그래프·최단 경로는 docs/qa/tmp/tv6_plan.py 와 같은 방식(차길 main·village, 1m 스냅, 끝점 3m 이음).
# 출력: loop_pts.txt(봇용 ~2m 간격), route_world.json(yaml 용 ~6m 간격), anchors.json(도로 위 출발·포장 지점), docs/map/townv6-tutorial-loop.png
import json, sys, os, heapq, math
from PIL import Image, ImageDraw, ImageFont
H = os.path.dirname(os.path.abspath(__file__)); R = os.path.abspath(os.path.join(H, "../../../.."))
j = json.load(open(os.path.join(R, "docs/map/town-v6b.json"), encoding="utf8"))
SKIP = set(json.load(open(os.path.join(H, "skip_roads.json"))))       # 정비소 밑으로 끈 길(인덱스)
AVOID = {51}   # 2차: 마을길 51 은 46 과 3 m 나란히 달려 서쪽 끝(-87,-20)에서만 만남 → 경로가 머리핀 되돌이를 만들고 봇이 (-80,-16) 집벽에 낌. 경로는 46 만 씀(그림엔 51 그대로)
car = [r for i, r in enumerate(j["roads"]) if r["kind"] in ("main", "village") and i not in SKIP and i not in AVOID]
def key(x, z): return (round(x), round(z))
pos, adj = {}, {}
def add(a, b, w): adj.setdefault(a, []).append((b, w)); adj.setdefault(b, []).append((a, w))
for r in car:
    ks = r["knots"]
    for i, (x, z, y) in enumerate(ks):
        k = key(x, z); pos[k] = (x, z, y)
        if i: p = ks[i - 1]; add(key(p[0], p[1]), k, math.dist((p[0], p[1]), (x, z)))
ends = [key(*r["knots"][0][:2]) for r in car] + [key(*r["knots"][-1][:2]) for r in car]
for e in ends:
    for k in list(pos):
        if k != e and math.dist(e, k) < 3.0: add(e, k, math.dist(e, k))
# T 이음: 길 끝이 다른 길 선분 한가운데에 닿는 경우(꼭짓점 공유 없음) — 투영점을 노드로 끼워 넣음
segs = []
for ri, r in enumerate(car):
    ks = r["knots"]
    for i in range(1, len(ks)): segs.append((ri, ks[i - 1], ks[i]))
for ri, r in enumerate(car):
    for e in (r["knots"][0], r["knots"][-1]):
        best = None
        for sj, a, b in segs:
            if sj == ri: continue
            dx, dz = b[0] - a[0], b[1] - a[1]; L = dx * dx + dz * dz
            t = 0 if L == 0 else max(0, min(1, ((e[0] - a[0]) * dx + (e[1] - a[1]) * dz) / L))
            px, pz = a[0] + t * dx, a[1] + t * dz; dd = math.hypot(e[0] - px, e[1] - pz)
            if dd < 4.0 and (best is None or dd < best[0]): best = (dd, px, pz, a[2] + t * (b[2] - a[2]), a, b)
        if best:
            dd, px, pz, py, a, b = best; k = key(px, pz); pos.setdefault(k, (px, pz, py)); ek = key(e[0], e[1])
            add(ek, k, max(dd, .01)); add(key(a[0], a[1]), k, math.dist(a[:2], (px, pz))); add(key(b[0], b[1]), k, math.dist(b[:2], (px, pz)))
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
# 도로 위 가장 가까운 점(선분 투영) + 진행 방향
def proj(x, z):
    best = None
    for r in car:
        ks = r["knots"]
        for i in range(1, len(ks)):
            ax, az, ay = ks[i - 1]; bx, bz, by = ks[i]; dx, dz = bx - ax, bz - az; L = dx * dx + dz * dz
            t = 0 if L == 0 else max(0, min(1, ((x - ax) * dx + (z - az) * dz) / L))
            px, pz = ax + t * dx, az + t * dz; dd = math.hypot(x - px, z - pz)
            if best is None or dd < best[0]: best = (dd, px, pz, ay + t * (by - ay), math.degrees(math.atan2(dx, dz)))
    return best
A = json.load(open(os.path.join(H, "anchors_in.json"), encoding="utf8"))   # 새 건물 정면 가운데
g = proj(*A["garage_front"]); d = proj(*A["depot_front"])
# 경유점을 가장 가까운 선분 위 노드로 끼워 넣음(가까운 꼭짓점으로 튀지 않게)
def insert(x, z):
    best = None
    for sj, a, b in segs:
        if car[sj] is None: continue
        dx, dz = b[0] - a[0], b[1] - a[1]; L = dx * dx + dz * dz
        t = 0 if L == 0 else max(0, min(1, ((x - a[0]) * dx + (z - a[1]) * dz) / L))
        px, pz = a[0] + t * dx, a[1] + t * dz; dd = math.hypot(x - px, z - pz)
        if best is None or dd < best[0]: best = (dd, px, pz, a[2] + t * (b[2] - a[2]), a, b)
    dd, px, pz, py, a, b = best; k = key(px, pz); pos.setdefault(k, (px, pz, py))
    add(key(a[0], a[1]), k, math.dist(a[:2], (px, pz))); add(key(b[0], b[1]), k, math.dist(b[:2], (px, pz)))
    return (px, pz)
GP = insert(g[1], g[2]); DP = insert(*A["pack_on_46"])
pts = [("Garage", GP), ("Depot", DP), ("Q_Shops", (91.2, -21.3)), ("Q_Teahouse", (181.4, 5.7)),
       ("v_down", (137, -45)), ("Q5_CoastRail", (100, -180)), ("c_west", (55, -203)), ("Garage_end", GP)]
full = []; legs = []
for (an, a), (bn, b) in zip(pts, pts[1:]):
    p, L = path(near(*a), near(*b)); full += p if not full else p[1:]; legs.append((an, bn, round(L, 1)))
poly = [pos[k][:2] for k in full]
# 촘촘히(2 m) 다시 찍기
dense = [poly[0]]
for a, b in zip(poly, poly[1:]):
    L = math.dist(a, b); n = max(1, int(L / 2.1))
    for i in range(1, n + 1): dense.append((a[0] + (b[0] - a[0]) * i / n, a[1] + (b[1] - a[1]) * i / n))
dense = [p for i, p in enumerate(dense) if i == 0 or math.dist(p, dense[i - 1]) > 0.3]
# 2차: 90~150° 꺾임(길 이음)은 봇이 (93,16,-15) 에서 끼였음 → 앞뒤 2점(±4 m)을 이차 베지어로 둥글게. 150° 넘는 되돌이(찻집)는 봇이 다리를 나누므로 그대로
def turn(a, b, c):
    u = (b[0] - a[0], b[1] - a[1]); v = (c[0] - b[0], c[1] - b[1]); d = math.hypot(*u) * math.hypot(*v)
    return 0 if d == 0 else math.degrees(math.acos(max(-1, min(1, (u[0] * v[0] + u[1] * v[1]) / d))))
#   2차 재시도: 귀환 다리의 61→16 꺾임(68°)에서 또 낌 → 기준 50°, 앞뒤 3점(±6 m)
i = 3
while i < len(dense) - 3:
    if 50 < turn(dense[i - 1], dense[i], dense[i + 1]) < 150:
        A, B, C = dense[i - 3], dense[i], dense[i + 3]
        arc = [((1 - t) ** 2 * A[0] + 2 * (1 - t) * t * B[0] + t * t * C[0], (1 - t) ** 2 * A[1] + 2 * (1 - t) * t * B[1] + t * t * C[1]) for t in (1 / 6, 2 / 6, 3 / 6, 4 / 6, 5 / 6)]
        dense = dense[:i - 2] + arc + dense[i + 3:]; i += 5
    else: i += 1
tot = sum(math.dist(a, b) for a, b in zip(dense, dense[1:]))
open(os.path.join(H, "loop_pts.txt"), "w").write("\n".join(f"{x:.2f} {z:.2f}" for x, z in dense) + "\n")
json.dump([[round(x, 1), round(z, 1)] for x, z in dense[::3]] + [[round(dense[-1][0], 1), round(dense[-1][1], 1)]], open(os.path.join(H, "route_world.json"), "w"))
json.dump({"garage_road": g, "depot_road": d, "garage_pt": GP, "pack_pt": DP, "legs": legs, "total_m": round(tot, 1)}, open(os.path.join(H, "anchors.json"), "w"), indent=1)
# 그림 (tv6_plan.py 와 같은 바탕: 1600×1600, 1px = 0.5 m)
S, O = 2.0, 400
img = Image.new("RGB", (int(800 * S), int(800 * S)), (40, 70, 95)); dr = ImageDraw.Draw(img)
def P(x, z): return ((x + O) * S, (O - z) * S)
for x, z, y in j["terrain"]["controlPoints"]:
    if y > 0: c = int(min(255, 70 + y * 2.2)); dr.rectangle([*P(x - 4, z + 4), *P(x + 4, z - 4)], fill=(c, c - 10, c - 40))
for i, r in enumerate(j["roads"]):
    if i in SKIP: continue
    col = {"main": (250, 250, 250), "village": (190, 190, 190)}.get(r["kind"], (120, 110, 100))
    dr.line([P(x, z) for x, z, y in r["knots"]], fill=col, width=3 if r["kind"] == "main" else 2)
B = json.load(open(os.path.join(H, "placed.json"), encoding="utf8"))
for b in B:
    dr.polygon([P(*v) for v in b["foot"]], fill=(255, 210, 60), outline=(0, 0, 0))
dr.line([P(*p) for p in dense], fill=(255, 120, 30), width=6)
try: font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 22)
except Exception: font = None
lab = {"Garage": "출발·귀환 정비소", "Depot": "보관소(달걀)", "Plaza": "광장", "Q_Shops": "상점가 오르막", "Q_Teahouse": "찻집(배달)", "Q5_CoastRail": "해안 직선"}
for n, (x, z) in pts:
    if n not in lab: continue
    cx, cy = P(x, z); dr.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=(255, 255, 255), outline=(0, 0, 0), width=2)
    dr.text((cx + 12, cy - 12), lab[n], fill=(255, 255, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0))
dr.text((20, 20), f"D-198 튜토리얼 고리 {tot:.0f} m — 정비소→보관소→광장→상점가→찻집→해안→정비소", fill=(255, 255, 255), font=font)
img.save(os.path.join(R, "docs/map/townv6-tutorial-loop.png"))
print(json.dumps({"legs": legs, "total_m": round(tot, 1), "n": len(dense), "garage_road": g, "depot_road": d}))
